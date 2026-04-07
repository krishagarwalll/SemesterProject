using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScreenFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Graphic fadeGraphic;
    [SerializeField] private bool blocksRaycastsWhileVisible = true;

    private CanvasGroup Group => this.ResolveComponent(ref canvasGroup);
    private Graphic FadeGraphic => this.ResolveComponent(ref fadeGraphic, true);

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        fadeGraphic = GetComponentInChildren<Graphic>(true);
    }

    private void Awake()
    {
        SetAlpha(Group ? Group.alpha : FadeGraphic ? FadeGraphic.color.a : 0f);
    }

    public Coroutine FadeOut(float duration)
    {
        return StartCoroutine(FadeTo(1f, duration));
    }

    public Coroutine FadeIn(float duration)
    {
        return StartCoroutine(FadeTo(0f, duration));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = Group.alpha;
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (Group)
        {
            Group.alpha = alpha;
            Group.blocksRaycasts = blocksRaycastsWhileVisible && alpha > 0.001f;
            Group.interactable = false;
        }

        if (FadeGraphic)
        {
            Color color = FadeGraphic.color;
            color.a = Mathf.Clamp01(alpha);
            FadeGraphic.color = color;
            FadeGraphic.raycastTarget = blocksRaycastsWhileVisible && alpha > 0.001f;
        }
    }
}
