using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    public static IEnumerator FadeOutThenLoad(MonoBehaviour runner, string sceneName, float duration = 0.25f)
    {
        ScreenFade fade = FindFirstObjectByType<ScreenFade>(FindObjectsInactive.Include) ?? CreateRuntimeFade();
        if (fade)
        {
            yield return fade.FadeOut(duration);
        }

        SceneManager.LoadScene(sceneName);
    }

    private static ScreenFade CreateRuntimeFade()
    {
        GameObject canvasObject = new("RuntimeScreenFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject fadeObject = new("ScreenFade", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(ScreenFade));
        RectTransform rect = fadeObject.GetComponent<RectTransform>();
        rect.SetParent(canvasObject.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = fadeObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        CanvasGroup group = fadeObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        ScreenFade fade = fadeObject.GetComponent<ScreenFade>();
        fade.canvasGroup = group;
        fade.fadeGraphic = image;
        return fade;
    }
}
