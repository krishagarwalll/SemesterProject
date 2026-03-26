using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using StoryTool.Runtime;

namespace StoryTool.Editor
{
    /// <summary>
    /// PropertyDrawer for <see cref="StartTrigger"/> that creates an input port
    /// for the GraphView via <see cref="CreatePropertyGUI"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(StartTrigger))]
    public class StartTriggerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var port = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Single,
                typeof(bool)
            );

            port.portName = property.displayName;

            port.userData = property;

            return port;
        }
    }
}
