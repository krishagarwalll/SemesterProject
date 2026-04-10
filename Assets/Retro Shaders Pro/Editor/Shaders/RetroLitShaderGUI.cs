using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace RetroShadersPro.URP
{
    internal class RetroLitShaderGUI : ShaderGUI
    {
        private MaterialProperty baseColorProp = null;
        private const string baseColorName = "_BaseColor";
        private readonly GUIContent baseColorInfo = new("Base Color",
            "Albedo color of the object.");

        private MaterialProperty baseTexProp = null;
        private const string baseTexName = "_BaseMap";
        private readonly GUIContent baseTexInfo = new("Base Texture",
            "Albedo texture of the object.");

        private MaterialProperty colorBitDepthProp = null;
        private const string colorBitDepthName = "_ColorBitDepth";
        private readonly GUIContent colorBitDepthInfo = new("Color Depth",
            "Limits the total number of values used for each color channel.");

        private MaterialProperty colorBitDepthOffsetProp = null;
        private const string colorBitDepthOffsetName = "_ColorBitDepthOffset";
        private readonly GUIContent colorBitDepthOffsetInfo = new("Color Depth Offset",
            "Increase this value if the bit depth offset makes your object too dark.");

        private MaterialProperty resolutionLimitProp = null;
        private const string resolutionLimitName = "_ResolutionLimit";
        private readonly GUIContent resolutionLimitInfo = new("Resolution Limit",
            "Limits the resolution of the texture to this value." +
            "\nNote that this setting only snaps the resolution to powers of two." +
            "\nAlso, make sure the Base Texture has mipmaps enabled.");

        private MaterialProperty affineTextureStrengthProp = null;
        private const string affineTextureStrengthName = "_AffineTextureStrength";
        private readonly GUIContent affineTextureStrengthInfo = new("Affine Texture Strength",
            "How strongly the affine texture mapping effect is applied." +
            "\nWhen this is set to 1, the shader uses affine texture mapping exactly like the PS1." +
            "\nWhen this is set to 0, the shader uses perspective-correct texture mapping, like modern systems.");

        private MaterialProperty filteringModeProp = null;
        private const string filteringModeName = "_FilterMode";
        private readonly GUIContent filteringModeInfo = new("Filtering Mode",
            "Which kind of filtering should the shader use while sampling the base texture?" +
            "\n  Bilinear: Blend between the nearest 4 pixels, which appears smooth." +
            "\n  Point: Use nearest neighbor sampling, which appears blocky." +
            "\n  N64: Use the limited 3-point sampling method from the Nintendo 64.");

        private MaterialProperty ditheringModeProp = null;
        private const string ditheringModeName = "_DitherMode";
        private readonly GUIContent ditheringModeInfo = new("Dithering Mode",
            "How should the shader dither colors which fall between color bit values?" +
            "\n  Screen: Use screen-space coordinates for dithering." +
            "\n    Note that this mode is driven by the pixel size in the CRT post process." +
            "\n  Texture: Use the texture coordinates for dithering." +
            "\n  Off: Don't use any dithering.");

        private MaterialProperty useVertexColorProp = null;
        private const string useVertexColorName = "_USE_VERTEX_COLORS";
        private readonly GUIContent useVertexColorInfo = new("Use Vertex Colors",
            "Should the base color of the object use vertex coloring?");

        private MaterialProperty snappingModeProp = null;
        private const string snappingModeName = "_SnapMode";
        private readonly GUIContent snappingModeInfo = new("Snapping Mode",
            "Should the shader snap vertices to a limited number of points in space?" +
            "\n  Object: Snap vertices relative to model coordinates." +
            "\n  World: Snap vertices relative to the scene coordinates." +
            "\n  View: Snap vertices relative to the camera coordinates." +
            "\n  Off: Don't do any snapping.");

        private MaterialProperty snapsPerUnitProp = null;
        private const string snapsPerUnitName = "_SnapsPerUnit";
        private readonly GUIContent snapsPerUnitInfo = new("Snaps Per Meter",
            "The mesh vertices snap to a limited number of points in space.");

        private MaterialProperty lightingModeProp = null;
        private const string lightingModeName = "_LightMode";
        private readonly GUIContent lightingModeInfo = new("Lighting Mode",
            "Choose how the object should be lit." +
            "\n  Lit: Use per-pixel lighting as standard." +
            "\n  Texel Lit: Snap lighting and shadows to the closest texel on the object's texture." +
            "\n  Vertex Lit: Use per-vertex lighting and interpolate light values for pixels." +
            "\n  Unlit: Don't use lighting calculations (everything is always fully lit).");

        private MaterialProperty useFlatShadingProp = null;
        private const string useFlatShadingName = "_USE_FLAT_SHADING";
        private readonly GUIContent useFlatShadingInfo = new("Use Flat Shading",
            "Should the shader flatten each triangle of the mesh when shading?");

        private MaterialProperty receiveShadowsModeProp = null;
        private const string receiveShadowsModeName = "_ReceiveShadowsMode";
        private readonly GUIContent receiveShadowsModeInfo = new("Receive Shadows",
            "Should the object receive shadows from other objects?");

        private MaterialProperty ambientToggleProp = null;
        private const string ambientToggleName = "_USE_AMBIENT_OVERRIDE";
        private readonly GUIContent ambientToggleInfo = new("Ambient Light Override",
            "Should the object use Unity's default ambient light, or a custom override amount?");

        private MaterialProperty ambientLightProp = null;
        private const string ambientLightName = "_AmbientLight";
        private readonly GUIContent ambientLightInfo = new("Ambient Light Strength",
            "When the ambient light override is used, apply this much ambient light.");

        private MaterialProperty useSpecularLightProp = null;
        private const string useSpecularLightName = "_USE_SPECULAR_LIGHT";
        private readonly GUIContent useSpecularLightInfo = new("Use Specular Lighting",
            "Should the shader apply a specular highlight to the object?");

        private MaterialProperty glossinessProp = null;
        private const string glossinessName = "_Glossiness";
        private readonly GUIContent glossinessInfo = new("Glossiness",
            "Gloss power value to use for specular lighting. The higher this value is, the smaller the highlight appears on the surface of the object.");

        private MaterialProperty useReflectionCubemapProp = null;
        private const string useReflectionCubemapName = "_USE_REFLECTION_CUBEMAP";
        private readonly GUIContent useReflectionCubemapInfo = new("Use Reflection Cubemap",
            "Should the shader overlay a cubemap which contains environmental reflections?");

        private MaterialProperty reflectionCubemapProp = null;
        private const string reflectionCubemapName = "_ReflectionCubemap";
        private readonly GUIContent reflectionCubemapInfo = new("Reflection Cubemap",
            "A cubemap which contains environmental reflections.");

        private MaterialProperty cubemapColorProp = null;
        private const string cubemapColorName = "_CubemapColor";
        private readonly GUIContent cubemapColorInfo = new("Cubemap Color",
            "A color tint applied to the cubemap. The alpha channel acts as a strength multiplier.");

        private MaterialProperty cubemapRotationProp = null;
        private const string cubemapRotationName = "_CubemapRotation";
        private readonly GUIContent cubemapRotationInfo = new("Cubemap Rotation",
            "How much to rotate the reflection cubemap around the Y-axis, in degrees.");

        private MaterialProperty alphaClipProp = null;
        private const string alphaClipName = "_AlphaClip";
        private readonly GUIContent alphaClipInfo = new("Alpha Clip",
            "Should the shader clip pixels based on alpha using a threshold value?");

        private MaterialProperty alphaClipThresholdProp = null;
        private const string alphaClipThresholdName = "_Cutoff";
        private readonly GUIContent alphaClipThresholdInfo = new("Threshold",
            "The threshold value to use for alpha clipping.");

        private MaterialProperty cullProp;
        private const string cullName = "_Cull";
        private readonly GUIContent cullInfo = new("Render Face",
            "Should Unity render Front, Back, or Both faces of the mesh?");

        private const string surfaceTypeName = "_Surface";
        private readonly GUIContent surfaceTypeInfo = new("Surface Type",
            "Should the object be transparent or opaque?");

        private const string alphaTestName = "_ALPHATEST_ON";

        private static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
        private static readonly string[] renderFaceNames = Enum.GetNames(typeof(RenderFace));

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

        private enum SurfaceType
        {
            Opaque = 0,
            Transparent = 1
        }

        private enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        private SurfaceType surfaceType = SurfaceType.Opaque;
        private RenderFace renderFace = RenderFace.Front;

        protected readonly MaterialHeaderScopeList materialScopeList = new MaterialHeaderScopeList(uint.MaxValue);
        protected MaterialEditor materialEditor;
        private bool firstTimeOpen = true;

        private void FindProperties(MaterialProperty[] props)
        {
            baseColorProp = FindProperty(baseColorName, props, true);
            baseTexProp = FindProperty(baseTexName, props, true);
            resolutionLimitProp = FindProperty(resolutionLimitName, props, true);
            snappingModeProp = FindProperty(snappingModeName, props, true);
            snapsPerUnitProp = FindProperty(snapsPerUnitName, props, true);
            colorBitDepthProp = FindProperty(colorBitDepthName, props, true);
            colorBitDepthOffsetProp = FindProperty(colorBitDepthOffsetName, props, true);
            ambientLightProp = FindProperty(ambientLightName, props, true);
            affineTextureStrengthProp = FindProperty(affineTextureStrengthName, props, true);
            ambientToggleProp = FindProperty(ambientToggleName, props, true);
            filteringModeProp = FindProperty(filteringModeName, props, true);
            ditheringModeProp = FindProperty(ditheringModeName, props, true);
            lightingModeProp = FindProperty(lightingModeName, props, true);
            useFlatShadingProp = FindProperty(useFlatShadingName, props, true);
            receiveShadowsModeProp = FindProperty(receiveShadowsModeName, props, true);
            useVertexColorProp = FindProperty(useVertexColorName, props, true);
            useSpecularLightProp = FindProperty(useSpecularLightName, props, true);
            glossinessProp = FindProperty(glossinessName, props, true);
            useReflectionCubemapProp = FindProperty(useReflectionCubemapName, props, true);
            reflectionCubemapProp = FindProperty(reflectionCubemapName, props, true);
            cubemapColorProp = FindProperty(cubemapColorName, props, true);
            cubemapRotationProp = FindProperty(cubemapRotationName, props, true);

            //surfaceTypeProp = FindProperty(kSurfaceTypeProp, props, false);
            cullProp = FindProperty(cullName, props, true);
            alphaClipProp = FindProperty(alphaClipName, props, true);
            alphaClipThresholdProp = FindProperty(alphaClipThresholdName, props, true);
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
                materialScopeList.RegisterHeaderScope(new GUIContent("Surface Options"), 1u << 0, DrawSurfaceOptions);
                materialScopeList.RegisterHeaderScope(new GUIContent("Retro Properties"), 1u << 1, DrawRetroProperties);
                firstTimeOpen = false;
            }

            materialScopeList.DrawHeaders(materialEditor, material);
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        private void DrawSurfaceOptions(Material material)
        {
            surfaceType = (SurfaceType)material.GetFloat(surfaceTypeName);
            renderFace = (RenderFace)material.GetFloat(cullName);

            // Display opaque/transparent options.
            bool surfaceTypeChanged = false;
            EditorGUI.BeginChangeCheck();
            {
                surfaceType = (SurfaceType)EditorGUILayout.EnumPopup(surfaceTypeInfo, surfaceType);
            }
            if (EditorGUI.EndChangeCheck())
            {
                surfaceTypeChanged = true;
            }

            // Display culling options.
            EditorGUI.BeginChangeCheck();
            {
                renderFace = (RenderFace)EditorGUILayout.EnumPopup(cullInfo, renderFace);
            }
            if (EditorGUI.EndChangeCheck())
            {
                switch (renderFace)
                {
                    case RenderFace.Both:
                        {
                            material.SetFloat(cullName, 0);
                            break;
                        }
                    case RenderFace.Back:
                        {
                            material.SetFloat(cullName, 1);
                            break;
                        }
                    case RenderFace.Front:
                        {
                            material.SetFloat(cullName, 2);
                            break;
                        }
                }
            }

            // Display alpha clip options.
            EditorGUI.BeginChangeCheck();
            {
                materialEditor.ShaderProperty(alphaClipProp, alphaClipInfo);
            }
            if (EditorGUI.EndChangeCheck())
            {
                surfaceTypeChanged = true;
            }

            bool alphaClip;

            if (surfaceTypeChanged)
            {
                switch (surfaceType)
                {
                    case SurfaceType.Opaque:
                        {
                            material.SetOverrideTag("RenderType", "Opaque");
                            material.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                            material.SetFloat("_ZWrite", 1);
                            material.SetFloat(surfaceTypeName, 0);

                            alphaClip = material.GetFloat(alphaClipName) >= 0.5f;
                            if (alphaClip)
                            {
                                material.EnableKeyword(alphaTestName);
                                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                                material.SetOverrideTag("RenderType", "TransparentCutout");
                            }
                            else
                            {
                                material.DisableKeyword(alphaTestName);
                                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                                material.SetOverrideTag("RenderType", "Opaque");
                            }


                            break;
                        }
                    case SurfaceType.Transparent:
                        {
                            alphaClip = material.GetFloat(alphaClipName) >= 0.5f;
                            if (alphaClip)
                            {
                                material.EnableKeyword(alphaTestName);
                            }
                            else
                            {
                                material.DisableKeyword(alphaTestName);
                            }
                            material.SetOverrideTag("RenderType", "Transparent");
                            material.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            material.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            material.SetFloat("_ZWrite", 0);
                            material.SetFloat(surfaceTypeName, 1);

                            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                            break;
                        }
                }
            }

            alphaClip = material.GetFloat(alphaClipName) >= 0.5f;
            if (alphaClip)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(alphaClipThresholdProp, alphaClipThresholdInfo);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRetroProperties(Material material)
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);

            EditorGUILayout.LabelField("Color & Texture Effects", LabelStyle);
            EditorGUILayout.Space(5);

            materialEditor.ShaderProperty(baseColorProp, baseColorInfo);
            materialEditor.ShaderProperty(baseTexProp, baseTexInfo);
            EditorGUILayout.Space(5);
            materialEditor.ShaderProperty(colorBitDepthProp, colorBitDepthInfo);
            materialEditor.ShaderProperty(colorBitDepthOffsetProp, colorBitDepthOffsetInfo);
            
            if(baseTexProp.textureValue != null)
            {
                EditorGUILayout.Space(5);
                materialEditor.ShaderProperty(resolutionLimitProp, resolutionLimitInfo);

                if (resolutionLimitProp.intValue < baseTexProp.textureValue.width &&
                resolutionLimitProp.intValue < baseTexProp.textureValue.height &&
                baseTexProp.textureValue.mipmapCount < 2)
                {
                    EditorGUILayout.HelpBox("Please enable mipmaps on the Base Texture to limit its resolution to a lower value.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(5);
            materialEditor.ShaderProperty(affineTextureStrengthProp, affineTextureStrengthInfo);
            EditorGUILayout.Space(5);
            materialEditor.ShaderProperty(filteringModeProp, filteringModeInfo);
            materialEditor.ShaderProperty(ditheringModeProp, ditheringModeInfo);
            EditorGUILayout.Space(5);
            materialEditor.ShaderProperty(useVertexColorProp, useVertexColorInfo);

            bool vertexColors = material.GetFloat(useVertexColorName) >= 0.5f;

            if (vertexColors)
            {
                material.EnableKeyword(useVertexColorName);
            }
            else
            {
                material.DisableKeyword(useVertexColorName);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);

            EditorGUILayout.LabelField("Vertex Snapping", LabelStyle);
            EditorGUILayout.Space(5);

            materialEditor.ShaderProperty(snappingModeProp, snappingModeInfo);

            if (material.GetInteger(snappingModeName) != 3) // Off.
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(snapsPerUnitProp, snapsPerUnitInfo);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);

            EditorGUILayout.LabelField("Lighting & Shadows", LabelStyle);
            EditorGUILayout.Space(5);

            materialEditor.ShaderProperty(lightingModeProp, lightingModeInfo);

            int lightMode = material.GetInteger(lightingModeName);

            if (lightMode != 3) // Unlit.
            {
                materialEditor.ShaderProperty(useFlatShadingProp, useFlatShadingInfo);

                bool flatShading = material.GetFloat(useFlatShadingName) >= 0.5f;

                if (flatShading)
                {
                    material.EnableKeyword(useFlatShadingName);
                }
                else
                {
                    material.DisableKeyword(useFlatShadingName);
                }

                materialEditor.ShaderProperty(receiveShadowsModeProp, receiveShadowsModeInfo);
                materialEditor.ShaderProperty(ambientToggleProp, ambientToggleInfo);

                bool ambient = material.GetFloat(ambientToggleName) >= 0.5f;

                if (ambient)
                {
                    material.EnableKeyword(ambientToggleName);

                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(ambientLightProp, ambientLightInfo);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    material.DisableKeyword(ambientToggleName);
                }

                materialEditor.ShaderProperty(useSpecularLightProp, useSpecularLightInfo);

                bool useSpecularLighting = material.GetFloat(useSpecularLightName) >= 0.5f;

                if (useSpecularLighting)
                {
                    material.EnableKeyword(useSpecularLightName);

                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(glossinessProp, glossinessInfo);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    material.DisableKeyword(useSpecularLightName);
                }

                materialEditor.ShaderProperty(useReflectionCubemapProp, useReflectionCubemapInfo);

                if(material.GetFloat(useReflectionCubemapName) >= 0.5f)
                {
                    material.EnableKeyword(useReflectionCubemapName);

                    EditorGUI.indentLevel++;
                    //materialEditor.ShaderProperty(reflectionCubemapProp, reflectionCubemapInfo);
                    materialEditor.TexturePropertyWithHDRColor(reflectionCubemapInfo, reflectionCubemapProp, cubemapColorProp, true);
                    materialEditor.ShaderProperty(cubemapRotationProp, cubemapRotationInfo);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    material.DisableKeyword(useReflectionCubemapName);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
