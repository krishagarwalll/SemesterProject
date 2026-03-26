using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    [StoryTaskMenu("Samples/VisualNovel/ShowCharacter")]
    public class ShowCharacter : StoryLine
    {
        [SerializeField]
        private CharacterData characterData;

        [SerializeField]
        private CharacterScenePosition position;
        
        protected override void ReceiveExecute()
        {
            Narrative.ShowCharacter(characterData, position, FinishExecute);
        }
    }
}