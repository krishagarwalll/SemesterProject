using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace RetroShadersPro.URP
{

    internal class RetroOutlineShaderGUI : ShaderGUI
    {
        private MaterialProperty baseColorProp = null;
        private const string baseColorName = "_BaseColor";
        private readonly GUIContent baseColorInfo = new("Base Color",
            "Albedo color of the object.");

        private MaterialProperty thicknessProp = null;
        private const string thicknessName = "_Thickness";
        private readonly GUIContent thicknessInfo = new("Thickness",
            "The thickness of the outline in world space units.");

        private MaterialProperty snappingModeProp = null;
        private const string snappingModeName = "_SNAPMODE";
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
            thicknessProp = FindProperty(thicknessName, props, true);
            snappingModeProp = FindProperty(snappingModeName, props, true);
            snapsPerUnitProp = FindProperty(snapsPerUnitName, props, true);
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

            EditorGUILayout.LabelField("Outlines", LabelStyle);
            EditorGUILayout.Space(5);

            materialEditor.ShaderProperty(baseColorProp, baseColorInfo);
            materialEditor.ShaderProperty(thicknessProp, thicknessInfo);
            materialEditor.ShaderProperty(snappingModeProp, snappingModeInfo);
            materialEditor.ShaderProperty(snapsPerUnitProp, snapsPerUnitInfo);

            EditorGUILayout.EndVertical();
        }
    }
}
