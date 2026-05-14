#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomPortal))]
public class RoomPortalEditor : Editor
{
    private SerializedProperty linkedPortal;
    private SerializedProperty spawnPoint;
    private SerializedProperty traversalMode;
    private SerializedProperty enterLabel;
    private SerializedProperty lockedLabel;
    private SerializedProperty inspectLabel;
    private SerializedProperty primaryGlyphId;
    private SerializedProperty inspectGlyphId;
    private SerializedProperty inspectText;
    private SerializedProperty lockMode;
    private SerializedProperty requiredFlag;
    private SerializedProperty requiredItem;
    private SerializedProperty consumeRequiredItem;
    private SerializedProperty startUnlocked;
    private SerializedProperty lockedInspectText;
    private SerializedProperty questToCompleteOnUnlock;
    private SerializedProperty fadeDuration;
    private SerializedProperty audioSource;
    private SerializedProperty enterRoom;
    private SerializedProperty lockedDoor;

    private static bool connectionFoldout = true;
    private static bool labelsFoldout = false;
    private static bool lockFoldout = true;
    private static bool transitionFoldout = false;
    private static bool audioFoldout = false;

    private void OnEnable()
    {
        linkedPortal = serializedObject.FindProperty("linkedPortal");
        spawnPoint = serializedObject.FindProperty("spawnPoint");
        traversalMode = serializedObject.FindProperty("traversalMode");
        enterLabel = serializedObject.FindProperty("enterLabel");
        lockedLabel = serializedObject.FindProperty("lockedLabel");
        inspectLabel = serializedObject.FindProperty("inspectLabel");
        primaryGlyphId = serializedObject.FindProperty("primaryGlyphId");
        inspectGlyphId = serializedObject.FindProperty("inspectGlyphId");
        inspectText = serializedObject.FindProperty("inspectText");
        lockMode = serializedObject.FindProperty("lockMode");
        requiredFlag = serializedObject.FindProperty("requiredFlag");
        requiredItem = serializedObject.FindProperty("requiredItem");
        consumeRequiredItem = serializedObject.FindProperty("consumeRequiredItem");
        startUnlocked = serializedObject.FindProperty("startUnlocked");
        lockedInspectText = serializedObject.FindProperty("lockedInspectText");
        questToCompleteOnUnlock = serializedObject.FindProperty("questToCompleteOnUnlock");
        fadeDuration = serializedObject.FindProperty("fadeDuration");
        audioSource = serializedObject.FindProperty("audioSource");
        enterRoom = serializedObject.FindProperty("enterRoom");
        lockedDoor = serializedObject.FindProperty("lockedDoor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        RoomPortal portal = (RoomPortal)target;

        // ── Connection status banner ─────────────────────────────────────────
        DrawConnectionBanner(portal);

        EditorGUILayout.Space(4);

        // ── Connection section ───────────────────────────────────────────────
        connectionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(connectionFoldout, "Connection");
        if (connectionFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(linkedPortal);
            EditorGUILayout.PropertyField(spawnPoint);
            EditorGUILayout.PropertyField(traversalMode);

            EditorGUILayout.Space(2);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-Connect Nearest", GUILayout.Height(22)))
            {
                AutoConnectNearest(portal);
            }
            GUI.enabled = portal.LinkedPortal != null;
            if (GUILayout.Button("Ping Linked", GUILayout.Height(22)))
            {
                EditorGUIUtility.PingObject(portal.LinkedPortal);
                Selection.activeObject = portal.LinkedPortal;
            }
            if (GUILayout.Button("Select Linked", GUILayout.Height(22)))
            {
                Selection.activeGameObject = portal.LinkedPortal?.gameObject;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── Lock section ─────────────────────────────────────────────────────
        lockFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(lockFoldout, "Lock");
        if (lockFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lockMode);

            PortalLockMode mode = (PortalLockMode)lockMode.enumValueIndex;
            if (mode == PortalLockMode.Flag)
            {
                EditorGUILayout.PropertyField(requiredFlag, new GUIContent("Required Flag"));
            }
            else if (mode == PortalLockMode.RequiredItem)
            {
                EditorGUILayout.PropertyField(requiredItem, new GUIContent("Required Item"));
                EditorGUILayout.PropertyField(consumeRequiredItem, new GUIContent("Consume on Use"));
                EditorGUILayout.PropertyField(questToCompleteOnUnlock, new GUIContent("Complete Quest on Unlock",
                    "Optional. This quest is accepted (if needed) and completed when the item lock is opened."));
                EditorGUILayout.PropertyField(startUnlocked, new GUIContent("Start Pre-Unlocked",
                    "Skip the item check — door opens from the start. Useful for testing."));
            }

            if (mode != PortalLockMode.None)
            {
                EditorGUILayout.PropertyField(lockedLabel, new GUIContent("Locked Label"));
                EditorGUILayout.PropertyField(lockedInspectText, new GUIContent("Locked Message"));
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── Labels section ───────────────────────────────────────────────────
        labelsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(labelsFoldout, "Labels & Glyphs");
        if (labelsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enterLabel);
            EditorGUILayout.PropertyField(inspectLabel);
            EditorGUILayout.PropertyField(inspectText, new GUIContent("Inspect Text"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(primaryGlyphId);
            EditorGUILayout.PropertyField(inspectGlyphId);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── Transition section ───────────────────────────────────────────────
        transitionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(transitionFoldout, "Transition");
        if (transitionFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fadeDuration);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── Audio section ────────────────────────────────────────────────────
        audioFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(audioFoldout, "Audio");
        if (audioFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(audioSource);
            EditorGUILayout.PropertyField(enterRoom, new GUIContent("Enter Sound"));
            EditorGUILayout.PropertyField(lockedDoor, new GUIContent("Locked Sound"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawConnectionBanner(RoomPortal portal)
    {
        RoomPortal linked = portal.LinkedPortal;

        if (!linked)
        {
            EditorGUILayout.HelpBox("No linked portal — this portal cannot be traversed.", MessageType.Warning);
            return;
        }

        string linkedRoom = linked.OwnerRoom ? linked.OwnerRoom.name : "(no Room parent)";
        bool isReciprocal = linked.LinkedPortal == portal;
        string traversalNote = GetTraversalNote(portal);
        string reciprocalNote = isReciprocal ? "" : "  ⚠ Linked portal does not point back to this one.";

        string banner = $"→  {linked.name}  in  {linkedRoom}{(string.IsNullOrEmpty(reciprocalNote) ? "" : "\n" + reciprocalNote)}";
        MessageType type = isReciprocal ? MessageType.Info : MessageType.Warning;
        EditorGUILayout.HelpBox(banner, type);

        if (!string.IsNullOrWhiteSpace(traversalNote))
        {
            EditorGUILayout.HelpBox(traversalNote, MessageType.None);
        }
    }

    private static string GetTraversalNote(RoomPortal portal)
    {
        if (!portal.CanTraverseFromThisSide && portal.CanReceiveTraversal)
            return "Entry Only — players can arrive here but cannot leave through this portal.";
        if (portal.CanTraverseFromThisSide && !portal.CanReceiveTraversal)
            return "Exit Only — players can leave through this portal but cannot enter from the linked side.";
        return string.Empty;
    }

    private static void AutoConnectNearest(RoomPortal portal)
    {
        Undo.RecordObject(portal, "Auto-Connect Portal");

        // Gather all portals in the scene that are not this one and not already linked back
        RoomPortal[] allPortals = FindObjectsByType<RoomPortal>(FindObjectsSortMode.None);
        RoomPortal best = null;
        float bestDist = float.MaxValue;

        foreach (RoomPortal candidate in allPortals)
        {
            if (candidate == portal) continue;
            if (candidate.LinkedPortal == portal) continue; // already a reciprocal — skip reassignment

            // Prefer portals in different rooms
            bool differentRoom = candidate.OwnerRoom != portal.OwnerRoom;
            if (!differentRoom && best != null && best.OwnerRoom != portal.OwnerRoom) continue;

            float dist = Vector3.Distance(portal.transform.position, candidate.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        if (best)
        {
            SerializedObject so = new(portal);
            so.FindProperty("linkedPortal").objectReferenceValue = best;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(portal);
            Debug.Log($"[RoomPortal] Auto-connected '{portal.name}' → '{best.name}'", portal);
        }
        else
        {
            Debug.LogWarning($"[RoomPortal] No suitable candidate found to auto-connect '{portal.name}'.", portal);
        }
    }

    private void OnSceneGUI()
    {
        RoomPortal portal = (RoomPortal)target;
        RoomPortal linked = portal.LinkedPortal;

        if (!linked) return;

        Vector3 from = portal.transform.position;
        Vector3 to = linked.transform.position;
        Vector3 mid = Vector3.Lerp(from, to, 0.5f);

        bool unlocked = portal.IsCurrentlyUnlocked;
        Handles.color = unlocked ? new Color(0.3f, 0.95f, 0.3f, 0.85f) : new Color(0.95f, 0.35f, 0.25f, 0.85f);

        // Thick connection line
        Handles.DrawLine(from, to, 2.5f);

        // Arrow at the destination end
        Vector3 dir = (to - from).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            float arrowSize = HandleUtility.GetHandleSize(to) * 0.25f;
            Handles.ArrowHandleCap(0, to - dir * arrowSize, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);
        }

        // Label at midpoint
        Handles.color = Color.white;
        GUIStyle labelStyle = new(EditorStyles.boldLabel)
        {
            normal = { textColor = unlocked ? new Color(0.2f, 0.9f, 0.2f) : new Color(0.9f, 0.3f, 0.2f) },
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        Handles.Label(mid + Vector3.up * 0.3f, $"{portal.name} → {linked.name}", labelStyle);

        // Spawn point handle
        if (portal.SpawnPoint && portal.SpawnPoint != portal.transform)
        {
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.75f);
            Handles.DrawWireDisc(portal.SpawnPoint.position, Vector3.forward, 0.2f);
            Handles.Label(portal.SpawnPoint.position + Vector3.up * 0.35f, "spawn", EditorStyles.miniLabel);
        }
    }
}
#endif
