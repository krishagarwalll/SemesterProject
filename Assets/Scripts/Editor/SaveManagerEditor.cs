using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SaveManager))]
public class SaveManagerEditor : Editor
{
    private string saveJson = "";
    private bool showJson;
    private Vector2 scroll;

    private void OnEnable()
    {
        RefreshJson();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Runtime Save", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Storage Key", SaveManager.SaveKey);
        EditorGUILayout.LabelField("Exists", PlayerPrefs.HasKey(SaveManager.SaveKey) ? "Yes" : "No");

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!Application.isPlaying || SaveManager.Instance == null))
            {
                if (GUILayout.Button("Save Current"))
                {
                    SaveManager.Instance.Save();
                    RefreshJson();
                }

                if (GUILayout.Button("Load And Apply"))
                {
                    SaveManager.Instance.LoadAndApply();
                }
            }

            if (GUILayout.Button("Delete Save"))
            {
                DeleteSave();
                RefreshJson();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh JSON"))
            {
                RefreshJson();
            }

            showJson = EditorGUILayout.ToggleLeft("Show JSON", showJson, GUILayout.Width(100f));
        }

        if (!showJson)
        {
            return;
        }

        EditorGUILayout.Space(4f);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160f));
        saveJson = EditorGUILayout.TextArea(saveJson, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Validate JSON"))
            {
                ValidateJson(showSuccess: true);
            }

            if (GUILayout.Button("Overwrite Save JSON"))
            {
                WriteJson();
            }
        }
    }

    [MenuItem("Tools/Save Manager/Delete Save")]
    private static void DeleteSaveMenu()
    {
        DeleteSave();
    }

    [MenuItem("Tools/Save Manager/Capture Current State")]
    private static void CaptureCurrentStateMenu()
    {
        if (!Application.isPlaying || SaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveManagerEditor] Enter Play Mode with a SaveManager in the scene to capture current state.");
            return;
        }

        SaveManager.Instance.Save();
    }

    private void RefreshJson()
    {
        saveJson = PlayerPrefs.GetString(SaveManager.SaveKey, "");
    }

    private static void DeleteSave()
    {
        if (!EditorUtility.DisplayDialog("Delete Save", "Delete the current PlayerPrefs save data?", "Delete", "Cancel"))
        {
            return;
        }

        PlayerPrefs.DeleteKey(SaveManager.SaveKey);
        PlayerPrefs.Save();
        Debug.Log("[SaveManagerEditor] Save deleted.");
    }

    private bool ValidateJson(bool showSuccess)
    {
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(saveJson);
            if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
            {
                EditorUtility.DisplayDialog("Invalid Save JSON", "The JSON did not contain valid SaveData with a sceneName.", "OK");
                return false;
            }

            if (showSuccess)
            {
                EditorUtility.DisplayDialog("Valid Save JSON", $"Scene: {data.sceneName}\nVersion: {data.version}", "OK");
            }

            return true;
        }
        catch (System.Exception exception)
        {
            EditorUtility.DisplayDialog("Invalid Save JSON", exception.Message, "OK");
            return false;
        }
    }

    private void WriteJson()
    {
        if (!ValidateJson(showSuccess: false))
        {
            return;
        }

        if (!EditorUtility.DisplayDialog("Write Save JSON", "Overwrite the current save data?", "Overwrite", "Cancel"))
        {
            return;
        }

        PlayerPrefs.SetString(SaveManager.SaveKey, saveJson);
        PlayerPrefs.Save();
        Debug.Log("[SaveManagerEditor] Save JSON overwritten.");
    }
}
