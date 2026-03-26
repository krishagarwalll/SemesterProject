using System;
using System.Collections.Generic;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    [StoryTaskMenu("BuiltIn/Branch")]
    public class BranchStoryTask : StoryTask
    {
        [SerializeField] private StartTrigger input;

        [SerializeField] private List<EndTrigger> outputs;

        protected override void OnAwake()
        {
            input.Triggered += ActivateTask;
        }

        private void ActivateTask()
        {
            ActivityFlag = StoryTaskActivityFlag.Active;
            foreach (var et in outputs)
            {
                try
                {
                    et.Trigger();
                }
                catch(Exception e)
                {
                    Debug.LogError(e);
                }
            }
            ActivityFlag = StoryTaskActivityFlag.Inactive;
        }
    }
}
