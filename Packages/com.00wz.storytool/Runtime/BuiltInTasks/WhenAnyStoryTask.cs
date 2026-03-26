using System.Collections.Generic;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    [StoryTaskMenu("BuiltIn/WhenAny")]
    public class WhenAnyStoryTask : StoryTask
    {
        [SerializeField]
        private List<StartTrigger> inputs;

        [SerializeField]
        private EndTrigger output;

        private bool _isCompleted;

        protected override void OnAwake()
        {
            _isCompleted = false;

            foreach (var st in inputs)
            {
                st.Triggered += () =>
                {
                    if (_isCompleted)
                    {
                        return;
                    }

                    ActivityFlag = StoryTaskActivityFlag.Active;
                    output.Trigger();
                    ActivityFlag = StoryTaskActivityFlag.Completed;
                    _isCompleted = true;
                };
            }
        }
    }
}
