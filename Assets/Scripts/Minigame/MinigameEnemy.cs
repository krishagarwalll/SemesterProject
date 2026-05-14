using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MinigameEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float chaseForce = 6f;
    [SerializeField] private float maxSpeed = 3.5f;
    [SerializeField] private float drag = 1.2f;

    [Header("Floaty Feel")]
    [SerializeField] private float gravityScale = 0.08f;
    [SerializeField] private float bobAmplitude = 0.5f;
    [SerializeField] private float bobFrequency = 0.7f;

    [Header("References")]
    [SerializeField] private Transform target;

    private Rigidbody2D rb;
    private float bobPhase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.linearDamping = drag;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        // Random phase so multiple ghosts don't bob in sync
        bobPhase = Random.value * Mathf.PI * 2f;
    }

    private void Start()
    {
        if (target) return;
        GameObject player = GameObject.FindWithTag("Player");
        if (player) target = player.transform;
    }

    private void FixedUpdate()
    {
        if (!target || !rb) return;

        bobPhase += Time.fixedDeltaTime * bobFrequency * Mathf.PI * 2f;
        float bobOffset = Mathf.Sin(bobPhase) * bobAmplitude;

        // Chase a point that bobs above/below the actual target
        Vector2 chaseTarget = (Vector2)target.position + Vector2.up * bobOffset;
        Vector2 toTarget = chaseTarget - (Vector2)transform.position;
        Vector2 dir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector2.zero;

        rb.AddForce(dir * chaseForce, ForceMode2D.Force);

        // Soft speed cap — dampen rather than hard-clamp so motion stays smooth
        if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    private void OnDrawGizmos()
    {
        if (target)
        {
            Gizmos.color = new Color(0.9f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawLine(transform.position, target.position);
        }

        Gizmos.color = new Color(0.6f, 0.4f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, bobAmplitude);
    }

    public void Die() => Destroy(gameObject);
}
