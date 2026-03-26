using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    [StoryTaskMenu("Samples/VisualNovel/HideCharacter")]
    public class HideCharacter : StoryLine
    {
        [SerializeField]
        private CharacterData characterData;

        protected override void ReceiveExecute()
        {
            Narrative.HideCharacter(characterData, FinishExecute);
        }
    }
}