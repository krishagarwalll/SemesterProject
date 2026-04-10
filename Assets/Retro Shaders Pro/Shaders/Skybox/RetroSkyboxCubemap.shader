Shader "Retro Shaders Pro/Skybox/Retro Skybox"
{
    Properties
    {
		[MainColor] [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		[NoScaleOffset] _BaseCubemap("Base Cubemap", Cube) = "grey" {}
		_Rotation("Rotation", Range(0.0, 360.0)) = 0.0
		_ResolutionLimit("Resolution Limit (Power of 2)", Integer) = 128
		_ColorBitDepth("Color Depth", Integer) = 16
		_ColorBitDepthOffset("Color Depth Offset", Range(0.0, 1.0)) = 0.0

		[HDR] _GroundColor("Ground Color", Color) = (0.5, 0.5, 0.5, 0.5)
		[HDR] _SkyColor("Sky Color", Color) = (1, 1, 1, 1)
		_ColorMixPower("Color Mix Power", Range(0, 10)) = 1

		_CloudHeightThresholds("Cloud Height Thresholds", Vector) = (0, 1, 0, 0)
		_CloudDensityThresholds("Cloud Density Thresholds", Vector) = (0, 1, 0, 0)
		[HDR] _CloudColor("Cloud Color", Color) = (1, 1, 1, 1)
		_CloudSizes("Cloud Sizes", Vector) = (5, 25, 0, 0)
		_CloudVelocity("Cloud Velocity", Vector) = (0.1, 0.25, 0, 0)

		[Toggle] _USE_POINT_FILTER("Use Point Filtering", Float) = 1
		[Toggle] _USE_CLOUDS("Use Clouds", Float) = 1
		[KeywordEnum(Screen, Texture, Off)] _DitherMode("Dithering Mode", Integer) = 0
		[KeywordEnum(Add, Multiply, Subtract, Divide)] _CombineMode("Combine Mode", Integer) = 1
		[KeywordEnum(Gradient, Cubemap)] _BackgroundMode("Background Mode", Integer) = 1
    }
    SubShader
    {
		Tags
		{
			"RenderType" = "Background"
			"Queue" = "Background"
			"PreviewType" = "Skybox"
			"RenderPipeline" = "UniversalPipeline"
		}

		Cull Off
		ZWrite Off

        Pass
        {
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

			#pragma shader_feature_local _USE_POINT_FILTER_ON
			#pragma shader_feature_local _USE_CLOUDS
			#pragma shader_feature_local_fragment _DITHERMODE_SCREEN _DITHERMODE_TEXTURE _DITHERMODE_OFF
			#pragma shader_feature_local _COMBINEMODE_ADD _COMBINEMODE_MULTIPLY _COMBINEMODE_SUBTRACT _COMBINEMODE_DIVIDE
			#pragma shader_feature_local _BACKGROUNDMODE_GRADIENT _BACKGROUNDMODE_CUBEMAP

			#define EPSILON 1e-06

			float3 dither(float3 col, float2 uv)
			{
				static float DITHER_THRESHOLDS[16] =
				{
					1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
					13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
					4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
					16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
				};
				uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;

				return col - DITHER_THRESHOLDS[index];
			}

			float random(float2 uv)
			{
				return frac(sin(dot(abs(uv), float2(12.9898f, 78.233f))) * 43758.5453f);
			}

			float valueNoise(float2 uv)
			{
				float2 i = floor(uv);
				float2 f = frac(uv);

				float r0 = random(i);
				float r1 = random(i + float2(1.0f, 0.0f));
				float r2 = random(i + float2(0.0f, 1.0f));
				float r3 = random(i + float2(1.0f, 1.0f));

				float2 u = f * f * (3.0f - 2.0f * f);

				return lerp(lerp(r0, r1, u.x), lerp(r2, r3, u.x), u.y);
			}

			float simpleNoise(float2 uv, float scale)
			{
				float t = 0.0f;

				float freq = 1.0f;
				float amp = 0.125f;
				t += valueNoise(uv * scale / freq) * amp;

				freq = 2.0f;
				amp = 0.25f;
				t += valueNoise(uv * scale / freq) * amp;

				freq = 4.0f;
				amp = 0.5f;
				t += valueNoise(uv * scale / freq) * amp;

				return t;
			}

            struct appdata
            {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4 positionCS : SV_POSITION;
				float3 uv : TEXCOORD0;
				float4 positionSS : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
            };

			TEXTURECUBE(_BaseCubemap);

			CBUFFER_START(UnityPerMaterial)
				float _Rotation;
				int _ResolutionLimit;

				float4 _BaseColor;
				float4 _BaseCubemap_TexelSize;
				float4 _BaseCubemap_ST;
				int _ColorBitDepth;
				float _ColorBitDepthOffset;

				float4 _GroundColor;
				float4 _SkyColor;
				float _ColorMixPower;

				float2 _CloudHeightThresholds;
				float2 _CloudDensityThresholds;
				float4 _CloudColor;
				float2 _CloudSizes;
				float2 _CloudVelocity;
			CBUFFER_END

			int _RetroPixelSize = 1;

			// From: https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Skybox-Cubed.shader
			float3 RotateAroundYInDegrees(float3 vertex, float degrees)
			{
				float alpha = degrees * PI / 180.0;
				float sina, cosa;
				sincos(alpha, sina, cosa);
				float2x2 m = float2x2(cosa, -sina, sina, cosa);
				return float3(mul(m, vertex.xz), vertex.y).xzy;
			}

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 positionRotated = RotateAroundYInDegrees(v.positionOS.xyz, _Rotation);
				o.positionCS = TransformObjectToHClip(positionRotated);
				o.uv = v.positionOS.xyz;
				o.positionSS = ComputeScreenPos(o.positionCS);

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float3 viewWS = normalize(i.uv);

#ifdef _BACKGROUNDMODE_CUBEMAP
				int targetResolution = (int)log2(_ResolutionLimit);
				int actualResolution = (int)log2(_BaseCubemap_TexelSize.zw);
				int lod = actualResolution - targetResolution;

#ifdef _USE_POINT_FILTER_ON
				float4 baseColor = _BaseColor * SAMPLE_TEXTURECUBE_LOD(_BaseCubemap, sampler_PointClamp, i.uv, lod);
#else
				float4 baseColor = _BaseColor * SAMPLE_TEXTURECUBE_LOD(_BaseCubemap, sampler_LinearClamp, i.uv, lod);
#endif

				// Posterize the base color.
				float colorBitDepth = max(2, _ColorBitDepth);

				float r = max((baseColor.r - EPSILON) * colorBitDepth, 0.0f);
				float g = max((baseColor.g - EPSILON) * colorBitDepth, 0.0f);
				float b = max((baseColor.b - EPSILON) * colorBitDepth, 0.0f);

				float divisor = colorBitDepth - 1.0f;

				// Apply dithering between posterized colors.
#if defined(_DITHERMODE_SCREEN)
				float3 remainders = float3(frac(r), frac(g), frac(b));
				float2 ditherUV = (i.positionSS.xy / i.positionSS.w) * _ScreenParams.xy / _RetroPixelSize;
				float3 ditheredColor = saturate(dither(remainders, ditherUV));
				ditheredColor = step(0.5f, ditheredColor);
#elif defined(_DITHERMODE_TEXTURE)
				float3 remainders = float3(frac(r), frac(g), frac(b));
				float3 ditheredColor = saturate(dither(remainders, (i.uv + 2.0f) * _BaseCubemap_TexelSize.zw));
				ditheredColor = step(0.5f, ditheredColor);
#else
				float3 ditheredColor = 0.0f;
#endif

				float3 posterizedColor = float3(floor(r), floor(g), floor(b)) + ditheredColor;
				posterizedColor /= divisor;
				posterizedColor += 1.0f / colorBitDepth * _ColorBitDepthOffset;

				baseColor.rgb = posterizedColor;

#else
				float height = saturate(viewWS.y);
				float blend = saturate(pow(height, pow(2.0f, _ColorMixPower - 1.0f)));
				float4 baseColor = lerp(_GroundColor, _SkyColor, blend);
#endif

#ifdef _USE_CLOUDS
				float2 cloudUV = viewWS.xz / viewWS.y + _CloudVelocity * _Time.x;
				float res = float(_ResolutionLimit);
				cloudUV = floor(cloudUV * res) / res;

				float noise1 = simpleNoise(cloudUV + 100.0f, _CloudSizes.x);
				float noise2 = simpleNoise(cloudUV, _CloudSizes.y);
				
#if defined(_COMBINEMODE_ADD)
				float noise = saturate(noise1 + noise2);
#elif defined(_COMBINEMODE_MULTIPLY)
				float noise = noise1 * noise2;
#elif defined(_COMBINEMODE_SUBTRACT)
				float noise = saturate(noise1 - noise2);
#else
				float noise = noise1 / max(noise2, 0.0001f);
#endif
				float clouds = smoothstep(_CloudDensityThresholds.x, _CloudDensityThresholds.y, noise);
				clouds *= smoothstep(_CloudHeightThresholds.x, _CloudHeightThresholds.y, viewWS.y);

				baseColor.rgb = lerp(baseColor.rgb, _CloudColor.rgb, clouds * _CloudColor.a);
#endif

				return baseColor;
			}
            ENDHLSL
        }
    }

	CustomEditor "RetroShadersPro.URP.RetroSkyboxShaderGUI"
}
