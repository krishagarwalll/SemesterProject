using UnityEngine;

[DisallowMultipleComponent]
public class InventoryTransferController : MonoBehaviour
{
    private enum TransferMode
    {
        None = 0,
        WorldToInventory = 1,
        InventoryToWorld = 2
    }

    [SerializeField] private Inventory inventory;
    [SerializeField] private PointerContext pointer;
    [SerializeField] private InventoryHotbar hotbar;
    [SerializeField] private RoomTransitionService roomTransitionService;

    private TransferMode mode;
    private PickupItem activeWorldItem;
    private PickupItem previewItem;
    private Inventory.Entry sourceEntry;
    private int sourceIndex = -1;
    private Vector3 restorePosition;
    private Quaternion restoreRotation;
    private Room restoreRoom;

    private Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    private PointerContext Pointer => pointer ? pointer : pointer = FindFirstObjectByType<PointerContext>(FindObjectsInactive.Include);
    private InventoryHotbar Hotbar => hotbar ? hotbar : hotbar = FindFirstObjectByType<InventoryHotbar>(FindObjectsInactive.Include);
    private RoomTransitionService Rooms => roomTransitionService ? roomTransitionService : roomTransitionService = FindFirstObjectByType<RoomTransitionService>(FindObjectsInactive.Include);

    public bool IsActive => mode != TransferMode.None;
    public bool IsDragging(PickupItem item) => item && activeWorldItem == item && mode == TransferMode.WorldToInventory;
    public bool IsPlacingFromInventory => mode == TransferMode.InventoryToWorld;

    public bool TryBeginWorldTransfer(PickupItem item)
    {
        if (IsActive || !item || !SceneInventory || !Pointer || !item.ItemDefinition)
        {
            return false;
        }

        mode = TransferMode.WorldToInventory;
        activeWorldItem = item;
        sourceEntry = new Inventory.Entry(item.ItemDefinition, item.Quantity);
        sourceIndex = -1;
        restorePosition = item.RootPosition;
        restoreRotation = item.RootRotation;
        restoreRoom = item.OwnerRoom ? item.OwnerRoom : Rooms ? Rooms.ActiveRoom : null;
        return true;
    }

    public void EndWorldTransfer(bool cancelled = false)
    {
        if (mode != TransferMode.WorldToInventory || !activeWorldItem)
        {
            ClearTransferState();
            return;
        }

        if (!cancelled && Hotbar && Hotbar.TryGetInventoryDropTarget(Pointer.ScreenPosition, out int slotIndex, out bool overBackpack))
        {
            bool stored = slotIndex >= 0
                ? SceneInventory.TryInsert(slotIndex, sourceEntry.Definition, sourceEntry.Quantity)
                : overBackpack && SceneInventory.TryAdd(sourceEntry.Definition, sourceEntry.Quantity);

            if (stored)
            {
                activeWorldItem.DestroyTransferRoot();
                ClearTransferState();
                return;
            }
        }

        if (!cancelled && activeWorldItem.CanPlaceAtCurrentPosition() && activeWorldItem.TryGetCommittedPose(out Vector3 worldPosition, out Quaternion worldRotation))
        {
            Transform parent = GetRestoreParent();
            activeWorldItem.CompleteTransfer(worldPosition, worldRotation, parent);
            ClearTransferState();
            return;
        }

        activeWorldItem.CancelTransfer(restorePosition, restoreRotation, GetRestoreParent());
        ClearTransferState();
    }

    public bool TryBeginPlacementFromInventory(int inventoryIndex)
    {
        if (IsActive || !SceneInventory || !Pointer || !SceneInventory.TryGetEntry(inventoryIndex, out sourceEntry) || !sourceEntry.Definition || !sourceEntry.Definition.CanPlaceBackIntoWorld)
        {
            sourceEntry = default;
            return false;
        }

        GameObject prefab = sourceEntry.Definition.WorldPrefab;
        if (!prefab)
        {
            sourceEntry = default;
            return false;
        }

        GameObject previewObject = Instantiate(prefab);
        PickupItem pickupItem = previewObject.GetComponentInChildren<PickupItem>(true);
        if (!pickupItem)
        {
            Destroy(previewObject);
            sourceEntry = default;
            return false;
        }

        Room activeRoom = Rooms ? Rooms.ActiveRoom : null;
        if (activeRoom && activeRoom.ContentRoot)
        {
            previewObject.transform.SetParent(activeRoom.ContentRoot, true);
        }

        pickupItem.BindTransferRoot(previewObject.transform);
        pickupItem.ConfigureFromInventory(sourceEntry.Definition, sourceEntry.Quantity);
        if (!pickupItem.BeginInventoryPlacement(Pointer))
        {
            Destroy(previewObject);
            sourceEntry = default;
            return false;
        }

        mode = TransferMode.InventoryToWorld;
        previewItem = pickupItem;
        sourceIndex = inventoryIndex;
        restorePosition = default;
        restoreRotation = pickupItem.RootRotation;
        restoreRoom = activeRoom;
        return true;
    }

    public void EndPlacementDrag(Vector2 screenPosition, bool cancelled = false)
    {
        if (mode != TransferMode.InventoryToWorld || !previewItem)
        {
            ClearTransferState();
            return;
        }

        if (!cancelled
            && previewItem.CanPlaceAtCurrentPosition()
            && previewItem.TryGetCommittedPose(out Vector3 worldPosition, out Quaternion worldRotation)
            && SceneInventory.TryTakeAt(sourceIndex, out Inventory.Entry taken, sourceEntry.Quantity))
        {
            if (taken.Definition == sourceEntry.Definition)
            {
                previewItem.CompleteTransfer(worldPosition, worldRotation, GetRestoreParent());
                ClearTransferState();
                return;
            }

            SceneInventory.TryInsert(sourceIndex, taken.Definition, taken.Quantity);
        }

        previewItem.DestroyTransferRoot();
        ClearTransferState();
    }

    private Transform GetRestoreParent()
    {
        return restoreRoom && restoreRoom.ContentRoot ? restoreRoom.ContentRoot : null;
    }

    private void ClearTransferState()
    {
        mode = TransferMode.None;
        activeWorldItem = null;
        previewItem = null;
        sourceEntry = default;
        sourceIndex = -1;
        restoreRoom = null;
        restorePosition = default;
        restoreRotation = default;
    }
}
