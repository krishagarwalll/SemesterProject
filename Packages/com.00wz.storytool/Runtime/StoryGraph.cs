using System;
using System.Collections.Generic;
using UnityEngine;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Serializable container that holds a collection of <see cref="StoryTask"/> instances
    /// to be executed by <see cref="StoryTool"/>.
    /// </summary>
    [Serializable]
    public class StoryGraph
    {
        /// <summary>
        /// All tasks that belong to this story graph.
        /// Tasks are stored as <see cref="SerializeReference"/> so user code can provide custom task types.
        /// </summary>
        [SerializeReference]
        public List<StoryTask> storyTasks = new List<StoryTask>();

#if UNITY_EDITOR
        /// <summary>
        /// All comments that belong to this story graph.
        /// Comments are used for annotation and organization in the editor.
        /// </summary>
        [SerializeReference]
        public List<StoryGraphComment> comments = new List<StoryGraphComment>();
#endif
    }
}
