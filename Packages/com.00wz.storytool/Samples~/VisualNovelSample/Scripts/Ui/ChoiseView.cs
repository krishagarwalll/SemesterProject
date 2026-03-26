using System;
using UnityEngine;
using UnityEngine.UI;

namespace StoryTool.Samples.VisualNovel
{
    public class ChoiseView : MonoBehaviour
    {
        [SerializeField]
        private Text text;

        [SerializeField]
        private Button button;

        public void SetUp(Action onClick, string text)
        {
            button.onClick.AddListener(new UnityEngine.Events.UnityAction(onClick));
            this.text.text = text;
        }
    }
}
