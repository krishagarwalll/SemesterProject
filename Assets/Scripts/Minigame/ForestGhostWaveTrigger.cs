using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ForestGhostWaveTrigger : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float warningDuration = 5f;

    [Header("Warning Text")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField, TextArea] private string warningMessage =
        "An evil ghost wave has started!\nUse your camera by pressing [Right Mouse] to fend them off!";

    [Header("Countdown Text")]
    [Tooltip("Optional. Shows the remaining seconds before the wave starts.")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Scene")]
    [SerializeField] private string cameraMinigameSceneName = "CameraMinigame";

    [Header("Return")]
    [Tooltip("Optional. Where the player should reappear when they return from the minigame. " +
             "Leave empty to reuse the player's current position at the moment the trigger fires.")]
    [SerializeField] private Transform returnPoint;

    [Header("Quest")]
    [Tooltip("Optional. Quest accepted the moment the player enters the trigger.")]
    [SerializeField] private Quest questToStart;

    [Header("Behaviour")]
    [Tooltip("If true, this trigger won't re-fire after the player returns from the minigame scene.")]
    [SerializeField] private bool oneShotPerSession = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private static bool s_alreadyFired;
    private bool triggered;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private Collider2D selfCollider;

    private void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
        if (warningText != null) warningText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (debugLogs)
        {
            Bounds b = selfCollider != null ? selfCollider.bounds : new Bounds(transform.position, Vector3.zero);
            Debug.Log($"[GhostWave] Awake '{name}' worldPos={transform.position} colliderBounds=center{b.center} size{b.size} isTrigger={(selfCollider != null && selfCollider.isTrigger)} layer={gameObject.layer}", this);
        }
    }

    private void FixedUpdate()
    {
        if (!debugLogs || triggered || selfCollider == null) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll((Vector2)selfCollider.bounds.center, selfCollider.bounds.size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || hits[i] == selfCollider) continue;
            if (IsPlayer(hits[i]))
            {
                Debug.Log($"[GhostWave] OverlapBoxAll sees player collider '{hits[i].name}' but OnTriggerEnter2D never fired — physics filter problem.", this);
                break;
            }
        }
    }

    private static bool IsPlayer(Collider2D col)
    {
        if (col == null) return false;
        if (col.GetComponentInParent<PoptropicaController>() != null) return true;
        if (col.GetComponentInParent<PlayerController>() != null) return true;
        if (col.transform.root.CompareTag("Player")) return true;
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLogs) Debug.Log($"[GhostWave] OnTriggerEnter2D from '{other.name}' (layer={other.gameObject.layer})", this);

        if (triggered)
        {
            if (debugLogs) Debug.Log("[GhostWave] Skipped: already triggered this scene.");
            return;
        }
        if (oneShotPerSession && s_alreadyFired)
        {
            if (debugLogs) Debug.Log("[GhostWave] Skipped: oneShotPerSession + already fired earlier.");
            return;
        }

        if (!IsPlayer(other))
        {
            if (debugLogs) Debug.Log($"[GhostWave] Skipped: '{other.name}' is not the player.");
            return;
        }

        if (debugLogs) Debug.Log("[GhostWave] Player matched. Starting warning coroutine.");
        triggered = true;
        if (oneShotPerSession) s_alreadyFired = true;

        if (questToStart != null && QuestController.Instance != null
            && !QuestController.Instance.isQuestHandedIn(questToStart.questID)
            && !QuestController.Instance.isQuestActive(questToStart.questID))
        {
            QuestController.Instance.AcceptQuest(questToStart);
        }

        StartCoroutine(RunWarningThenLoadScene());
    }

    private IEnumerator RunWarningThenLoadScene()
    {
        if (warningText != null)
        {
            warningText.text = warningMessage;
            warningText.gameObject.SetActive(true);
        }

        if (countdownText != null) countdownText.gameObject.SetActive(true);

        float remaining = warningDuration;
        int lastSecondsLeft = -1;
        while (remaining > 0f)
        {
            int secondsLeft = Mathf.CeilToInt(remaining);
            if (countdownText != null)
                countdownText.text = secondsLeft.ToString();

            if (secondsLeft != lastSecondsLeft)
            {
                lastSecondsLeft = secondsLeft;
                if (questToStart != null && QuestController.Instance != null)
                {
                    QuestController.Instance.SetObjectiveDescription(questToStart.questID, 0, $"Starting in {secondsLeft}");
                }
            }

            yield return null;
            remaining -= Time.deltaTime;
        }

        if (warningText != null) warningText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        PauseService.ClearAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (SaveManager.Instance != null)
        {
            Vector3 savePos;
            if (returnPoint != null)
            {
                savePos = returnPoint.position;
            }
            else
            {
                var player = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Exclude);
                savePos = player != null ? player.transform.position : transform.position;
            }
            SaveManager.Instance.SaveWithPlayerPosition(savePos);
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[GhostWave] No SaveManager.Instance found — minigame return state will not be preserved.", this);
        }

        SceneManager.sceneLoaded -= ClearPauseAfterLoad;
        SceneManager.sceneLoaded += ClearPauseAfterLoad;
        SceneManager.LoadScene(cameraMinigameSceneName);
    }

    private static void ClearPauseAfterLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ClearPauseAfterLoad;
        PauseService.ClearAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
