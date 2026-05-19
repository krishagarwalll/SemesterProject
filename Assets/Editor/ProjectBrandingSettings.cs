using UnityEditor;
using UnityEngine;

public static class ProjectBrandingSettings
{
    private const string ProductName = "How to Get to Heaven";
    private const string IconPath = "Assets/Branding/HowToGetToHeavenIcon.png";

    [InitializeOnLoadMethod]
    private static void ApplyOnLoad()
    {
        EditorApplication.delayCall -= Apply;
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/Project/Apply Branding Settings")]
    public static void Apply()
    {
        PlayerSettings.productName = ProductName;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon)
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, BuildStandaloneIconSet(icon));
        }

        AssetDatabase.SaveAssets();
    }

    private static Texture2D[] BuildStandaloneIconSet(Texture2D icon)
    {
        Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Standalone);
        if (icons == null || icons.Length == 0)
        {
            icons = new Texture2D[8];
        }

        for (int i = 0; i < icons.Length; i++)
        {
            icons[i] = icon;
        }

        return icons;
    }
}
