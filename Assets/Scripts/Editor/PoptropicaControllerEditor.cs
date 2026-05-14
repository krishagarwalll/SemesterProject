#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoptropicaController))]
public class PoptropicaControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        PoptropicaController pc = (PoptropicaController)target;

        if (!Application.isPlaying) return;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Grounded", pc.IsGrounded);
            EditorGUILayout.Toggle("Airborne", pc.IsAirborne);
            EditorGUILayout.Slider("Jump Strength", pc.JumpStrengthNormalized, 0f, 1f);
        }
    }

    private void OnSceneGUI()
    {
        PoptropicaController pc = (PoptropicaController)target;
        if (!pc) return;

        SerializedObject so = serializedObject;
        float jumpThreshold = so.FindProperty("jumpThreshold").floatValue;
        float groundCheckDist = so.FindProperty("groundCheckDistance").floatValue;
        float groundSnapDist = so.FindProperty("groundSnapDistance").floatValue;

        Vector3 pos = pc.transform.position;

        // Jump threshold line
        Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        Vector3 thresholdCenter = pos + Vector3.up * jumpThreshold;
        Handles.DrawLine(thresholdCenter - Vector3.right * 0.5f, thresholdCenter + Vector3.right * 0.5f, 2f);
        Handles.Label(thresholdCenter + Vector3.right * 0.55f, "jump threshold", EditorStyles.miniLabel);

        // Ground check zone
        Handles.color = new Color(0.2f, 0.9f, 0.2f, 0.4f);
        Vector3 groundCheckCenter = pos + Vector3.down * (groundCheckDist * 0.5f);
        Handles.DrawWireDisc(groundCheckCenter, Vector3.forward, 0.1f);

        // Ground snap zone
        if (groundSnapDist > 0f)
        {
            Handles.color = new Color(0.2f, 0.9f, 0.2f, 0.15f);
            Handles.DrawSolidRectangleWithOutline(
                new Rect(pos.x - 0.3f, pos.y - groundSnapDist, 0.6f, groundSnapDist),
                new Color(0.2f, 0.9f, 0.2f, 0.05f),
                new Color(0.2f, 0.9f, 0.2f, 0.3f));
        }

        // Jump strength indicator at runtime
        if (Application.isPlaying && pc.IsGrounded && pc.JumpStrengthNormalized > 0f)
        {
            float t = pc.JumpStrengthNormalized;
            Color jumpColor = Color.Lerp(new Color(0.3f, 0.8f, 1f), new Color(0f, 1f, 0.4f), t);
            jumpColor.a = 0.7f;
            Handles.color = jumpColor;
            float size = HandleUtility.GetHandleSize(pos) * 0.3f;
            Handles.ArrowHandleCap(0, pos + Vector3.up * 0.3f, Quaternion.LookRotation(Vector3.up), size, EventType.Repaint);
        }
    }
}
#endif
