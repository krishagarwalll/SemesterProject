Shader "Retro Shaders Pro/Post Processing/CRT"
{
	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}

		Pass
		{
			ZTest Always
            Cull Off
            ZWrite Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#pragma target 3.0

			#pragma multi_compile_local_fragment _ _CHROMATIC_ABERRATION_ON
			#pragma multi_compile_local_fragment _ _TRACKING_ON
			#pragma multi_compile_local_fragment _ _INTERLACING_ON
			#pragma multi_compile_local_fragment _ _POINT_FILTERING_ON
			#pragma multi_compile_local_fragment _ _DITHERING_ON
			#pragma multi_compile_local_fragment _COLOR_RAMP_NONE _COLOR_RAMP_RGB _COLOR_RAMP_LUMINANCE _COLOR_RAMP_INTENSITY _COLOR_RAMP_SLIDERS

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			TEXTURE2D(_InputTexture);
			TEXTURE2D(_RGBTex);
			TEXTURE2D(_ScanlineTex);
			TEXTURE2D(_TrackingTex);
			TEXTURE2D(_ColorRampTex);

#if UNITY_VERSION < 600000
			float4 _BlitTexture_TexelSize;
#endif
			float4 _TintColor;
			float4 _BackgroundColor;
			float _DistortionStrength;
			float _DistortionSmoothing;
			int _Size;
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
			int _RedValues;
			int _GreenValues;
			int _BlueValues;
			int _Interlacing;

			#define EPSILON 1e-06

			// Code 'liberated' from Shader Graph's Simple Noise node.
			inline float randomValue(float2 uv)
			{
				return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
			}

			float3 rgb2yiq(float3 col)
			{
				static float3x3 yivMatrix = float3x3(
					0.299f,	 0.587f,	 0.114f,
					0.596f,	-0.275f,	-0.321f,
					0.212f,	-0.523f,	 0.311f
					);

				return mul(yivMatrix, col);
			}

			float3 yiq2rgb(float3 col)
			{
				static float3x3 rgbMatrix = float3x3(
					1.000f,	 0.956f,	 0.619f,
					1.000f,	-0.272f,	-0.647f,
					1.000f,	-1.106f,	 1.703f
					);

				return mul(rgbMatrix, col);
			}

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

            float4 frag (Varyings i) : SV_Target
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// Apply barrel distortion to UVs.
				float2 originalUVs = i.texcoord - 0.5f;
				float offset = length(originalUVs);
				float2 UVs = originalUVs * (1 + _DistortionStrength * offset * offset) + 0.5f;

				// Save UVs to use for barrel distortion later.
				float2 distortedUVs = UVs;

				// Set up UVs to use for screen-space effects.
				float2 screenUVs = UVs * _ScreenSize.xy / _Size;

				// Get RGB overlay texture.
				float3 rgbCells = SAMPLE_TEXTURE2D(_RGBTex, sampler_LinearRepeat, screenUVs).rgb;

				// Get scanline overlay texture.
				screenUVs.y += _Time.y * _ScrollSpeed;
				float3 scanlines = SAMPLE_TEXTURE2D(_ScanlineTex, sampler_LinearRepeat, screenUVs).rgb;

#ifdef _TRACKING_ON
				float2 trackingUVs = float2(UVs.y * _TrackingSize + _Time.y * _TrackingSpeed + randomValue(_Time.xx) * _TrackingJitter, 0.5f);

				// Get tracking amount.
				float3 trackingSample = (SAMPLE_TEXTURE2D(_TrackingTex, sampler_LinearRepeat, trackingUVs).rgb - 0.5f) * 2.0f;
				float trackingStrength = trackingSample.r;

				// Offset UVs horizontally based on tracking amount.
				float trackingOffset = trackingStrength * _BlitTexture_TexelSize.x * _TrackingStrength;
				UVs.x += trackingOffset;
#endif

				// Offset UVs horizontally based on random tape wear.
				float randomOffset = randomValue(float2(i.texcoord.y, _Time.x));
				UVs.x += randomOffset * _BlitTexture_TexelSize.x * _RandomWear;

				// Sample the blit texture, applying chromatic aberration if enabled.
#ifdef _CHROMATIC_ABERRATION_ON
				float2 redUVs = UVs + originalUVs * _AberrationStrength * _BlitTexture_TexelSize.xy;
				float2 blueUVs = UVs - originalUVs * _AberrationStrength * _BlitTexture_TexelSize.xy;
	#ifdef _POINT_FILTERING_ON
				float red = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, redUVs).r;
				float2 greenAlpha = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, UVs).ga;
				float blue = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, blueUVs).b;
	#else
				float red = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, redUVs).r;
				float2 greenAlpha = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, UVs).ga;
				float blue = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, blueUVs).b;
	#endif
				float3 col = float3(red, greenAlpha.x, blue);
				float alpha = greenAlpha.y;
