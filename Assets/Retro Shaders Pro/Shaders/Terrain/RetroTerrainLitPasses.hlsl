#ifndef UNIVERSAL_TERRAIN_LIT_PASSES_INCLUDED
#define UNIVERSAL_TERRAIN_LIT_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

#define EPSILON 1e-06

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color : COLOR;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uvMainAndLM              : TEXCOORD0; // xy: control, zw: lightmap
    #ifndef TERRAIN_SPLAT_BASEPASS
        float4 uvSplat01                : TEXCOORD1; // xy: splat0, zw: splat1
        float4 uvSplat23                : TEXCOORD2; // xy: splat2, zw: splat3
    #endif

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
        half4 normal                    : TEXCOORD3;    // xyz: normal, w: viewDir.x
        half4 tangent                   : TEXCOORD4;    // xyz: tangent, w: viewDir.y
        half4 bitangent                 : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
    #else
        half3 normal                    : TEXCOORD3;
        half3 vertexSH                  : TEXCOORD4; // SH
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light
    #else
        half  fogFactor                 : TEXCOORD6;
    #endif

    float3 positionWS               : TEXCOORD7;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord              : TEXCOORD8;
    #endif

#if defined(DYNAMICLIGHTMAP_ON)
    float2 dynamicLightmapUV        : TEXCOORD9;
#endif
    
    float4 positionSS               : TEXCOORD10;
    
#ifdef _LIGHTMODE_VERTEXLIT
    float3 diffuseLightColor        : TEXCOORD11;
    float3 specularLightColor       : TEXCOORD12;
#endif
    
    float4 color                    : COLOR;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

inline float2x2 invert(float2x2 m)
{
    float det = m[0][0] * m[1][1] - m[0][1] * m[1][0];

    if (abs(det) < 1e-9)
    {
        return float2x2(0, 0, 0, 0);
    }

    return (1.0f / det) * float2x2(m[1][1], -m[0][1], -m[1][0], m[0][0]);
}

void InitializeInputData(Varyings IN, half3 normalTS, int targetResolution, int actualResolution, out InputData inputData)
{
    inputData = (InputData)0;
    
    // Find an offset vector in world space to snap lighting calcs to texel grid.
    // With massive thanks to: https://discussions.unity.com/t/the-quest-for-efficient-per-texel-lighting/700574
#ifdef _LIGHTMODE_TEXELLIT
    float2 uv = IN.uvSplat01.xy;
    float2 actualTexelSize = pow(2, min(targetResolution, actualResolution));
    float2 texelUV = floor(uv * actualTexelSize) / actualTexelSize + (0.5f / actualTexelSize);
    float2 dUV = (texelUV - uv);

    // Construct matrix to get from a texel-aligned space to UV space.
    float2 ddxUV = ddx(uv);
    float2 ddyUV = ddy(uv);
    float2x2 texelToUVSpace = invert(float2x2(ddxUV.x, ddyUV.x, ddxUV.y, ddyUV.y));

    // Get deltas along UV space.
    float2 uvDeltas = mul(texelToUVSpace, dUV);

    // Apply those deltas in world space along surface directions.
    float3 ddxWorldPos = ddx(IN.positionWS);
    float3 ddyWorldPos = ddy(IN.positionWS);

    inputData.positionWS = IN.positionWS + clamp(ddxWorldPos * uvDeltas.x + ddyWorldPos * uvDeltas.y, -1.0f, 1.0f);
#else
    inputData.positionWS = IN.positionWS;
#endif
    
    inputData.positionCS = IN.clipPos;

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
        half3 viewDirWS = half3(IN.normal.w, IN.tangent.w, IN.bitangent.w);
        inputData.tangentToWorld = half3x3(-IN.tangent.xyz, IN.bitangent.xyz, IN.normal.xyz);
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
        half3 SH = 0;
    #elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
        float2 sampleCoords = (IN.uvMainAndLM.xy / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
        half3 normalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
        half3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, normalWS);
        inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(-tangentWS, cross(normalWS, tangentWS), normalWS));
        half3 SH = 0;
    #else
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
        inputData.normalWS = IN.normal;
        half3 SH = IN.vertexSH;
    #endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = IN.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactorAndVertexLight.x);
        inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
    #else
    inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
    #endif

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(IN.uvMainAndLM.zw, IN.dynamicLightmapUV, SH, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(IN.uvMainAndLM.zw, SH, inputData.normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.clipPos);
    inputData.shadowMask = SAMPLE_SHADOWMASK(IN.uvMainAndLM.zw)

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = IN.dynamicLightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = IN.uvMainAndLM.zw;
    #else
    inputData.vertexSH = SH;
    #endif
    #endif
}

