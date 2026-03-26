using System;
using UnityEngine;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Start marker for a <see cref="StoryTask"/>. Used to render an input port in the story graph.
    /// </summary>
    [Serializable]
    public class StartTrigger
    {
        [SerializeReference]
        private TriggerLink _triggerlink = new();

        /// <summary>
        /// Event that is raised when this start trigger is activated.
        /// Handlers are proxied to the underlying <see cref="TriggerLink"/> instance.
        /// </summary>
        public event Action Triggered
        {
            add => _triggerlink.Triggered += value;
            remove => _triggerlink.Triggered -= value;
        }
    }
}
