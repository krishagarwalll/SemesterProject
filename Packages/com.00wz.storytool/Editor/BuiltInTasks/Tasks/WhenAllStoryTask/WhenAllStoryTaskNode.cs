using StoryTool.BuiltInTasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace StoryTool.Editor.BuiltInTasks
{
    [StoryTaskNodeDrawer(typeof(WhenAllStoryTask))]
    public class WhenAllStoryTaskNode : StoryTaskNode
    {
        public WhenAllStoryTaskNode(SerializedProperty taskProperty) : base(taskProperty)
        {
        }

        protected override string GetTitle()
        {
            return "WhenAll";
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
            
            tooltip = "WhenAll: activates output once when all input triggers have fired. After completion it ignores further input signals.";
        }
    }
}
