using System.Collections;
using UnityEngine;

/// <summary>
/// Place on a trigger collider that acts as a vertical "line" in a room.
/// When the player crosses it moving forward (to the right of the trigger),
/// the camera smoothly zooms out. When the player crosses back (to the left),
/// the camera zooms back in to its previous size.
///
/// Drives <see cref="RoomTransitionService.SetUserZoom"/> so it reuses the
/// existing room camera pipeline (the controller writes orthographic size every
/// LateUpdate, so changing the camera directly would be overwritten).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CameraZoomTrigger : MonoBehaviour
{
    [SerializeField] private RoomTransitionService roomTransitions;
    [SerializeField] private string playerTag = "Player";

    [Header("Zoom")]
    [Tooltip("If true, crossing forward zooms out as far as the RoomTransitionService allows (fully out).")]
    [SerializeField] private bool zoomOutToMax = true;
    [Tooltip("Orthographic size after crossing forward when 'Zoom Out To Max' is off. Larger = more zoomed out.")]
    [SerializeField, Min(0.5f)] private float zoomedOutSize = 3f;
    [Tooltip("Orthographic size after crossing back. If <= 0, restores the zoom level from before the first crossing.")]
    [SerializeField] private float zoomedInSize = -1f;
    [Tooltip("Seconds to interpolate between zoom levels. 0 = instant.")]
    [SerializeField, Min(0f)] private float zoomDuration = 0.6f;
    [Tooltip("Easing over the zoom. X = normalized time (0..1), Y = progress (0..1). Default eases in (slow start, fast finish).")]
    [SerializeField] private AnimationCurve zoomCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 0f));

    private float defaultSize;
    private bool capturedDefault;
    private Coroutine zoomRoutine;

    private RoomTransitionService Transitions => roomTransitions
        ? roomTransitions
        : roomTransitions = FindFirstObjectByType<RoomTransitionService>(FindObjectsInactive.Include);

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        RoomTransitionService transitions = Transitions;
        if (!transitions)
        {
            return;
        }

        if (!capturedDefault)
        {
            defaultSize = transitions.UserOrthographicSize;
            capturedDefault = true;
        }

        bool crossedForward = other.transform.position.x > transform.position.x;
        float target = crossedForward
            ? (zoomOutToMax ? transitions.MaxOrthographicSize : zoomedOutSize)
            : (zoomedInSize > 0f ? zoomedInSize : defaultSize);

        // Lerp toward the value the service will actually settle on, so the easing
        // doesn't saturate against the clamp before the curve finishes.
        target = Mathf.Clamp(target, transitions.MinOrthographicSize, transitions.MaxOrthographicSize);

        StartZoom(target);
    }

    private void StartZoom(float target)
    {
        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
            zoomRoutine = null;
        }

        if (zoomDuration <= 0f || !isActiveAndEnabled)
        {
            Transitions.SetUserZoom(target);
            return;
        }

        zoomRoutine = StartCoroutine(ZoomRoutine(target));
    }

    private IEnumerator ZoomRoutine(float target)
    {
        RoomTransitionService transitions = Transitions;
        float start = transitions.UserOrthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomDuration);
            float k = zoomCurve != null && zoomCurve.length > 0 ? zoomCurve.Evaluate(t) : t;
            transitions.SetUserZoom(Mathf.Lerp(start, target, k));
            yield return null;
        }

        transitions.SetUserZoom(target);
        zoomRoutine = null;
    }
}
