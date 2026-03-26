using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StoryTool.Samples.VisualNovel
{
    [CustomPropertyDrawer(typeof(Choise))]
    public class ChoiseDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var textProp = property.FindPropertyRelative("text");
            if (textProp != null)
            {
                var flagField = new PropertyField(textProp);
                flagField.label = string.Empty;

                flagField.BindProperty(property.serializedObject);

                root.Add(flagField);
            }

            var selectTriggerProp = property.FindPropertyRelative("selectTrigger");
            if (selectTriggerProp != null)
            {
                var port = Port.Create<Edge>(
                    Orientation.Horizontal,
                    Direction.Output,
                    Port.Capacity.Single,
                    typeof(bool)
                );

                port.userData = selectTriggerProp;
                port.portName = string.Empty;
                port.style.flexShrink = 0;

                root.Add(port);
            }

            return root;
        }
    }
}