#ifndef RETRO_META_PASS_INCLUDED
#define RETRO_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct appdata
{
	float4 positionOS : POSITION;
    float4 color : COLOR;
	float3 normalOS : NORMAL;
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	float4 positionCS : SV_POSITION;
    float4 color : COLOR;
	float2 uv : TEXCOORD0;
	float3 affineUV : TEXCOORD1;
    float4 positionSS : TEXCOORD2;

#ifdef EDITOR_VISUALIZATION
	float2 vizUV : TEXCOORD3;
	float4 lightCoord : TEXCOORD4;
#endif
};

v2f metaVert(appdata v)
{
	v2f o = (v2f)0;

	float4 vertex = v.positionOS;

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
	float4 positionVS = mul(UNITY_MATRIX_V, vertex);
	//o.positionCS = TransformWorldToHClip(vertex.xyz);
#else
	float4 positionVS = mul(UNITY_MATRIX_MV, v.positionOS.xyz);
	//o.positionCS = TransformObjectToHClip(vertex);
#endif

	positionVS = floor(positionVS * _SnapsPerUnit) / _SnapsPerUnit;
	o.positionCS = mul(UNITY_MATRIX_P, positionVS);

	o.uv = TRANSFORM_TEX(v.uv0, _BaseMap);
    o.affineUV = float3(TRANSFORM_TEX(v.uv0, _BaseMap) * o.positionCS.w, o.positionCS.w);
#ifdef EDITOR_VISUALIZATION
	UnityEditorVizData(v.positionOS.xyz, v.uv0, v.uv1, v.uv2, o.vizUV, o.lightCoord);
#endif
	
    o.positionSS = ComputeScreenPos(o.positionCS);
	
    o.color = v.color;

	return o;
}

float4 metaFrag(v2f i) : SV_TARGET
{
	int targetResolution = (int)log2(_ResolutionLimit);
	int actualResolution = (int)log2(_BaseMap_TexelSize.zw);
	int lod = actualResolution - targetResolution;
	
    float2 uv = lerp(i.uv, i.affineUV.xy / i.affineUV.z, _AffineTextureStrength);

#if defined(_FILTERMODE_POINT)
	float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uv, lod) * i.color;
#else
    float4 baseColor = _BaseColor * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearRepeat, uv, lod) * i.color;
#endif
	
	// Posterize the base color.
    float colorBitDepth = max(2, _ColorBitDepth);

    float r = max((baseColor.r - EPSILON) * colorBitDepth, 0.0f);
    float g = max((baseColor.g - EPSILON) * colorBitDepth, 0.0f);
    float b = max((baseColor.b - EPSILON) * colorBitDepth, 0.0f);

    float divisor = colorBitDepth - 1.0f;

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
	
    baseColor = float4(posterizedColor, baseColor.a);

	MetaInput metaInput; 
	metaInput.Albedo = baseColor.rgb;
	metaInput.Emission = 1;

#ifdef EDITOR_VISUALIZATION
	metaInput.VizUV = i.vizUV;
	metaInput.LightCoord = i.lightCoord;
#endif

	return UnityMetaFragment(metaInput);
}

#endif // RETRO_META_PASS_INCLUDED
