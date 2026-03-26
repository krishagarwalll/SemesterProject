using System;
using System.Collections.Generic;
using System.Linq;
using StoryTool.Runtime;
using UnityEditor;
using UnityEngine;

namespace StoryTool.Editor
{
    public class StoryGraphController
    {
        private SerializedProperty _tasksProp;
        private SerializedProperty _commentsProp;

        public StoryGraphController(SerializedProperty storyGraphProperty)
        {
            if (!storyGraphProperty.IsOfType<StoryGraph>())
            {
                throw new ArgumentException(
                    $"[StoryTool] SerializedProperty '{storyGraphProperty?.propertyPath}' must reference a serialized field " +
                    $"or [SerializeReference] whose type is {nameof(StoryGraph)}.",
                    nameof(storyGraphProperty));
            }
            
            _tasksProp = storyGraphProperty.FindPropertyRelative(StoryGraphPropertyNames.StoryTasks);
            _commentsProp = storyGraphProperty.FindPropertyRelative(StoryGraphPropertyNames.Comments);
        }

        /// <summary>
        /// Synchronizes <see cref="TriggerLink"/> references across all story tasks
        /// in the underlying <see cref="StoryGraph"/>.
        /// </summary>
        /// <remarks>
        /// Call this method after story tasks or triggers have been added, removed
        /// or modified to ensure that all trigger links are valid and consistent.
        /// 
        /// This method:
        /// - removes duplicate or invalid <see cref="TriggerLink"/> assignments
        ///   from <see cref="EndTrigger"/> fields;
        /// - initializes new <see cref="TriggerLink"/> instances for
        ///   <see cref="StartTrigger"/> fields that do not have a link assigned;
        /// - guarantees a one-to-one mapping between <see cref="TriggerLink"/>
        ///   instances and start/end trigger pairs.
        /// </remarks>
        public void SyncTriggerLinks()
        {
            var iterator = _tasksProp.Copy();
            var endProperty = _tasksProp.GetEndProperty();
            
            Dictionary<TriggerLink, SerializedProperty> outputTriggerLinks = new();
            HashSet<TriggerLink> inputTriggerLinks = new();

            while (iterator.NextVisible(true))
            {
                if (SerializedProperty.EqualContents(iterator, endProperty))
                    break;

                var property = iterator.Copy();

                if (property.IsOfType<EndTrigger>())
                {
                    if (!ValidateNotNull(property, asError: false))
                    {
                        continue;
                    }
                    
                    var triggerLinkProperty = property.FindPropertyRelative(StoryGraphPropertyNames.NextTriggerLink);
                    var triggerLink = triggerLinkProperty.managedReferenceValue as TriggerLink;

                    if (triggerLink != null && !outputTriggerLinks.TryAdd(triggerLink, triggerLinkProperty))
                    {
                        triggerLinkProperty.managedReferenceValue = null;
                        triggerLinkProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                else if (property.IsOfType<StartTrigger>())
                {
                    if (!ValidateNotNull(property, asError: false))
                    {
                        continue;
                    }

                    var triggerLinkProperty = property.FindPropertyRelative(StoryGraphPropertyNames.TriggerLink);
                    var triggerLink = triggerLinkProperty.managedReferenceValue as TriggerLink;

                    if (triggerLink == null || !inputTriggerLinks.Add(triggerLink))
                    {
                        TriggerLink newTriggerLink = new();
                        triggerLinkProperty.managedReferenceValue = newTriggerLink;
                        triggerLinkProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            // reset invalid EndTriggers
            foreach(var (link, property) in outputTriggerLinks)
            {
                if(!inputTriggerLinks.Contains(link))
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="StoryTask"/> instance and adds it to the shared collection.
        /// </summary>
        /// <param name="type">Concrete StoryTask type to instantiate.</param>
        /// <param name="position">Initial node position in graph space.</param>
        public void CreateStoryTask(System.Type type, Vector2 position)
        {
            StoryTask instance;
            try
            {
                instance = (StoryTask)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            var elementProp = _tasksProp.AddArrayElementToEnd();
            elementProp.managedReferenceValue = instance;
            elementProp.FindPropertyRelative(StoryGraphPropertyNames.NodePosition).vector2Value = position;
            _tasksProp.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Removes the link from the specified <see cref="EndTrigger"/>.
        /// </summary>
        /// <param name="endTriggerProperty">
        /// Serialized property representing the <see cref="EndTrigger"/> to unlink.
        /// </param>
        public void Unlink(SerializedProperty endTriggerProperty)
        {
            if (endTriggerProperty == null) 
                throw new NullReferenceException($"[StoryTool] argument \"{nameof(endTriggerProperty)}\" is null when trying to unlink");
            if (!endTriggerProperty.IsOfType<EndTrigger>()) 
                throw new ArgumentException($"[StoryTool] SerializedProperty '{endTriggerProperty.propertyPath}' " + 
                $"is not a property of the {nameof(EndTrigger)} type");

            if (!ValidateNotNull(endTriggerProperty, asError: true))
            {
                return;
            }
            
            endTriggerProperty.FindPropertyRelative(StoryGraphPropertyNames.NextTriggerLink).managedReferenceValue = null;
            endTriggerProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Creates a link from an <see cref="EndTrigger"/> to a <see cref="StartTrigger"/>.
        /// </summary>
        /// <param name="endTriggerProperty">
        /// Serialized property representing the source <see cref="EndTrigger"/>.
        /// </param>
        /// <param name="startTriggerProperty">
        /// Serialized property representing the target <see cref="StartTrigger"/>.
        /// </param>
        public void Link(SerializedProperty endTriggerProperty, SerializedProperty startTriggerProperty)
        {
            if (endTriggerProperty == null) 
                throw new NullReferenceException($"[StoryTool] argument \"{nameof(endTriggerProperty)}\" is null when trying to link");
            if (!endTriggerProperty.IsOfType<EndTrigger>()) 
                throw new ArgumentException($"[StoryTool] SerializedProperty '{endTriggerProperty.propertyPath}' " + 
                $"is not a property of the {nameof(EndTrigger)} type");
            if (startTriggerProperty == null) 
                throw new NullReferenceException($"[StoryTool] argument \"{nameof(startTriggerProperty)}\" is null when trying to link");
            if (!startTriggerProperty.IsOfType<StartTrigger>()) 
                throw new ArgumentException($"[StoryTool] SerializedProperty '{startTriggerProperty.propertyPath}' " + 
                $"is not a property of the {nameof(StartTrigger)} type");
                
            if (!ValidateNotNull(endTriggerProperty, asError: true))
            {
                return;
            }

            if (!ValidateNotNull(startTriggerProperty, asError: true))
            {
                return;
            }

            var link = startTriggerProperty.FindPropertyRelative(StoryGraphPropertyNames.TriggerLink).managedReferenceValue;
            if (link == null)
            {
                throw new NullReferenceException("[StoryTool] StartTrigger link is null when linking");
            }

            endTriggerProperty.FindPropertyRelative(StoryGraphPropertyNames.NextTriggerLink).managedReferenceValue = link;
            endTriggerProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Removes the specified <see cref="StoryTask"/> instances from the underlying <see cref="StoryGraph"/>.
        /// </summary>
        /// <param name="tasksSerializedProperties">
        /// Collection of <see cref="SerializedProperty"/> entries that reference <see cref="StoryTask"/> instances to remove.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if any of the provided properties does not reference a <see cref="StoryTask"/>.
        /// </exception>
        public void RemoveTasks(IEnumerable<SerializedProperty> tasksSerializedProperties)
        {
            if (tasksSerializedProperties.Any(sp => !sp.IsOfType<StoryTask>()))
                throw new ArgumentException(
                    "[StoryTool] All SerializedProperties passed to RemoveTasks must be properties of the StoryTask type.",
                    nameof(tasksSerializedProperties));

            var removedTasks = tasksSerializedProperties
            .Select(tsp => tsp.managedReferenceValue)
            .ToList();

            if (removedTasks.Any())
            {
                removedTasks.ForEach(t => _tasksProp.RemoveManagedReferenceElement(t));
                _tasksProp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskProperty"></param>
        /// <param name="newPosition"></param>
        public void SetNodePosition(SerializedProperty taskProperty, Vector2 newPosition)
        {
            taskProperty.FindPropertyRelative(StoryGraphPropertyNames.NodePosition).vector2Value = newPosition;
            taskProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Creates a new <see cref="Runtime.StoryGraphComment"/> instance and adds it to the shared collection.
        /// </summary>
        /// <param name="position">Initial comment position in graph space.</param>
        public void CreateComment(Vector2 position)
        {
            var elementProp = _commentsProp.AddArrayElementToEnd();
            var rect = new Rect(position, StoryGraphComment.defaultSize);
            var commentInstance = new StoryTool.Runtime.StoryGraphComment(rect, "Comment");
            elementProp.managedReferenceValue = commentInstance;
            _commentsProp.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Removes the specified <see cref="Runtime.StoryGraphComment"/> instances from the underlying <see cref="StoryGraph"/>.
        /// </summary>
        /// <param name="commentsSerializedProperties">
        /// Collection of <see cref="SerializedProperty"/> entries that reference <see cref="Runtime.StoryGraphComment"/> instances to remove.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if any of the provided properties does not reference a <see cref="Runtime.StoryGraphComment"/>.
        /// </exception>
        public void RemoveComments(IEnumerable<SerializedProperty> commentsSerializedProperties)
        {
            if (commentsSerializedProperties.Any(sp => !sp.IsOfType<Runtime.StoryGraphComment>()))
                throw new ArgumentException(
                    "[StoryTool] All SerializedProperties passed to RemoveComments must be properties of the StoryGraphComment type.",
                    nameof(commentsSerializedProperties));

            // Collect indices to remove (in reverse order to avoid index shifting issues)
            var indicesToRemove = new List<int>();
            foreach (var commentProp in commentsSerializedProperties)
            {
                for (int i = 0; i < _commentsProp.arraySize; i++)
                {
                    var element = _commentsProp.GetArrayElementAtIndex(i);
                    if (SerializedProperty.EqualContents(element, commentProp))
                    {
                        indicesToRemove.Add(i);
                        break;
                    }
                }
            }

            // Remove in reverse order
            indicesToRemove.Sort();
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                _commentsProp.DeleteArrayElementAtIndex(indicesToRemove[i]);
            }

            if (indicesToRemove.Any())
            {
                _commentsProp.serializedObject.ApplyModifiedProperties();
            }
        }
        
        private static bool ValidateNotNull(SerializedProperty property, bool asError)
        {
            if (property.IsValueNotNull())
            {
                return true;
            }
            
            var message =
                $"[StoryTool] SerializedProperty '{property.propertyPath}' has a null value. " +
                "If this field is marked with [SerializeReference], make sure it is assigned.";

            if (asError)
            {
                Debug.LogError(message);
            }
            else
            {
                Debug.LogWarning(message);
            }

            return false;
        }
    }
}