#ifndef TERRAIN_SPLAT_BASEPASS

void NormalMapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl, inout half3 mixedNormal)
{
    #if defined(_NORMALMAP)
		int targetResolution = (int)log2(_ResolutionLimit);
		int normal0Resolution = (int)log2(_Normal0_TexelSize.zw);
		int normal1Resolution = (int)log2(_Normal1_TexelSize.zw);
		int normal2Resolution = (int)log2(_Normal2_TexelSize.zw);
		int normal3Resolution = (int)log2(_Normal3_TexelSize.zw);

        half3 nrm = half(0.0);
        nrm += splatControl.r * UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_Normal0, sampler_PointRepeat, uvSplat01.xy, normal0Resolution - targetResolution), _NormalScale0);
        nrm += splatControl.g * UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_Normal1, sampler_PointRepeat, uvSplat01.zw, normal1Resolution - targetResolution), _NormalScale1);
        nrm += splatControl.b * UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_Normal2, sampler_PointRepeat, uvSplat23.xy, normal2Resolution - targetResolution), _NormalScale2);
        nrm += splatControl.a * UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_Normal3, sampler_PointRepeat, uvSplat23.zw, normal3Resolution - targetResolution), _NormalScale3);

        // avoid risk of NaN when normalizing.
        #if HAS_HALF
            nrm.z += half(0.01);
        #else
            nrm.z += 1e-5f;
        #endif

        mixedNormal = normalize(nrm.xyz);
    #endif
}

float3 dither(float3 col, float2 uv)
{
    static float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;

    return col - DITHER_THRESHOLDS[index];
}

// From: https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Skybox-Cubed.shader
float3 RotateAroundYInDegrees(float3 vertex, float degrees)
{
    float alpha = degrees * PI / 180.0;
    float sina, cosa;
    sincos(alpha, sina, cosa);
    float2x2 m = float2x2(cosa, -sina, sina, cosa);
    return float3(mul(m, vertex.xz), vertex.y).xzy;
}

float3 DitherLayer(float3 albedo, float2 uv, float2 texelSize, float4 positionSS)
{
    // Posterize the base color.
    float colorBitDepth = max(2, _ColorBitDepth);

    float r = max((albedo.r - EPSILON) * colorBitDepth, 0.0f);
    float g = max((albedo.g - EPSILON) * colorBitDepth, 0.0f);
    float b = max((albedo.b - EPSILON) * colorBitDepth, 0.0f);

    float divisor = colorBitDepth - 1.0f;
    
    // Apply dithering between posterized colors.
#if defined(_DITHERMODE_SCREEN)
    float3 remainders = float3(frac(r), frac(g), frac(b));
    float2 ditherUV = (positionSS.xy / positionSS.w) * _ScreenParams.xy / _RetroPixelSize;
    float3 ditheredColor = saturate(dither(remainders, ditherUV));
    ditheredColor = step(0.5f, ditheredColor);
#elif defined(_DITHERMODE_TEXTURE)
    float3 remainders = float3(frac(r), frac(g), frac(b));
    float3 ditheredColor = saturate(dither(remainders, uv * texelSize));
    ditheredColor = step(0.5f, ditheredColor);
#else
    float3 ditheredColor = 0.0f;
#endif

    float3 posterizedColor = float3(floor(r), floor(g), floor(b)) + ditheredColor;
    posterizedColor /= divisor;
    posterizedColor += 1.0f / colorBitDepth * _ColorBitDepthOffset;
    
    return posterizedColor;
}

