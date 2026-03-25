using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public class PlayerVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.StateChanged += HandleStateChanged;
        }

        RefreshVisualState();
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.StateChanged -= HandleStateChanged;
        }
    }

    public void RefreshVisualState()
    {
        if (playerController == null)
        {
            return;
        }

        if (playerController.IsGhost)
        {
            ApplyGhostVisuals();
        }
        else
        {
            ApplyNormalVisuals();
        }
    }

    public void ApplyGhostVisuals()
    {
        // Ghost visuals will be added later.
    }

    public void ApplyNormalVisuals()
    {
        // Normal visuals will be added later.
    }

    public void HandleStateChanged(PlayerController.PlayerState previousState, PlayerController.PlayerState newState)
    {
        RefreshVisualState();
    }

    private void CacheReferences()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
    }
}