#else
	#ifdef _POINT_FILTERING_ON
				float4 sampleCol = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, UVs);
	#else
				float4 sampleCol = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, UVs);
	#endif

				float3 col = sampleCol.rgb;
				float alpha = sampleCol.a;
#endif

				col *= _TintColor.rgb;

				// Apply brightness and contrast modifiers.
				col = saturate(col * _Brightness);
				col = col - _Contrast * (col - 1.0f) * col * (col - 0.5f);

				// Apply global color ramps if enabled.
#if defined(_COLOR_RAMP_RGB)
				float r = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(col.r, 0.5f)).r;
				float g = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(col.g, 0.5f)).g;
				float b = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(col.b, 0.5f)).b;

				col.rgb = float3(r, g, b);
#elif defined(_COLOR_RAMP_LUMINANCE)
				float l = Luminance(col.rgb);
				col.rgb = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(l, 0.5f)).rgb;
#elif defined(_COLOR_RAMP_INTENSITY)
				float l = Luminance(col.rgb);

				float r = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(col.r, 0.5f)).r;
				float g = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(col.g, 0.5f)).g;
				float b = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(col.b, 0.5f)).b;
				float intensity = SAMPLE_TEXTURE2D(_ColorRampTex, sampler_PointClamp, float2(l, 0.5f)).a;

				col.rgb = float3(r, g, b);
				col.rgb *= intensity;
#elif defined(_COLOR_RAMP_SLIDERS)
				float3 levels = float3(_RedValues, _GreenValues, _BlueValues);
				float r = max((col.r - EPSILON) * levels.r, 0.0f);
				float g = max((col.g - EPSILON) * levels.g, 0.0f);
				float b = max((col.b - EPSILON) * levels.b, 0.0f);

				float3 divisor = levels - 1.0f;

#if defined(_DITHERING_ON)
				float3 remainders = float3(frac(r), frac(g), frac(b));
				float2 ditherUV = UVs * _ScreenParams.xy / _Size;
				float3 ditheredColor = saturate(dither(remainders, ditherUV));
				ditheredColor = step(0.5f, ditheredColor);
#else
				float3 ditheredColor = 0.0f;
#endif
				col.rgb = (float3(floor(r), floor(g), floor(b)) + ditheredColor) / divisor;
#endif

#ifdef _TRACKING_ON
				// Apply tracking lines.
				float t = _Time.x % 1.0f + 2.307f;
				float x = step(_TrackingLinesThreshold, randomValue(float2((UVs.x + UVs.y * 28.303f) * 0.00005f, t)));
				float y = step(0.7f, randomValue(float2(UVs.y * 236.2144f, t)));

				float trackingLines = abs(trackingSample.g) * saturate(x * y);
				col = lerp(col, _TrackingLinesColor.rgb, trackingLines * _TrackingLinesColor.a);

				// Rotate to new chrominance values in YIV color space.
				float3 yiqCol = rgb2yiq(col);

				float rotationAmount = _TrackingColorDamage * 2.0f * PI * abs(trackingStrength);
				float s = sin(rotationAmount);
				float c = cos(rotationAmount);
				float2x2 rotMatrix = float2x2(c, -s, s, c);
				yiqCol.yz = mul(yiqCol.yz, rotMatrix);

				col = yiq2rgb(yiqCol);
#endif

				// Apply RGB and scanline overlay texture.
				col = lerp(col, col * rgbCells, _RGBStrength);
				col = lerp(col, col * scanlines, _ScanlineStrength);

				// Apply interlacing if enabled.
#ifdef _INTERLACING_ON
				float3 inputCol = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, i.texcoord).rgb;
				col = lerp(col, inputCol, floor(UVs.y * _BlitTexture_TexelSize.w + _Interlacing) % 2.0f);
#endif

				UVs = distortedUVs;

				float2 smoothedEdges = smoothstep(0.0f, _DistortionSmoothing, UVs.xy);
				smoothedEdges *= (1.0f - smoothstep(1.0f - _DistortionSmoothing, 1.0f, UVs.xy));

				col = lerp(_BackgroundColor.rgb, col, min(smoothedEdges.x, smoothedEdges.y));

				return float4(col, 1.0f);
            }
            ENDHLSL
        }

		/*
		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float4 frag (Varyings i) : SV_Target
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				return 0.5f;
			}

			ENDHLSL
		}
		*/
    }
}