// Calculate N64 3-point bilinear filtering for one of the splatmaps
float4 SampleSplatmapN64(Texture2D tex, float4 texelSize, float2 uv, int lod)
{
    float modifier = pow(2.0f, lod);
    float4 targetTexelSize = float4(texelSize.xy * modifier, texelSize.zw / modifier);

	// With thanks to: https://www.emutalk.net/threads/emulating-nintendo-64-3-sample-bilinear-filtering-using-shaders.54215/
    float2 uvA = float2(targetTexelSize.x, 0.0f);
    float2 uvB = float2(0.0f, targetTexelSize.y);
    float2 uvC = float2(targetTexelSize.x, targetTexelSize.y);
    float2 uvHalf = uvC * 0.5f;
    float2 uvCenter = uv - uvHalf;

    float4 baseColorMain = SAMPLE_TEXTURE2D_LOD(tex, sampler_PointRepeat, uvCenter, lod);
    float4 baseColorA = SAMPLE_TEXTURE2D_LOD(tex, sampler_PointRepeat, uvCenter + uvA, lod);
    float4 baseColorB = SAMPLE_TEXTURE2D_LOD(tex, sampler_PointRepeat, uvCenter + uvB, lod);
    float4 baseColorC = SAMPLE_TEXTURE2D_LOD(tex, sampler_PointRepeat, uvCenter + uvC, lod);

    float interpX = modf(uvCenter.x * targetTexelSize.z, targetTexelSize.z);
    float interpY = modf(uvCenter.y * targetTexelSize.w, targetTexelSize.w);

    if (uvCenter.x < 0.0f)
    {
        interpX = 1.0f - (interpX * -1.0f);
    }

    if (uvCenter.y < 0.0f)
    {
        interpY = 1.0f - (interpY * -1.0f);
    }
    
    float4 baseColor = (baseColorMain + interpX * (baseColorA - baseColorMain) + interpY * (baseColorB - baseColorMain)) * (1.0f - step(1.0f, interpX + interpY));
    baseColor += (baseColorC + (1.0f - interpX) * (baseColorB - baseColorC) + (1.0f - interpY) * (baseColorA - baseColorC)) * step(1.0f, interpX + interpY);
    
    return baseColor;
}

void SplatmapMix(float4 uvMainAndLM, float4 uvSplat01, float4 uvSplat23, float4 positionSS, inout half4 splatControl, out half weight, out half4 mixedDiffuse, out half4 defaultSmoothness, inout half3 mixedNormal)
{
    half4 diffAlbedo[4];

	int targetResolution = (int)log2(_ResolutionLimit);
    int splat0LOD = int(log2(_Splat0_TexelSize.z)) - targetResolution;
    int splat1LOD = int(log2(_Splat1_TexelSize.z)) - targetResolution;
    int splat2LOD = int(log2(_Splat2_TexelSize.z)) - targetResolution;
    int splat3LOD = int(log2(_Splat3_TexelSize.z)) - targetResolution;
    
#if defined(_FILTERMODE_BILINEAR)
    diffAlbedo[0] = SAMPLE_TEXTURE2D_LOD(_Splat0, sampler_LinearRepeat, uvSplat01.xy, splat0LOD);
    diffAlbedo[1] = SAMPLE_TEXTURE2D_LOD(_Splat1, sampler_LinearRepeat, uvSplat01.zw, splat1LOD);
    diffAlbedo[2] = SAMPLE_TEXTURE2D_LOD(_Splat2, sampler_LinearRepeat, uvSplat23.xy, splat2LOD);
    diffAlbedo[3] = SAMPLE_TEXTURE2D_LOD(_Splat3, sampler_LinearRepeat, uvSplat23.zw, splat3LOD);
#elif defined(_FILTERMODE_N64)
    diffAlbedo[0] = SampleSplatmapN64(_Splat0, _Splat0_TexelSize, uvSplat01.xy, splat0LOD);
    diffAlbedo[1] = SampleSplatmapN64(_Splat1, _Splat1_TexelSize, uvSplat01.zw, splat1LOD);
    diffAlbedo[2] = SampleSplatmapN64(_Splat2, _Splat2_TexelSize, uvSplat23.xy, splat2LOD);
    diffAlbedo[3] = SampleSplatmapN64(_Splat3, _Splat3_TexelSize, uvSplat23.zw, splat3LOD);
#else
    diffAlbedo[0] = SAMPLE_TEXTURE2D_LOD(_Splat0, sampler_PointRepeat, uvSplat01.xy, splat0LOD);
    diffAlbedo[1] = SAMPLE_TEXTURE2D_LOD(_Splat1, sampler_PointRepeat, uvSplat01.zw, splat1LOD);
    diffAlbedo[2] = SAMPLE_TEXTURE2D_LOD(_Splat2, sampler_PointRepeat, uvSplat23.xy, splat2LOD);
    diffAlbedo[3] = SAMPLE_TEXTURE2D_LOD(_Splat3, sampler_PointRepeat, uvSplat23.zw, splat3LOD);
#endif

#ifndef _DITHERMODE_OFF
    diffAlbedo[0].rgb = DitherLayer(diffAlbedo[0].rgb, uvSplat01.xy, _Splat0_TexelSize.zw, positionSS);
    diffAlbedo[1].rgb = DitherLayer(diffAlbedo[1].rgb, uvSplat01.zw, _Splat1_TexelSize.zw, positionSS);
    diffAlbedo[2].rgb = DitherLayer(diffAlbedo[2].rgb, uvSplat23.xy, _Splat2_TexelSize.zw, positionSS);
    diffAlbedo[3].rgb = DitherLayer(diffAlbedo[3].rgb, uvSplat23.zw, _Splat3_TexelSize.zw, positionSS);
#endif
    
    // This might be a bit of a gamble -- the assumption here is that if the diffuseMap has no
    // alpha channel, then diffAlbedo[n].a = 1.0 (and _DiffuseHasAlphaN = 0.0)
    // Prior to coming in, _SmoothnessN is actually set to max(_DiffuseHasAlphaN, _SmoothnessN)
    // This means that if we have an alpha channel, _SmoothnessN is locked to 1.0 and
    // otherwise, the true slider value is passed down and diffAlbedo[n].a == 1.0.
    defaultSmoothness = half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a);
    defaultSmoothness *= half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);

