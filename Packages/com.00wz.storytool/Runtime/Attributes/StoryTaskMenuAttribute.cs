using System;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Attribute that defines the menu path in the StoryTask creation context menu
    /// (similar to Unity's <c>CreateAssetMenu</c> attribute for ScriptableObjects).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class StoryTaskMenuAttribute : Attribute
    {
        /// <summary>
        /// Menu path used when displaying this StoryTask in the editor context menu.
        /// </summary>
        public string MenuPath { get; }

        /// <summary>
        /// Creates a new <see cref="StoryTaskMenuAttribute"/> with the specified menu path.
        /// </summary>
        /// <param name="menuPath">Hierarchical menu path (e.g. "Common/Logic/Delay").</param>
        public StoryTaskMenuAttribute(string menuPath)
        {
            MenuPath = menuPath;
        }
    }
}
