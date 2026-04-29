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
        Vector2 direction = mouseWorld - transform.position;
        float deg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, deg);
    }

    private void Fire()
    {
        if (projectilePrefab == null) return;

        cooldownTimer = cooldown;

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Instantiate(projectilePrefab, spawnPoint.position, transform.rotation);
    }
}
