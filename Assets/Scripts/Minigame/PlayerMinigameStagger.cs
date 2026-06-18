using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class PlayerMinigameStagger : MonoBehaviour
{
    [Header("Stagger")]
    [SerializeField, Min(0f)] private float staggerDuration = 0.35f;
    [SerializeField, Min(0f)] private float knockbackDistance = 1.1f;
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.6f;
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Vignette (URP Post-Processing)")]
    [Tooltip("A Volume in the scene whose profile has a Vignette override. The Vignette intensity is animated from 0 → max → 0 on each stagger.")]
    [SerializeField] private Volume vignetteVolume;
    [Tooltip("Overrides the Vignette's color when staggered. Disable to keep whatever's in the profile.")]
    [SerializeField] private bool overrideVignetteColor = true;
    [SerializeField] private Color vignetteColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField, Range(0f, 1f)] private float vignetteMaxIntensity = 0.55f;
    [SerializeField, Min(0f)] private float vignetteFadeOutDuration = 0.4f;

    private bool invulnerable;
    private Coroutine staggerRoutine;
    private Vignette vignetteOverride;

    public bool IsInvulnerable => invulnerable;

    private void Awake()
    {
        CacheVignetteOverride();
    }

    public void Stagger(Vector2 hitSourcePosition)
    {
        if (invulnerable) return;

        Vector2 pushDir = (Vector2)transform.position - hitSourcePosition;
        if (pushDir.sqrMagnitude < 0.0001f) pushDir = Vector2.right;
        pushDir.Normalize();

        if (staggerRoutine != null) StopCoroutine(staggerRoutine);
        staggerRoutine = StartCoroutine(RunStagger(pushDir));
    }

    private IEnumerator RunStagger(Vector2 pushDir)
    {
        invulnerable = true;
        PauseService.Pause(PauseType.Input);

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (Vector3)(pushDir * knockbackDistance);

        ApplyVignetteIntensity(vignetteMaxIntensity);
        if (overrideVignetteColor && vignetteOverride != null)
        {
            vignetteOverride.color.overrideState = true;
            vignetteOverride.color.value = vignetteColor;
        }

        float elapsed = 0f;
        while (elapsed < staggerDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / staggerDuration);
            float eased = knockbackCurve.Evaluate(t);
            transform.position = Vector3.LerpUnclamped(startPos, endPos, eased);
            yield return null;
        }
        transform.position = endPos;

        PauseService.Resume(PauseType.Input);

        float fade = 0f;
        float fadeDuration = Mathf.Max(0.0001f, vignetteFadeOutDuration);
        while (fade < fadeDuration)
        {
            fade += Time.deltaTime;
            float t = Mathf.Clamp01(fade / fadeDuration);
            ApplyVignetteIntensity(Mathf.Lerp(vignetteMaxIntensity, 0f, t));
            yield return null;
        }
        ApplyVignetteIntensity(0f);

        float remainingIFrames = invulnerabilityDuration - staggerDuration - vignetteFadeOutDuration;
        if (remainingIFrames > 0f) yield return new WaitForSeconds(remainingIFrames);

        invulnerable = false;
        staggerRoutine = null;
    }

    private void CacheVignetteOverride()
    {
        if (vignetteVolume == null || vignetteVolume.profile == null) return;
        if (!vignetteVolume.profile.TryGet(out vignetteOverride))
        {
            vignetteOverride = vignetteVolume.profile.Add<Vignette>(true);
        }
        ApplyVignetteIntensity(0f);
    }

    private void ApplyVignetteIntensity(float intensity)
    {
        if (vignetteOverride == null) return;
        vignetteOverride.intensity.overrideState = true;
        vignetteOverride.intensity.value = intensity;
        vignetteOverride.active = intensity > 0.001f;
    }
}
