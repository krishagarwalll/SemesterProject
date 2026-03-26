using System;
using UnityEngine;

namespace StoryTool.Editor
{
    /// <summary>
    /// Attribute that associates a specific <see cref="StoryTask"/> type with its custom
    /// <see cref="StoryTaskNode"/> implementation in the GraphView.
    /// Works similarly to Unity's <c>CustomPropertyDrawer(typeof(SomeType))</c>.
    /// Example:
    /// <code>
    /// [StoryTaskNodeDrawer(typeof(MyStoryTask))]
    /// public class MyStoryTaskNode : StoryTaskNode { ... }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StoryTaskNodeDrawerAttribute : Attribute
    {
        /// <summary>
        /// Array of StoryTask types that this node can represent.
        /// </summary>
        public Type[] StoryTaskTypes { get; }

        /// <summary>
        /// Creates a new attribute that links one or more <see cref="StoryTask"/> types
        /// to a custom <see cref="StoryTaskNode"/> implementation.
        /// </summary>
        /// <param name="storyTaskTypes">One or more <see cref="StoryTask"/> types.</param>
        public StoryTaskNodeDrawerAttribute(params Type[] storyTaskTypes)
        {
            StoryTaskTypes = storyTaskTypes ?? Array.Empty<Type>();
        }
    }
}
