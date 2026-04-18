using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MinigameEnemy : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("References")]
    [SerializeField] private Transform target;

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        if (target != null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) target = player.transform;
    }

    private void Update()
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
