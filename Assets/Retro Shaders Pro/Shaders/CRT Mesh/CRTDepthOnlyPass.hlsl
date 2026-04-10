#ifndef CRT_DEPTH_ONLY_PASS_INCLUDED
#define CRT_DEPTH_ONLY_PASS_INCLUDED

struct appdata
{
    float4 position     : POSITION;
    float2 uv     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f depthOnlyVert(appdata v)
{
    v2f output = (v2f) 0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    output.positionCS = TransformObjectToHClip(v.position.xyz);
    return output;
}

half depthOnlyFrag(v2f i) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    Alpha(SampleAlbedoAlpha(i.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);

    return i.positionCS.z;
}
    
#endif // CRT_DEPTH_ONLY_PASS_INCLUDED
