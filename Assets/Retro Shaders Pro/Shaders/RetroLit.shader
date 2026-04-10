Shader "Retro Shaders Pro/Retro Lit"
{
    Properties
    {
		[MainColor] [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		[MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
		_ResolutionLimit("Resolution Limit (Power of 2)", Integer) = 64
		_SnapsPerUnit("Snapping Points per Meter", Integer) = 64
		_ColorBitDepth("Bit Depth", Integer) = 64
		_ColorBitDepthOffset("Bit Depth Offset", Range(0.0, 1.0)) = 0.0
		_AmbientLight("Ambient Light Strength", Range(0.0, 1.0)) = 0.02
		_AffineTextureStrength("Affine Texture Strength", Range(0.0, 1.0)) = 1.0
		_Glossiness("Glossiness", Range(1.0, 20.0)) = 5.0
		_ReflectionCubemap("Reflection Cubemap", Cube) = "black" {}
		_CubemapColor("Cubemap Color", Color) = (1, 1, 1, 1)
		_CubemapRotation("Cubemap Rotation", Range(0.0, 360.0)) = 0

		[KeywordEnum(Lit, TexelLit, VertexLit, Unlit)] _LightMode("Lighting Mode", Integer) = 1
		[KeywordEnum(Bilinear, Point, N64)] _FilterMode("Filtering Mode", Integer) = 1
		[KeywordEnum(Screen, Texture, Off)] _DitherMode("Dithering Mode", Integer) = 0
		[KeywordEnum(Object, World, View, Off)] _SnapMode("Snapping Mode", Integer) = 2
		[KeywordEnum(On, Off)] _ReceiveShadowsMode("Receive Shadows Mode", Integer) = 0

		[Toggle] _USE_AMBIENT_OVERRIDE("Ambient Light Override", Float) = 0
		[ToggleOff] _USE_VERTEX_COLORS("Use Vertex Colors", Float) = 0
		[Toggle] _USE_SPECULAR_LIGHT("Use Specular Lighting", Float) = 0
		[Toggle] _USE_REFLECTION_CUBEMAP("Use Reflection Cubemap", Float) = 0
		[Toggle] _USE_FLAT_SHADING("Use Flat Shading", Float) = 0

		[ToggleUI] _AlphaClip("Alpha Clip", Float) = 0.0
		[HideInInspector] _Cutoff("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
		[HideInInspector] _Cull("_Cull", Float) = 2.0
		[HideInInspector] _Surface("_Surface", Float) = 0.0
    }
    SubShader
    {
		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
		#include "RetroSurfaceInput.hlsl"

		#define EPSILON 1e-06

		float3 dither(float3 col, float2 uv)
		{
			static float DITHER_THRESHOLDS[16] =
			{
				1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
				13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
				4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
				16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
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
		ENDHLSL

        Pass
        {
			Tags
			{
				"LightMode" = "UniversalForwardOnly"
			}

			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]
			Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

            #pragma multi_compile_fog

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

#if UNITY_VERSION >= 60010000
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#else
			#pragma multi_compile _ _FORWARD_PLUS
#endif

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON

			#pragma shader_feature_local _LIGHTMODE_LIT _LIGHTMODE_TEXELLIT _LIGHTMODE_VERTEXLIT _LIGHTMODE_UNLIT
			#pragma shader_feature_local_fragment _FILTERMODE_BILINEAR _FILTERMODE_POINT _FILTERMODE_N64
			#pragma shader_feature_local_fragment _DITHERMODE_SCREEN _DITHERMODE_TEXTURE _DITHERMODE_OFF
			#pragma shader_feature_local_vertex _SNAPMODE_OBJECT _SNAPMODE_WORLD _SNAPMODE_VIEW _SNAPMODE_OFF
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local _USE_AMBIENT_OVERRIDE
			#pragma shader_feature_local_fragment _USE_VERTEX_COLORS
			#pragma shader_feature_local _USE_SPECULAR_LIGHT
			#pragma shader_feature_local_fragment _USE_REFLECTION_CUBEMAP
			#pragma shader_feature_local _USE_FLAT_SHADING
			#pragma shader_feature_local _RECEIVESHADOWSMODE_ON _RECEIVESHADOWSMODE_OFF

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			float3 calculateDiffuse(InputData inputData, float4 vertexColor)
			{
#ifndef _USE_AMBIENT_OVERRIDE
				float3 ambientLight = SampleSHVertex(inputData.normalWS);
#else
				float3 ambientLight = _AmbientLight;
#endif

				float3 lightColor;
				float lightAmount;
				float3 totalLighting;

				Light light = GetMainLight(inputData.shadowCoord);

#ifdef _LIGHT_LAYERS
				uint meshRenderingLayers = GetMeshRenderingLayer();
				if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
				{
					// Main light diffuse calculation.
					lightColor = light.color * light.distanceAttenuation;

#if _RECEIVESHADOWSMODE_ON
					lightColor *= light.shadowAttenuation;
#endif
					lightAmount = saturate(dot(inputData.normalWS, light.direction));
					totalLighting = lightAmount * lightColor + ambientLight;
				}

#if defined(_ADDITIONAL_LIGHTS)
				uint pixelLightCount = GetAdditionalLightsCount();

				// Loop through all secondary lights.
				LIGHT_LOOP_BEGIN(pixelLightCount)
					light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);

#ifdef _LIGHT_LAYERS
					if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
					{
						// Secondary light diffuse calculation.
						lightColor = light.color * light.distanceAttenuation;

#if _RECEIVESHADOWSMODE_ON
						lightColor *= light.shadowAttenuation;
#endif

						totalLighting += saturate(dot(light.direction, inputData.normalWS)) * lightColor;
					}
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

				float3 lightColor;
				float3 reflectedVector;
				float3 specularLighting;
				float glossPower = pow(2.0f, _Glossiness);

				Light light = GetMainLight(inputData.shadowCoord);

#ifdef _LIGHT_LAYERS
				uint meshRenderingLayers = GetMeshRenderingLayer();
				if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
				{
					lightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;

					// Main light specular calculation.
					reflectedVector = reflect(-light.direction, inputData.normalWS);
					specularLighting = pow(saturate(dot(reflectedVector, inputData.viewDirectionWS)), glossPower) * lightColor;
				}

#if defined(_ADDITIONAL_LIGHTS)
				uint pixelLightCount = GetAdditionalLightsCount();

				// Loop through all secondary lights.
				LIGHT_LOOP_BEGIN(pixelLightCount)
					light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);

#ifdef _LIGHT_LAYERS
					if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
					{
						lightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;

						// Secondary light specular calculation.
						reflectedVector = reflect(-light.direction, inputData.normalWS);
						specularLighting += pow(saturate(dot(reflectedVector, inputData.viewDirectionWS)), glossPower) * lightColor;
					}
				LIGHT_LOOP_END
#endif			
				return specularLighting;
#endif
			}

            struct appdata
            {
				float4 positionOS : POSITION;
				float4 color : COLOR;
				float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
				float2 staticLightmapUV : TEXCOORD1;
				float2 dynamicLightmapUV : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float4 affineUVAndFog : TEXCOORD1;
#ifdef _USE_FLAT_SHADING
				nointerpolation float3 normalWS : TEXCOORD2;
#else
				float3 normalWS : TEXCOORD2;
#endif
				float3 positionWS : TEXCOORD3;
				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
				float2 dynamicLightmapUV : TEXCOORD5;
				float4 positionSS : TEXCOORD6;
#ifdef _LIGHTMODE_VERTEXLIT
				float3 diffuseLightColor : TEXCOORD7;
				float3 specularLightColor : TEXCOORD8;
#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

			v2f vert(appdata v)
			{
				v2f o = (v2f)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#if defined(_SNAPMODE_OBJECT)
				float4 positionOS = floor(v.positionOS * _SnapsPerUnit) / _SnapsPerUnit;
				o.positionCS = TransformObjectToHClip(positionOS.xyz);
#elif defined(_SNAPMODE_WORLD)
				float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
				positionWS = floor(positionWS * _SnapsPerUnit) / _SnapsPerUnit;
				o.positionCS = TransformWorldToHClip(positionWS);
#elif defined(_SNAPMODE_VIEW)
				float4 positionVS = mul(UNITY_MATRIX_MV, v.positionOS);
				positionVS = floor(positionVS * _SnapsPerUnit) / _SnapsPerUnit;
				o.positionCS = mul(UNITY_MATRIX_P, positionVS);
#else
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
#endif

				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.affineUVAndFog.xyz = float3(TRANSFORM_TEX(v.uv, _BaseMap) * o.positionCS.w, o.positionCS.w);
				o.affineUVAndFog.w = ComputeFogFactor(o.positionCS.z);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				OUTPUT_SH(o.normalWS, o.vertexSH);
				OUTPUT_LIGHTMAP_UV(v.staticLightmapUV, unity_LightmapST, o.staticLightmapUV);
				o.dynamicLightmapUV = v.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				o.positionSS = ComputeScreenPos(o.positionCS);
				o.color = v.color;

#ifdef _LIGHTMODE_VERTEXLIT
				float3 viewWS = GetWorldSpaceNormalizeViewDir(o.positionWS);
				InputData inputData = (InputData)0;
				inputData.positionWS = o.positionWS;
				inputData.normalWS = o.normalWS;
				inputData.viewDirectionWS = viewWS;
				inputData.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
				inputData.shadowMask = SAMPLE_SHADOWMASK(o.dynamicLightmapUV);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(o.positionCS);

				o.diffuseLightColor = calculateDiffuse(inputData, v.color);
				o.specularLightColor = calculateSpecular(inputData);
#endif

				return o;
			}

			inline float2x2 invert(float2x2 m)
			{
				float det = m[0][0] * m[1][1] - m[0][1] * m[1][0];

				if(abs(det) < 1e-9)
				{
					return float2x2(0, 0, 0, 0);
				}

				return (1.0f / det) * float2x2(m[1][1], -m[0][1], -m[1][0], m[0][0]);
			}

			void frag(
				v2f i, 
				out float4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
				, out float4 outRenderingLayers : SV_Target1
#endif
			)
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// Apply resolution limit to the base texture.
				int targetResolution = (int)log2(_ResolutionLimit);
				int actualResolution = (int)log2(_BaseMap_TexelSize.zw);
				int lod = clamp(actualResolution - targetResolution, 0, 10);

				// Apply affine texture mapping.
				float2 uv = lerp(i.uv, i.affineUVAndFog.xy / i.affineUVAndFog.z, _AffineTextureStrength);

#if defined(_FILTERMODE_BILINEAR)
				float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearRepeat, uv, lod);
#elif defined(_FILTERMODE_N64)
				// Calculate N64 3-point bilinear filtering.

				float modifier = pow(2.0f, lod);
				float4 targetTexelSize = float4(_BaseMap_TexelSize.xy * modifier, _BaseMap_TexelSize.zw / modifier);

				// With thanks to: https://www.emutalk.net/threads/emulating-nintendo-64-3-sample-bilinear-filtering-using-shaders.54215/
				float2 uvA = float2(targetTexelSize.x, 0.0f);
				float2 uvB = float2(0.0f, targetTexelSize.y);
				float2 uvC = float2(targetTexelSize.x, targetTexelSize.y);
				float2 uvHalf = uvC * 0.5f;
				float2 uvCenter = uv - uvHalf;

				float4 baseColorMain = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter, lod);
				float4 baseColorA = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvA, lod);
				float4 baseColorB = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvB, lod);
				float4 baseColorC = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvC, lod);

				float interpX = modf(uvCenter.x * targetTexelSize.z, targetTexelSize.z);
				float interpY = modf(uvCenter.y * targetTexelSize.w, targetTexelSize.w);

				if(uvCenter.x < 0.0f)
				{
					interpX = 1.0f - (interpX * -1.0f);
				}

				if(uvCenter.y < 0.0f)
				{
					interpY = 1.0f - (interpY * -1.0f);
				}

				float4 baseColor = (baseColorMain + interpX * (baseColorA - baseColorMain) + interpY * (baseColorB - baseColorMain)) * (1.0f - step(1.0f, interpX + interpY));
				baseColor += (baseColorC + (1.0f - interpX) * (baseColorB - baseColorC) + (1.0f - interpY) * (baseColorA - baseColorC)) * step(1.0f, interpX + interpY);

				baseColor *= _BaseColor;
#else
				float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uv, lod);
