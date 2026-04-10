Shader "Retro Shaders Pro/CRT (Mesh)"
{
    Properties
    {
        [MainColor] [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		[MainTexture] _BaseMap("Base Texture", 2D) = "white" {}

        _BackgroundColor("Background Color", Color) = (0, 0, 0, 0)
		_DistortionStrength("Distortion Strength", Range(0.0, 1.0)) = 0
		_DistortionSmoothing("Distortion Smoothing", Range(0.0, 0.1)) = 0.01
		_PixelSize("Pixel Size", Integer) = 1
        _RGBPixelSize("RGB Pixel Size", Integer) = 4
        _RGBTex("RGB Subpixel Texture", 2D) = "white" {}
		_RGBStrength("RGB Strength", Range(0.0, 1.0)) = 0.0
        _ScanlineTex("Scanline Texture", 2D) = "white" {}
		_ScanlineStrength("Scanline Strength", Range(0.0, 1.0)) = 0.0
		_RandomWear("Random Wear", Range(0.0, 5.0)) = 0.2
		_ScrollSpeed("Scanline Scroll Speed", Range(0.0, 10.0)) = 0.0
		_AberrationStrength("Aberration Strength", Range(0.0, 10.0)) = 0.5
		_TrackingTex("Tracking Texture", 2D) = "gray" {}
        _TrackingSize("Tracking Size", Range(0.1, 2.0)) = 1.0
		_TrackingStrength("Tracking Strength", Range(0.0, 50.0)) = 0.1
		_TrackingSpeed("Tracking Speed", Range(-2.5, 2.5)) = 0.1
		_TrackingJitter("Tracking Jitter", Range(0.0, 0.1)) = 0.01
		_TrackingColorDamage("Tracking Color Damage", Range(0.0, 1.0)) = 0.05
		_TrackingLinesThreshold("Tracking Lines Threshold", Range(0.0, 1.0)) = 0.9
		_TrackingLinesColor("Tracking Lines Color", Color) = (1, 1, 1, 0.5)
		_Brightness("Brightness", Range(0.0, 3.0)) = 1.0
		_Contrast("Contrast", Range(0.0, 3.0)) = 1.0
		[Toggle] _POINT_FILTERING("Force Point Filtering", Float) = 0.0
		[HideInInspector] [Enum(RetroShadersPro.URP.ColorRampMode)] _ColorRampMode("Color Ramp Mode", Integer) = 0
		_ColorRampTex("Color Ramp Texture", 2D) = "white" {}

        [ToggleUI] _AlphaClip("Alpha Clip", Float) = 0.0
		[HideInInspector] _Cutoff("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
		[HideInInspector] _Cull("_Cull", Float) = 2.0
		[HideInInspector] _Surface("_Surface", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
        #include "CRTSurfaceInput.hlsl"
		
		ENDHLSL

        Pass
        {
            Name "Unlit"

            Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]
			Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
			#pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			#pragma multi_compile_local_fragment _ _CHROMATIC_ABERRATION_ON
			#pragma multi_compile_local_fragment _ _TRACKING_ON
			#pragma multi_compile_local_fragment _ _POINT_FILTERING_ON
			#pragma multi_compile_local_fragment _COLOR_RAMP_NONE _COLOR_RAMP_RGB _COLOR_RAMP_LUMINANCE _COLOR_RAMP_INTENSITY
            #pragma shader_feature_local_fragment _ALPHATEST_ON

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

            struct appdata
            {
				float4 positionOS : POSITION;
				float4 color : COLOR;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float fog : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

			v2f vert(appdata v)
			{
				v2f o = (v2f)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.fog = ComputeFogFactor(o.positionCS.z);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				return o;
			}

			float4 frag(v2f i, float facing : VFACE) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				int downsamples = (int)log2(_PixelSize);
				float4 texelSize = float4(_BaseMap_TexelSize.xy * downsamples, _BaseMap_TexelSize.zw / downsamples);

                // Apply barrel distortion to UVs.
				float2 originalUVs = i.uv - 0.5f;
				float2 UVs = originalUVs * (1 + _DistortionStrength * length(originalUVs) * length(originalUVs)) + 0.5f;

                // Save UVs to use for barrel distortion later.
				float2 distortedUVs = UVs;

                // Set up UVs to use for screen-space effects.
				float2 screenUVs = UVs * texelSize.zw * _RGBPixelSize * 0.5f;

                // Get RGB overlay texture.
				float3 rgbCells = SAMPLE_TEXTURE2D(_RGBTex, sampler_PointRepeat, screenUVs).rgb;

                // Get scanline overlay texture.
				screenUVs.y += _Time.y * _ScrollSpeed;
				float3 scanlines = SAMPLE_TEXTURE2D(_ScanlineTex, sampler_LinearRepeat, screenUVs).rgb;

#ifdef _TRACKING_ON
				float2 trackingUVs = float2(UVs.y * _TrackingSize + _Time.y * _TrackingSpeed + randomValue(_Time.xx) * _TrackingJitter, 0.5f);

				// Get tracking amount.
				float3 trackingSample = (SAMPLE_TEXTURE2D(_TrackingTex, sampler_LinearRepeat, trackingUVs).rgb - 0.5f) * 2.0f;
				float trackingStrength = trackingSample.r;

				// Offset UVs horizontally based on tracking amount.
				float trackingOffset = trackingStrength * texelSize.x * _TrackingStrength;
				UVs.x += trackingOffset;
#endif

                // Offset UVs horizontally based on random tape wear.
				float randomOffset = randomValue(float2(i.uv.y, _Time.x));
				UVs.x += randomOffset * texelSize.x * _RandomWear;

                // Sample the blit texture, applying chromatic aberration if enabled.
#ifdef _CHROMATIC_ABERRATION_ON
				float2 redUVs = UVs + originalUVs * _AberrationStrength * texelSize.xy;
				float2 blueUVs = UVs - originalUVs * _AberrationStrength * texelSize.xy;
	#ifdef _POINT_FILTERING_ON
				float red = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointClamp, redUVs, downsamples).r;
				float2 green = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointClamp, UVs, downsamples).ga;
				float blue = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointClamp, blueUVs, downsamples).b;
	#else
				float red = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearClamp, redUVs, downsamples).r;
				float2 green = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearClamp, UVs, downsamples).ga;
				float blue = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearClamp, blueUVs, downsamples).b;
	#endif
				float4 baseColor = float4(red, green.x, blue, green.y);
