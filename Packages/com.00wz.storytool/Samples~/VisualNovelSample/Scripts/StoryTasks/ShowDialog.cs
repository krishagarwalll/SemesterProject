using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    [StoryTaskMenu("Samples/VisualNovel/ShowDialog")]
    public class ShowDialog : StoryLine
    {
        [SerializeField]
        private CharacterData speaker;

        [TextArea(3, 5)]
        [SerializeField]
        private string text;
        protected override void ReceiveExecute()
        {
            Narrative.ShowDialogue(speaker?.characterName, text, FinishExecute);
        }
    }
}