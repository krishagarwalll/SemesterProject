using System;
using UnityEngine;

namespace StoryTool.Runtime
{
    /// <summary>
    /// End marker for a <see cref="StoryTask"/>. Used to render an output port in the story graph.
    /// </summary>
    [Serializable]
    public class EndTrigger
    {
        [SerializeReference]
        private TriggerLink _nextTriggerLink;

        /// <summary>
        /// Activates the next <see cref="StartTrigger"/> in the chain, if it is assigned.
        /// </summary>
        public void Trigger()
        {
            _nextTriggerLink?.Trigger();
        }
    }
}
