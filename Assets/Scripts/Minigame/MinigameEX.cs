using UnityEngine;

public class MinigameEX : MonoBehaviour, IInteractable
{
    public bool IsCompleted { get; private set; }

    [SerializeField] private GameObject minigameRoot;
    [SerializeField] private Sprite completedMinigameSprite;

    public void Interact()
    {
        if (IsCompleted) return;

        OpenGame();
    }

    public string GetInteractiveText()
    {
        return IsCompleted ? "Completed" : "Play Minigame";
    }

    public void OpenGame()
    {
        minigameRoot.SetActive(true);
    }

    public void CompleteMinigame()
    {
        IsCompleted = true;

        GetComponent<SpriteRenderer>().sprite = completedMinigameSprite;

        minigameRoot.SetActive(false);
    }
}