#endif

#if _USE_VERTEX_COLORS
				baseColor *= i.color;
#endif

				// Clip pixels based on alpha.
#ifdef _ALPHATEST_ON
				clip(baseColor.a - _Cutoff);
#endif

				// Posterize the base color.
				float colorBitDepth = max(2, _ColorBitDepth);

				float r = max((baseColor.r - EPSILON) * colorBitDepth, 0.0f);
				float g = max((baseColor.g - EPSILON) * colorBitDepth, 0.0f);
				float b = max((baseColor.b - EPSILON) * colorBitDepth, 0.0f);

				float divisor = colorBitDepth - 1.0f;

				// Apply dithering between posterized colors.
#if defined(_DITHERMODE_SCREEN)
				float3 remainders = float3(frac(r), frac(g), frac(b));
				float2 ditherUV = (i.positionSS.xy / i.positionSS.w) * _ScreenParams.xy / _RetroPixelSize;
				float3 ditheredColor = saturate(dither(remainders, ditherUV));
				ditheredColor = step(0.5f, ditheredColor);
#elif defined(_DITHERMODE_TEXTURE)
				float3 remainders = float3(frac(r), frac(g), frac(b));
				float3 ditheredColor = saturate(dither(remainders, uv * _BaseMap_TexelSize.zw));
				ditheredColor = step(0.5f, ditheredColor);
