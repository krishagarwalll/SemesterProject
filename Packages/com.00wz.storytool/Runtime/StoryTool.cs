using System;
using System.Collections.Generic;
using UnityEngine;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Main component that owns a <see cref="StoryGraph"/> and controls the lifecycle of its <see cref="StoryTask"/> collection.
    /// Attach this component to a scene object to execute a story graph at runtime.
    /// </summary>
    public class StoryTool : MonoBehaviour
    {
        [SerializeField]
        private StoryGraph _storyGraph;

        /// <summary>
        /// Read-only view of all <see cref="StoryTask"/> instances contained in the underlying <see cref="StoryGraph"/>.
        /// </summary>
        public IReadOnlyCollection<StoryTask> StoryTasks => _storyGraph.storyTasks;

        private void Awake()
        {
            // TODO: OnAwake is not called for tasks that are added to the graph at runtime.
            foreach (var task in _storyGraph.storyTasks)
            {
                task.OnAwake();
            }
        }

        private void Start()
        {
            foreach (var task in _storyGraph.storyTasks)
            {
                task.OnStart();
            }
        }
    }
}
