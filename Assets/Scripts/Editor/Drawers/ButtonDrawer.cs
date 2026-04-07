#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
public class ButtonDrawer : Editor
{
    private static readonly Dictionary<Type, List<(MethodInfo method, ButtonAttribute attribute)>> MethodCache = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DrawButtonsForTargetType(target != null ? target.GetType() : null);
    }

    private void DrawButtonsForTargetType(Type inspectedType)
    {
        if (inspectedType == null)
        {
            return;
        }

        List<(MethodInfo method, ButtonAttribute attribute)> methods = GetButtonMethods(inspectedType);
        if (methods.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(8f);
        for (int i = 0; i < methods.Count; i++)
        {
            (MethodInfo method, ButtonAttribute attribute) entry = methods[i];
            if (entry.attribute.PlayModeOnly && !Application.isPlaying)
            {
                continue;
            }

            if (entry.attribute.EditModeOnly && Application.isPlaying)
            {
                continue;
            }

            string label = string.IsNullOrWhiteSpace(entry.attribute.Label)
                ? ObjectNames.NicifyVariableName(entry.method.Name)
                : entry.attribute.Label;

            if (!GUILayout.Button(label, GUILayout.Height(Mathf.Max(18f, entry.attribute.Height))))
            {
                continue;
            }

            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                UnityEngine.Object currentTarget = targets[targetIndex];
                if (currentTarget == null)
                {
                    continue;
                }

                Undo.RecordObject(currentTarget, $"Invoke {entry.method.Name}");
                entry.method.Invoke(currentTarget, null);
                EditorUtility.SetDirty(currentTarget);
            }
        }
    }

    private static List<(MethodInfo method, ButtonAttribute attribute)> GetButtonMethods(Type type)
    {
        if (MethodCache.TryGetValue(type, out List<(MethodInfo method, ButtonAttribute attribute)> cachedMethods))
        {
            return cachedMethods;
        }

        List<(MethodInfo method, ButtonAttribute attribute)> methods = new();
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        MethodInfo[] declaredMethods = type.GetMethods(Flags);
        for (int i = 0; i < declaredMethods.Length; i++)
        {
            MethodInfo method = declaredMethods[i];
            if (method == null || method.GetParameters().Length != 0 || method.ReturnType != typeof(void))
            {
                continue;
            }

            ButtonAttribute attribute = method.GetCustomAttribute<ButtonAttribute>(true);
            if (attribute == null)
            {
                continue;
            }

            methods.Add((method, attribute));
        }

        MethodCache[type] = methods;
        return methods;
    }
}
#endif
