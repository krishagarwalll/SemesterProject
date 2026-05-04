using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class CameraFlashProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;

    [Header("Sprite Alignment")]
    [Tooltip("Angle (deg) the sprite art faces in source. 0=right, 90=up, 180=left, -90=down.")]
    [SerializeField] private float spriteForwardAngle = 0f;
    [Tooltip("Mirror sprite horizontally (toggle if it looks reversed).")]
    [SerializeField] private bool flipX = false;
    [Tooltip("Mirror sprite vertically (toggle if it looks upside down).")]
    [SerializeField] private bool flipY = false;

    private Rigidbody2D rb;
    private Vector2 launchDirection = Vector2.right;

    public void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
            launchDirection = direction.normalized;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        if (TryGetComponent(out SpriteRenderer sr))
        {
            sr.flipX = flipX;
            sr.flipY = flipY;
        }
    }

    private void Start()
    {
        float motionAngle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, motionAngle - spriteForwardAngle);
        rb.linearVelocity = launchDirection * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MinigameEnemy enemy = other.GetComponentInParent<MinigameEnemy>();
        if (enemy == null) return;

        enemy.Die();
    }
}
