using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float duration = 60f;
    [SerializeField] private string nextSceneName = "Sprint3";
    [Tooltip("If true, restore from SaveManager when the timer ends instead of just loading nextSceneName. " +
             "Use this for the camera minigame so the player returns to Sprint3 with inventory, quests, and position intact.")]
    [SerializeField] private bool restoreFromSave = true;
    [Tooltip("Optional. If set, this quest is marked as handed in (in the save file) right before reloading. " +
             "Use this for quests that complete by surviving the minigame.")]
    [SerializeField] private Quest questToCompleteOnTimerEnd;

    private float timeRemaining;
    private bool finished;

    private void Start()
    {
        timeRemaining = duration;
        UpdateText();
    }

    private void Update()
    {
        if (finished) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            finished = true;
            UpdateText();

            if (restoreFromSave && SaveManager.Instance != null && SaveManager.Instance.HasSave())
            {
                if (questToCompleteOnTimerEnd != null)
                {
                    SaveManager.Instance.MarkQuestHandedInInSave(questToCompleteOnTimerEnd.questID);
                }
                SaveManager.Instance.LoadAndApply();
            }
            else
            {
                StartCoroutine(ScreenFade.FadeOutThenLoad(this, nextSceneName));
            }
            return;
        }
        UpdateText();
    }

    private void UpdateText()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes}:{seconds:D2}";
    }
}
