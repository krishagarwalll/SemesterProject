#ifndef RETRO_DEPTH_ONLY_PASS_INCLUDED
#define RETRO_DEPTH_ONLY_PASS_INCLUDED

struct appdata
{
	float4 positionOS : POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

v2f depthOnlyVert(appdata v)
{
	v2f o = (v2f)0;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 positionVS = mul(UNITY_MATRIX_MV, v.positionOS);
	positionVS = floor(positionVS * _SnapsPerUnit) / _SnapsPerUnit;
	o.positionCS = mul(UNITY_MATRIX_P, positionVS);

	o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

	return o;
}

float depthOnlyFrag(v2f i) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	Alpha(SampleAlbedoAlpha(i.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);

	return i.positionCS.z;
}

#endif // RETRO_DEPTH_ONLY_PASS_INCLUDED
