using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform playerCenter;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float spawnInterval = 1.35f;
    [SerializeField] private int maxEnemies = 14;

    [Tooltip("Optional. If set, spawn points are clamped into this collider's area so enemies never appear outside the floor.")]
    [SerializeField] private Collider2D spawnBounds;
    [Tooltip("Random rejection-sampling attempts before falling back to clamping onto the bounds edge.")]
    [SerializeField, Min(1)] private int spawnSampleAttempts = 8;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        if (enemyPrefab == null || playerCenter == null) return;
        if (FindObjectsByType<MinigameEnemy>(FindObjectsSortMode.None).Length >= maxEnemies) return;

        Vector3 spawnPos = PickSpawnPosition();
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    private Vector3 PickSpawnPosition()
    {
        Vector3 candidate = default;
        for (int i = 0; i < spawnSampleAttempts; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            candidate = playerCenter.position + new Vector3(randomDir.x, randomDir.y) * spawnRadius;

            if (spawnBounds == null || spawnBounds.OverlapPoint(candidate))
            {
                return candidate;
            }
        }

        Vector2 clamped = spawnBounds.ClosestPoint(candidate);
        return new Vector3(clamped.x, clamped.y, candidate.z);
    }
}
