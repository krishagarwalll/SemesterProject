using UnityEngine;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Describes the current activity state of a <see cref="StoryTask"/>.
    /// Used both at runtime and in the editor for visualization.
    /// </summary>
    public enum StoryTaskActivityFlag
    {
        /// <summary>
        /// The task is currently inactive or idle.
        /// </summary>
        Inactive,

        /// <summary>
        /// The task is currently active (e.g. running, processing).
        /// </summary>
        Active,

        /// <summary>
        /// The task has failed or finished with an error.
        /// </summary>
        Failed,

        /// <summary>
        /// The task has successfully finished and should not run again.
        /// </summary>
        Completed
    }
}