#ifndef _TERRAIN_BLEND_HEIGHT // density blending
    if(_NumLayersCount <= 4)
    {
        // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
        half4 opacityAsDensity = saturate((half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a) - (1 - splatControl)) * 20.0);
        opacityAsDensity += 0.001h * splatControl;      // if all weights are zero, default to what the blend mask says
        half4 useOpacityAsDensityParam = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
        splatControl = lerp(opacityAsDensity, splatControl, useOpacityAsDensityParam);
    }
#endif

    // Now that splatControl has changed, we can compute the final weight and normalize
    weight = dot(splatControl, 1.0h);

#ifdef TERRAIN_SPLAT_ADDPASS
    clip(weight <= 0.005h ? -1.0h : 1.0h);
#endif

#ifndef _TERRAIN_BASEMAP_GEN
    // Normalize weights before lighting and restore weights in final modifier functions so that the overal
    // lighting result can be correctly weighted.
    splatControl /= (weight + HALF_MIN);
#endif

    mixedDiffuse = 0.0h;
    mixedDiffuse += diffAlbedo[0] * half4(_DiffuseRemapScale0.rgb * splatControl.rrr, 1.0h);
    mixedDiffuse += diffAlbedo[1] * half4(_DiffuseRemapScale1.rgb * splatControl.ggg, 1.0h);
    mixedDiffuse += diffAlbedo[2] * half4(_DiffuseRemapScale2.rgb * splatControl.bbb, 1.0h);
    mixedDiffuse += diffAlbedo[3] * half4(_DiffuseRemapScale3.rgb * splatControl.aaa, 1.0h);

    NormalMapMix(uvSplat01, uvSplat23, splatControl, mixedNormal);
}

#endif

