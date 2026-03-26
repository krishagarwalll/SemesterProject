using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using StoryTool.BuiltInTasks;
using UnityEditor.Experimental.GraphView;

namespace StoryTool.Editor.BuiltInTasks
{
    /// <summary>
    /// PropertyDrawer for <see cref="StartTriggerWithFlag"/>.
    /// Draws the StartTrigger port with a read-only _isTriggered_editor flag on the right.
    /// </summary>
    [CustomPropertyDrawer(typeof(StartTriggerWithFlag))]
    public class StartTriggerWithFlagDrawer : PropertyDrawer
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

            var startTriggerProp = property.FindPropertyRelative("startTrigger");
            if (startTriggerProp != null)
            {
                var port = Port.Create<Edge>(
                    Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Single,
                    typeof(bool)
                );

                port.userData = startTriggerProp;
                port.portName = string.Empty;

                root.Add(port);
            }

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            root.Add(spacer);

            var flagProp = property.FindPropertyRelative("_isTriggered_editor");
            if (flagProp != null)
            {
                var flagField = new PropertyField(flagProp);
                flagField.label = string.Empty;
                flagField.style.width = 18f;
                flagField.style.alignSelf = Align.Center;

                flagField.BindProperty(property.serializedObject);
                flagField.SetEnabled(false);

                root.Add(flagField);
            }

            return root;
        }
    }
}