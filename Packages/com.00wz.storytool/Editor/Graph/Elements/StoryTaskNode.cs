using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using StoryTool.Runtime;

namespace StoryTool.Editor
{
    /// <summary>
    /// GraphView node that represents a <see cref="StoryTool.Runtime.StoryTask"/> in the editor,
    /// including its serialized properties and trigger ports.
    /// </summary>
    public class StoryTaskNode : Node
    {
        public SerializedProperty SerializedTaskProperty { get; }

        private StoryTaskNodeBorder borderElement;

        public StoryTaskNode(SerializedProperty taskProperty)
        {
            SerializedTaskProperty = taskProperty;

            RefreshExpandedState();

            title = GetTitle();
            InitializeActivityVisualization();
            BindPositionToSerializedProperty();

            BuildContent();
        }

        /// <summary>
        /// Returns the node title string.
        /// </summary>
        protected virtual string GetTitle()
        {
            var managedReference = SerializedTaskProperty.managedReferenceValue;
            return managedReference != null
                ? managedReference.GetType().Name
                : nameof(StoryTask);
        }

        /// <summary>
        /// Configures visual highlighting of the node activity.
        /// </summary>
        private void InitializeActivityVisualization()
        {
            // Insert custom border element as the first child (drawn in the background).
            borderElement = new StoryTaskNodeBorder();
            hierarchy.Insert(0, borderElement);

            var activityFlagProperty = SerializedTaskProperty.FindPropertyRelative("_activityFlag_editor");
            borderElement.SetActivityState((StoryTaskActivityFlag)activityFlagProperty.enumValueIndex);
            borderElement.TrackPropertyValue(
                activityFlagProperty,
                property => borderElement.SetActivityState((StoryTaskActivityFlag)property.enumValueIndex));
        }

        /// <summary>
        /// Binds the node position in the GraphView to the serialized field _editorNodePosition.
        /// </summary>
        private void BindPositionToSerializedProperty()
        {
            var editorNodePositionProperty = SerializedTaskProperty.FindPropertyRelative("_editorNodePosition");
            SetPosition(new Rect(editorNodePositionProperty.vector2Value, GetPosition().size));
            this.TrackPropertyValue(
                editorNodePositionProperty,
                property => SetPosition(new Rect(property.vector2Value, GetPosition().size)));
        }

        /// <summary>
        /// Builds the node contents (main visual layout).
        /// By default, renders a <see cref="PropertyField"/> for all visible fields of the StoryTask.
        /// Derived classes can override this method to fully control the layout and elements
        /// inside the node.
        /// </summary>
        protected virtual void BuildContent()
        {
            // Make the mainContainer background slightly opaque.
            var backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.9f);
            mainContainer.style.backgroundColor = backgroundColor;

            var iterator = SerializedTaskProperty.Copy();
            var endProperty = SerializedTaskProperty.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                        break;
                    if (iterator.name == "m_Script") continue;

                    var propertyForField = iterator.Copy();

                    var propertyField = new PropertyField(propertyForField);
                    propertyField.BindProperty(SerializedTaskProperty.serializedObject);

                    mainContainer.Add(propertyField);
                }
                while (iterator.NextVisible(false));
            }
        }
    }
}
