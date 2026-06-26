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
                if (!existingToggle)
                {
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
                else
                {
                    changed |= NormalizeVSyncRow(existingToggle);
                }

                changed |= EnsureBackButtonBottom(settingsPanel);
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
        if (toggleRect && toggleRect.sizeDelta != new Vector2(28f, 28f))
        {
            toggleRect.sizeDelta = new Vector2(28f, 28f);
            changed = true;
        }

        LayoutElement toggleLayout = vSyncToggle.GetComponent<LayoutElement>();
        if (toggleLayout)
        {
            if (!Mathf.Approximately(toggleLayout.preferredWidth, 28f))
            {
                toggleLayout.preferredWidth = 28f;
                changed = true;
            }

            if (!Mathf.Approximately(toggleLayout.preferredHeight, 28f))
            {
                toggleLayout.preferredHeight = 28f;
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

            if (rowRect.sizeDelta != new Vector2(0f, 30f))
            {
                rowRect.sizeDelta = new Vector2(0f, 30f);
                changed = true;
            }

            LayoutElement rowLayout = rowRect.GetComponent<LayoutElement>();
            if (rowLayout)
            {
                if (!Mathf.Approximately(rowLayout.preferredWidth, 540f))
                {
                    rowLayout.preferredWidth = 540f;
                    changed = true;
                }

                if (!Mathf.Approximately(rowLayout.preferredHeight, 30f))
                {
                    rowLayout.preferredHeight = 30f;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static bool EnsureBackButtonBottom(SettingsPanel settingsPanel)
    {
        bool changed = false;

        VerticalLayoutGroup rootLayout = settingsPanel.GetComponent<VerticalLayoutGroup>();
        if (rootLayout)
        {
            if (!rootLayout.childControlHeight)
            {
                rootLayout.childControlHeight = true;
                changed = true;
            }

            if (rootLayout.childForceExpandHeight)
            {
                rootLayout.childForceExpandHeight = false;
                changed = true;
            }
        }

        Transform content = settingsPanel.transform.Find("Content");
        if (content)
        {
            LayoutElement contentLayout = content.GetComponent<LayoutElement>();
            if (!contentLayout)
            {
                contentLayout = content.gameObject.AddComponent<LayoutElement>();
                changed = true;
            }

            if (!Mathf.Approximately(contentLayout.flexibleHeight, 1f))
            {
                contentLayout.flexibleHeight = 1f;
                changed = true;
            }
        }

        SerializedObject serializedPanel = new(settingsPanel);
        Button backButton = serializedPanel.FindProperty("backButton").objectReferenceValue as Button;
        if (backButton && backButton.transform.parent == settingsPanel.transform)
        {
            Transform spacer = settingsPanel.transform.Find("SettingsBottomSpacer");
            if (!spacer)
            {
                GameObject spacerObject = new("SettingsBottomSpacer", typeof(RectTransform), typeof(LayoutElement));
                spacerObject.transform.SetParent(settingsPanel.transform, false);
                spacer = spacerObject.transform;
                changed = true;
            }

            LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
            if (!spacerLayout)
            {
                spacerLayout = spacer.gameObject.AddComponent<LayoutElement>();
                changed = true;
            }

            if (!Mathf.Approximately(spacerLayout.minHeight, 0f))
            {
                spacerLayout.minHeight = 0f;
                changed = true;
            }

            if (!Mathf.Approximately(spacerLayout.preferredHeight, 0f))
            {
                spacerLayout.preferredHeight = 0f;
                changed = true;
            }

            if (!Mathf.Approximately(spacerLayout.flexibleHeight, 1f))
            {
                spacerLayout.flexibleHeight = 1f;
                changed = true;
            }

            int backIndex = backButton.transform.GetSiblingIndex();
            if (spacer.GetSiblingIndex() != backIndex - 1)
            {
                spacer.SetSiblingIndex(Mathf.Max(0, backIndex));
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(settingsPanel);
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
        rowRect.sizeDelta = new Vector2(0f, 30f);
        if (windowModeRow)
        {
            row.transform.SetSiblingIndex(windowModeRow.GetSiblingIndex() + 1);
        }

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredWidth = 540f;
        rowLayout.preferredHeight = 30f;

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
        label.fontSize = 19f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        if (font) label.font = font;

        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.minWidth = 170f;
        labelLayout.preferredWidth = 170f;
        labelLayout.flexibleWidth = 0f;

        GameObject toggleObject = new("VSyncToggle", typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(LayoutElement));
        toggleObject.transform.SetParent(row.transform, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(28f, 28f);

        LayoutElement toggleLayout = toggleObject.GetComponent<LayoutElement>();
        toggleLayout.preferredWidth = 28f;
        toggleLayout.preferredHeight = 28f;
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
