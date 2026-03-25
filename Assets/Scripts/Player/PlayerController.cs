using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PointToMove))]
[RequireComponent(typeof(PlayerCollisionHandler))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Normal = 0,
        Ghost = 1
    }

    [Header("State")]
    [SerializeField] private PlayerState startingState = PlayerState.Ghost;

    [Header("References")]
    [SerializeField] private PointToMove pointToMove;
    [SerializeField] private PlayerVisualController playerVisualController;
    [SerializeField] private PlayerCollisionHandler playerCollisionHandler;

    private PlayerState currentState;

    public PlayerState CurrentState => currentState;
    public bool IsGhost => currentState == PlayerState.Ghost;
    public PointToMove Movement => pointToMove;

    public event Action<PlayerState, PlayerState> StateChanged;

    private void Awake()
    {
        CacheReferences();
        currentState = startingState;
    }

    private void OnEnable()
    {
        SubscribeToCollisionHandler();
    }

    private void Start()
    {
        playerVisualController?.RefreshVisualState();
    }

    private void OnDisable()
    {
        UnsubscribeFromCollisionHandler();
    }

    public void SetState(PlayerState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        PlayerState previousState = currentState;
        currentState = newState;
        StateChanged?.Invoke(previousState, currentState);
    }

    public void SetGhostState()
    {
        SetState(PlayerState.Ghost);
    }

    public void SetNormalState()
    {
        SetState(PlayerState.Normal);
    }

    private void HandleTriggerEntered(Collider2D other)
    {
        // Player-specific trigger rules can be added here later.
    }

    private void HandleTriggerExited(Collider2D other)
    {
        // Player-specific trigger exit rules can be added here later.
    }

    private void HandleCollisionEntered(Collision2D collision)
    {
        // Player-specific collision rules can be added here later.
    }

    private void SubscribeToCollisionHandler()
    {
        if (playerCollisionHandler == null)
        {
            return;
        }

        playerCollisionHandler.TriggerEntered += HandleTriggerEntered;
        playerCollisionHandler.TriggerExited += HandleTriggerExited;
        playerCollisionHandler.CollisionEntered += HandleCollisionEntered;
    }

    private void UnsubscribeFromCollisionHandler()
    {
        if (playerCollisionHandler == null)
        {
            return;
        }

        playerCollisionHandler.TriggerEntered -= HandleTriggerEntered;
        playerCollisionHandler.TriggerExited -= HandleTriggerExited;
        playerCollisionHandler.CollisionEntered -= HandleCollisionEntered;
    }

    private void CacheReferences()
    {
        if (pointToMove == null)
        {
            pointToMove = GetComponent<PointToMove>();
        }

        if (playerVisualController == null)
        {
            playerVisualController = GetComponent<PlayerVisualController>();
        }

        if (playerCollisionHandler == null)
        {
            playerCollisionHandler = GetComponent<PlayerCollisionHandler>();
        }
    }
}