#ifdef _TERRAIN_BLEND_HEIGHT
void HeightBasedSplatModify(inout half4 splatControl, in half4 masks[4])
{
    // heights are in mask blue channel, we multiply by the splat Control weights to get combined height
    half4 splatHeight = half4(masks[0].b, masks[1].b, masks[2].b, masks[3].b) * splatControl.rgba;
    half maxHeight = max(splatHeight.r, max(splatHeight.g, max(splatHeight.b, splatHeight.a)));

    // Ensure that the transition height is not zero.
    half transition = max(_HeightTransition, 1e-5);

    // This sets the highest splat to "transition", and everything else to a lower value relative to that, clamping to zero
    // Then we clamp this to zero and normalize everything
    half4 weightedHeights = splatHeight + transition - maxHeight.xxxx;
    weightedHeights = max(0, weightedHeights);

    // We need to add an epsilon here for active layers (hence the blendMask again)
    // so that at least a layer shows up if everything's too low.
    weightedHeights = (weightedHeights + 1e-6) * splatControl;

    // Normalize (and clamp to epsilon to keep from dividing by zero)
    half sumHeight = max(dot(weightedHeights, half4(1, 1, 1, 1)), 1e-6);
    splatControl = weightedHeights / sumHeight.xxxx;
}
#endif

float3 calculateDiffuse(InputData inputData, float4 vertexColor)
{
#ifndef _USE_AMBIENT_OVERRIDE
    float3 ambientLight = SampleSHVertex(inputData.normalWS);
#else
    float3 ambientLight = _AmbientLight;
#endif

    Light light = GetMainLight(inputData.shadowCoord);

    // Main light diffuse calculation.
    float3 lightColor = light.color * light.distanceAttenuation;

#if _RECEIVESHADOWSMODE_ON
    lightColor *= light.shadowAttenuation;
#endif
    float lightAmount = saturate(dot(inputData.normalWS, light.direction));
    float3 totalLighting = lightAmount * lightColor + ambientLight;

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    // Loop through all secondary lights.
    LIGHT_LOOP_BEGIN(pixelLightCount)
        light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);

        // Secondary light diffuse calculation.
        lightColor = light.color * light.distanceAttenuation;

#if _RECEIVESHADOWSMODE_ON
        lightColor *= light.shadowAttenuation;
#endif

        totalLighting += saturate(dot(light.direction, inputData.normalWS)) * lightColor;
    LIGHT_LOOP_END
#endif

#if _USE_VERTEX_COLORS
    totalLighting *= vertexColor.rgb;
#endif

    return totalLighting;
}

float3 calculateSpecular(InputData inputData)
{
#ifndef _USE_SPECULAR_LIGHT
    return 0.0f;
#else
    Light light = GetMainLight(inputData.shadowCoord);
    float3 lightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;

    // Main light specular calculation.
    float glossPower = pow(2.0f, _Glossiness);
    float3 reflectedVector = reflect(-light.direction, inputData.normalWS);
    float3 specularLighting = pow(saturate(dot(reflectedVector, inputData.viewDirectionWS)), glossPower) * lightColor;

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    // Loop through all secondary lights.
    LIGHT_LOOP_BEGIN(pixelLightCount)
        light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
        lightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;

        // Secondary light specular calculation.
        reflectedVector = reflect(-light.direction, inputData.normalWS);
        specularLighting += pow(saturate(dot(reflectedVector, inputData.viewDirectionWS)), glossPower) * lightColor;
    LIGHT_LOOP_END
#endif			
    return specularLighting;
#endif
}

void SplatmapFinalColor(inout half4 color, half fogCoord)
{
    color.rgb *= color.a;

    #ifdef TERRAIN_SPLAT_ADDPASS
        color.rgb = MixFogColor(color.rgb, half3(0,0,0), fogCoord);
    #else
        color.rgb = MixFog(color.rgb, fogCoord);
    #endif
}

