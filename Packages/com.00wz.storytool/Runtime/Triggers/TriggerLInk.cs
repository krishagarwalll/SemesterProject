using UnityEngine;
using System;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Lightweight link object that connects <see cref="EndTrigger"/> and <see cref="StartTrigger"/>.
    /// It stores the subscription list and invokes all registered callbacks when triggered.
    /// </summary>
    [Serializable]
    public class TriggerLink
    {
        /// <summary>
        /// Event raised when this link is triggered.
        /// Subscribers are typically <see cref="StartTrigger"/> handlers or story task logic.
        /// </summary>
        public event Action Triggered;

        /// <summary>
        /// Invokes the <see cref="Triggered"/> event if there are any subscribers.
        /// </summary>
        public void Trigger()
        {
            if (Triggered != null)
            {
                foreach (Action handler in Triggered.GetInvocationList())
                {
                    try
                    {
                        handler();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }
    }
}