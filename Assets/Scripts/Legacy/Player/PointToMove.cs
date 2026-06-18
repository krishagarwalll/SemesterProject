using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class PointToMove : MonoBehaviour
{
    Rigidbody2D rb;
    private DragDropHandler dragDropHandler;
    private SpriteFlipper spriteFlipper;
    private Camera cam;

    [SerializeField] private Transform head;

    [Tooltip("Optional. If set, click targets are clamped to this collider's area so the player cannot walk outside of it. " +
             "Use a trigger Collider2D over the playable floor.")]
    [SerializeField] private Collider2D movementBounds;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        dragDropHandler = FindFirstObjectByType<DragDropHandler>();
        spriteFlipper = GetComponentInChildren<SpriteFlipper>();
        cam = Camera.main;
    }

    void Update()
    {
        if (PauseService.IsGameplayInputPaused(this))
        {
            StopAllCoroutines();
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var mouse = Mouse.current.position.ReadValue();
            if (dragDropHandler != null && dragDropHandler.BlocksMovementClick(mouse))
            {
                return;
            }

            if (!cam) cam = Camera.main;
            if (!cam) return;

            var pos = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0));
            pos.z = 0;

            if (movementBounds != null)
            {
                Vector2 clamped = movementBounds.ClosestPoint(pos);
                pos = new Vector3(clamped.x, clamped.y, 0);
            }

            StopAllCoroutines();
            StartCoroutine(DoTheThing(pos));
        }
    }

    IEnumerator DoTheThing(Vector3 pos)
    {
        Vector2 direction = pos - transform.position;
        if (spriteFlipper != null && direction.sqrMagnitude > 0.001f)
            spriteFlipper.SetFacing(direction);

        while (Vector3.Distance(transform.position, pos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                pos,
                5.0f * Time.deltaTime
            );

            yield return null;
        }
        transform.position = pos;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("enemy"))
        {
            Debug.Log("died");
        }
        else if (collision.CompareTag("decoration"))
        {
            collision.transform.SetParent(head);
            collision.transform.localPosition = Vector3.zero;
        }
        else if (collision.CompareTag("minigame"))
        {
            Debug.Log("enter minigame");
        }
    }
}