void SetupTerrainDebugTextureData(inout InputData inputData, float2 uv)
{
    #if defined(DEBUG_DISPLAY)
        #if defined(TERRAIN_SPLAT_ADDPASS)
            if (_DebugMipInfoMode != DEBUGMIPINFOMODE_NONE)
            {
                discard; // Layer 4 & beyond are done additively, doesn't make sense for the mipmap streaming debug views -> stop.
            }
        #endif

        switch (_DebugMipMapTerrainTextureMode)
        {
            case DEBUGMIPMAPMODETERRAINTEXTURE_CONTROL:
                SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, TRANSFORM_TEX(uv, _Control), _Control);
                break;
            case DEBUGMIPMAPMODETERRAINTEXTURE_LAYER0:
                SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, TRANSFORM_TEX(uv, _Splat0), _Splat0);
                break;
            case DEBUGMIPMAPMODETERRAINTEXTURE_LAYER1:
                SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, TRANSFORM_TEX(uv, _Splat1), _Splat1);
                break;
            case DEBUGMIPMAPMODETERRAINTEXTURE_LAYER2:
                SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, TRANSFORM_TEX(uv, _Splat2), _Splat2);
                break;
            case DEBUGMIPMAPMODETERRAINTEXTURE_LAYER3:
                SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, TRANSFORM_TEX(uv, _Splat3), _Splat3);
                break;
            default:
                break;
        }

        // TERRAIN_STREAM_INFO: no streamInfo will have been set (no MeshRenderer); set status to "6" to reflect in the debug status that this is a terrain
        // also, set the per-material status to "4" to indicate warnings
        inputData.streamInfo = TERRAIN_STREAM_INFO;
    #endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard Terrain shader
Varyings SplatmapVert(Attributes v)
{
    Varyings o = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainInstancing(v.positionOS, v.normalOS, v.texcoord);

    VertexPositionInputs Attributes = GetVertexPositionInputs(v.positionOS.xyz);

    o.uvMainAndLM.xy = v.texcoord;
    o.uvMainAndLM.zw = v.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;

    #ifndef TERRAIN_SPLAT_BASEPASS
        o.uvSplat01.xy = TRANSFORM_TEX(v.texcoord, _Splat0);
        o.uvSplat01.zw = TRANSFORM_TEX(v.texcoord, _Splat1);
        o.uvSplat23.xy = TRANSFORM_TEX(v.texcoord, _Splat2);
        o.uvSplat23.zw = TRANSFORM_TEX(v.texcoord, _Splat3);
    #endif

#if defined(DYNAMICLIGHTMAP_ON)
    o.dynamicLightmapUV = v.texcoord * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(Attributes.positionWS);
        float4 vertexTangent = float4(cross(float3(0, 0, 1), v.normalOS), 1.0);
        VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, vertexTangent);

        o.normal = half4(normalInput.normalWS, viewDirWS.x);
        o.tangent = half4(normalInput.tangentWS, viewDirWS.y);
        o.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
    #else
        o.normal = TransformObjectToWorldNormal(v.normalOS);
        o.vertexSH = SampleSH(o.normal);
    #endif

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(Attributes.positionCS.z);
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        o.fogFactorAndVertexLight.x = fogFactor;
        o.fogFactorAndVertexLight.yzw = VertexLighting(Attributes.positionWS, o.normal.xyz);
    #else
        o.fogFactor = fogFactor;
    #endif

	o.positionWS = Attributes.positionWS;
    
#if defined(_SNAPMODE_OBJECT)
    float4 positionOS = floor(v.positionOS * _SnapsPerUnit) / _SnapsPerUnit;
    o.clipPos = TransformObjectToHClip(positionOS.xyz);
#elif defined(_SNAPMODE_WORLD)
    float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
    positionWS = floor(positionWS * _SnapsPerUnit) / _SnapsPerUnit;
    o.clipPos = TransformWorldToHClip(positionWS);
#elif defined(_SNAPMODE_VIEW)
    float4 positionVS = mul(UNITY_MATRIX_MV, v.positionOS);
    positionVS = floor(positionVS * _SnapsPerUnit) / _SnapsPerUnit;
    o.clipPos = mul(UNITY_MATRIX_P, positionVS);
#else
    o.clipPos = TransformObjectToHClip(v.positionOS.xyz);
#endif
    
    o.positionSS = ComputeScreenPos(o.clipPos);
    o.color = v.color;
    
    #ifdef _LIGHTMODE_VERTEXLIT
        float3 viewWS = GetWorldSpaceNormalizeViewDir(o.positionWS);
        InputData inputData = (InputData)0;
        inputData.positionWS = o.positionWS;
        inputData.normalWS = o.normal;
        inputData.viewDirectionWS = viewWS;
        inputData.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(o.clipPos);

        o.diffuseLightColor = calculateDiffuse(inputData, v.color);
        o.specularLightColor = calculateSpecular(inputData);
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        o.shadowCoord = GetShadowCoord(Attributes);
    #endif

    return o;
}

