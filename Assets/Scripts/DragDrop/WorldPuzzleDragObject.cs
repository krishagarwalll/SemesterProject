using UnityEngine;

[DisallowMultipleComponent]
public class WorldPuzzleDragObject : MonoBehaviour
{
    [SerializeField] private bool canBeDragged = true;
    [SerializeField] private bool keepPointerOffset = true;
    [SerializeField] private bool disableRigidbodySimulationWhileDragging = true;

    private Rigidbody2D body2D;
    private Vector3 dragOffset;
    private bool startSimulationState;

    public bool CanBeDragged => canBeDragged;

    private void Awake()
    {
        body2D = GetComponent<Rigidbody2D>();
    }

    public bool CanStartDrag()
    {
        return canBeDragged && enabled && gameObject.activeInHierarchy;
    }

    public void BeginDrag(Vector3 pointerWorldPosition)
    {
        dragOffset = keepPointerOffset ? transform.position - pointerWorldPosition : Vector3.zero;

        if (disableRigidbodySimulationWhileDragging && body2D != null)
        {
            startSimulationState = body2D.simulated;
            body2D.simulated = false;
        }
    }

    public void UpdateDrag(Vector3 pointerWorldPosition)
    {
        Vector3 targetPosition = pointerWorldPosition + dragOffset;
        targetPosition.z = transform.position.z;
        transform.position = targetPosition;
    }

    public void CancelDrag()
    {
        RestorePhysicsState();
    }

    public void CompleteDrag()
    {
        RestorePhysicsState();
    }

    private void RestorePhysicsState()
    {
        if (disableRigidbodySimulationWhileDragging && body2D != null)
        {
            body2D.simulated = startSimulationState;
        }
    }
}
