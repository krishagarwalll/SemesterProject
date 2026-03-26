using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    [CreateAssetMenu(menuName = "VisualNovel/SceneData")]
    public class SceneData : ScriptableObject
    {
        [field: SerializeField]
        public Sprite sprite {get; private set;}
    }
}
