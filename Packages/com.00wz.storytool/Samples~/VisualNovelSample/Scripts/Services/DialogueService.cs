using System;
using UnityEngine;
using UnityEngine.UI;

namespace StoryTool.Samples.VisualNovel
{
    public class DialogueService : MonoBehaviour
    {
        [SerializeField]
        private Button dialoguePanel;

        [SerializeField]
        private GameObject arrow;

        [SerializeField]
        private Text headerText;

        [SerializeField]
        private Text bodyText;

        [SerializeField]
        private RectTransform choisesRoot;

        private ChoiseView _choisePrefab;

        void Awake()
        {
            _choisePrefab = Resources.Load<ChoiseView>("StoryToolVisualNovelSample/Choise");
            dialoguePanel.gameObject.SetActive(false);
        }

        public void ShowDialogue(string header, string body, Action onContinue)
        {
            if (dialoguePanel.gameObject.activeSelf)
            {
                throw new Exception("Dialogue is already shown");
            }

            dialoguePanel.gameObject.SetActive(true);
            arrow.SetActive(true);

            headerText.gameObject.SetActive(!string.IsNullOrEmpty(header));
            bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));

            headerText.text = header;
            bodyText.text = body;

            dialoguePanel.onClick.AddListener(() => OnDialogueComplete(onContinue));
        }

        public void ShowDialogue(string header, 
            string body,
            params (string choiseText, Action choiseAction)[] choises)
        {
            if (dialoguePanel.gameObject.activeSelf)
            {
                throw new Exception("Dialogue is already shown");
            }

            dialoguePanel.gameObject.SetActive(true);
            arrow.SetActive(false);

            headerText.gameObject.SetActive(!string.IsNullOrEmpty(header));
            bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));

            headerText.text = header;
            bodyText.text = body;

            foreach (var choise in choises)
            {
                var choiseView = Instantiate(_choisePrefab, choisesRoot);
                choiseView.SetUp(() => OnChoosing(choise.choiseAction), choise.choiseText);
            }
        }

        private void OnDialogueComplete(Action onContinue)
        {
            dialoguePanel.gameObject.SetActive(false);
            dialoguePanel.onClick.RemoveAllListeners();

            onContinue?.Invoke();
        }

        private void OnChoosing(Action onContinue)
        {
            dialoguePanel.gameObject.SetActive(false);
            for (int i = 0; i < choisesRoot.childCount; i++) 
            {
                Destroy(choisesRoot.GetChild(i).gameObject);
            }
            
            onContinue?.Invoke();
        }
    }
}
