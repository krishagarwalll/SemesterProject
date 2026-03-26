using System;
using System.Linq;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    [StoryTaskMenu("Samples/VisualNovel/ShowDialogWithChoises")]
    public class ShowDialogWithChoises : StoryTask
    {
        [SerializeField]
        private StartTrigger start;

        [SerializeField]
        private CharacterData speaker;

        [TextArea(3, 5)]
        [SerializeField]
        private string text;

        [SerializeField]
        private Choise[] choises;

        protected override void OnAwake()
        {
            start.Triggered += ReceiveExecute;
        }

        private void ReceiveExecute()
        {
            ActivityFlag = StoryTaskActivityFlag.Active;
            
            Narrative.ShowDialogue(speaker?.characterName,
                text,
                choises.Select<Choise, (string, Action)>(c => (c.text, () => Select(c))).ToArray());
        }

        private void Select(Choise choise)
        {
            ActivityFlag = StoryTaskActivityFlag.Inactive;
            choise.selectTrigger.Trigger();
        }
    }

    [Serializable]
    public class Choise
    {
        [TextArea(0, 5)]
        public string text;
        public EndTrigger selectTrigger;
    }
}