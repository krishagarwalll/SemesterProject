#ifndef CRT_META_PASS_INCLUDED
#define CRT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct appdata
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;

#ifdef EDITOR_VISUALIZATION
	float2 vizUV : TEXCOORD2;
	float4 lightCoord : TEXCOORD3;
#endif
};

v2f metaVert(appdata v)
{
    v2f o = (v2f) 0;

    float3 vertex = v.positionOS.xyz;

#ifndef EDITOR_VISUALIZATION
    if (unity_MetaVertexControl.x)
    {
        vertex.xy = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
        vertex.z = vertex.z > 0 ? REAL_MIN : 0.0f;
    }
    if (unity_MetaVertexControl.y)
    {
        vertex.xy = v.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
        vertex.z = vertex.z > 0 ? REAL_MIN : 0.0f;
    }
#endif
    
    o.positionCS = TransformObjectToHClip(vertex);
    o.uv = TRANSFORM_TEX(v.uv0, _BaseMap);
#ifdef EDITOR_VISUALIZATION
	UnityEditorVizData(v.positionOS.xyz, v.uv0, v.uv1, v.uv2, o.vizUV, o.lightCoord);
#endif

    return o;
}

float4 metaFrag(v2f i) : SV_TARGET
{
    float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearRepeat, i.uv, 0);

    MetaInput metaInput;
    metaInput.Albedo = baseColor.rgb;
    metaInput.Emission = 1;

#ifdef EDITOR_VISUALIZATION
	metaInput.VizUV = i.vizUV;
	metaInput.LightCoord = i.lightCoord;
#endif

    return UnityMetaFragment(metaInput);
}

#endif // CRT_META_PASS_INCLUDED