void ComputeMasks(out half4 masks[4], half4 hasMask, Varyings IN)
{
    masks[0] = 0.5h;
    masks[1] = 0.5h;
    masks[2] = 0.5h;
    masks[3] = 0.5h;

#ifdef _MASKMAP
    masks[0] = lerp(masks[0], SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, IN.uvSplat01.xy), hasMask.x);
    masks[1] = lerp(masks[1], SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, IN.uvSplat01.zw), hasMask.y);
    masks[2] = lerp(masks[2], SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, IN.uvSplat23.xy), hasMask.z);
    masks[3] = lerp(masks[3], SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, IN.uvSplat23.zw), hasMask.w);
#endif

    masks[0] *= _MaskMapRemapScale0.rgba;
    masks[0] += _MaskMapRemapOffset0.rgba;
    masks[1] *= _MaskMapRemapScale1.rgba;
    masks[1] += _MaskMapRemapOffset1.rgba;
    masks[2] *= _MaskMapRemapScale2.rgba;
    masks[2] += _MaskMapRemapOffset2.rgba;
    masks[3] *= _MaskMapRemapScale3.rgba;
    masks[3] += _MaskMapRemapOffset3.rgba;
}

// Used in Standard Terrain shader
void SplatmapFragment(
    Varyings IN
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
#ifdef _ALPHATEST_ON
    ClipHoles(IN.uvMainAndLM.xy);
#endif

	int targetResolution = (int)log2(_ResolutionLimit);
	int actualResolution = (int)log2(_MainTex_TexelSize.zw);
	int lod = actualResolution - targetResolution;

    half3 normalTS = half3(0.0h, 0.0h, 1.0h);
#ifdef TERRAIN_SPLAT_BASEPASS
    half3 albedo = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_PointRepeat, IN.uvMainAndLM.xy, lod).rgb;
    half smoothness = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_PointRepeat, IN.uvMainAndLM.xy, lod).a;
    half metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, IN.uvMainAndLM.xy).r;
    half alpha = 1;
    half occlusion = 1;
#else

    half4 hasMask = half4(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3);
    half4 masks[4];
    ComputeMasks(masks, hasMask, IN);

    float2 splatUV = (IN.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

    half alpha = dot(splatControl, 1.0h);
#ifdef _TERRAIN_BLEND_HEIGHT
    // disable Height Based blend when there are more than 4 layers (multi-pass breaks the normalization)
    if (_NumLayersCount <= 4)
        HeightBasedSplatModify(splatControl, masks);
#endif

    half weight;
    half4 mixedDiffuse;
    half4 defaultSmoothness;
    SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, IN.positionSS, splatControl, weight, mixedDiffuse, defaultSmoothness, normalTS);
    half3 albedo = mixedDiffuse.rgb;

    half4 defaultMetallic = half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
    half4 defaultOcclusion = half4(_MaskMapRemapScale0.g, _MaskMapRemapScale1.g, _MaskMapRemapScale2.g, _MaskMapRemapScale3.g) +
                            half4(_MaskMapRemapOffset0.g, _MaskMapRemapOffset1.g, _MaskMapRemapOffset2.g, _MaskMapRemapOffset3.g);

    half4 maskSmoothness = half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
    defaultSmoothness = lerp(defaultSmoothness, maskSmoothness, hasMask);
    half smoothness = dot(splatControl, defaultSmoothness);

    half4 maskMetallic = half4(masks[0].r, masks[1].r, masks[2].r, masks[3].r);
    defaultMetallic = lerp(defaultMetallic, maskMetallic, hasMask);
    half metallic = dot(splatControl, defaultMetallic);

    half4 maskOcclusion = half4(masks[0].g, masks[1].g, masks[2].g, masks[3].g);
    defaultOcclusion = lerp(defaultOcclusion, maskOcclusion, hasMask);
    half occlusion = dot(splatControl, defaultOcclusion);
#endif

    InputData inputData;
    InitializeInputData(IN, normalTS, targetResolution, actualResolution, inputData);
    SetupTerrainDebugTextureData(inputData, IN.uvMainAndLM.xy);

