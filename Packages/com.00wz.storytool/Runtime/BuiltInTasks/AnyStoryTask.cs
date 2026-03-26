using System.Collections.Generic;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    /// <summary>
    /// Activates the output every time any input trigger fires.
    /// </summary>
    [StoryTaskMenu("BuiltIn/Any")]
    public class AnyStoryTask : StoryTask
    {
        [SerializeField]
        private List<StartTrigger> inputs;

        [SerializeField]
        private EndTrigger output;

        protected override void OnAwake()
        {
            foreach (var st in inputs)
            {
                if (st == null)
                {
                    continue;
                }

                st.Triggered += () =>
                {
                    ActivityFlag = StoryTaskActivityFlag.Active;
                    output.Trigger();
                    ActivityFlag = StoryTaskActivityFlag.Inactive;
                };
            }
        }
    }
}