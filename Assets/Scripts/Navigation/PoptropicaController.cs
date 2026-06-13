using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PoptropicaController : MonoBehaviour
{
    private const float DirectionDeadzone = 0.0001f;
    private const float GroundSnapDeadzone = 0.005f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float moveAcceleration = 45f;
    [SerializeField, Min(0f)] private float moveDeceleration = 55f;
    [SerializeField, Min(0.01f)] private float moveEaseDistance = 1.25f;
    [SerializeField, Range(0.05f, 1f)] private float minimumEaseSpeedFactor = 0.18f;
    [SerializeField, Min(1f)] private float jumpForce = 14f;
    [SerializeField, Range(0.01f, 0.3f)] private float jumpGestureThreshold = 0.05f;
    [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0.01f)] private float clickMoveStopDistance = 0.12f;
    [SerializeField] private bool ignorePointerOverUi = true;

    [Header("Ground Detection")]
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckOffset = new(0f, -0.4f);
    [SerializeField, Min(0f)] private float groundSnapDistance = 0.25f;
    [SerializeField, Min(0f)] private float groundResolveDistance = 0.18f;
    [SerializeField, Min(0f)] private float maxFallSpeed = 18f;

    [Header("Animation")]
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
    private Collider[] colliders3D;
    private IWorldDraggable activeDrag;
    private PendingAction pendingAction;
    private Vector2 cursorWorldPos;
    private Vector2 smoothedDirection;
    private Vector3 lastPosition;
    private float fixedDepth;
    private float moveTargetX;
    private bool isGrounded;
    private bool movementLocked;
    private bool hasMoveTarget;
    private bool isAirborne;
    private bool hasJumpGesturePosition;
    private bool jumpGestureArmed = true;
    private bool jumpPending;
    private float previousJumpGestureY;
    private float jumpUpGestureDistance;
    private float jumpDownGestureDistance;
    private float coyoteTimer;

    private PointerContext Pointer => this.ResolveSceneComponent(ref pointer);
    private Inventory SceneInventory => this.ResolveSceneComponent(ref inventory);
    private SpriteFlipper Flipper => this.ResolveComponent(ref spriteFlipper, true);
    private Animator Anim => this.ResolveComponent(ref animator, true);
    private Rigidbody2D Body2D => this.ResolveComponent(ref body2D, true);
    private Collider2D[] Colliders2D => colliders2D ??= GetComponentsInChildren<Collider2D>(true);
    private Collider[] Colliders3D => colliders3D ??= GetComponentsInChildren<Collider>(true);
    private Vector3 Position => transform.position;
    private bool IsPointerBlocked => ignorePointerOverUi && Pointer && Pointer.IsPointerOverUi;

    private IWorldDraggable ActiveDrag
    {
        get
        {
            if (activeDrag is UnityEngine.Object dragObject && !dragObject)
            {
                activeDrag = null;
            }

            return activeDrag;
        }
    }

    public bool HasActiveInteraction => ActiveDrag != null || pendingAction.IsValid;
    public bool IsGrounded => isGrounded;
    public bool IsAirborne => isAirborne;
    private bool CanJump => !isAirborne && (isGrounded || coyoteTimer > 0f);

    private void Awake()
    {
        fixedDepth = transform.position.z;
        ApplyRuntimeSetup();
        ConfigureRigidbody();
        ApplyStableCollisionMaterial();
        ResolveGroundMask();
    }

    private void OnEnable()
    {
        fixedDepth = transform.position.z;
        ApplyRuntimeSetup();
        ConfigureRigidbody();
        ApplyStableCollisionMaterial();
        ResolveGroundMask();
        SubscribePointerEvents();
        ResetJumpGesture();
        ResetPresentationState();
    }

    private void OnDisable()
    {
        UnsubscribePointerEvents();
        Cancel();
        ResetPresentationState();
    }

    private void OnValidate()
    {
        groundSnapDistance = Mathf.Max(0f, groundSnapDistance);
        groundResolveDistance = Mathf.Max(0f, groundResolveDistance);
        maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
        moveEaseDistance = Mathf.Max(0.01f, moveEaseDistance);
        minimumEaseSpeedFactor = Mathf.Clamp(minimumEaseSpeedFactor, 0.05f, 1f);
    }

    private void Update()
    {
        UpdateCursorWorldPosition();
        RefreshGroundCheck();
        RefreshAirborneState();
        UpdateCoyoteTimer();
        RefreshJumpGesture();
        RefreshPresentation();
        HandleDragEnd();
        SnapDepth();
    }

    private void RefreshAirborneState()
    {
        // Land when grounded and no longer rising
        if (isAirborne && isGrounded && Body2D && Body2D.linearVelocity.y <= 0.05f)
            isAirborne = false;
    }

    private void UpdateCoyoteTimer()
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else if (isAirborne)
            coyoteTimer = 0f;
        else
            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);
    }

    private void FixedUpdate()
    {
        // Resolve ground penetration / snapping inside the physics step so
        // Rigidbody2D interpolation can smooth the vertical correction. Done in
        // Update it fought interpolation and made the sprite buzz while walking.
        ResolveGroundPenetration();
        SnapToGroundIfNeeded();

        if (PauseService.IsGameplayInputPaused(this) || movementLocked || ActiveDrag != null)
        {
            hasMoveTarget = false;
            StopHorizontalMovement();
            return;
        }

        if (pendingAction.IsValid)
        {
            hasMoveTarget = false;
            ApplyHorizontalMovement(pendingAction.Target.GetApproachPoint(Position).x);
            if (pendingAction.Target && pendingAction.Target.IsInRange(Position))
                ExecutePendingAction();
            return;
        }

        bool buttonHeld = IsMovementButtonHeld();
        bool blockedByUi = IsPointerBlocked;
        if (buttonHeld && !blockedByUi)
        {
            hasMoveTarget = false;
            pendingAction = default;
            ApplyHorizontalMovement(cursorWorldPos.x);
            TryJump();
            return;
        }

        if (hasMoveTarget)
        {
            // While airborne let physics carry the existing velocity; only steer when grounded
            if (!isAirborne)
            {
                ApplyHorizontalMovement(moveTargetX);
                if (Mathf.Abs(moveTargetX - transform.position.x) <= clickMoveStopDistance)
                {
                    StopHorizontalMovement();
                    hasMoveTarget = false;
                }
            }
        }
        else
        {
            if (!isAirborne) StopHorizontalMovement();
        }
    }

    private bool IsMovementButtonHeld()
    {
        if (Pointer) return Pointer.IsPrimaryPressed;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
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
        hasMoveTarget = false;
        Cancel();
        return true;
    }

    public void LockMovement()
    {
        movementLocked = true;
        hasMoveTarget = false;
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
            if (!target.IsInRange(Position)) return false;
            Cancel();
            draggable.BeginDrag(Pointer);
            if (!draggable.IsDragging) return false;
            activeDrag = draggable;
            return true;
        }

        if (!action.RequiresApproach || target.IsInRange(Position))
        {
            hasMoveTarget = false;
            Cancel();
            return target.Execute(CreateContext(target), action);
        }

        hasMoveTarget = false;
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

        Vector2 screenPos = GetPointerScreenPosition();

        // Project cursor onto the player's Z depth plane
        float depth = Mathf.Abs(cam.transform.position.z - fixedDepth);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        cursorWorldPos = new Vector2(world.x, world.y);
    }

    private Vector2 GetPointerScreenPosition()
    {
        if (Pointer)
        {
            return Pointer.ScreenPosition;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return Input.mousePosition;
    }

    private void RefreshJumpGesture()
    {
        if (PauseService.IsGameplayInputPaused(this)
            || movementLocked
            || ActiveDrag != null
            || IsPointerBlocked
            || !IsMovementButtonHeld())
        {
            ResetJumpGesture();
            return;
        }

        Vector2 screenPos = GetPointerScreenPosition();
        if (!hasJumpGesturePosition)
        {
            previousJumpGestureY = screenPos.y;
            hasJumpGesturePosition = true;
            return;
        }

        float deltaY = (screenPos.y - previousJumpGestureY) / Mathf.Max(1f, Screen.height);
        previousJumpGestureY = screenPos.y;

        if (deltaY < 0f)
        {
            jumpDownGestureDistance += -deltaY;
            jumpUpGestureDistance = 0f;
            if (jumpDownGestureDistance >= jumpGestureThreshold)
            {
                jumpGestureArmed = true;
            }

            return;
        }

        if (deltaY <= 0f)
        {
            return;
        }

        jumpUpGestureDistance += deltaY;
        jumpDownGestureDistance = 0f;
        if (!jumpGestureArmed || jumpUpGestureDistance < jumpGestureThreshold)
        {
            return;
        }

        jumpPending = true;
        jumpGestureArmed = false;
        jumpUpGestureDistance = 0f;
    }

    private void ResetJumpGesture()
    {
        hasJumpGesturePosition = false;
        jumpGestureArmed = true;
        jumpPending = false;
        jumpUpGestureDistance = 0f;
        jumpDownGestureDistance = 0f;
    }

    private void RefreshGroundCheck()
    {
        int mask = ResolveGroundMask();
        isGrounded = HasGroundBelow(GetGroundProbeBounds(), mask);
    }

    private int ResolveGroundMask()
    {
        int mask = groundLayer.value;
        AddLayerToMask("Walkable", ref mask);
        AddLayerToMask("Environment", ref mask);
        return mask == 0 ? Physics2D.AllLayers : mask;
    }

    private Bounds GetGroundProbeBounds()
    {
        Bounds bounds = new(transform.position, Vector3.zero);
        bool initialized = false;
        for (int i = 0; i < Colliders2D.Length; i++)
        {
            Collider2D collider = Colliders2D[i];
            if (!collider.IsUsable() || collider.isTrigger)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = collider.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds.min);
                bounds.Encapsulate(collider.bounds.max);
            }
        }

        if (initialized)
        {
            return bounds;
        }

        Vector2 fallbackCenter = (Vector2)transform.position + groundCheckOffset;
        return new Bounds(fallbackCenter, new Vector3(0.5f, 0.1f, 0f));
    }

    private bool HasGroundBelow(Bounds bounds, int mask)
    {
        const float SkinWidth = 0.03f;
        float halfWidth = Mathf.Max(0.05f, bounds.extents.x - SkinWidth);
        float originY = bounds.min.y + SkinWidth;
        float distance = Mathf.Max(groundCheckDistance, SkinWidth * 2f);

        return IsGroundHit(new Vector2(bounds.center.x - halfWidth, originY), distance, mask)
            || IsGroundHit(new Vector2(bounds.center.x, originY), distance, mask)
            || IsGroundHit(new Vector2(bounds.center.x + halfWidth, originY), distance, mask);
    }

    private bool IsGroundHit(Vector2 origin, float distance, int mask)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, distance, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (!hit || hit.isTrigger || IsOwnCollider(hit))
            {
                continue;
            }

            return true;
        }

        return TryGet3DGroundHit(origin, distance, mask, out _);
    }

    private void SnapToGroundIfNeeded()
    {
        if (!Body2D || Body2D.linearVelocity.y > 0f)
        {
            return;
        }

        if (maxFallSpeed > 0f && Body2D.linearVelocity.y < -maxFallSpeed)
        {
            Body2D.linearVelocity = new Vector2(Body2D.linearVelocity.x, -maxFallSpeed);
        }

        Bounds bounds = GetGroundProbeBounds();
        int mask = ResolveGroundMask();
        if (!TryGetGroundHit(bounds, Mathf.Max(groundCheckDistance, groundSnapDistance), mask, out GroundHit hit))
        {
            return;
        }

        isGrounded = true;
        float desiredBottom = hit.PointY + 0.01f;
        float deltaY = desiredBottom - bounds.min.y;
        if (deltaY <= GroundSnapDeadzone || deltaY > groundSnapDistance + 0.01f)
        {
            return;
        }

        Body2D.position += Vector2.up * deltaY;
        Body2D.linearVelocity = new Vector2(Body2D.linearVelocity.x, Mathf.Max(0f, Body2D.linearVelocity.y));
    }

    private void ResolveGroundPenetration()
    {
        if (!Body2D || groundResolveDistance <= 0f || Body2D.linearVelocity.y > 0f)
        {
            return;
        }

        Bounds bounds = GetGroundProbeBounds();
        Vector2 probeCenter = new(bounds.center.x, bounds.min.y - 0.02f);
        Vector2 probeSize = new(Mathf.Max(0.08f, bounds.size.x * 0.85f), Mathf.Max(0.06f, groundResolveDistance));
        Collider2D[] hits = Physics2D.OverlapBoxAll(probeCenter, probeSize, 0f, ResolveGroundMask());
        float bestTop = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!hit || hit.isTrigger || IsOwnCollider(hit))
            {
                continue;
            }

            float top = hit.bounds.max.y;
            if (top < bounds.min.y - groundResolveDistance || top > bounds.min.y + groundResolveDistance)
            {
                continue;
            }

            if (!found || top > bestTop)
            {
                bestTop = top;
                found = true;
            }
        }

        if (!found)
        {
            return;
        }

        float deltaY = bestTop + 0.01f - bounds.min.y;
        if (deltaY <= GroundSnapDeadzone || deltaY > groundResolveDistance + 0.01f)
        {
            return;
        }

        Body2D.position += Vector2.up * deltaY;
        Body2D.linearVelocity = new Vector2(Body2D.linearVelocity.x, Mathf.Max(0f, Body2D.linearVelocity.y));
        isGrounded = true;
    }

    private bool TryGetGroundHit(Bounds bounds, float distance, int mask, out GroundHit bestHit)
    {
        const float SkinWidth = 0.03f;
        bestHit = default;
        float halfWidth = Mathf.Max(0.05f, bounds.extents.x - SkinWidth);
        float originY = bounds.min.y + SkinWidth;
        bool found = false;
        found |= TryGetGroundHit(new Vector2(bounds.center.x - halfWidth, originY), distance, mask, ref bestHit);
        found |= TryGetGroundHit(new Vector2(bounds.center.x, originY), distance, mask, ref bestHit);
        found |= TryGetGroundHit(new Vector2(bounds.center.x + halfWidth, originY), distance, mask, ref bestHit);
        return found;
    }

    private bool TryGetGroundHit(Vector2 origin, float distance, int mask, ref GroundHit bestHit)
    {
        bool found = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, distance, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (!hit || hit.isTrigger || IsOwnCollider(hit))
            {
                continue;
            }

            if (!found || hits[i].distance < bestHit.Distance)
            {
                bestHit = new GroundHit(hits[i].distance, hits[i].point.y);
                found = true;
            }
        }

        if (TryGet3DGroundHit(origin, distance, mask, out GroundHit hit3D)
            && (!found || hit3D.Distance < bestHit.Distance))
        {
            bestHit = hit3D;
            found = true;
        }

        return found;
    }

    private bool TryGet3DGroundHit(Vector2 origin, float distance, int mask, out GroundHit bestHit)
    {
        bestHit = default;
        Ray ray = new(new Vector3(origin.x, origin.y, fixedDepth), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, distance, mask, QueryTriggerInteraction.Ignore);
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i].collider;
            if (!hit || hit.isTrigger || IsOwnCollider(hit))
            {
                continue;
            }

            if (!found || hits[i].distance < bestHit.Distance)
            {
                bestHit = new GroundHit(hits[i].distance, hits[i].point.y);
                found = true;
            }
        }

        return found;
    }

    private bool IsOwnCollider(Collider2D hit)
    {
        for (int i = 0; i < Colliders2D.Length; i++)
        {
            if (Colliders2D[i] == hit)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOwnCollider(Collider hit)
    {
        for (int i = 0; i < Colliders3D.Length; i++)
        {
            if (Colliders3D[i] == hit)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyHorizontalMovement(float targetX)
    {
        float dx = targetX - transform.position.x;
        float distance = Mathf.Abs(dx);
        float targetVelocity = 0f;
        if (distance > clickMoveStopDistance)
        {
            float ease = Mathf.InverseLerp(clickMoveStopDistance, Mathf.Max(clickMoveStopDistance + 0.01f, moveEaseDistance), distance);
            ease = Mathf.SmoothStep(minimumEaseSpeedFactor, 1f, ease);
            targetVelocity = Mathf.Sign(dx) * moveSpeed * ease;
        }

        if (Body2D)
        {
            float acceleration = Mathf.Abs(targetVelocity) > 0.001f ? moveAcceleration : moveDeceleration;
            float velocity = Mathf.MoveTowards(Body2D.linearVelocity.x, targetVelocity, acceleration * Time.fixedDeltaTime);
            Body2D.linearVelocity = new Vector2(velocity, Body2D.linearVelocity.y);
        }
    }

    private void StopHorizontalMovement()
    {
        if (Body2D)
        {
            float velocity = Mathf.MoveTowards(Body2D.linearVelocity.x, 0f, moveDeceleration * Time.fixedDeltaTime);
            Body2D.linearVelocity = new Vector2(velocity, Body2D.linearVelocity.y);
        }
    }

    private void TryJump()
    {
        if (!jumpPending || !CanJump || !Body2D) return;

        jumpPending = false;
        coyoteTimer = 0f;
        Body2D.linearVelocity = new Vector2(Body2D.linearVelocity.x, 0f);
        Body2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isAirborne = true;
        // Clear click target so the character continues on trajectory rather than
        // stopping horizontal movement at moveTargetX mid-air
        hasMoveTarget = false;
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
        if (ActiveDrag == null || Pointer == null || !Pointer.DragEndedThisFrame) return;
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
        hasMoveTarget = false;
        pendingAction = default;
        if (ActiveDrag == null) return;
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
        if (PauseService.IsGameplayInputPaused(this)) return;
        if (IsPointerBlocked || ActiveDrag != null) return;

        InteractionTarget target = context.ClickedTarget;

        if (!target || target.SupportsDrag)
        {
            pendingAction = default;
            moveTargetX = cursorWorldPos.x;
            hasMoveTarget = true;
            return;
        }

        if (!target.TryGetPrimaryAction(CreateContext(target), out InteractionAction action) || !action.Enabled)
        {
            hasMoveTarget = false;
            return;
        }

        ExecuteAction(target, action);
    }

    private void HandleDragStarted(PointerContext context)
    {
        if (PauseService.IsGameplayInputPaused(this)) return;
        if (IsPointerBlocked && !context.TryGetWorldDragTarget(out _)) return;
        InteractionTarget target = context.DragTarget;
        if (!target && context) context.TryGetWorldDragTarget(out target);
        if (!target || !target.TryGetDraggable(out IWorldDraggable draggable) || !Pointer) return;
        if (!target.IsInRange(Position)) return;
        hasMoveTarget = false;
        Cancel();
        draggable.BeginDrag(Pointer);
        if (draggable.IsDragging) activeDrag = draggable;
    }

    private void HandleDragUpdated(PointerContext context) => ActiveDrag?.UpdateDrag(context);

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
        Body2D.bodyType = RigidbodyType2D.Dynamic;
        Body2D.gravityScale = 1f;
        Body2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        Body2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Body2D.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void ApplyStableCollisionMaterial()
    {
        Physics2DDefaults.ApplyStableMaterial(Colliders2D);
    }

    private static void AddLayerToMask(string layerName, ref int mask)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            mask |= 1 << layer;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!isGrounded || !IsMovementButtonHeld() || IsPointerBlocked) return;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
        Vector3 startPos = transform.position;
        Vector3 startVel = new(Body2D ? Body2D.linearVelocity.x : 0f, jumpForce, 0f);
        float gravity = Physics2D.gravity.y * (Body2D ? Body2D.gravityScale : 1f);
        float dt = 0.05f;
        Vector3 prev = startPos;
        for (int i = 1; i <= 20; i++)
        {
            float t = i * dt;
            Vector3 next = startPos + startVel * t + new Vector3(0f, 0.5f * gravity * t * t, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
            if (next.y < startPos.y) break;
        }
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

    private readonly struct GroundHit
    {
        public GroundHit(float distance, float pointY)
        {
            Distance = distance;
            PointY = pointY;
        }

        public float Distance { get; }
        public float PointY { get; }
    }
}
