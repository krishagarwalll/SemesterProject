#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    private SerializedProperty roomId;
    private SerializedProperty musicClip;
    private SerializedProperty boundsVolume;
    private SerializedProperty defaultAnchor;
    private SerializedProperty contentRoot;
    private SerializedProperty backdropRoot;
    private SerializedProperty cameraOffset;
    private SerializedProperty orthographicPadding;
    private SerializedProperty orthographicSizeBuffer;
    private SerializedProperty containmentPadding;

    private static bool settingsFoldout = false;
    private static bool debugFoldout = true;

    private void OnEnable()
    {
        roomId = serializedObject.FindProperty("roomId");
        musicClip = serializedObject.FindProperty("musicClip");
        boundsVolume = serializedObject.FindProperty("boundsVolume");
        defaultAnchor = serializedObject.FindProperty("defaultAnchor");
        contentRoot = serializedObject.FindProperty("contentRoot");
        backdropRoot = serializedObject.FindProperty("backdropRoot");
        cameraOffset = serializedObject.FindProperty("cameraOffset");
        orthographicPadding = serializedObject.FindProperty("orthographicPadding");
        orthographicSizeBuffer = serializedObject.FindProperty("orthographicSizeBuffer");
        containmentPadding = serializedObject.FindProperty("containmentPadding");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Room room = (Room)target;

        // ── Room ID + music ──────────────────────────────────────────────────
        EditorGUILayout.PropertyField(roomId);
        EditorGUILayout.PropertyField(musicClip);

        // ── References ───────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(boundsVolume);
        EditorGUILayout.PropertyField(defaultAnchor);
        EditorGUILayout.PropertyField(contentRoot);
        EditorGUILayout.PropertyField(backdropRoot);

        if (!boundsVolume.objectReferenceValue)
        {
            EditorGUILayout.HelpBox("No Bounds Volume — room containment and camera sizing will not work.", MessageType.Warning);
        }

        // ── Camera settings ──────────────────────────────────────────────────
        settingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(settingsFoldout, "Camera & Containment");
        if (settingsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cameraOffset);
            EditorGUILayout.PropertyField(orthographicPadding);
            EditorGUILayout.PropertyField(orthographicSizeBuffer);
            EditorGUILayout.PropertyField(containmentPadding);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── Portal overview ──────────────────────────────────────────────────
        debugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(debugFoldout, "Portals in this Room");
        if (debugFoldout)
        {
            DrawPortalOverview(room);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── Auto-setup buttons ───────────────────────────────────────────────
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Auto-Fill Missing References", GUILayout.Height(22)))
        {
            AutoFillReferences(room);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPortalOverview(Room room)
    {
        RoomPortal[] portals = room.GetComponentsInChildren<RoomPortal>(true);

        if (portals.Length == 0)
        {
            EditorGUILayout.HelpBox("No portals found in this room.", MessageType.None);
            return;
        }

        foreach (RoomPortal portal in portals)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Status indicator
            bool hasLink = portal.LinkedPortal != null;
            bool isReciprocal = hasLink && portal.LinkedPortal.LinkedPortal == portal;
            GUIContent statusIcon = hasLink
                ? (isReciprocal ? new GUIContent("✓", "Linked (reciprocal)") : new GUIContent("⚠", "Linked but not reciprocal"))
                : new GUIContent("✗", "Not linked");
            Color prevColor = GUI.color;
            GUI.color = hasLink ? (isReciprocal ? Color.green : Color.yellow) : Color.red;
            GUILayout.Label(statusIcon, GUILayout.Width(18));
            GUI.color = prevColor;

            // Portal name + link target
            string linkInfo = hasLink
                ? $"→  {portal.LinkedPortal.name}  ({(portal.LinkedPortal.OwnerRoom ? portal.LinkedPortal.OwnerRoom.name : "?")})"
                : "not connected";
            EditorGUILayout.LabelField(portal.name, linkInfo);

            if (GUILayout.Button("Select", GUILayout.Width(52), GUILayout.Height(17)))
            {
                Selection.activeGameObject = portal.gameObject;
                EditorGUIUtility.PingObject(portal.gameObject);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void AutoFillReferences(Room room)
    {
        Undo.RecordObject(room, "Auto-Fill Room References");
        SerializedObject so = new(room);

        if (!so.FindProperty("boundsVolume").objectReferenceValue)
        {
            Collider2D col = room.GetComponentInChildren<Collider2D>(true);
            if (col) so.FindProperty("boundsVolume").objectReferenceValue = col;
        }

        if (!so.FindProperty("defaultAnchor").objectReferenceValue)
        {
            RoomAnchor anchor = room.GetComponentInChildren<RoomAnchor>(true);
            if (anchor) so.FindProperty("defaultAnchor").objectReferenceValue = anchor;
        }

        if (!so.FindProperty("contentRoot").objectReferenceValue)
        {
            so.FindProperty("contentRoot").objectReferenceValue = room.transform;
        }

        if (!so.FindProperty("backdropRoot").objectReferenceValue)
        {
            Transform backdrop = room.transform.Find("Backdrop");
            if (backdrop) so.FindProperty("backdropRoot").objectReferenceValue = backdrop;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(room);
        Debug.Log($"[Room] Auto-filled references on '{room.name}'.", room);
    }

    private void OnSceneGUI()
    {
        Room room = (Room)target;

        if (!room.BoundsVolume) return;

        // Draw room bounds overlay
        Bounds b = room.BoundsVolume.bounds;
        Handles.color = new Color(0.4f, 0.8f, 1f, 0.2f);
        Handles.DrawSolidRectangleWithOutline(
            new Rect(b.min.x, b.min.y, b.size.x, b.size.y),
            new Color(0.4f, 0.8f, 1f, 0.04f),
            new Color(0.4f, 0.8f, 1f, 0.5f));

        // Camera position marker
        Vector3 camPos = room.GetCameraPosition();
        Handles.color = new Color(1f, 0.9f, 0.2f, 0.85f);
        Handles.DrawWireDisc(camPos, Vector3.forward, 0.25f);
        Handles.Label(camPos + Vector3.up * 0.4f, "Camera", EditorStyles.miniLabel);
    }
}
#endif
