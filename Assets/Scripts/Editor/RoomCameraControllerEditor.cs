#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomCameraController))]
public class RoomCameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        RoomCameraController controller = (RoomCameraController)target;

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Auto-Find Player Follow Target", GUILayout.Height(22)))
        {
            PoptropicaController pc = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
            if (pc)
            {
                Undo.RecordObject(controller, "Set Follow Target");
                SerializedObject so = new(controller);
                so.FindProperty("followTarget").objectReferenceValue = pc.transform;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
                Debug.Log($"[RoomCameraController] Follow target set to '{pc.name}'.", controller);
            }
            else
            {
                Debug.LogWarning("[RoomCameraController] No PoptropicaController found in scene.", controller);
            }
        }
    }

    private void OnSceneGUI()
    {
        RoomCameraController controller = (RoomCameraController)target;
        Room room = controller.GetComponentInParent<Room>(true);

        if (!room || !room.BoundsVolume) return;
        if (!Camera.main) return;

        float aspect = Camera.main.aspect > 0.01f ? Camera.main.aspect : 16f / 9f;
        room.TryGetOrthographicSize(aspect, out float orthoSize);
        orthoSize = Mathf.Clamp(orthoSize, 1.25f, 5f);

        Bounds b = room.BoundsVolume.bounds;

        // Show the safe zone the camera can be centred within
        float safeW = Mathf.Max(0f, b.size.x - orthoSize * aspect * 2f);
        float safeH = Mathf.Max(0f, b.size.y - orthoSize * 2f);
        Rect safeZone = new(b.center.x - safeW * 0.5f, b.center.y - safeH * 0.5f, safeW, safeH);

        Handles.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Handles.DrawSolidRectangleWithOutline(
            new Rect(safeZone.x, safeZone.y, safeZone.width, safeZone.height),
            new Color(1f, 0.8f, 0.2f, 0.04f),
            new Color(1f, 0.8f, 0.2f, 0.5f));

        Handles.Label(new Vector3(b.min.x + 0.1f, b.max.y - 0.15f, 0f),
            "Camera safe zone", EditorStyles.miniLabel);
    }
}
#endif