#else
				float3 ditheredColor = 0.0f;
#endif

				float3 posterizedColor = float3(floor(r), floor(g), floor(b)) + ditheredColor;
				posterizedColor /= divisor;
				posterizedColor += 1.0f / colorBitDepth * _ColorBitDepthOffset;

				// Find an offset vector in world space to snap lighting calcs to texel grid.
				// With massive thanks to: https://discussions.unity.com/t/the-quest-for-efficient-per-texel-lighting/700574
#ifdef _LIGHTMODE_TEXELLIT
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
				float3 ddxWorldPos = ddx(i.positionWS);
				float3 ddyWorldPos = ddy(i.positionWS);

				float3 positionWS = i.positionWS + clamp(ddxWorldPos * uvDeltas.x + ddyWorldPos * uvDeltas.y, -1.0f, 1.0f);
#else
				float3 positionWS = i.positionWS;
#endif

				float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
				float4 shadowMask = SAMPLE_SHADOWMASK(i.dynamicLightmapUV);

				float3 normalWS = normalize(i.normalWS);

				float3 viewWS = GetWorldSpaceNormalizeViewDir(positionWS);

#if defined(_LIGHTMODE_LIT) || defined(_LIGHTMODE_TEXELLIT)
				InputData inputData = (InputData)0;
				inputData.positionWS = positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = viewWS;
				inputData.shadowCoord = shadowCoord;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

				float3 diffuseLightColor = calculateDiffuse(inputData, i.color);
				float3 specularLightColor = calculateSpecular(inputData);

