#ifndef CRT_INPUT_SURFACE_INCLUDED
#define CRT_INPUT_SURFACE_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_RGBTex);
TEXTURE2D(_ScanlineTex);
TEXTURE2D(_TrackingTex);
TEXTURE2D(_ColorRampTex);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap_TexelSize;
    half4 _BaseColor;
    float4 _BackgroundColor;
    float _DistortionStrength;
    float _DistortionSmoothing;
    int _PixelSize;
    int _RGBPixelSize;
    float _RGBStrength;
    float _ScanlineStrength;
    float _RandomWear;
    float _ScrollSpeed;
    float _AberrationStrength;
    float _TrackingSize;
    float _TrackingStrength;
    float _TrackingSpeed;
    float _TrackingJitter;
    float _TrackingColorDamage;
    float _TrackingLinesThreshold;
    float4 _TrackingLinesColor;
    float _Brightness;
    float _Contrast;
    half _Cutoff;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _BackgroundColor)
    UNITY_DOTS_INSTANCED_PROP(float,  _DistortionStrength)
    UNITY_DOTS_INSTANCED_PROP(float,  _DistortionSmoothing)
    UNITY_DOTS_INSTANCED_PROP(int,    _PixelSize)
    UNITY_DOTS_INSTANCED_PROP(int,    _RGBPixelSize)
    UNITY_DOTS_INSTANCED_PROP(float,  _RGBStrength)
    UNITY_DOTS_INSTANCED_PROP(float,  _ScanlineStrength)
    UNITY_DOTS_INSTANCED_PROP(float,  _RandomWear)
    UNITY_DOTS_INSTANCED_PROP(float,  _ScrollSpeed)
    UNITY_DOTS_INSTANCED_PROP(float,  _AberrationStrength)
    UNITY_DOTS_INSTANCED_PROP(float,  _TrackingSize)
    UNITY_DOTS_INSTANCED_PROP(float,  _TrackingStrength)
    UNITY_DOTS_INSTANCED_PROP(float,  _TrackingSpeed)
    UNITY_DOTS_INSTANCED_PROP(float,  _TrackingJitter)
    UNITY_DOTS_INSTANCED_PROP(float,  _TrackingColorDamage)
    UNITY_DOTS_INSTANCED_PROP(float,  _TrackingLinesThreshold)
    UNITY_DOTS_INSTANCED_PROP(float4, _TrackingLinesColor)
    UNITY_DOTS_INSTANCED_PROP(float,  _Brightness)
    UNITY_DOTS_INSTANCED_PROP(float,  _Contrast)
    UNITY_DOTS_INSTANCED_PROP(half,   _Cutoff)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
#define _BackgroundColor;           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BackgroundColor)
#define _DistortionStrength;        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _DistortionStrength)
#define _DistortionSmoothing;       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _DistortionSmoothing)
#define _PixelSize;                 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int,    _PixelSize)
#define _RGBPixelSize;              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int,    _RGBPixelSize)
#define _RGBStrength;               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _RGBStrength)
#define _ScanlineStrength;          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _ScanlineStrength)
#define _RandomWear;                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _RandomWear)
#define _ScrollSpeed;               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _ScrollSpeed)
#define _AberrationStrength;        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _AberrationStrength)
#define _TrackingSize;              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _TrackingSize)
#define _TrackingStrength;          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _TrackingStrength)
#define _TrackingSpeed;             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _TrackingSpeed)
#define _TrackingJitter;            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _TrackingJitter)
#define _TrackingColorDamage;       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _TrackingColorDamage)
#define _TrackingLinesThreshold;    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _TrackingLinesThreshold)
#define _TrackingLinesColor;        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _TrackingLinesColor)
#define _Brightness;                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _Brightness)
#define _Contrast;                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _Contrast)
#define _Cutoff                     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(half,   _Cutoff)

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

#endif // CRT_INPUT_SURFACE_INCLUDED
