using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using StoryTool.Runtime;

namespace StoryTool.Editor
{
    /// <summary>
    /// PropertyDrawer for <see cref="EndTrigger"/> that creates an output port
    /// for the GraphView via <see cref="CreatePropertyGUI"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(EndTrigger))]
    public class EndTriggerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var port = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool)
            );

            port.portName = property.displayName;

            port.userData = property;

            return port;
        }
    }
}
