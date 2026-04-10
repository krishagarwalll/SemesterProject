#ifndef RETRO_SHADER_GRAPH_FUNCTIONS_INCLUDED
#define RETRO_SHADER_GRAPH_FUNCTIONS_INCLUDED

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void CalculateAffineUVs_float(float4 PositionOS, float2 UVs, out float2 AffineUVs)
{
    float4 positionCS = TransformObjectToHClip(PositionOS);
    AffineUVs = UVs * positionCS.w;
}

void CalculateAffineUVs_half(half4 PositionOS, half2 UVs, out half2 AffineUVs)
{
    half4 positionCS = TransformObjectToHClip(PositionOS);
    AffineUVs = UVs * positionCS.w;
}

// Many thanks to https://github.com/Cyanilux/URP_ShaderGraphCustomLighting/blob/main/CustomLighting.hlsl:
#ifndef SHADERGRAPH_PREVIEW
	#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
	#if (SHADERPASS != SHADERPASS_FORWARD)
		#undef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
	#endif
#endif

void CalculateLight_float(float2 LightmapUV, float3 PositionWS, float3 NormalWS, /*float AmbientLight, */out float3 LightColor)
{
    LightColor = 0.0f;
	
#ifndef SHADERGRAPH_PREVIEW

#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
	float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(PositionWS));
#else
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
#endif
    
    OUTPUT_LIGHTMAP_UV(LightmapUV, unity_LightmapST, LightmapUV);
    float4 shadowMask = SAMPLE_SHADOWMASK(LightmapUV);
	
    // Apply the main light.
    Light light = GetMainLight(shadowCoord);

    float3 normalDir = normalize(NormalWS);
    float lightAmount = saturate(dot(normalDir, light.direction) * light.distanceAttenuation * light.shadowAttenuation);

// Note: if you would like to use the ambient light manual override, add a Boolean keyword
// to your graph named "_USE_AMBIENT_OVERRIDE". Make it a Local ShaderFeature.

//#ifndef _USE_AMBIENT_OVERRIDE
    LightColor = lerp(SampleSH(NormalWS), 1.0f, lightAmount) * light.color;
//#else
	//LightColor = lerp(AmbientLight, 1.0f, lightAmount) * light.color;
//#endif

#ifdef _ADDITIONAL_LIGHTS

	// Apply secondary lights.
	uint lightCount = GetAdditionalLightsCount();

#if USE_FORWARD_PLUS
	InputData inputData = (InputData)0;
	inputData.positionWS = PositionWS;
	inputData.normalWS = NormalWS;
	inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(PositionWS);
	inputData.shadowCoord = shadowCoord;

	float4 screenPos = ComputeScreenPos(TransformWorldToHClip(PositionWS));
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(screenPos);

	// Apply secondary lights (Forward+ rendering).
	LIGHT_LOOP_BEGIN(lightsCount)
		Light light = GetAdditionalLight(lightIndex, PositionWS, shadowMask);

		float3 color = dot(light.direction, normalDir);
		color *= light.color;
		color *= light.distanceAttenuation;
		color *= light.shadowAttenuation;
		color = max(color, 0.0f);

		LightColor += color;
	LIGHT_LOOP_END
#else
	// Apply secondary lights (Forward rendering).
	for (uint lightIndex = 0; lightIndex < lightCount; ++lightIndex) 
	{
		Light light = GetAdditionalLight(lightIndex, PositionWS, shadowMask);

		float3 color = dot(light.direction, normalDir);
		color *= light.color;
		color *= light.distanceAttenuation;
		color *= light.shadowAttenuation;
		color = max(color, 0.0f);

		LightColor += color;
}
#endif
#endif
	
#endif
}

void CalculateLight_half(half2 LightmapUV, half3 PositionWS, half3 NormalWS, /*half AmbientLight, */out half3 LightColor)
{
    LightColor = 0.0f;
	
#ifndef SHADERGRAPH_PREVIEW

#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
	half4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(PositionWS));
#else
    half4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
#endif
    
    OUTPUT_LIGHTMAP_UV(LightmapUV, unity_LightmapST, LightmapUV);
    half4 shadowMask = SAMPLE_SHADOWMASK(LightmapUV);
	
    // Apply the main light.
    Light light = GetMainLight(shadowCoord);

    half3 normalDir = normalize(NormalWS);
    half lightAmount = saturate(dot(normalDir, light.direction) * light.distanceAttenuation * light.shadowAttenuation);

// Note: if you would like to use the ambient light manual override, add a Boolean keyword
// to your graph named "_USE_AMBIENT_OVERRIDE". Make it a Local ShaderFeature.

//#ifndef _USE_AMBIENT_OVERRIDE
    LightColor = lerp(SampleSH(NormalWS), 1.0f, lightAmount) * light.color;
//#else
	//LightColor = lerp(AmbientLight, 1.0f, lightAmount) * light.color;
//#endif

#ifdef _ADDITIONAL_LIGHTS

	// Apply secondary lights.
	uint lightCount = GetAdditionalLightsCount();

#if USE_FORWARD_PLUS
	InputData inputData = (InputData)0;
	inputData.positionWS = PositionWS;
	inputData.normalWS = NormalWS;
	inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(PositionWS);
	inputData.shadowCoord = shadowCoord;

	half4 screenPos = ComputeScreenPos(TransformWorldToHClip(PositionWS));
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(screenPos);

	// Apply secondary lights (Forward+ rendering).
	LIGHT_LOOP_BEGIN(lightsCount)
		Light light = GetAdditionalLight(lightIndex, PositionWS, shadowMask);

		half3 color = dot(light.direction, normalDir);
		color *= light.color;
		color *= light.distanceAttenuation;
		color *= light.shadowAttenuation;
		color = max(color, 0.0f);

		LightColor += color;
	LIGHT_LOOP_END
#else
	// Apply secondary lights (Forward rendering).
	for (uint lightIndex = 0; lightIndex < lightCount; ++lightIndex) 
	{
		Light light = GetAdditionalLight(lightIndex, PositionWS, shadowMask);

		half3 color = dot(light.direction, normalDir);
		color *= light.color;
		color *= light.distanceAttenuation;
		color *= light.shadowAttenuation;
		color = max(color, 0.0f);

		LightColor += color;
}
#endif
#endif
	
#endif
}

void ApplyFog_float(float3 Color, float Fog, out float3 OutColor)
{
#ifdef SHADERGRAPH_PREVIEW
	OutColor = Color;
#else
    OutColor = MixFog(Color, Fog);
#endif
}

void ApplyFog_half(half3 Color, half Fog, out half3 OutColor)
{
#ifdef SHADERGRAPH_PREVIEW
	OutColor = Color;
#else
    OutColor = MixFog(Color, Fog);
#endif
}

#endif // RETRO_SHADER_GRAPH_FUNCTIONS_INCLUDED
