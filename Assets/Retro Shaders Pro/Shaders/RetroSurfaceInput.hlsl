#ifndef RETRO_INPUT_SURFACE_INCLUDED
#define RETRO_INPUT_SURFACE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

#ifdef _USE_REFLECTION_CUBEMAP
TEXTURECUBE(_ReflectionCubemap);
SAMPLER(sampler_ReflectionCubemap);
#endif

CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float4 _BaseMap_TexelSize;
float4 _BaseMap_ST;
float4 _BaseMap_MipInfo;
int _ResolutionLimit;
int _SnapsPerUnit;
int _ColorBitDepth;
float _ColorBitDepthOffset;
float _AmbientLight;
float _AffineTextureStrength;
float _Glossiness;
float4 _CubemapColor;
float _CubemapRotation;
float _Cutoff;
CBUFFER_END

// Pixel Size used for dithering, set by CRT effect (if one is active).
int _RetroPixelSize = 1;

#ifdef UNITY_DOTS_INSTANCING_ENABLED

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
	UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DOTS_INSTANCED_PROP(int,    _ResolutionLimit)
	UNITY_DOTS_INSTANCED_PROP(int,    _SnapsPerUnit)
	UNITY_DOTS_INSTANCED_PROP(int,    _ColorBitDepth)
	UNITY_DOTS_INSTANCED_PROP(float,  _ColorBitDepthOffset)
	UNITY_DOTS_INSTANCED_PROP(float,  _AmbientLight)
	UNITY_DOTS_INSTANCED_PROP(float,  _AffineTextureStrength)
	UNITY_DOTS_INSTANCED_PROP(float,  _Glossiness)
	UNITY_DOTS_INSTANCED_PROP(float4, _CubemapColor)
	UNITY_DOTS_INSTANCED_PROP(float,  _CubemapRotation)
	UNITY_DOTS_INSTANCED_PROP(float,  _Cutoff)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor				UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
#define _ResolutionLimit		UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int,    _ResolutionLimit)
#define _SnapsPerUnit			UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int,    _SnapsPerUnit)
#define _ColorBitDepth			UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int,    _ColorBitDepth)
#define _ColorBitDepthOffset	UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _ColorBitDepthOffset)
#define _AmbientLight			UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _AmbientLight)
#define _AffineTextureStrength	UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _AffineTextureStrength)
#define _Glossiness				UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _Glossiness)
#define _CubemapColor			UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _CubemapColor)
#define _CubemapRotation		UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _CubemapRotation)
#define _Cutoff					UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _Cutoff)

#endif

///////////////////////////////////////////////////////////////////////////////
//                      Material Property Helpers                            //
///////////////////////////////////////////////////////////////////////////////
half Alpha(half albedoAlpha, half4 color, half cutoff)
{
#if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) && !defined(_GLOSSINESS_FROM_BASE_ALPHA)
	half alpha = albedoAlpha * color.a;
#else
	half alpha = color.a;
#endif

	alpha = AlphaDiscard(alpha, cutoff);

	return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
	return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
#ifdef _NORMALMAP
	half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
#if BUMP_SCALE_NOT_SUPPORTED
	return UnpackNormal(n);
#else
	return UnpackNormalScale(n, scale);
#endif
#else
	return half3(0.0h, 0.0h, 1.0h);
#endif
}

half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
{
#ifndef _EMISSION
	return 0;
#else
	return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor;
#endif
}

#endif // RETRO_INPUT_SURFACE_INCLUDED
