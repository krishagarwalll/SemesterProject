using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(WorldPuzzleDragObject))]
public class PuzzlePiece : MonoBehaviour
{
    private WorldPuzzleDragObject drag;
    private Camera cam;

    private bool isDragging;

    [Header("Puzzle Settings")]
    [SerializeField] private Transform correctSlot;
    [SerializeField] private float snapDistance = 0.75f;
    [SerializeField] private bool lockIntoPlace = true;

    private bool isLockedInPlace;

    public bool IsLockedInPlace => isLockedInPlace;

    public System.Action<PuzzlePiece> OnLockedInPlace;

    private void Awake()
    {
        drag = GetComponent<WorldPuzzleDragObject>();
        cam = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDrag();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            ContinueDrag();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }
    }

    private void TryStartDrag()
    {
        if (isLockedInPlace) return;
        if (PauseService.IsGameplayInputPaused(this)) return;
        if (!drag.CanStartDrag()) return;
        if (!MouseIsOverThisPiece()) return;

        Debug.Log($"Clicked puzzle piece: {name}");

        isDragging = true;
        drag.BeginDrag(GetMouseWorld());
    }

    private void ContinueDrag()
    {
        if (!isDragging) return;

        if (PauseService.IsGameplayInputPaused(this))
        {
            CancelDrag();
            return;
        }

        drag.UpdateDrag(GetMouseWorld());
    }

    private void EndDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        drag.CompleteDrag();

        TrySnap();
    }

    private void TrySnap()
    {
        if (correctSlot == null) return;
        if (isLockedInPlace) return;

        float distance = Vector2.Distance(transform.position, correctSlot.position);

        if (distance <= snapDistance)
        {
            transform.position = correctSlot.position;

            isLockedInPlace = true;

            Debug.Log($"{name} snapped into its correct slot!");

            OnLockedInPlace?.Invoke(this);

            if (lockIntoPlace)
            {
                drag.enabled = false;
                enabled = false;
            }
        }
    }

    private void OnDisable()
    {
        CancelDrag();
    }

    private void CancelDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        drag.CancelDrag();
    }

    private bool MouseIsOverThisPiece()
    {
        Vector2 mouseWorld = GetMouseWorld();

        Collider2D hit = Physics2D.OverlapPoint(mouseWorld);

        return hit != null && hit.gameObject == gameObject;
    }

    private Vector3 GetMouseWorld()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        Vector3 mouse = new Vector3(
            mouseScreen.x,
            mouseScreen.y,
            Mathf.Abs(cam.transform.position.z)
        );

        return cam.ScreenToWorldPoint(mouse);
    }
}