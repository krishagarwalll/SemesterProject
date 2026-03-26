using StoryTool.BuiltInTasks;
using UnityEditor;
using UnityEditor.UIElements;

namespace StoryTool.Editor.BuiltInTasks
{
    [StoryTaskNodeDrawer(typeof(BranchStoryTask))]
    public class BranchStoryTaskNode : StoryTaskNode
    {
        public BranchStoryTaskNode(SerializedProperty taskProperty) : base(taskProperty)
        {
        }

        protected override string GetTitle()
        {
            return "Branch";
        }

        protected override void BuildContent()
        {
            var startProp = SerializedTaskProperty.FindPropertyRelative("input").Copy();
            var startPropertyField = new PropertyField(startProp);
            startPropertyField.BindProperty(SerializedTaskProperty.serializedObject);
            inputContainer.Add(startPropertyField);

            var endsProp = SerializedTaskProperty.FindPropertyRelative("outputs").Copy();
            var endsPropertyField = new PropertyField(endsProp);
            endsPropertyField.BindProperty(SerializedTaskProperty.serializedObject);
            outputContainer.Add(endsPropertyField);

            RefreshPorts();
            
            tooltip = "Branch: splits the flow into multiple outputs.";
        }
    }
}
