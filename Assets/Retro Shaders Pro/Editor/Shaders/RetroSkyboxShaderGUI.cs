using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace RetroShadersPro.URP
{

    internal class RetroSkyboxShaderGUI : ShaderGUI
    {
        private MaterialProperty baseColorProp = null;
        private const string baseColorName = "_BaseColor";
        private readonly GUIContent baseColorInfo = new("Base Color",
            "Albedo color of the object.");

        private MaterialProperty baseCubemapProp = null;
        private const string baseCubemapName = "_BaseCubemap";
        private readonly GUIContent baseCubemapInfo = new("Base Cubemap",
            "Cubemap texture for the albedo color of the sky.");

        private MaterialProperty rotationProp = null;
        private const string rotationName = "_Rotation";
        private readonly GUIContent rotationInfo = new("Rotation",
            "How much to rotate the background, in degrees.");

        private MaterialProperty resolutionLimitProp = null;
        private const string resolutionLimitName = "_ResolutionLimit";
        private readonly GUIContent resolutionLimitInfo = new("Resolution Limit",
            "Limits the resolution of the texture to this value." +
            "\nNote that this setting only snaps the resolution to powers of two." +
            "\nAlso, make sure the Base Texture has mipmaps enabled.");

        private MaterialProperty colorBitDepthProp = null;
        private const string colorBitDepthName = "_ColorBitDepth";
        private readonly GUIContent colorBitDepthInfo = new("Color Depth",
            "Limits the total number of values used for each color channel.");

        private MaterialProperty colorBitDepthOffsetProp = null;
        private const string colorBitDepthOffsetName = "_ColorBitDepthOffset";
        private readonly GUIContent colorBitDepthOffsetInfo = new("Color Depth Offset",
            "Increase this value if the bit depth offset makes your object too dark.");

        private MaterialProperty groundColorProp = null;
        private const string groundColorName = "_GroundColor";
        private readonly GUIContent groundColorInfo = new("Ground Color",
            "The color of the sky background on the ground.");

        private MaterialProperty skyColorProp = null;
        private const string skyColorName = "_SkyColor";
        private readonly GUIContent skyColorInfo = new("Sky Color",
            "The color of the sky background at the top of the sky.");

        private MaterialProperty colorMixPowerProp = null;
        private const string colorMixPowerName = "_ColorMixPower";
        private readonly GUIContent colorMixPowerInfo = new("Color Mix Power",
            "Controls the blend between the Ground Color and the Sky Color.");

        private MaterialProperty cloudHeightThresholdsProp = null;
        private const string cloudHeightThresholdsName = "_CloudHeightThresholds";
        private readonly GUIContent cloudHeightThresholdsInfo = new("Cloud Height Thresholds",
            "...");

        private MaterialProperty cloudDensityThresholdsProp = null;
        private const string cloudDensityThresholdsName = "_CloudDensityThresholds";
        private readonly GUIContent cloudDensityThresholdsInfo = new("Cloud Density Thresholds",
            "...");

        private MaterialProperty cloudColorProp = null;
        private const string cloudColorName = "_CloudColor";
        private readonly GUIContent cloudColorInfo = new("Cloud Color",
            "Color of the randomly generated cloud pattern.");

        private MaterialProperty cloudSizesProp = null;
        private const string cloudSizesName = "_CloudSizes";
        private readonly GUIContent cloudSizesInfo = new("Cloud Sizes",
            "...");

        private MaterialProperty cloudVelocityProp = null;
        private const string cloudVelocityName = "_CloudVelocity";
        private readonly GUIContent cloudVelocityInfo = new("Cloud Velocity",
            "Speed and direction that the clouds travel in the X-Z plane.");

        private MaterialProperty usePointFilterProp = null;
        private const string usePointFilterName = "_USE_POINT_FILTER";
        private readonly GUIContent usePointFilterInfo = new("Use Point Filter",
            "Should the shader force point (nearest-neighbor) filtering for the cubemap?");

        private MaterialProperty useCloudsProp = null;
        private const string useCloudsName = "_USE_CLOUDS";
        private readonly GUIContent useCloudsInfo = new("Use Clouds",
            "Should the shader procedurally generate a cloud pattern to scroll over the sky?");

        private MaterialProperty ditheringModeProp = null;
        private const string ditheringModeName = "_DitherMode";
        private readonly GUIContent ditheringModeInfo = new("Dithering Mode",
            "How should the shader dither colors which fall between color bit values?" +
            "\n  Screen: Use screen-space coordinates for dithering." +
            "\n    Note that this mode is driven by the pixel size in the CRT post process." +
            "\n  Texture: Use the texture coordinates for dithering." +
            "\n  Off: Don't use any dithering.");

        private MaterialProperty combineModeProp = null;
        private const string combineModeName = "_CombineMode";
        private readonly GUIContent combineModeInfo = new("Combine Mode",
            "How should the shader combine the two generated cloud maps?");

        private MaterialProperty backgroundModeProp = null;
        private const string backgroundModeName = "_BackgroundMode";
        private readonly GUIContent backgroundModeInfo = new("Background Mode",
            "Should the shader use a two-color gradient or sample a cubemap for the base sky background?");

        private static GUIStyle _boxStyle;
        private static GUIStyle BoxStyle
        {
            get
            {
                return _boxStyle ?? (_boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 5, 10)
                });
            }
        }

        private static GUIStyle _labelStyle;
        private static GUIStyle LabelStyle
        {
            get
            {
                return _labelStyle ?? (_labelStyle = new GUIStyle(EditorStyles.boldLabel));
            }
        }

        protected readonly MaterialHeaderScopeList materialScopeList = new MaterialHeaderScopeList(uint.MaxValue);
        protected MaterialEditor materialEditor;
        private bool firstTimeOpen = true;

        private void FindProperties(MaterialProperty[] props)
        {
            baseColorProp = FindProperty(baseColorName, props, true);
            baseCubemapProp = FindProperty(baseCubemapName, props, true);
            rotationProp = FindProperty(rotationName, props, true);
            resolutionLimitProp = FindProperty(resolutionLimitName, props, true);
            colorBitDepthProp = FindProperty(colorBitDepthName, props, true);
            colorBitDepthOffsetProp = FindProperty(colorBitDepthOffsetName, props, true);
            groundColorProp = FindProperty(groundColorName, props, true);
            skyColorProp = FindProperty(skyColorName, props, true);
            colorMixPowerProp = FindProperty(colorMixPowerName, props, true);
            cloudHeightThresholdsProp = FindProperty(cloudHeightThresholdsName, props, true);
            cloudDensityThresholdsProp = FindProperty(cloudDensityThresholdsName, props, true);
            cloudColorProp = FindProperty(cloudColorName, props, true);
            cloudSizesProp = FindProperty(cloudSizesName, props, true);
            cloudVelocityProp = FindProperty(cloudVelocityName, props, true);
            usePointFilterProp = FindProperty(usePointFilterName, props, true);
            useCloudsProp = FindProperty(useCloudsName, props, true);
            ditheringModeProp = FindProperty(ditheringModeName, props, true);
            combineModeProp = FindProperty(combineModeName, props, true);
            backgroundModeProp = FindProperty(backgroundModeName, props, true);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (materialEditor == null)
            {
                throw new ArgumentNullException("No MaterialEditor found (RetroLitShaderGUI).");
            }

            Material material = materialEditor.target as Material;
            this.materialEditor = materialEditor;

            FindProperties(properties);

            if (firstTimeOpen)
            {
                materialScopeList.RegisterHeaderScope(new GUIContent("Retro Properties"), 1u << 0, DrawRetroProperties);
                firstTimeOpen = false;
            }

            materialScopeList.DrawHeaders(materialEditor, material);
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        private void DrawRetroProperties(Material material)
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);

            EditorGUILayout.LabelField("Resolution & Orientation", LabelStyle);
            EditorGUILayout.Space(5);

            materialEditor.ShaderProperty(resolutionLimitProp, resolutionLimitInfo);
            materialEditor.ShaderProperty(rotationProp, rotationInfo);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);

            EditorGUILayout.LabelField("Sky Background Color", LabelStyle);
            EditorGUILayout.Space(5);

            materialEditor.ShaderProperty(backgroundModeProp, backgroundModeInfo);

            if(material.GetInteger(backgroundModeName) == 1) // Sample cubemap.
            {
                materialEditor.ShaderProperty(baseColorProp, baseColorInfo);
                materialEditor.ShaderProperty(baseCubemapProp, baseCubemapInfo);
                EditorGUILayout.Space(5);
                materialEditor.ShaderProperty(colorBitDepthProp, colorBitDepthInfo);
                materialEditor.ShaderProperty(colorBitDepthOffsetProp, colorBitDepthOffsetInfo);
                EditorGUILayout.Space(5);
                materialEditor.ShaderProperty(usePointFilterProp, usePointFilterInfo);

                if(material.GetFloat(usePointFilterName) >= 0.5f)
                {
                    material.EnableKeyword(usePointFilterName);
                }
                else
                {
                    material.DisableKeyword(usePointFilterName);
                }

                materialEditor.ShaderProperty(ditheringModeProp, ditheringModeInfo);
            }
            else // Use color gradient.
            {
                materialEditor.ShaderProperty(groundColorProp, groundColorInfo);
                materialEditor.ShaderProperty(skyColorProp, skyColorInfo);
                materialEditor.ShaderProperty(colorMixPowerProp, colorMixPowerInfo);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);

            EditorGUILayout.LabelField("Procedural Clouds", LabelStyle);
            EditorGUILayout.Space(5);

            materialEditor.ShaderProperty(useCloudsProp, useCloudsInfo);

            if(material.GetFloat(useCloudsName) >= 0.5f)
            {
                material.EnableKeyword(useCloudsName);

                materialEditor.ShaderProperty(cloudHeightThresholdsProp, cloudHeightThresholdsInfo);
                materialEditor.ShaderProperty(cloudDensityThresholdsProp, cloudDensityThresholdsInfo);
                materialEditor.ShaderProperty(cloudColorProp, cloudColorInfo);
                materialEditor.ShaderProperty(cloudSizesProp, cloudSizesInfo);
                materialEditor.ShaderProperty(cloudVelocityProp, cloudVelocityInfo);
                materialEditor.ShaderProperty(combineModeProp, combineModeInfo);
            }
            else
            {
                material.DisableKeyword(useCloudsName);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
