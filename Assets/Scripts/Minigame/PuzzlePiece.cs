using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(WorldPuzzleDragObject))]
public class PuzzlePiece : MonoBehaviour
{
    // Scene-unload destroys every piece (OnDestroy) before the next load's Awake runs,
    // so this resets itself naturally on minigame replay.
    private static readonly List<PuzzlePiece> allPieces = new();
    private static readonly HashSet<Transform> occupiedSlots = new();

    private WorldPuzzleDragObject drag;
    private Camera cam;
    private Collider2D pieceCollider;

    private bool isDragging;

    [Header("Puzzle Settings")]
    [SerializeField] private Transform correctSlot;
    [SerializeField] private float snapDistance = 0.75f;
    [SerializeField] private bool lockIntoPlace = true;

    private bool isLockedInPlace;
    private Transform lockedSlot;

    public bool IsLockedInPlace => isLockedInPlace;

    public System.Action<PuzzlePiece> OnLockedInPlace;

    private void Awake()
    {
        drag = GetComponent<WorldPuzzleDragObject>();
        pieceCollider = GetComponent<Collider2D>();
        cam = Camera.main;
        allPieces.Add(this);
    }

    private void OnDestroy()
    {
        allPieces.Remove(this);
        if (lockedSlot != null)
        {
            occupiedSlots.Remove(lockedSlot);
        }
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
        if (isLockedInPlace) return;

        Transform targetSlot = FindNearestAvailableSlot();
        if (targetSlot == null) return;

        transform.position = targetSlot.position;

        isLockedInPlace = true;
        lockedSlot = targetSlot;
        occupiedSlots.Add(targetSlot);

        OnLockedInPlace?.Invoke(this);

        if (lockIntoPlace)
        {
            drag.enabled = false;
            enabled = false;

            // Locked pieces no longer need to receive clicks, and leaving their
            // collider on lets it swallow clicks meant for whatever gets dropped
            // on top of it later, which is what made pieces feel permanently stuck.
            if (pieceCollider)
            {
                pieceCollider.enabled = false;
            }
        }
    }

    private Transform FindNearestAvailableSlot()
    {
        Transform best = null;
        float bestDistance = snapDistance;

        for (int i = 0; i < allPieces.Count; i++)
        {
            Transform slot = allPieces[i] ? allPieces[i].correctSlot : null;
            if (slot == null || occupiedSlots.Contains(slot)) continue;

            float distance = Vector2.Distance(transform.position, slot.position);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = slot;
            }
        }

        return best;
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

        // OverlapPoint only returns a single, arbitrarily-chosen collider, so when two
        // unlocked pieces overlap it can keep resolving to the same one and make the
        // other permanently unclickable. OverlapPointAll lets each piece check for
        // itself regardless of which other piece is also under the pointer.
        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject == gameObject)
            {
                return true;
            }
        }

        return false;
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