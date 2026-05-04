using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CameraFlash : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float cooldown = 1f;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private float cooldownTimer;
    private Camera mainCamera;
    private Vector2 aimDirection = Vector2.right;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (PauseService.IsGameplayInputPaused(this))
        {
            return;
        }

        cooldownTimer -= Time.deltaTime;
        RotateTowardMouse();

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && cooldownTimer <= 0f)
            Fire();
    }

    private void RotateTowardMouse()
    {
        if (Mouse.current == null) return;
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));
        Vector2 direction = (Vector2)mouseWorld - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;
        aimDirection = direction.normalized;
        float deg = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, deg);
    }

    private void Fire()
    {
        if (projectilePrefab == null) return;

        cooldownTimer = cooldown;

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        GameObject instance = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        if (instance.TryGetComponent(out CameraFlashProjectile projectile))
            projectile.Launch(aimDirection);
    }
}
