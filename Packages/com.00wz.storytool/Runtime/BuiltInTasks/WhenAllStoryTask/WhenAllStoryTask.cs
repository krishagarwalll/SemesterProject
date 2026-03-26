using System;
using System.Collections.Generic;
using System.Linq;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    [StoryTaskMenu("BuiltIn/WhenAll")]
    public class WhenAllStoryTask : StoryTask
    {
        [SerializeField]
        private List<StartTriggerWithFlag> inputs;

        [SerializeField]
        private EndTrigger output;

        private bool _isCompleted;

        protected override void OnAwake()
        {
            _isCompleted = false;

            foreach (var st in inputs)
            {
                Action triggeredAction = () =>
                {
                    if (_isCompleted)
                    {
                        return;
                    }

                    st.IsTriggered = true;

                    if (inputs.All(t => t.IsTriggered))
                    {
                        ActivityFlag = StoryTaskActivityFlag.Active;
                        output.Trigger();
                        ActivityFlag = StoryTaskActivityFlag.Completed;
                        _isCompleted = true;
                    }
                };

                st.Trigger.Triggered += triggeredAction;
            }
        }
    }
}
