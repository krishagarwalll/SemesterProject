using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float duration = 60f;
    [SerializeField] private string nextSceneName = "Sprint3";

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
            SceneManager.LoadScene(nextSceneName);
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