#if defined(_DBUFFER)
    half3 specular = half3(0.0h, 0.0h, 0.0h);
    ApplyDecal(IN.clipPos,
        albedo,
        specular,
        inputData.normalWS,
        metallic,
        occlusion,
        smoothness);
#endif

	// Posterize the base color.
    // When dithering is enabled, posterization is handled in SplatmapMix.
#if defined(_DITHERMODE_OFF)
    float colorBitDepth = max(2, _ColorBitDepth);

    int r = max((albedo.r - EPSILON) * colorBitDepth, 0);
    int g = max((albedo.g - EPSILON) * colorBitDepth, 0);
    int b = max((albedo.b - EPSILON) * colorBitDepth, 0);

    float divisor = colorBitDepth - 1.0f;

    float3 posterizedColor = float3(floor(r), floor(g), floor(b));
    posterizedColor /= divisor;
    posterizedColor += 1.0f / colorBitDepth * _ColorBitDepthOffset;

	albedo = posterizedColor;
#endif
    
#ifdef _USE_VERTEX_COLORS
    albedo *= IN.color;
#endif
    
#if defined(_LIGHTMODE_LIT) || defined(_LIGHTMODE_TEXELLIT)
    float3 diffuseLightColor = calculateDiffuse(inputData, IN.color);
    float3 specularLightColor = calculateSpecular(inputData);

    diffuseLightColor += inputData.bakedGI;

#elif defined(_LIGHTMODE_VERTEXLIT)
    float3 diffuseLightColor = IN.diffuseLightColor;
    float3 specularLightColor = IN.specularLightColor;
#else
    float3 diffuseLightColor = 1.0f;
    float3 specularLightColor = 0.0f;
#endif
    
#ifdef _USE_REFLECTION_CUBEMAP
    float3 reflectedVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    reflectedVector = RotateAroundYInDegrees(reflectedVector, _CubemapRotation);

#ifdef _FILTERMODE_POINT
	float4 cubemapLighting = SAMPLE_TEXTURECUBE_LOD(_ReflectionCubemap, sampler_PointClamp, reflectedVector, lod);
#else
    float4 cubemapLighting = SAMPLE_TEXTURECUBE_LOD(_ReflectionCubemap, sampler_LinearClamp, reflectedVector, lod);
#endif
    albedo += cubemapLighting.rgb * cubemapLighting.a * _CubemapColor.rgb * _CubemapColor.a;
#endif
    
    float4 color = float4(albedo * diffuseLightColor + specularLightColor, 1.0f);
    
    SplatmapFinalColor(color, inputData.fogCoord);

    outColor = half4(color.rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

// Shadow pass

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

struct AttributesLean
{
    float4 position     : POSITION;
    float3 normalOS       : NORMAL;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsLean
{
    float4 clipPos      : SV_POSITION;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsLean ShadowPassVertex(AttributesLean v)
{
    VaryingsLean o = (VaryingsLean)0;
    UNITY_SETUP_INSTANCE_ID(v);
    TerrainInstancing(v.position, v.normalOS, v.texcoord);

    float3 positionWS = TransformObjectToWorld(v.position.xyz);
    float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif

    o.clipPos = clipPos;

    o.texcoord = v.texcoord;

    return o;
}

half4 ShadowPassFragment(VaryingsLean IN) : SV_TARGET
{
#ifdef _ALPHATEST_ON
    ClipHoles(IN.texcoord);
#endif
    return 0;
}

// Depth pass

VaryingsLean DepthOnlyVertex(AttributesLean v)
{
    VaryingsLean o = (VaryingsLean)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainInstancing(v.position, v.normalOS);
    o.clipPos = TransformObjectToHClip(v.position.xyz);
    o.texcoord = v.texcoord;
    return o;
}

half4 DepthOnlyFragment(VaryingsLean IN) : SV_TARGET
{
#ifdef _ALPHATEST_ON
    ClipHoles(IN.texcoord);
#endif
#ifdef SCENESELECTIONPASS
    // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
    return half4(_ObjectId, _PassValue, 1.0, 1.0);
#endif
    return IN.clipPos.z;
}

#endif
