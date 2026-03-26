using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StoryTool.Samples.VisualNovel
{
    public class SceneService : MonoBehaviour
    {
        [SerializeField]
        private Image background;

        void Awake()
        {
            SetBackgroundAlpha(0f);
        }

        public void ShowScene(SceneData sceneData, Action onComplete = null, float transitionDuration = 2f)
        {
            StartCoroutine(ShowSceneRoutine(sceneData, onComplete, transitionDuration));
        }

        private IEnumerator ShowSceneRoutine(SceneData sceneData, Action onComplete, float transitionDuration)
        {
            transitionDuration /= 2f;

            while(background.color.a != 0f)
            {
                var newBackgroundAlpha = 
                    Mathf.MoveTowards(background.color.a, 0f, Time.deltaTime / transitionDuration);
                SetBackgroundAlpha(newBackgroundAlpha);
                yield return null;
            }

            background.sprite = sceneData.sprite;

            while(background.color.a != 1f)
            {
                
                var newBackgroundAlpha = 
                    Mathf.MoveTowards(background.color.a, 1f, Time.deltaTime / transitionDuration);
                SetBackgroundAlpha(newBackgroundAlpha);
                yield return null;
            }

            onComplete?.Invoke();
        }

        private void SetBackgroundAlpha(float alpha)
        {
            var tmpColor = background.color;
            tmpColor.a = alpha;
            background.color = tmpColor;
        }
    }
}
