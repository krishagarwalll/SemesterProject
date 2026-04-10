Shader "Hidden/Retro Shaders Pro/Terrain/Lit (Add Pass)"
{
    Properties
    {
        // Layer count is passed down to guide height-blend enable/disable, due
        // to the fact that heigh-based blend will be broken with multipass.
        [HideInInspector] [PerRendererData] _NumLayersCount ("Total Layer Count", Float) = 1.0

        // set by terrain engine
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 1.0

        // used in fallback on old cards & base map
        [HideInInspector] _BaseMap("BaseMap (RGB)", 2D) = "white" {}
        [HideInInspector] _BaseColor("Main Color", Color) = (1,1,1,1)

        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}

		// Retro properties.
		[HideInInspector] _ResolutionLimit("Resolution Limit (Power of 2)", Integer) = 256
		[HideInInspector] _SnapsPerUnit("Snapping Points per Meter", Integer) = 128
		[HideInInspector] _ColorBitDepth("Bit Depth", Integer) = 32
		[HideInInspector] _ColorBitDepthOffset("Bit Depth Offset", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _AmbientLight("Ambient Light Strength", Range(0.0, 1.0)) = 0.02
		[HideInInspector] _Glossiness("Glossiness", Range(1.0, 20.0)) = 5.0
		[HideInInspector] _ReflectionCubemap("Reflection Cubemap", Cube) = "black" {}
		[HideInInspector] _CubemapColor("Cubemap Color", Color) = (1, 1, 1, 1)
		[HideInInspector] _CubemapRotation("Cubemap Rotation", Range(0.0, 360.0)) = 0

        [KeywordEnum(Lit, TexelLit, VertexLit, Unlit)] _LightMode("Lighting Mode", Integer) = 1
		[KeywordEnum(Bilinear, Point, N64)] _FilterMode("Filtering Mode", Integer) = 1
		[KeywordEnum(Screen, Texture, Off)] _DitherMode("Dithering Mode", Integer) = 0
		[KeywordEnum(Object, World, View, Off)] _SnapMode("Snapping Mode", Integer) = 2
		[KeywordEnum(On, Off)] _ReceiveShadowsMode("Receive Shadows Mode", Integer) = 0

        [Toggle] _USE_AMBIENT_OVERRIDE("Ambient Light Override", Float) = 0
		[ToggleOff] _USE_VERTEX_COLORS("Use Vertex Colors", Float) = 0
		[Toggle] _USE_SPECULAR_LIGHT("Use Specular Lighting", Float) = 0
		[Toggle] _USE_REFLECTION_CUBEMAP("Use Reflection Cubemap", Float) = 0

    }

    HLSLINCLUDE

    #pragma multi_compile_fragment __ _ALPHATEST_ON

    ENDHLSL

    SubShader
    {
        Tags 
        { 
            "Queue" = "Geometry-99" 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit" 
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "TerrainAddLit"

            Tags 
            { 
                "LightMode" = "UniversalForwardOnly" 
            }

            Blend One One
            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex SplatmapVert
            #pragma fragment SplatmapFragment

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _MASKMAP
            // Sample normal in pixel shader when doing instancing
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
            #define TERRAIN_SPLAT_ADDPASS

            #pragma shader_feature_local _USE_POINT_FILTER_ON
            #pragma shader_feature_local _USE_DITHERING

			#include "RetroTerrainLitInput.hlsl"
			#include "RetroTerrainLitPasses.hlsl"
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
