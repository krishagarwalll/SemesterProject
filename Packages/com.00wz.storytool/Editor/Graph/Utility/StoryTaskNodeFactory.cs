using System;
using System.Collections.Generic;
using UnityEditor;
using StoryTool.Runtime;

namespace StoryTool.Editor
{
    /// <summary>
    /// Factory for creating <see cref="StoryTaskNode"/> instances.
    /// Based on the runtime type of a <see cref="StoryTask"/> it looks up a matching
    /// custom node type (marked with <see cref="StoryTaskNodeDrawerAttribute"/>).
    /// If no custom node is found, falls back to the base <see cref="StoryTaskNode"/>.
    /// </summary>
    internal static class StoryTaskNodeFactory
    {
        /// <summary>
        /// Mapping: StoryTask type -> custom StoryTaskNode type.
        /// </summary>
        private static readonly Dictionary<Type, Type> _nodeTypesByTaskType;

        static StoryTaskNodeFactory()
        {
            _nodeTypesByTaskType = new Dictionary<Type, Type>();

            // Collect all types marked with StoryTaskNodeDrawerAttribute.
            foreach (var nodeType in TypeCache.GetTypesWithAttribute<StoryTaskNodeDrawerAttribute>())
            {
                if (!typeof(StoryTaskNode).IsAssignableFrom(nodeType))
                {
                    continue;
                }

                var attr = (StoryTaskNodeDrawerAttribute)Attribute.GetCustomAttribute(
                    nodeType,
                    typeof(StoryTaskNodeDrawerAttribute),
                    inherit: false);

                if (attr == null)
                    continue;

                foreach (var taskType in attr.StoryTaskTypes)
                {
                    if (taskType == null || !typeof(StoryTask).IsAssignableFrom(taskType))
                        continue;

                    // If multiple StoryTaskNode types are registered for the same StoryTask and
                    // they are in an inheritance relationship, pick the most specific (the most
                    // derived) node type. For unrelated node types, keep the "first wins"
                    // behavior, similar to CustomPropertyDrawer.
                    if (!_nodeTypesByTaskType.TryGetValue(taskType, out var existingNodeType))
                    {
                        _nodeTypesByTaskType.Add(taskType, nodeType);
                        continue;
                    }

                    if (existingNodeType == nodeType)
                        continue;

                    // existingNodeType is base, nodeType is more specific
                    if (existingNodeType.IsAssignableFrom(nodeType))
                    {
                        _nodeTypesByTaskType[taskType] = nodeType;
                    }
                    // nodeType is base, existingNodeType is more specific: keep existing mapping
                    else if (nodeType.IsAssignableFrom(existingNodeType))
                    {
                        // keep existing
                    }
                    // If node types are not related by inheritance, keep the first registered one.
                }
            }
        }

        /// <summary>
        /// Creates the most appropriate node for the given <paramref name="taskProperty"/> (a StoryTask).
        /// If a custom node type is registered via <see cref="StoryTaskNodeDrawerAttribute"/>, it will be used.
        /// Otherwise, a default <see cref="StoryTaskNode"/> instance is created.
        /// </summary>
        /// <param name="taskProperty">SerializedProperty pointing to a <see cref="StoryTask"/> instance.</param>
        public static StoryTaskNode CreateNode(SerializedProperty taskProperty)
        {
            if (taskProperty == null)
                throw new ArgumentNullException(nameof(taskProperty));

            var managedRef = taskProperty.managedReferenceValue;
            var taskInstance = managedRef as StoryTask;
            var taskType = taskInstance?.GetType();

            if (taskType != null)
            {
                var nodeType = GetNodeTypeForTaskType(taskType);
                if (nodeType != null)
                {
                    try
                    {
                        return (StoryTaskNode)Activator.CreateInstance(nodeType, taskProperty);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            // Fallback — create the default node.
            return new StoryTaskNode(taskProperty);
        }

        /// <summary>
        /// Finds the most appropriate node type for the given <see cref="StoryTask"/> type.
        /// First tries an exact match, then walks up the inheritance chain.
        /// This is similar to how <c>CustomPropertyDrawer</c> with <c>useForChildren = true</c> behaves.
        /// </summary>
        /// <param name="taskType">Concrete StoryTask type to find a node for.</param>
        /// <returns>Matched node type or <c>null</c> if nothing suitable was found.</returns>
        private static Type GetNodeTypeForTaskType(Type taskType)
        {
            // Exact match.
            if (_nodeTypesByTaskType.TryGetValue(taskType, out var direct))
                return direct;

            // Walk the base types while they are assignable to StoryTask.
            var current = taskType.BaseType;
            while (current != null && typeof(StoryTask).IsAssignableFrom(current))
            {
                if (_nodeTypesByTaskType.TryGetValue(current, out var nodeType))
                    return nodeType;

                current = current.BaseType;
            }

            return null;
        }
    }
}
