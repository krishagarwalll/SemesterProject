#ifndef CRT_DEPTH_NORMALS_PASS_INCLUDED
#define CRT_DEPTH_NORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

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
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f depthNormalsVert(appdata v)
{
    v2f o = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);

    return o;
}

float4 depthNormalsFrag(v2f i) : SV_TARGET0
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        
    float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, i.uv, 0);
        
    Alpha(baseColor.a, _BaseColor, _Cutoff);
        
    return float4(NormalizeNormalPerPixel(i.normalWS), 0.0f);
}

#endif // CRT_DEPTH_NORMALS_PASS_INCLUDED
