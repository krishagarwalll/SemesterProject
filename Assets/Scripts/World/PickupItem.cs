using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
[RequireComponent(typeof(DragBody2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PickupItem : MonoBehaviour, IInteractionActionProvider, IWorldDraggable
{
    [FieldHeader("Item")]
    [SerializeField] private InventoryItemDefinition itemDefinition;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField] private string saveId;

    [FieldHeader("References")]
    [SerializeField] private DragBody2D dragBody;
    [SerializeField] private Transform placementAnchor;
    [SerializeField] private InventoryTransferController transferController;

    [FieldHeader("Actions")]
    [SerializeField] private string dragLabel = "Drag";
    [SerializeField] private string storeLabel = "Store";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string dragGlyphId = "Primary";
    [SerializeField] private string storeGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";
    [SerializeField, Min(0.01f)] private float autoStoreTravelTime = 0.28f;

    [FieldHeader("Content")]
    [SerializeField, TextArea] private string inspectText;

    public event Action StoredToInventory;

    public InventoryItemDefinition ItemDefinition => itemDefinition;
    public string SaveId => ResolveSaveId();
    public int Quantity => quantity;
    public Room OwnerRoom => DragBody ? DragBody.OwnerRoom : GetComponentInParent<Room>(true);
    public bool SupportsDrag => DragBody && enabled && gameObject.activeInHierarchy;
    public bool IsDragging => DragBody && DragBody.IsDragging;
    public Transform RootTransform => DragBody ? DragBody.RootTransform : transform;
    public Transform PlacementAnchor => placementAnchor ? placementAnchor : RootTransform;
    public Vector3 RootPosition => RootTransform.position;
    public Quaternion RootRotation => RootTransform.rotation;

    private bool storeInProgress;

    private DragBody2D DragBody => dragBody ? dragBody : dragBody = GetComponent<DragBody2D>() ?? gameObject.GetOrAddComponent<DragBody2D>();
    private InventoryTransferController TransferController => transferController ? transferController : transferController = FindFirstObjectByType<InventoryTransferController>(FindObjectsInactive.Include);

    private void Awake()
    {
        ApplyRuntimeSetup();
    }

    private void Reset()
    {
        dragBody = GetComponent<DragBody2D>() ?? gameObject.GetOrAddComponent<DragBody2D>();
        placementAnchor = transform;
        EnsureSerializedSaveId();
        ApplyRuntimeSetup();
    }

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
        EnsureSerializedSaveId();
        ApplyRuntimeSetup();
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        actions.Add(new InteractionAction(this, InteractionMode.Drag, dragLabel, dragGlyphId, SupportsDrag, requiresApproach: false));
        if (itemDefinition)
        {
            bool canStore = context.Inventory
                && (!context.Inventory.IsFull || itemDefinition.CollectibleOnly && context.Inventory.Contains(itemDefinition));
            actions.Add(new InteractionAction(this, InteractionMode.Primary, storeLabel, storeGlyphId, canStore, requiresApproach: false, priority: 20));
            actions.Add(new InteractionAction(this, InteractionMode.Store, storeLabel, storeGlyphId, canStore, requiresApproach: false, priority: -5));
        }

        if (!string.IsNullOrWhiteSpace(GetInspectText()))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        switch (action.Mode)
        {
            case InteractionMode.Primary:
            case InteractionMode.Store:
                return TryStoreWithAnimation(context.Inventory);

            case InteractionMode.Drag:
                if (context.Pointer == null)
                {
                    return false;
                }

                BeginDrag(context.Pointer);
                return IsDragging || TransferController && TransferController.IsStoreTransfer(this);

            case InteractionMode.Inspect:
                string inspect = GetInspectText();
                if (string.IsNullOrWhiteSpace(inspect))
                {
                    return false;
                }

                InteractionFeedback.Show(inspect, this);
                return true;
        }

        return false;
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return SupportsDrag && DragBody.CanStartDrag(pointer);
    }

    public void BeginDrag(PointerContext pointer)
    {
        if (!CanStartDrag(pointer))
        {
            return;
        }

        if (!DragBody.BeginDrag(pointer))
        {
            return;
        }

        if (itemDefinition)
        {
            TransferController?.TryBeginStoreTransfer(this);
        }
    }

    public void UpdateDrag(PointerContext pointer)
    {
        if (pointer)
        {
            DragBody.UpdateDragScreen(pointer.ScreenPosition);
        }
    }

    public void EndDrag()
    {
        bool isStoreTransfer = TransferController && TransferController.IsStoreTransfer(this);
        DragBody.EndDrag(restoreInvalidPose: true);
        if (isStoreTransfer)
        {
            TransferController.EndStoreTransfer(cancelled: false);
        }
    }

    public bool TryGetInventoryEntry(out Inventory.Entry entry)
    {
        if (!itemDefinition)
        {
            entry = default;
            return false;
        }

        entry = new Inventory.Entry(itemDefinition, quantity);
        return true;
    }

    public void SuspendStoreTransfer()
    {
        DragBody.EndDrag(restoreInvalidPose: true);
        SetRootActive(false);
    }

    public bool ResumeStoreTransfer(PointerContext pointer, Vector2 screenPosition)
    {
        SetRootActive(true);
        return DragBody.BeginUnrestrictedDrag(pointer, screenPosition);
    }

    public void CancelStoreTransfer()
    {
        DragBody.RestoreLastValidPose();
        SetRootActive(true);
    }

    public void CompleteStoreToInventory()
    {
        StoredToInventory?.Invoke();
        DragBody.EndDrag(restoreInvalidPose: false);
        SetRootActive(false);
    }

    public void ConfigureWorldItem(InventoryItemDefinition definition, int entryQuantity, Room ownerRoom)
    {
        itemDefinition = definition;
        quantity = Mathf.Max(1, entryQuantity);
        DragBody.SetOwnerRoom(ownerRoom);
        SetRootActive(true);
    }

    public void SeedPlacementPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        DragBody.SeedPose(GetPlacementRootPosition(worldPosition), worldRotation);
    }

    public bool BeginPlacementFromInventory(PointerContext pointer, Vector2 screenPosition, Room ownerRoom)
    {
        DragBody.SetOwnerRoom(ownerRoom);
        if (ownerRoom && pointer.TryGetWorldPointAtDepth(screenPosition, RootPosition.z, out Vector3 startPoint))
        {
            Vector3 anchorPoint = ownerRoom.ClampPosition(startPoint);
            DragBody.SeedPose(GetPlacementRootPosition(anchorPoint), RootRotation);
        }

        return DragBody.BeginUnrestrictedDrag(pointer, screenPosition);
    }

    public void UpdatePlacementDrag(Vector2 screenPosition)
    {
        DragBody.UpdateDragScreen(screenPosition);
    }

    public bool CanPlaceInRoom()
    {
        return DragBody.CanPlace();
    }

    public bool TryGetCurrentValidPose(out Vector3 worldPosition, out Quaternion worldRotation)
    {
        return DragBody.TryGetValidPose(out worldPosition, out worldRotation);
    }

    public void FinishPlacementDrag(bool commit)
    {
        DragBody.EndDrag(restoreInvalidPose: !commit);
    }

    public bool TryStoreDirect(Inventory inventory)
    {
        storeInProgress = false;
        if (!inventory || !itemDefinition || !inventory.TryStoreAnywhere(itemDefinition, quantity))
        {
            return false;
        }

        CompleteStoreToInventory();
        return true;
    }

    public bool TryStoreWithAnimation(Inventory inventory)
    {
        if (storeInProgress
            || !inventory
            || !itemDefinition
            || inventory.IsFull && !(itemDefinition.CollectibleOnly && inventory.Contains(itemDefinition)))
        {
            return false;
        }

        storeInProgress = true;
        StartCoroutine(StoreWithAnimationRoutine(inventory));
        return true;
    }

    private IEnumerator StoreWithAnimationRoutine(Inventory inventory)
    {
        if (!inventory || !itemDefinition)
        {
            storeInProgress = false;
            yield break;
        }

        DragBody.EndDrag(restoreInvalidPose: false);
        Vector3 start = RootPosition;
        Vector3 target = ResolveInventoryTargetPosition(start);
        float elapsed = 0f;
        while (elapsed < autoStoreTravelTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / autoStoreTravelTime));
            RootTransform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        if (!TryStoreDirect(inventory))
        {
            storeInProgress = false;
        }
    }

    private Vector3 ResolveInventoryTargetPosition(Vector3 fallback)
    {
        InventoryHotbar hotbar = FindFirstObjectByType<InventoryHotbar>(FindObjectsInactive.Include);
        if (!hotbar) return fallback + Vector3.up * 1.5f;

        Canvas canvas = hotbar.GetComponentInParent<Canvas>(true);
        Camera camera = Camera.main;
        RectTransform rect = hotbar.transform as RectTransform;
        if (!canvas || !rect || !camera) return fallback + Vector3.up * 1.5f;

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.position);
        float depth = Mathf.Abs(camera.transform.position.z - fallback.z);
        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        world.z = fallback.z;
        return world;
    }

    private string GetInspectText()
    {
        if (!string.IsNullOrWhiteSpace(inspectText))
        {
            return inspectText;
        }

        return itemDefinition ? itemDefinition.Description : string.Empty;
    }

    private void ApplyRuntimeSetup()
    {
        this.ApplyWorldPresentation("WorldItem", "WorldItem");
    }

    private string ResolveSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId))
        {
            return saveId;
        }

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name;
        string itemId = itemDefinition ? itemDefinition.ItemId : "item";
        return $"{sceneName}:{itemId}:{GetHierarchyPath(transform)}";
    }

    private void EnsureSerializedSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId) || Application.isPlaying)
        {
            return;
        }

        saveId = Guid.NewGuid().ToString("N");
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (!current)
        {
            return string.Empty;
        }

        string path = current.name;
        while (current.parent)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private Vector3 GetPlacementRootPosition(Vector3 anchorPoint)
    {
        Transform anchor = PlacementAnchor;
        Vector3 rootOffset = RootPosition - anchor.position;
        Vector3 rootPosition = anchorPoint + rootOffset;
        rootPosition.z = RootPosition.z;
        return rootPosition;
    }

    private void SetRootActive(bool isActive)
    {
        GameObject rootObject = RootTransform ? RootTransform.gameObject : gameObject;
        if (rootObject)
        {
            rootObject.SetActive(isActive);
        }
    }
}
