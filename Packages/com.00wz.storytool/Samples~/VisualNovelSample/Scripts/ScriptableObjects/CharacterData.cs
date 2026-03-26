using UnityEngine;

[CreateAssetMenu(menuName = "VisualNovel/CharacterData")]
public class CharacterData : ScriptableObject
{
    [field: SerializeField]
    public string characterName { get; private set; }

    [field: SerializeField]
    public Sprite sprite { get; private set; }
}
