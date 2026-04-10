#ifndef RETRO_DEPTH_NORMALS_PASS_INCLUDED
#define RETRO_DEPTH_NORMALS_PASS_INCLUDED

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

	float4 positionVS = mul(UNITY_MATRIX_MV, v.positionOS);
	positionVS = floor(positionVS * _SnapsPerUnit) / _SnapsPerUnit;
	o.positionCS = mul(UNITY_MATRIX_P, positionVS);

	o.normalWS = TransformObjectToWorldNormal(v.normalOS);
	o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

	return o;
}

void depthNormalsFrag(
	v2f i,
	out float4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	int targetResolution = (int)log2(_ResolutionLimit);
	int actualResolution = (int)log2(_BaseMap_TexelSize.zw);
	int lod = actualResolution - targetResolution;

#if defined(_FILTERMODE_POINT)
	float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, i.uv, lod);
#else
	float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearRepeat, i.uv, lod);
#endif

	Alpha(baseColor.a, _BaseColor, _Cutoff);

	outNormalWS = float4(NormalizeNormalPerPixel(i.normalWS), 0.0f);
		
#ifdef _WRITE_RENDERING_LAYERS
	uint renderingLayers = GetMeshRenderingLayer();
	outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

#endif // RETRO_DEPTH_NORMALS_PASS_INCLUDED
