using UnityEngine;

[DisallowMultipleComponent]
public class AutoSaveIndicator : MonoBehaviour
{
    [SerializeField, Min(0f)] private float visibleSeconds = 1.5f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.35f;
    [SerializeField] private CanvasGroup group;

    private float visibleTimer;
    private float alpha;

    private void Awake()
    {
        if (!group)
        {
            group = GetComponentInChildren<CanvasGroup>(true);
        }

        SetAlpha(0f);
    }

    private void OnEnable()
    {
        SaveManager.SaveWritten += Show;
    }

    private void OnDisable()
    {
        SaveManager.SaveWritten -= Show;
    }

    private void Update()
    {
        if (!group)
        {
            return;
        }

        if (visibleTimer > 0f)
        {
            visibleTimer -= Time.unscaledDeltaTime;
            alpha = Mathf.MoveTowards(alpha, 1f, Time.unscaledDeltaTime / fadeSeconds);
        }
        else
        {
            alpha = Mathf.MoveTowards(alpha, 0f, Time.unscaledDeltaTime / fadeSeconds);
        }

        SetAlpha(alpha);
    }

    private void Show()
    {
        visibleTimer = visibleSeconds;
        enabled = true;
    }

    private void SetAlpha(float value)
    {
        if (!group)
        {
            return;
        }

        group.alpha = value;
    }
}
