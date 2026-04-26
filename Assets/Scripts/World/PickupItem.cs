using System.Collections.Generic;
using System;
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

    [FieldHeader("Content")]
    [SerializeField, TextArea] private string inspectText;

    public InventoryItemDefinition ItemDefinition => itemDefinition;
    public string SaveId => ResolveSaveId();
    public int Quantity => quantity;
    public Room OwnerRoom => DragBody ? DragBody.OwnerRoom : GetComponentInParent<Room>(true);
    public bool SupportsDrag => itemDefinition && DragBody && enabled && gameObject.activeInHierarchy;
    public bool IsDragging => DragBody && DragBody.IsDragging;
    public Transform RootTransform => DragBody ? DragBody.RootTransform : transform;
    public Transform PlacementAnchor => placementAnchor ? placementAnchor : RootTransform;
    public Vector3 RootPosition => RootTransform.position;
    public Quaternion RootRotation => RootTransform.rotation;

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
        if (!itemDefinition)
        {
            return;
        }

        actions.Add(new InteractionAction(this, InteractionMode.Drag, dragLabel, dragGlyphId, SupportsDrag, requiresApproach: false));
        bool canStore = context.Inventory && (!context.Inventory.IsFull || context.Inventory.Contains(itemDefinition));
        actions.Add(new InteractionAction(this, InteractionMode.Store, storeLabel, storeGlyphId, canStore, requiresApproach: false, priority: -5));
        if (!string.IsNullOrWhiteSpace(GetInspectText()))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        switch (action.Mode)
        {
            case InteractionMode.Store:
                return TryStoreDirect(context.Inventory);

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

        TransferController?.TryBeginStoreTransfer(this);
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

    private bool TryStoreDirect(Inventory inventory)
    {
        if (!inventory || !itemDefinition || !inventory.TryStoreAnywhere(itemDefinition, quantity))
        {
            return false;
        }

        CompleteStoreToInventory();
        return true;
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
