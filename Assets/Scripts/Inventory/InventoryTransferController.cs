using UnityEngine;

[DisallowMultipleComponent]
public class InventoryTransferController : MonoBehaviour
{
    private enum TransferMode
    {
        None = 0,
        Store = 1,
        Placement = 2
    }

    private enum StorePhase
    {
        None = 0,
        WorldDrag = 1,
        UiGhost = 2
    }

    [SerializeField] private Inventory inventory;
    [SerializeField] private PointerContext pointer;
    [SerializeField] private InventoryHotbar hotbar;
    [SerializeField] private RoomTransitionService roomTransitionService;

    private TransferMode mode;
    private StorePhase storePhase;
    private PickupItem activeStoreItem;
    private PickupItem activePlacementItem;
    private GameObject activePlacementRoot;
    private Inventory.Entry sourceEntry;
    private int sourceIndex = -1;

    private Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    private PointerContext Pointer => pointer ? pointer : pointer = FindFirstObjectByType<PointerContext>(FindObjectsInactive.Include);
    private InventoryHotbar Hotbar => hotbar ? hotbar : hotbar = FindFirstObjectByType<InventoryHotbar>(FindObjectsInactive.Include);
    private RoomTransitionService Rooms => roomTransitionService ? roomTransitionService : roomTransitionService = FindFirstObjectByType<RoomTransitionService>(FindObjectsInactive.Include);

    public bool IsActive => mode != TransferMode.None;
    public bool IsPlacingFromInventory => mode == TransferMode.Placement;

    private void Update()
    {
        if (mode != TransferMode.Store || !activeStoreItem || !Pointer || !Hotbar)
        {
            return;
        }

        bool overInventory = Hotbar.IsInventoryArea(Pointer.ScreenPosition);
        bool canWorldPreview = !overInventory && CanPreviewPlacementAt(Pointer.ScreenPosition);

        if (storePhase == StorePhase.WorldDrag && overInventory)
        {
            activeStoreItem.SuspendStoreTransfer();
            storePhase = StorePhase.UiGhost;
            Hotbar.ShowTransferPreview(sourceEntry, Pointer.ScreenPosition);
        }

        if (storePhase == StorePhase.UiGhost)
        {
            if (canWorldPreview && activeStoreItem.ResumeStoreTransfer(Pointer, Pointer.ScreenPosition))
            {
                storePhase = StorePhase.WorldDrag;
                Hotbar.HideTransferPreview();
                return;
            }

            Hotbar.UpdateTransferPreview(Pointer.ScreenPosition);
        }
    }

    public bool IsStoreTransfer(PickupItem item)
    {
        return item && mode == TransferMode.Store && activeStoreItem == item;
    }

    public bool CanPreviewPlacementAt(Vector2 screenPosition)
    {
        if (!Pointer)
        {
            return false;
        }

        Room room = Rooms ? Rooms.ActiveRoom : null;
        if (!room || !Pointer.TryGetWorldPointAtDepth(screenPosition, room.DefaultItemDepth, out Vector3 pointerPoint))
        {
            return false;
        }

        return room.ContainsPoint(pointerPoint);
    }

    public bool TryBeginStoreTransfer(PickupItem item)
    {
        if (IsActive || !item || !item.TryGetInventoryEntry(out sourceEntry))
        {
            sourceEntry = default;
            return false;
        }

        mode = TransferMode.Store;
        storePhase = StorePhase.WorldDrag;
        activeStoreItem = item;
        sourceIndex = -1;
        return true;
    }

    public void EndStoreTransfer(bool cancelled = false)
    {
        if (mode != TransferMode.Store || !activeStoreItem)
        {
            ClearTransferState();
            return;
        }

        bool stored = false;
        if (!cancelled
            && storePhase == StorePhase.UiGhost
            && Pointer
            && Hotbar
            && SceneInventory
            && Hotbar.TryGetStoreDropTarget(Pointer.ScreenPosition, out int slotIndex, out bool overBackpack))
        {
            stored = slotIndex >= 0
                ? SceneInventory.TryStoreExact(slotIndex, sourceEntry.Definition, sourceEntry.Quantity)
                : overBackpack && SceneInventory.TryStoreAnywhere(sourceEntry.Definition, sourceEntry.Quantity);
        }

        if (stored)
        {
            activeStoreItem.CompleteStoreToInventory();
        }
        else if (storePhase == StorePhase.UiGhost)
        {
            activeStoreItem.CancelStoreTransfer();
        }

        ClearTransferState();
    }

