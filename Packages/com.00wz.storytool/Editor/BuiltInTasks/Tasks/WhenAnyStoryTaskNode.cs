using StoryTool.BuiltInTasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace StoryTool.Editor.BuiltInTasks
{
    [StoryTaskNodeDrawer(typeof(WhenAnyStoryTask))]
    public class WhenAnyStoryTaskNode : StoryTaskNode
    {
        public WhenAnyStoryTaskNode(SerializedProperty taskProperty) : base(taskProperty)
        {
        }

        protected override string GetTitle()
        {
            return "WhenAny";
        }

        protected override void BuildContent()
        {
            var startProp = SerializedTaskProperty.FindPropertyRelative("inputs").Copy();
            var startPropertyField = new PropertyField(startProp);
            startPropertyField.BindProperty(SerializedTaskProperty.serializedObject);
            inputContainer.Add(startPropertyField);

            var endsProp = SerializedTaskProperty.FindPropertyRelative("output").Copy();
            var endsPropertyField = new PropertyField(endsProp);
            endsPropertyField.BindProperty(SerializedTaskProperty.serializedObject);
            outputContainer.Add(endsPropertyField);

            RefreshPorts();
            
            tooltip = "WhenAny: activates output once when any input trigger has fired. After completion it ignores further input signals.";
        }
    }
}
