using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StoryTool.Samples.VisualNovel
{
    public class CharacterService : MonoBehaviour
    {
        [SerializeField]
        private RectTransform characterLayer;
        
        [SerializeField]
        private RectTransform leftPosition;

        [SerializeField]
        private RectTransform leftPositionOutside;

        [SerializeField]
        private RectTransform rightPosition;

        [SerializeField]
        private RectTransform rightPositionOutside;

        private Dictionary<CharacterData, Image> characterInstances = new Dictionary<CharacterData, Image>();

        public void ShowCharacter(CharacterData characterData, 
            CharacterScenePosition scenePosition, 
            Action onComplete = null, 
            float transitionDuration = 1f)
        {
            if (characterInstances.ContainsKey(characterData))
            {
                throw new Exception("Character already shown");
            }

            var characterPrefab = Resources.Load<Image>("StoryToolVisualNovelSample/Character");
            var characterInstance = Instantiate(characterPrefab, characterLayer);
            characterInstance.sprite = characterData.sprite;
            characterInstances.Add(characterData, characterInstance);

            (Vector3 sourcePosition, Vector3 targetPosition) = scenePosition switch
            {
                CharacterScenePosition.Left => (leftPositionOutside.position, leftPosition.position),
                CharacterScenePosition.Right => (rightPositionOutside.position, rightPosition.position),
                _ => throw new ArgumentOutOfRangeException(nameof(scenePosition), scenePosition, null)
            };
            
            characterInstance.StartCoroutine(AnimateCharacterRoutine(characterInstance, 
                sourcePosition, 
                targetPosition, 
                onComplete, 
                transitionDuration));
        }

        public void HideCharacter(CharacterData characterData, Action onComplete = null, float transitionDuration = 1f)
        {
            if (!characterInstances.TryGetValue(characterData, out var characterInstance))
            {
                throw new Exception("Character not shown");
            }

            var closestOutsidePoint = new []{leftPositionOutside, rightPositionOutside}
                .OrderBy(rt => Vector3.Distance(characterInstance.transform.position, rt.position))
                .First();

            characterInstances.Remove(characterData);

            onComplete += () => Destroy(characterInstance.gameObject);
            characterInstance.StartCoroutine(AnimateCharacterRoutine(characterInstance,
                characterInstance.transform.position,
                closestOutsidePoint.position,
                onComplete,
                transitionDuration));
        }

        private IEnumerator AnimateCharacterRoutine(Image characterInstance,
            Vector3 sourcePosition,
            Vector3 targetPosition, 
            Action onComplete, 
            float transitionDuration)
        {
            var totalTime = 0f;
            while(totalTime < transitionDuration)
            {
                totalTime += Time.deltaTime;
                var newPosition = Vector3.Lerp(sourcePosition, targetPosition, totalTime / transitionDuration);
                characterInstance.transform.position = newPosition;
                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
