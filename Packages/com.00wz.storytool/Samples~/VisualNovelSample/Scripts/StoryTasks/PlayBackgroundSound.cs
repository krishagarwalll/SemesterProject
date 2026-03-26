using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    [StoryTaskMenu("Samples/VisualNovel/PlayBackgroundSound")]
    public class PlayBackgroundSound : StoryPoint
    {
        [SerializeField]
        private AudioClip clip;

        protected override void ReceiveExecute()
        {
            Narrative.PlayBackgroundSound(clip);
        }
    }
}