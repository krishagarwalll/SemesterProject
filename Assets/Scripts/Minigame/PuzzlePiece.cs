using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(WorldPuzzleDragObject))]
public class PuzzlePiece : MonoBehaviour
{
    private WorldPuzzleDragObject drag;
    private Camera cam;

    private bool isDragging;
    private bool pointerIsOverThisPiece;
    
    [SerializeField] private Transform correctSlot;
    [SerializeField] private float snapDistance = 0.75f;
    [SerializeField] private bool lockIntoPlace = true;

    void Awake()
    {
        drag = GetComponent<WorldPuzzleDragObject>();
        cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryStartDrag();

        if (Mouse.current.leftButton.isPressed)
            ContinueDrag();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    void TryStartDrag()
    {
        if (PauseService.IsGameplayInputPaused(this)) return;
        if (!drag.CanStartDrag()) return;
        if (!MouseIsOverThisPiece()) return;

        Debug.Log("Clicked puzzle piece");

        isDragging = true;
        drag.BeginDrag(GetMouseWorld());
    }

    void ContinueDrag()
    {
        if (!isDragging) return;

        if (PauseService.IsGameplayInputPaused(this))
        {
            CancelDrag();
            return;
        }

        drag.UpdateDrag(GetMouseWorld());
    }

    void EndDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        drag.CompleteDrag();

        TrySnap();
    }
    
    private void TrySnap()
    {
        if (correctSlot == null) return;

        float distance = Vector2.Distance(transform.position, correctSlot.position);

        if (distance <= snapDistance)
        {
            transform.position = correctSlot.position;

            if (lockIntoPlace)
            {
                drag.enabled = false;
                enabled = false;
            }

            Debug.Log($"{name} snapped into its correct slot!");
        }
    }

   
    
    private void SnapToPoint(PuzzleSnapPoint myPoint, PuzzleSnapPoint targetPoint)
    {
        Vector3 offset = targetPoint.transform.position - myPoint.transform.position;

        transform.position += offset;

        myPoint.isConnected = true;
        targetPoint.isConnected = true;

        Debug.Log("Pieces snapped together!");
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

    Vector3 GetMouseWorld()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        Vector3 mouse = new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(cam.transform.position.z));

        return cam.ScreenToWorldPoint(mouse);
    }
}