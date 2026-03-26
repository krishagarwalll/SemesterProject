using System;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    /// <summary>
    /// Abstract convenience base class built on top of <see cref="StoryTask"/>.
    /// Implementors only need to override <see cref="ReceiveExecute"/>,
    /// which is called when the line starts execution, and call <see cref="FinishExecute"/>
    /// when the line has finished its work.
    /// </summary>
    public abstract class StoryLine : StoryTask
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
            if (ActivityFlag == StoryTaskActivityFlag.Active)
            {
                Debug.LogWarning($"[StoryTool] StoryLine '{GetType().Name}' started while it is already active (ActivityFlag == {StoryTaskActivityFlag.Active}).");
            }

            ActivityFlag = StoryTaskActivityFlag.Active;
            try
            {
                ReceiveExecute();
            }
            catch(Exception e)
            {
                ActivityFlag = StoryTaskActivityFlag.Failed;
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Marks the line as finished and triggers the outgoing <see cref="EndTrigger"/>.
        /// Implementations must call this method exactly once when their work is complete.
        /// </summary>
        protected void FinishExecute()
        {
            if (ActivityFlag == StoryTaskActivityFlag.Inactive)
            {
                Debug.LogWarning($"[StoryTool] StoryLine '{GetType().Name}' FinishExecute was called while it is already inactive (ActivityFlag == {StoryTaskActivityFlag.Inactive}).");
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
