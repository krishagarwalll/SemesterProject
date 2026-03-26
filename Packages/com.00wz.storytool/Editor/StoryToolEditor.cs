using UnityEditor;
using UnityEngine;

namespace StoryTool.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="StoryTool.Runtime.StoryTool"/> that adds a button
    /// to open the visual story graph editor window.
    /// </summary>
    [CustomEditor(typeof(StoryTool.Runtime.StoryTool))]
    public class StoryToolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open graph"))
            {
                StoryGraphWindow.OpenWindow((StoryTool.Runtime.StoryTool)target);
            }
        }
    }
}
