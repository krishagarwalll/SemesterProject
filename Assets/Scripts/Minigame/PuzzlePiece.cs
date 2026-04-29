using UnityEngine;

[RequireComponent(typeof(WorldPuzzleDragObject))]
public class PuzzlePiece : MonoBehaviour
{
    private WorldPuzzleDragObject drag;
    private Camera cam;

    private bool isDragging;

    void Awake()
    {
        drag = GetComponent<WorldPuzzleDragObject>();
        cam = Camera.main;
    }

    void OnMouseDown()
    {
        if (PauseService.IsGameplayInputPaused(this)) return;
        if (!drag.CanStartDrag()) return;

        isDragging = true;
        drag.BeginDrag(GetMouseWorld());
    }

    void OnMouseDrag()
    {
        if (PauseService.IsGameplayInputPaused(this))
        {
            CancelDrag();
            return;
        }

        if (!isDragging) return;

        drag.UpdateDrag(GetMouseWorld());
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;
        drag.CompleteDrag();
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

    Vector3 GetMouseWorld()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(mouse);
    }
}
