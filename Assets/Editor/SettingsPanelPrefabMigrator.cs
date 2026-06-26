using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsPanelPrefabMigrator
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/UI/SettingsPanel.prefab",
        "Assets/Prefabs/UI/Pause Menu Canvas.prefab"
    };

    [InitializeOnLoadMethod]
    public static void EnsureVSyncRows()
    {
        foreach (string prefabPath in PrefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            bool changed = false;
            try
            {
                SettingsPanel settingsPanel = root.GetComponentInChildren<SettingsPanel>(true);
                if (!settingsPanel)
                {
                    continue;
                }

                SerializedObject serializedPanel = new(settingsPanel);
                SerializedProperty vSyncToggleProperty = serializedPanel.FindProperty("vSyncToggle");
                Toggle existingToggle = vSyncToggleProperty.objectReferenceValue as Toggle;
                if (existingToggle)
                {
                    continue;
                }

                SerializedProperty windowModeDropdownProperty = serializedPanel.FindProperty("windowModeDropdown");
                TMP_Dropdown windowModeDropdown = windowModeDropdownProperty.objectReferenceValue as TMP_Dropdown;
                Transform windowModeRow = windowModeDropdown ? windowModeDropdown.transform.parent : null;
                Transform displayContent = windowModeRow ? windowModeRow.parent : settingsPanel.transform;

                Toggle vSyncToggle = FindExistingVSyncToggle(settingsPanel);
                if (!vSyncToggle)
                {
                    vSyncToggle = CreateVSyncRow(displayContent, windowModeRow, FindFont(windowModeRow));
                    changed = true;
                }
                else
                {
                    changed |= NormalizeVSyncRow(vSyncToggle);
                }

                vSyncToggleProperty.objectReferenceValue = vSyncToggle;
                serializedPanel.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settingsPanel);
                changed = true;
            }
            finally
            {
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static Toggle FindExistingVSyncToggle(SettingsPanel settingsPanel)
    {
        Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i].name.ToLowerInvariant().Contains("vsync"))
            {
                return toggles[i];
            }
        }

        return null;
    }

    private static bool NormalizeVSyncRow(Toggle vSyncToggle)
    {
        bool changed = false;

        RectTransform toggleRect = vSyncToggle.transform as RectTransform;
        if (toggleRect && toggleRect.sizeDelta != new Vector2(36f, 36f))
        {
            toggleRect.sizeDelta = new Vector2(36f, 36f);
            changed = true;
        }

        LayoutElement toggleLayout = vSyncToggle.GetComponent<LayoutElement>();
        if (toggleLayout)
        {
            if (!Mathf.Approximately(toggleLayout.preferredWidth, 36f))
            {
                toggleLayout.preferredWidth = 36f;
                changed = true;
            }

            if (!Mathf.Approximately(toggleLayout.preferredHeight, 36f))
            {
                toggleLayout.preferredHeight = 36f;
                changed = true;
            }
        }

        RectTransform rowRect = vSyncToggle.transform.parent as RectTransform;
        if (rowRect && rowRect.name == "VSyncRow")
        {
            if (rowRect.anchorMin != Vector2.zero)
            {
                rowRect.anchorMin = Vector2.zero;
                changed = true;
            }

            if (rowRect.anchorMax != Vector2.zero)
            {
                rowRect.anchorMax = Vector2.zero;
                changed = true;
            }

            if (rowRect.sizeDelta != new Vector2(0f, 42f))
            {
                rowRect.sizeDelta = new Vector2(0f, 42f);
                changed = true;
            }

            LayoutElement rowLayout = rowRect.GetComponent<LayoutElement>();
            if (rowLayout)
            {
                if (!Mathf.Approximately(rowLayout.preferredWidth, 620f))
                {
                    rowLayout.preferredWidth = 620f;
                    changed = true;
                }

                if (!Mathf.Approximately(rowLayout.preferredHeight, 42f))
                {
                    rowLayout.preferredHeight = 42f;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static Toggle CreateVSyncRow(Transform displayContent, Transform windowModeRow, TMP_FontAsset font)
    {
        GameObject row = new("VSyncRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(displayContent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = Vector2.zero;
        rowRect.anchorMax = Vector2.zero;
        rowRect.sizeDelta = new Vector2(0f, 42f);
        if (windowModeRow)
        {
            row.transform.SetSiblingIndex(windowModeRow.GetSiblingIndex() + 1);
        }

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredWidth = 620f;
        rowLayout.preferredHeight = 42f;

        HorizontalLayoutGroup rowGroup = row.GetComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 8f;
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = false;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = false;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(row.transform, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "VSync";
        label.fontSize = 23f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        if (font) label.font = font;

        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.minWidth = 210f;
        labelLayout.preferredWidth = 210f;
        labelLayout.flexibleWidth = 0f;

        GameObject toggleObject = new("VSyncToggle", typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(LayoutElement));
        toggleObject.transform.SetParent(row.transform, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(36f, 36f);

        LayoutElement toggleLayout = toggleObject.GetComponent<LayoutElement>();
        toggleLayout.preferredWidth = 36f;
        toggleLayout.preferredHeight = 36f;
        toggleLayout.flexibleWidth = 0f;

        Image background = toggleObject.GetComponent<Image>();
        background.color = new Color(0.18f, 0.22f, 0.25f, 1f);

        GameObject checkmarkObject = new("Checkmark", typeof(RectTransform), typeof(Image));
        checkmarkObject.transform.SetParent(toggleObject.transform, false);
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.22f, 0.22f);
        checkmarkRect.anchorMax = new Vector2(0.78f, 0.78f);
        checkmarkRect.offsetMin = Vector2.zero;
        checkmarkRect.offsetMax = Vector2.zero;

        Image checkmark = checkmarkObject.GetComponent<Image>();
        checkmark.color = new Color(0.25f, 0.55f, 0.92f, 1f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = true;
        return toggle;
    }

    private static TMP_FontAsset FindFont(Transform windowModeRow)
    {
        if (!windowModeRow)
        {
            return null;
        }

        TextMeshProUGUI label = windowModeRow.GetComponentInChildren<TextMeshProUGUI>(true);
        return label ? label.font : null;
    }
}
