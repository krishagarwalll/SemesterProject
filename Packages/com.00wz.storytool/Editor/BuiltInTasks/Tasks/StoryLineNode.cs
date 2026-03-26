using StoryTool.BuiltInTasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;

namespace StoryTool.Editor.BuiltInTasks
{
    [StoryTaskNodeDrawer(typeof(StoryLine), typeof(StoryPoint))]
    public class StoryLineNode : StoryTaskNode
    {
        public StoryLineNode(SerializedProperty taskProperty) 
            : base(taskProperty)
        {
        }

        protected override void BuildContent()
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

                    if(iterator.name == "start")
                    {
                        inputContainer.Add(propertyField);
                        continue;
                    }
                    if(iterator.name == "end")
                    {
                        outputContainer.Add(propertyField);
                        continue;
                    }
                    mainContainer.Add(propertyField);
                }
                while (iterator.NextVisible(false));
            }

            RefreshPorts();
        }
    }
}
