using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class DragDropHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;

    [Header("Raycast")]
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private bool ignorePointerOverUI = true;

    private WorldPuzzleDragObject activeDraggedObject;

    public bool IsDragging => activeDraggedObject != null;
    public WorldPuzzleDragObject ActiveDraggedObject => activeDraggedObject;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Mouse.current == null || worldCamera == null)
        {
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = GetWorldPosition(screenPosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDrag(worldPosition);
        }

        if (!IsDragging)
        {
            return;
        }

        activeDraggedObject.UpdateDrag(worldPosition);

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            activeDraggedObject.CompleteDrag();
            activeDraggedObject = null;
        }
    }

    public void CancelCurrentDrag()
    {
        if (!IsDragging)
        {
            return;
        }

        activeDraggedObject.CancelDrag();
        activeDraggedObject = null;
    }

    private void TryStartDrag(Vector3 worldPosition)
    {
        if (ignorePointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        WorldPuzzleDragObject dragObject = FindObjectAtPoint(worldPosition);
        if (dragObject == null || !dragObject.CanStartDrag())
        {
            return;
        }

        activeDraggedObject = dragObject;
        activeDraggedObject.BeginDrag(worldPosition);
    }

    private WorldPuzzleDragObject FindObjectAtPoint(Vector3 worldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition, interactableLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            WorldPuzzleDragObject dragObject = hit.GetComponentInParent<WorldPuzzleDragObject>();
            if (dragObject != null)
            {
                return dragObject;
            }
        }

        return null;
    }

    private Vector3 GetWorldPosition(Vector2 screenPosition)
    {
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(worldCamera.transform.position.z)));
        worldPosition.z = 0f;
        return worldPosition;
    }
}
