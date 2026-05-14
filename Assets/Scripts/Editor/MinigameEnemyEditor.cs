#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MinigameEnemy))]
public class MinigameEnemyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        MinigameEnemy enemy = (MinigameEnemy)target;

        if (!Application.isPlaying) return;

        EditorGUILayout.Space(4);

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Speed", rb.linearVelocity.magnitude);
            }
        }
    }

    private void OnSceneGUI()
    {
        MinigameEnemy enemy = (MinigameEnemy)target;
        SerializedObject so = serializedObject;

        Transform target = so.FindProperty("target").objectReferenceValue as Transform;
        if (!target) return;

        // Line to target
        Handles.color = new Color(0.9f, 0.3f, 0.3f, 0.6f);
        Handles.DrawLine(enemy.transform.position, target.position, 1.5f);

        // Direction arrow
        Vector3 dir = (target.position - enemy.transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            float size = HandleUtility.GetHandleSize(enemy.transform.position) * 0.3f;
            Handles.ArrowHandleCap(0,
                enemy.transform.position + dir * size,
                Quaternion.LookRotation(dir),
                size, EventType.Repaint);
        }

        // Bob amplitude ring
        float amplitude = so.FindProperty("bobAmplitude").floatValue;
        Handles.color = new Color(0.6f, 0.4f, 1f, 0.3f);
        Handles.DrawWireDisc(enemy.transform.position, Vector3.forward, amplitude);
    }
}
#endif
