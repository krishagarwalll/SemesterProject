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

    [FieldHeader("References")]
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

        if (storePhase == StorePhase.WorldDrag && (overInventory || !canWorldPreview))
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

    public bool TryDropEntryToWorld(int inventoryIndex)
    {
        if (IsActive
            || !SceneInventory
            || !SceneInventory.TryGetEntry(inventoryIndex, out Inventory.Entry entry)
            || !entry.Definition
            || !entry.Definition.CanPlaceBackIntoWorld
            || !entry.Definition.WorldPrefab)
        {
            return false;
        }

        Room activeRoom = Rooms ? Rooms.ActiveRoom : null;
        if (!activeRoom)
        {
            return false;
        }

        GameObject root = Instantiate(entry.Definition.WorldPrefab);
        PickupItem droppedItem = root.GetComponentInChildren<PickupItem>(true);
        if (!droppedItem)
        {
            Destroy(root);
            return false;
        }

        if (activeRoom.ContentRoot)
        {
            root.transform.SetParent(activeRoom.ContentRoot, true);
        }

        droppedItem.ConfigureWorldItem(entry.Definition, entry.Quantity, activeRoom);
        if (!TrySeedDirectDropPose(droppedItem, activeRoom))
        {
            Destroy(root);
            return false;
        }

        if (!SceneInventory.TryTakeAt(inventoryIndex, out Inventory.Entry takenEntry, entry.Quantity)
            || takenEntry.Definition != entry.Definition)
        {
            Destroy(root);
            return false;
        }

        return true;
    }

    public bool TryUseEntryOnWorldTarget(int inventoryIndex, Vector2 screenPosition)
    {
        if (IsActive
            || !SceneInventory
            || !SceneInventory.TryGetEntry(inventoryIndex, out Inventory.Entry entry)
            || !entry.Definition
            || entry.Definition.CollectibleOnly
            || !TryFindRoomPortal(screenPosition, out RoomPortal portal))
        {
            return false;
        }

        return portal.TryUseInventoryItem(SceneInventory, entry.Definition, entry.Quantity);
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

    private Vector3 ResolveDirectDropPosition(Room room)
    {
        if (!room)
        {
            return Vector3.zero;
        }

        if (Pointer && Pointer.Actor)
        {
            return room.ClampPosition(Pointer.Actor.transform.position);
        }

        if (room.DefaultAnchor)
        {
            return room.ClampPosition(room.DefaultAnchor.transform.position);
        }

        return room.ClampPosition(room.transform.position);
    }

    private bool TrySeedDirectDropPose(PickupItem item, Room room)
    {
        if (!item || !room)
        {
            return false;
        }

        Vector3 origin = ResolveDirectDropPosition(room);
        if (TrySeedAndValidate(item, origin))
        {
            return true;
        }

        float[] radii = { 0.45f, 0.9f, 1.35f };
        Vector2[] directions =
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down,
            new(1f, 1f),
            new(-1f, 1f),
            new(1f, -1f),
            new(-1f, -1f)
        };

        for (int radiusIndex = 0; radiusIndex < radii.Length; radiusIndex++)
        {
            for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                Vector2 direction = directions[directionIndex].normalized;
                Vector3 candidate = origin + (Vector3)(direction * radii[radiusIndex]);
                candidate = room.ClampPosition(candidate);
                if (TrySeedAndValidate(item, candidate))
                {
                    return true;
                }
            }
        }

        if (room.DefaultAnchor && TrySeedAndValidate(item, room.ClampPosition(room.DefaultAnchor.transform.position)))
        {
            return true;
        }

        return false;
    }

    private static bool TrySeedAndValidate(PickupItem item, Vector3 position)
    {
        item.SeedPlacementPose(position, item.RootRotation);
        return item.CanPlaceInRoom();
    }

    private bool TryFindRoomPortal(Vector2 screenPosition, out RoomPortal portal)
    {
        portal = null;
        if (!Pointer)
        {
            return false;
        }

        Camera camera = Pointer.WorldCamera ? Pointer.WorldCamera : Camera.main;
        float depth = 0f;
        if (camera)
        {
            depth = Mathf.Abs(camera.transform.position.z);
        }

        Vector3 worldPoint = camera
            ? camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth))
            : (Vector3)screenPosition;

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        int bestPriority = int.MinValue;
        for (int i = 0; i < hits.Length; i++)
        {
            RoomPortal candidate = hits[i] ? hits[i].GetComponentInParent<RoomPortal>() : null;
            if (!candidate)
            {
                continue;
            }

            InteractionTarget target = candidate.GetComponent<InteractionTarget>();
            int priority = target ? target.SelectionPriority : 0;
            if (!portal || priority >= bestPriority)
            {
                portal = candidate;
                bestPriority = priority;
            }
        }

        return portal;
    }
}
