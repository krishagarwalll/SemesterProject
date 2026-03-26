using StoryTool.BuiltInTasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StoryTool.Editor.BuiltInTasks
{
    [StoryTaskNodeDrawer(typeof(StartStoryTask))]
    public class StartStoryTaskNode : StoryTaskNode
    {
        public StartStoryTaskNode(SerializedProperty taskProperty)
            : base(taskProperty)
        {
        }

        protected override string GetTitle()
        {
            return "Start";
        }

        protected override void BuildContent()
        {
            titleButtonContainer.Clear();

            var startProp = SerializedTaskProperty.FindPropertyRelative("start").Copy();

            var centerContainer = new VisualElement();
            centerContainer.style.flexGrow = 1;
            centerContainer.style.justifyContent = Justify.Center;
            centerContainer.style.alignItems = Align.Center;

            var port = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool)
            );

            port.userData = startProp;
            port.portName = string.Empty;

            centerContainer.Add(port);
            titleButtonContainer.Add(centerContainer);
            
            tooltip = "Start: entry point of the story graph.";
        }
    }
}
