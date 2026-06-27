using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class CameraFlash : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float cooldown = 1.5f;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private float cooldownTimer;
    private Camera mainCamera;
    private Vector2 aimDirection = Vector2.right;
    private Image cooldownFill;
    private TextMeshProUGUI cooldownText;

    private void Awake()
    {
        mainCamera = Camera.main;
        EnsureCooldownUi();
    }

    private void Update()
    {
        if (PauseService.IsGameplayInputPaused(this))
        {
            return;
        }

        cooldownTimer -= Time.deltaTime;
        RotateTowardMouse();
        UpdateCooldownUi();

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

    private void EnsureCooldownUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (!canvas)
        {
            GameObject canvasObject = new("CameraFlashCooldownCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4500;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform existing = canvas.transform.Find("CameraFlashCooldown");
        if (existing)
        {
            cooldownFill = existing.Find("Fill")?.GetComponent<Image>();
            cooldownText = existing.Find("Label")?.GetComponent<TextMeshProUGUI>();
            return;
        }

        GameObject root = new("CameraFlashCooldown", typeof(RectTransform), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-36f, 36f);
        rect.sizeDelta = new Vector2(240f, 28f);
        root.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.82f);

        GameObject fillObject = new("Fill", typeof(RectTransform), typeof(Image));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(rect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        cooldownFill = fillObject.GetComponent<Image>();
        cooldownFill.color = new Color(0.25f, 0.72f, 1f, 0.88f);
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillMethod = Image.FillMethod.Horizontal;
        cooldownFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        cooldownText = labelObject.GetComponent<TextMeshProUGUI>();
        cooldownText.text = "FLASH READY";
        cooldownText.fontSize = 16f;
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.color = Color.white;
        cooldownText.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset) cooldownText.font = TMP_Settings.defaultFontAsset;
    }

    private void UpdateCooldownUi()
    {
        if (!cooldownFill || !cooldownText) return;

        float remaining = Mathf.Max(0f, cooldownTimer);
        float ready = cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(remaining / cooldown);
        cooldownFill.fillAmount = ready;
        cooldownText.text = remaining > 0f ? $"FLASH {remaining:0.0}s" : "FLASH READY";
    }
}
