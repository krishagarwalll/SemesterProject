using System;
using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    public class Narrative : MonoBehaviour
    {
        [SerializeField]
        private SceneService sceneService;

        [SerializeField]
        private CharacterService characterService;

        [SerializeField]
        private DialogueService dialogueService;

        [SerializeField]
        private BackgroundSoundService backgroundSoundService;
        
        private static Narrative _instance;
        private static Narrative Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<Narrative>();
                }
                if (_instance == null)
                {
                    throw new System.Exception("No Narrative instance found");
                }

                return _instance;
            }
        }

        public static void ShowScene(SceneData sceneData, Action onComplete = null, float transitionDuration = 2f)
            => Instance.sceneService.ShowScene(sceneData, onComplete, transitionDuration);

        public static void ShowCharacter(CharacterData characterData, 
            CharacterScenePosition scenePosition, 
            Action onComplete = null, 
            float transitionDuration = 1f) 
            => Instance.characterService.ShowCharacter(characterData, scenePosition, onComplete, transitionDuration);

        public static void HideCharacter(CharacterData characterData, 
            Action onComplete = null, 
            float transitionDuration = 1f)
            => Instance.characterService.HideCharacter(characterData, onComplete, transitionDuration);

        public static void ShowDialogue(string header, string body, Action onContinue)
            => Instance.dialogueService.ShowDialogue(header, body, onContinue);

        public static void ShowDialogue(string header, 
            string body,
            params (string choiseText, Action choiseAction)[] choises)
            => Instance.dialogueService.ShowDialogue(header, body, choises);

        public static void PlayBackgroundSound(AudioClip clip)
            => Instance.backgroundSoundService.Play(clip);
    }
}
