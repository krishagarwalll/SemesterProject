#ifndef RETRO_SHADOW_CASTER_PASS_INCLUDED
#define RETRO_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct appdata
{
	float4 positionOS : POSITION;
	float2 uv : TEXCOORD0;
	float3 normalOS : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normalWS : TEXCOORD1;
};

float4 GetShadowPositionHClip(appdata i)
{
	float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
    float3 positionWS = TransformObjectToWorld(i.positionOS.xyz);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
	float3 lightDirectionWS = _LightDirection;
#endif

	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
	positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

	return positionCS;
}

v2f shadowPassVert(appdata v)
{
	v2f o = (v2f)0;
	UNITY_SETUP_INSTANCE_ID(v);

	o.positionCS = GetShadowPositionHClip(v);
	o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

	return o;
}

float4 shadowPassFrag(v2f i) : SV_TARGET
{
	int targetResolution = (int)log2(_ResolutionLimit);
	int actualResolution = (int)log2(_BaseMap_TexelSize.zw);
	int lod = actualResolution - targetResolution;

#if defined(_FILTERMODE_POINT)
	float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, i.uv, lod);
#else
	float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearRepeat, i.uv, lod);
#endif

	Alpha(baseColor.a, _BaseColor, _Cutoff);

	return 0;
}

#endif // RETRO_SHADOW_CASTER_PASS_INCLUDED
