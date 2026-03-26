using System;
using UnityEngine;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Abstract base class for atomic story events used by StoryTool.
    /// All user-defined story events must inherit from <see cref="StoryTask"/>.
    /// </summary>
    [Serializable]
    public abstract class StoryTask
    {
#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private Vector2 _editorNodePosition;

        /// <summary>
        /// Activity flag used in the editor to visualize the task state on the graph.
        /// </summary>
        [SerializeField, HideInInspector]
        private StoryTaskActivityFlag _activityFlag_editor = StoryTaskActivityFlag.Inactive;

        /// <summary>
        /// Current activity state of the task in the editor.
        /// Implementations can change it to drive editor visualization.
        /// </summary>
        public virtual StoryTaskActivityFlag ActivityFlag
        {
            get => _activityFlag_editor;
            protected set => _activityFlag_editor = value;
        }
#else
        /// <summary>
        /// Backing field for <see cref="ActivityFlag"/> at runtime.
        /// </summary>
        private StoryTaskActivityFlag _activityFlag = StoryTaskActivityFlag.Inactive;

        /// <summary>
        /// Current activity state of the task at runtime.
        /// Implementations can change it to reflect task progress or failure.
        /// </summary>
        public virtual StoryTaskActivityFlag ActivityFlag
        {
            get => _activityFlag;
            protected set => _activityFlag = value;
        }
#endif

        /// <summary>
        /// Called from <see cref="StoryTool.Awake"/> to initialize the task instance.
        /// Use this for one-time setup that should run before the graph starts (subscribe to triggers, etc.).
        /// </summary>
        protected internal virtual void OnAwake() {}

        /// <summary>
        /// Called from <see cref="StoryTool.Start"/> when the story graph begins execution.
        /// </summary>
        protected internal virtual void OnStart() {}
    }
}
