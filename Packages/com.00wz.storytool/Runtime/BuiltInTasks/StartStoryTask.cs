using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    [StoryTaskMenu("BuiltIn/Start")]
    public class StartStoryTask : StoryTask
    {
        [SerializeField] EndTrigger start;

        protected override void OnStart()
        {
            start.Trigger();
        }
    }
}
