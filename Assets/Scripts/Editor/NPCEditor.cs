#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPC))]
public class NPCEditor : Editor
{
    private SerializedProperty dialogueData;
    private SerializedProperty talkLabel;
    private SerializedProperty continueLabel;
    private SerializedProperty glyphId;
    private SerializedProperty actionPriority;
    private SerializedProperty dismissDistance;

    private static bool dialogueFoldout = true;
    private static bool settingsFoldout = false;

    private void OnEnable()
    {
        dialogueData = serializedObject.FindProperty("dialogueData");
        talkLabel = serializedObject.FindProperty("talkLabel");
        continueLabel = serializedObject.FindProperty("continueLabel");
        glyphId = serializedObject.FindProperty("glyphId");
        actionPriority = serializedObject.FindProperty("actionPriority");
        dismissDistance = serializedObject.FindProperty("dismissDistance");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        NPC npc = (NPC)target;

        // ── Runtime state banner ─────────────────────────────────────────────
        if (Application.isPlaying)
            DrawRuntimeBanner(npc);

        EditorGUILayout.Space(4);

        // ── Dialogue section ─────────────────────────────────────────────────
        dialogueFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(dialogueFoldout, "Dialogue");
        if (dialogueFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(dialogueData);

            if (npc.dialogueData)
                DrawDialoguePreview(npc.dialogueData);
            else
                EditorGUILayout.HelpBox("Assign an NPCDialogue asset to configure this NPC.", MessageType.Info);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── Settings section ─────────────────────────────────────────────────
        settingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(settingsFoldout, "Settings");
        if (settingsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(talkLabel);
            EditorGUILayout.PropertyField(continueLabel);
            EditorGUILayout.PropertyField(glyphId);
            EditorGUILayout.PropertyField(actionPriority);
            EditorGUILayout.PropertyField(dismissDistance, new GUIContent("Dismiss Distance",
                "Dialogue auto-closes when the player moves further than this distance."));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRuntimeBanner(NPC npc)
    {
        // Reflect private state via SerializedObject (read-only display)
        var so = new SerializedObject(npc);
        bool isDialogueActive = so.FindProperty("isDialogueActive")?.boolValue ?? false;
        bool isTyping = so.FindProperty("isTyping")?.boolValue ?? false;
        int dialogueIndex = so.FindProperty("dialogueIndex")?.intValue ?? 0;

        if (isDialogueActive)
        {
            string status = isTyping ? "Typing..." : "Waiting for input";
            string line = npc.dialogueData?.dialogueLines != null
                && dialogueIndex < npc.dialogueData.dialogueLines.Length
                ? $"Line {dialogueIndex}: \"{TruncateLabel(npc.dialogueData.dialogueLines[dialogueIndex], 60)}\""
                : string.Empty;
            EditorGUILayout.HelpBox($"ACTIVE  [{status}]\n{line}", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("Idle — dialogue not running", MessageType.None);
        }
    }

    private void DrawDialoguePreview(NPCDialogue data)
    {
        EditorGUILayout.Space(2);
        if (data.dialogueLines == null || data.dialogueLines.Length == 0) return;

        int shown = Mathf.Min(data.dialogueLines.Length, 4);
        for (int i = 0; i < shown; i++)
        {
            string label = $"[{i}] {TruncateLabel(data.dialogueLines[i], 70)}";
            bool isEnd = data.endDialogueLines != null && i < data.endDialogueLines.Length && data.endDialogueLines[i];
            bool isAuto = data.autoProgressLines != null && i < data.autoProgressLines.Length && data.autoProgressLines[i];
            if (isEnd) label += "  ■";
            if (isAuto) label += "  ▶";
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
        }

        if (data.dialogueLines.Length > shown)
            EditorGUILayout.LabelField($"...and {data.dialogueLines.Length - shown} more lines", EditorStyles.miniLabel);
    }

    private void OnSceneGUI()
    {
        NPC npc = (NPC)target;
        SerializedObject so = serializedObject;
        float dist = so.FindProperty("dismissDistance").floatValue;

        Handles.color = new Color(1f, 0.6f, 0.2f, 0.25f);
        Handles.DrawSolidDisc(npc.transform.position, Vector3.forward, dist);
        Handles.color = new Color(1f, 0.6f, 0.2f, 0.7f);
        Handles.DrawWireDisc(npc.transform.position, Vector3.forward, dist);
        Handles.Label(npc.transform.position + Vector3.up * (dist + 0.15f),
            "dismiss radius", EditorStyles.miniLabel);
    }

    private static string TruncateLabel(string s, int max)
        => s != null && s.Length > max ? s[..max] + "…" : s ?? string.Empty;
}
#endif
