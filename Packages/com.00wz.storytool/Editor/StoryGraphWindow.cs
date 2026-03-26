using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StoryTool.Editor
{
    /// <summary>
    /// Editor window used for visual editing of a <see cref="StoryGraph"/> associated with a <see cref="StoryTool.Runtime.StoryTool"/> component.
    /// </summary>
    public class StoryGraphWindow : EditorWindow
    {
        private StoryTool.Runtime.StoryTool _storyTool;
        private StoryGraphView _graphView;

        public static void OpenWindow(StoryTool.Runtime.StoryTool storyTool, string title = null)
        {
            var window = GetWindow<StoryGraphWindow>();
            window.titleContent = new GUIContent(title ?? "Story Graph");
            window.Initialize(storyTool);
            window.Show();
        }

        private void Initialize(StoryTool.Runtime.StoryTool storyTool)
        {
            _storyTool = storyTool;
            CreateGraphView();
        }

        private void CreateGraphView()
        {
            rootVisualElement.Clear();
            var so = new SerializedObject(_storyTool);
            var storyGraphProperty = so.FindProperty("_storyGraph");
            var storyGraphController = new StoryGraphController(storyGraphProperty);
            _graphView = new StoryGraphView(storyGraphProperty, storyGraphController);
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void OnEnable()
        {
            if (_storyTool != null)
                CreateGraphView();
        }
        private void OnDisable()
        {        
            if (_graphView != null)
            {
                rootVisualElement.Remove(_graphView);
                _graphView = null;
            }
        }
    }
}
