using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PoptropicaController : MonoBehaviour
{
    private const float DirectionDeadzone = 0.0001f;

    [FieldHeader("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(1f)] private float jumpForce = 14f;
    [SerializeField, Min(0.1f)] private float jumpThreshold = 1.2f;
    [SerializeField, Min(0f)] private float movementDeadzone = 0.25f;
    [SerializeField] private bool ignorePointerOverUi = true;

    [FieldHeader("Ground Detection")]
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckOffset = new(0f, -0.4f);

    [FieldHeader("Animation")]
    [SerializeField] private string movingParameter = "isMoving";
    [SerializeField] private string horizontalSpeedParameter = "moveX";
    [SerializeField] private string verticalSpeedParameter = "moveY";
    [SerializeField, Min(0f)] private float animationSmoothing = 10f;
    [SerializeField, Min(0f)] private float flipThreshold = 0.05f;

    private PointerContext pointer;
    private Inventory inventory;
    private SpriteFlipper spriteFlipper;
    private Animator animator;
    private Rigidbody2D body2D;
    private Collider2D[] colliders2D;
    private IWorldDraggable activeDrag;
    private PendingAction pendingAction;
    private Vector2 cursorWorldPos;
    private Vector2 smoothedDirection;
    private Vector3 lastPosition;
    private float fixedDepth;
    private bool isGrounded;
    private bool movementLocked;

    private PointerContext Pointer => this.ResolveSceneComponent(ref pointer);
    private Inventory SceneInventory => this.ResolveSceneComponent(ref inventory);
    private SpriteFlipper Flipper => this.ResolveComponent(ref spriteFlipper, true);
    private Animator Anim => this.ResolveComponent(ref animator, true);
    private Rigidbody2D Body2D => this.ResolveComponent(ref body2D, true);
    private Collider2D[] Colliders2D => colliders2D ??= GetComponentsInChildren<Collider2D>(true);
    private Vector3 Position => transform.position;
    private bool IsPointerBlocked => ignorePointerOverUi && Pointer && Pointer.IsPointerOverUi && activeDrag == null;

    public bool HasActiveInteraction => activeDrag != null || pendingAction.IsValid;
    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        fixedDepth = transform.position.z;
        ApplyRuntimeSetup();
        ConfigureRigidbody();
    }

    private void OnEnable()
    {
        fixedDepth = transform.position.z;
        ApplyRuntimeSetup();
        ConfigureRigidbody();
        SubscribePointerEvents();
        ResetPresentationState();
    }

    private void OnDisable()
    {
        UnsubscribePointerEvents();
        Cancel();
        ResetPresentationState();
    }

    private void Update()
    {
        UpdateCursorWorldPosition();
        RefreshGroundCheck();
        RefreshPresentation();
        HandleDragEnd();
        SnapDepth();
    }

    private void FixedUpdate()
    {
        if (movementLocked || activeDrag != null)
        {
            if (Body2D) Body2D.linearVelocity = new Vector2(0f, Body2D.linearVelocity.y);
            return;
        }

        if (pendingAction.IsValid)
        {
            // Approach-then-interact always runs regardless of button state
            ApplyHorizontalMovement(pendingAction.Target.GetApproachPoint(Position).x);
            if (pendingAction.Target && pendingAction.Target.IsInRange(Position))
                ExecutePendingAction();
            return;
        }

        bool buttonHeld = IsMovementButtonHeld();
        bool blockedByUi = ignorePointerOverUi && Pointer && Pointer.IsPointerOverUi && activeDrag == null;
        if (buttonHeld && !blockedByUi)
        {
            ApplyHorizontalMovement(cursorWorldPos.x);
            TryJump();
        }
        else
        {
            if (Body2D) Body2D.linearVelocity = new Vector2(0f, Body2D.linearVelocity.y);
        }
    }

    private bool IsMovementButtonHeld()
    {
        if (Mouse.current != null) return Mouse.current.leftButton.isPressed;
        return Pointer && Pointer.IsPrimaryPressed;
    }

    // ── Public API (mirrors PointClickController for compatibility) ──────────

    public bool TryWarp(Vector3 worldPosition)
    {
        worldPosition.z = fixedDepth;
        transform.position = worldPosition;
        if (Body2D)
        {
            Body2D.position = new Vector2(worldPosition.x, worldPosition.y);
            Body2D.linearVelocity = Vector2.zero;
        }
        Cancel();
        return true;
    }

    public void LockMovement()
    {
        movementLocked = true;
        if (Body2D)
            Body2D.linearVelocity = new Vector2(0f, Body2D.linearVelocity.y);
    }

    public void UnlockMovement() => movementLocked = false;

    public void GetAvailableActions(InteractionTarget target, List<InteractionAction> actions)
    {
        if (!target) { actions.Clear(); return; }
        target.GetActions(CreateContext(target), actions);
    }

    public bool ExecuteAction(InteractionTarget target, InteractionMode mode)
    {
        return target
            && target.TryGetAction(CreateContext(target), mode, out InteractionAction action)
            && ExecuteAction(target, action);
    }

    public bool ExecuteAction(InteractionTarget target, InteractionAction action)
    {
        if (!target || !action.IsValid || !action.Enabled)
            return false;

        if (action.Mode == InteractionMode.Drag && target.TryGetDraggable(out IWorldDraggable draggable))
        {
            if (!Pointer) return false;
            Cancel();
            draggable.BeginDrag(Pointer);
            if (!draggable.IsDragging) return false;
            activeDrag = draggable;
            return true;
        }

        if (!action.RequiresApproach || target.IsInRange(Position))
        {
            Cancel();
            return target.Execute(CreateContext(target), action);
        }

        pendingAction = new PendingAction(target, action);
        return true;
    }

    public bool RequestSmoothClearance(Bounds blockerBounds, float clearance) => true;

    public Bounds GetWorldBounds(float padding)
    {
        if (Colliders2D.Length > 0)
        {
            bool initialized = false;
            Bounds bounds = new(transform.position, Vector3.zero);
            for (int i = 0; i < Colliders2D.Length; i++)
            {
                if (!Colliders2D[i].IsUsable()) continue;
                if (!initialized) { bounds = Colliders2D[i].bounds; initialized = true; }
                else { bounds.Encapsulate(Colliders2D[i].bounds.min); bounds.Encapsulate(Colliders2D[i].bounds.max); }
            }
            bounds.Expand(padding * 2f);
            return bounds;
        }
        return new Bounds(transform.position, Vector3.one + Vector3.one * padding * 2f);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateCursorWorldPosition()
    {
        Camera cam = (Pointer && Pointer.WorldCamera) ? Pointer.WorldCamera : Camera.main;
        if (!cam) return;

        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        // Project cursor onto the player's Z depth plane
        float depth = Mathf.Abs(cam.transform.position.z - fixedDepth);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        cursorWorldPos = new Vector2(world.x, world.y);
    }

    private void RefreshGroundCheck()
    {
        Vector2 origin = (Vector2)transform.position + groundCheckOffset;
        isGrounded = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void ApplyHorizontalMovement(float targetX)
    {
        float dx = targetX - transform.position.x;
        float vel = Mathf.Abs(dx) > movementDeadzone ? Mathf.Sign(dx) * moveSpeed : 0f;
        if (Body2D)
            Body2D.linearVelocity = new Vector2(vel, Body2D.linearVelocity.y);
    }

    private void TryJump()
    {
        if (!isGrounded || !Body2D) return;
        float dy = cursorWorldPos.y - transform.position.y;
        if (dy > jumpThreshold)
        {
            // Zero vertical velocity before applying jump so force is consistent
            Body2D.linearVelocity = new Vector2(Body2D.linearVelocity.x, 0f);
            Body2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    private void ExecutePendingAction()
    {
        InteractionTarget target = pendingAction.Target;
        InteractionAction action = pendingAction.Action;
        pendingAction = default;
        ExecuteAction(target, action);
    }

    private void HandleDragEnd()
    {
        if (activeDrag == null || Pointer == null || !Pointer.DragEndedThisFrame) return;
        activeDrag.EndDrag();
        activeDrag = null;
    }

    private void SnapDepth()
    {
        if (Mathf.Abs(transform.position.z - fixedDepth) < 0.001f) return;
        Vector3 pos = transform.position;
        pos.z = fixedDepth;
        transform.position = pos;
    }

    private void Cancel()
    {
        pendingAction = default;
        if (activeDrag == null) return;
        activeDrag.EndDrag();
        activeDrag = null;
    }

    private InteractionContext CreateContext(InteractionTarget target)
        => new(this, Pointer, target, SceneInventory);

    private void SubscribePointerEvents()
    {
        if (!Pointer) return;
        Pointer.PrimaryClicked += HandlePrimaryClick;
        Pointer.DragStarted += HandleDragStarted;
        Pointer.DragUpdated += HandleDragUpdated;
    }

    private void UnsubscribePointerEvents()
    {
        if (!pointer) return;
        pointer.PrimaryClicked -= HandlePrimaryClick;
        pointer.DragStarted -= HandleDragStarted;
        pointer.DragUpdated -= HandleDragUpdated;
    }

    private void HandlePrimaryClick(PointerContext context)
    {
        if (IsPointerBlocked || activeDrag != null) return;
        if (!context.ClickedTarget) return;

        InteractionTarget target = context.ClickedTarget;
        if (!target.TryGetPrimaryAction(CreateContext(target), out InteractionAction action) || !action.Enabled)
            return;

        ExecuteAction(target, action);
    }

    private void HandleDragStarted(PointerContext context)
    {
        InteractionTarget target = context.DragTarget;
        if (!target && context) context.TryGetWorldDragTarget(out target);
        if (!target || !target.TryGetDraggable(out IWorldDraggable draggable) || !Pointer) return;
        Cancel();
        draggable.BeginDrag(Pointer);
        if (draggable.IsDragging) activeDrag = draggable;
    }

    private void HandleDragUpdated(PointerContext context) => activeDrag?.UpdateDrag(context);

    private void ResetPresentationState()
    {
        smoothedDirection = Vector2.zero;
        lastPosition = transform.position;
        ApplyPresentation(Vector2.zero, false);
    }

    private void RefreshPresentation()
    {
        Vector3 velocity = Time.deltaTime > 0f ? (transform.position - lastPosition) / Time.deltaTime : Vector3.zero;
        Vector2 direction = velocity.sqrMagnitude <= DirectionDeadzone ? Vector2.zero : ((Vector2)velocity).normalized;
        float speed = velocity.magnitude;
        smoothedDirection = animationSmoothing <= 0f
            ? direction
            : Vector2.MoveTowards(smoothedDirection, direction, animationSmoothing * Time.deltaTime);
        ApplyPresentation(smoothedDirection, speed > 0.05f);
        lastPosition = transform.position;
    }

    private void ApplyPresentation(Vector2 direction, bool moving)
    {
        if (Flipper && direction.sqrMagnitude > flipThreshold * flipThreshold)
            Flipper.SetFacing(direction);
        if (!Anim) return;
        SetBool(movingParameter, moving);
        SetFloat(horizontalSpeedParameter, direction.x);
        SetFloat(verticalSpeedParameter, direction.y);
    }

    private void SetBool(string parameterName, bool value)
    {
        if (!Anim || string.IsNullOrWhiteSpace(parameterName)) return;
        Anim.SetBool(Animator.StringToHash(parameterName), value);
    }

    private void SetFloat(string parameterName, float value)
    {
        if (!Anim || string.IsNullOrWhiteSpace(parameterName)) return;
        Anim.SetFloat(Animator.StringToHash(parameterName), value);
    }

    private void ApplyRuntimeSetup()
    {
        int layer = LayerMask.NameToLayer("Character");
        if (layer >= 0) gameObject.layer = layer;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingLayerName = "Character";
    }

    private void ConfigureRigidbody()
    {
        if (!Body2D) return;
        Body2D.gravityScale = 1f;
        Body2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        Body2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private readonly struct PendingAction
    {
        public PendingAction(InteractionTarget target, InteractionAction action)
        {
            Target = target;
            Action = action;
        }

        public InteractionTarget Target { get; }
        public InteractionAction Action { get; }
        public bool IsValid => Target && Action.IsValid;
    }
}