#if DYNAMICLIGHTMAP_ON
				float3 bakedGI = SAMPLE_GI(i.staticLightmapUV, i.dynamicLightmapUV, i.vertexSH, normalWS);
#else
				float3 bakedGI = SAMPLE_GI(i.staticLightmapUV, i.vertexSH, normalWS);
#endif

				diffuseLightColor += bakedGI;

#elif defined(_LIGHTMODE_VERTEXLIT)
				float3 diffuseLightColor = i.diffuseLightColor;
				float3 specularLightColor = i.specularLightColor;
#else
				float3 diffuseLightColor = 1.0f;
				float3 specularLightColor = 0.0f;
#endif

#ifdef _DBUFFER
                float3 specular = 0;
                float metallic = 0;
                float occlusion = 1;
                float smoothness = 0;
                float3 norm = normalWS;
                ApplyDecal(i.positionCS, posterizedColor, specular, norm, metallic, occlusion, smoothness);
#endif

#ifdef _USE_REFLECTION_CUBEMAP
				float3 reflectedVector = reflect(-viewWS, normalWS);
				reflectedVector = RotateAroundYInDegrees(reflectedVector, _CubemapRotation);
#ifdef _FILTERMODE_POINT
				float4 cubemapLighting = SAMPLE_TEXTURECUBE_LOD(_ReflectionCubemap, sampler_PointClamp, reflectedVector, lod);
#else
				float4 cubemapLighting = SAMPLE_TEXTURECUBE_LOD(_ReflectionCubemap, sampler_LinearClamp, reflectedVector, lod);
#endif
				posterizedColor += cubemapLighting.rgb * cubemapLighting.a * _CubemapColor.rgb * _CubemapColor.a;
#endif

				// Combine everything.
				float3 finalColor = posterizedColor * diffuseLightColor + specularLightColor;
				finalColor = MixFog(finalColor, i.affineUVAndFog.w);

				outColor = float4(finalColor, baseColor.a);

#ifdef _WRITE_RENDERING_LAYERS
				uint renderingLayers = GetMeshRenderingLayer();
				outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
			}
            ENDHLSL
        }

		Pass
		{
			Name "ShadowCaster"

			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma vertex shadowPassVert
			#pragma fragment shadowPassFrag

			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _FILTERMODE_BILINEAR _FILTERMODE_POINT _FILTERMODE_N64

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "RetroShadowCasterPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"

			Tags
			{
				"LightMode" = "DepthOnly"
			}

			ZWrite On
			ColorMask R
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex depthOnlyVert
			#pragma fragment depthOnlyFrag

			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _FILTERMODE_BILINEAR _FILTERMODE_POINT _FILTERMODE_N64

			#include "RetroSurfaceInput.hlsl"
			#include "RetroDepthOnlyPass.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DepthNormals"

			Tags
			{
				"LightMode" = "DepthNormals"
			}

			ZWrite On
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex depthNormalsVert
			#pragma fragment depthNormalsFrag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _FILTERMODE_BILINEAR _FILTERMODE_POINT _FILTERMODE_N64

			#include "RetroDepthNormalsPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Meta"

			Tags
			{
				"LightMode" = "Meta"
			}

			Cull Off

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex metaVert
			#pragma fragment metaFrag

			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _FILTERMODE_BILINEAR _FILTERMODE_POINT _FILTERMODE_N64
			#pragma shader_feature_local_fragment _DITHERMODE_SCREEN _DITHERMODE_TEXTURE _DITHERMODE_OFF
			#pragma shader_feature EDITOR_VISUALIZATION

			#include "RetroMetaPass.hlsl"
			ENDHLSL
		}
    }

	CustomEditor "RetroShadersPro.URP.RetroLitShaderGUI"
}