#else
	#ifdef _POINT_FILTERING_ON
				float4 baseColor = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointClamp, UVs, downsamples);
	#else
				float4 baseColor = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_LinearClamp, UVs, downsamples);
	#endif
#endif

				baseColor *= _BaseColor;

				float3 col = baseColor.rgb;

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

				UVs = distortedUVs;

				float2 smoothedEdges = smoothstep(0.0f, _DistortionSmoothing, UVs.xy);
				smoothedEdges *= (1.0f - smoothstep(1.0f - _DistortionSmoothing, 1.0f, UVs.xy));

				col = lerp(_BackgroundColor.rgb, col, min(smoothedEdges.x, smoothedEdges.y));

#ifdef _ALPHATEST_ON
				clip(baseColor.a - _Cutoff);
#endif

#ifdef _DBUFFER
                float3 specular = 0;
                float metallic = 0;
                float occlusion = 0;
                float smoothness = 0;
                float3 norm = normalize(i.normalWS);
                ApplyDecal(i.positionCS, col, specular, norm, metallic, occlusion, smoothness);
#endif

				return float4(col.rgb, baseColor.a);
			}

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex depthOnlyVert
            #pragma fragment depthOnlyFrag

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "CRTDepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"

            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex depthNormalsVert
            #pragma fragment depthNormalsFrag

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "CRTDepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"

            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
			#pragma vertex metaVert
			#pragma fragment metaFrag

			#pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "CRTMetaPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "RetroShadersPro.URP.CRTMeshShaderGUI"
}
