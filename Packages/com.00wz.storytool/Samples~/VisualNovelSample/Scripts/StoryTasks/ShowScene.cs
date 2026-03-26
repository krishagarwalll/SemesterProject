using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    [StoryTaskMenu("Samples/VisualNovel/ShowScene")]
    public class ShowScene : StoryLine
    {
        [SerializeField]
        private SceneData sceneData;
        protected override void ReceiveExecute()
        {
            Narrative.ShowScene(sceneData, FinishExecute);
        }
    }
}