    public bool TryBeginPlacementTransfer(int inventoryIndex, Vector2 screenPosition)
    {
        if (IsActive
            || !SceneInventory
            || !SceneInventory.TryGetEntry(inventoryIndex, out sourceEntry)
            || !sourceEntry.Definition
            || !sourceEntry.Definition.CanPlaceBackIntoWorld
            || !sourceEntry.Definition.WorldPrefab
            || !Pointer)
        {
            sourceEntry = default;
            return false;
        }

        GameObject root = Instantiate(sourceEntry.Definition.WorldPrefab);
        PickupItem placedItem = root.GetComponentInChildren<PickupItem>(true);
        if (!placedItem)
        {
            Destroy(root);
            sourceEntry = default;
            return false;
        }

        Room activeRoom = Rooms ? Rooms.ActiveRoom : null;
        if (activeRoom && activeRoom.ContentRoot)
        {
            root.transform.SetParent(activeRoom.ContentRoot, true);
        }

        placedItem.ConfigureWorldItem(sourceEntry.Definition, sourceEntry.Quantity, activeRoom);
        SeedPlacementPose(placedItem, activeRoom, screenPosition);
        if (!placedItem.BeginPlacementFromInventory(Pointer, screenPosition, activeRoom))
        {
            Destroy(root);
            sourceEntry = default;
            return false;
        }

        mode = TransferMode.Placement;
        activePlacementItem = placedItem;
        activePlacementRoot = root;
        sourceIndex = inventoryIndex;
        return true;
    }

    public void UpdatePlacementTransfer(Vector2 screenPosition)
    {
        if (mode == TransferMode.Placement && activePlacementItem)
        {
            activePlacementItem.UpdatePlacementDrag(screenPosition);
        }
    }

    public void EndPlacementTransfer(Vector2 screenPosition, bool cancelled = false)
    {
        if (mode != TransferMode.Placement || !activePlacementItem)
        {
            ClearTransferState();
            return;
        }

        activePlacementItem.UpdatePlacementDrag(screenPosition);
        bool committed = !cancelled
            && activePlacementItem.CanPlaceInRoom()
            && activePlacementItem.TryGetCurrentValidPose(out _, out _)
            && SceneInventory
            && SceneInventory.TryTakeAt(sourceIndex, out Inventory.Entry takenEntry, sourceEntry.Quantity)
            && takenEntry.Definition == sourceEntry.Definition;

        if (committed)
        {
            activePlacementItem.FinishPlacementDrag(commit: true);
            ClearTransferState();
            return;
        }

        activePlacementItem.FinishPlacementDrag(commit: false);
        if (activePlacementRoot)
        {
            Destroy(activePlacementRoot);
        }

        ClearTransferState();
    }

    public bool TryBeginWorldTransfer(PickupItem item)
    {
        return TryBeginStoreTransfer(item);
    }

    public void EndWorldTransfer(bool cancelled = false)
    {
        EndStoreTransfer(cancelled);
    }

    public bool TryBeginPlacementFromInventory(int inventoryIndex, Vector2 screenPosition)
    {
        return TryBeginPlacementTransfer(inventoryIndex, screenPosition);
    }

    public void EndPlacementDrag(Vector2 screenPosition, bool cancelled = false)
    {
        EndPlacementTransfer(screenPosition, cancelled);
    }

    private void ClearTransferState()
    {
        Hotbar?.HideTransferPreview();
        mode = TransferMode.None;
        storePhase = StorePhase.None;
        activeStoreItem = null;
        activePlacementItem = null;
        activePlacementRoot = null;
        sourceEntry = default;
        sourceIndex = -1;
    }

    private void SeedPlacementPose(PickupItem item, Room room, Vector2 screenPosition)
    {
        if (!item)
        {
            return;
        }

        Vector3 seedPosition = room && room.DefaultAnchor ? room.DefaultAnchor.transform.position : item.transform.position;
        if (Pointer && Pointer.TryGetWorldPointAtDepth(screenPosition, seedPosition.z, out Vector3 pointerPoint))
        {
            seedPosition = room ? room.ClampPosition(pointerPoint) : pointerPoint;
        }
        else if (room)
        {
            seedPosition = room.ClampPosition(seedPosition);
        }

        item.SeedPlacementPose(seedPosition, item.transform.rotation);
    }
}
