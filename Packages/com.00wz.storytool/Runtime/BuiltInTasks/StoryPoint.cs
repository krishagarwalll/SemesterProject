using System;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    public abstract class StoryPoint : StoryTask
    {
        [SerializeField]
        private StartTrigger start;

        [SerializeField]
        private EndTrigger end;

        protected override void OnAwake()
        {
            start.Triggered += ActivateTask;
        }

        /// <summary>
        /// Internal entry point invoked when the <see cref="StartTrigger"/> is fired.
        /// Updates the activity flag and delegates actual work to <see cref="ReceiveExecute"/>.
        /// </summary>
        private void ActivateTask()
        {
            ActivityFlag = StoryTaskActivityFlag.Active;
            try
            {
                ReceiveExecute();
            }
            catch(Exception e)
            {
                ActivityFlag = StoryTaskActivityFlag.Failed;
                Debug.LogError(e);
                return;
            }
            ActivityFlag = StoryTaskActivityFlag.Inactive;
            end.Trigger();
        }

        /// <summary>
        /// Called when the line is executed.
        /// Implement this method to start the actual story action and
        /// call <see cref="FinishExecute"/> when the action has completed.
        /// </summary>
        protected abstract void ReceiveExecute();
    }
}
