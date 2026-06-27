using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class RoomPortal : MonoBehaviour, IInteractionActionProvider
{
    [FieldHeader("Connection")]
    [SerializeField] private string saveId;
    [SerializeField] private RoomPortal linkedPortal;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PortalTraversalMode traversalMode = PortalTraversalMode.Bidirectional;

    [FieldHeader("Labels")]
    [SerializeField] private string enterLabel = "Enter";
    [SerializeField] private string lockedLabel = "Locked";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string primaryGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";
    [SerializeField, TextArea] private string inspectText;

    [FieldHeader("Lock")]
    [SerializeField] private PortalLockMode lockMode;
    [ConditionalField("lockMode", (int)PortalLockMode.Flag, header: "Flag Lock")]
    [SerializeField] private string requiredFlag;
    [ConditionalField("lockMode", (int)PortalLockMode.RequiredItem, header: "Item Lock")]
    [SerializeField] private InventoryItemDefinition requiredItem;
    [ConditionalField("lockMode", (int)PortalLockMode.RequiredItem)]
    [SerializeField, Min(1)] private int requiredItemQuantity = 1;
    [ConditionalField("lockMode", (int)PortalLockMode.RequiredItem)]
    [SerializeField] private bool consumeRequiredItem;
    [ConditionalField("lockMode", (int)PortalLockMode.RequiredItem)]
    [Tooltip("Skip the item check — portal starts pre-unlocked. Useful for testing or one-time events.")]
    [SerializeField] private bool startUnlocked;
    [ConditionalField("lockMode", (int)PortalLockMode.RequiredItem)]
    [Tooltip("If true, the required item must be dragged from inventory onto this portal. Clicking the portal while the item is in inventory only shows guidance.")]
    [SerializeField] private bool requireDraggedRequiredItemToUnlock;
    [ConditionalField("lockMode", (int)PortalLockMode.None, invertEnumMatch: true)]
    [SerializeField, TextArea] private string lockedInspectText;

    [FieldHeader("Transition")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

    [Header("Quest")]
    [Tooltip("Optional. Accepted when the player tries this locked portal without the required item quantity.")]
    [SerializeField] private Quest questToStartWhenLocked;
    [Tooltip("Optional. When the lock is opened by consuming the required item, this quest is handed in immediately.")]
    [SerializeField] private Quest questToCompleteOnUnlock;

    [FieldHeader("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterRoom;
    [SerializeField] private AudioClip lockedDoor;

    private RoomTransitionService transitionService;
    private RoomStateFlags stateFlags;
    private bool unlockedByItem;
    private int RequiredItemQuantity => Mathf.Max(1, requiredItemQuantity);
    private bool RequiresDraggedRequiredItem => requireDraggedRequiredItemToUnlock || IsGarageKeyRequirement();

    public RoomPortal LinkedPortal => linkedPortal;
    public Room OwnerRoom => GetComponentInParent<Room>(true);
    public Transform SpawnPoint => spawnPoint ? spawnPoint : transform;
    public string SaveId => ResolveSaveId();
    public bool WasUnlockedByItem => unlockedByItem;

    private RoomTransitionService TransitionService => transitionService ? transitionService : transitionService = FindFirstObjectByType<RoomTransitionService>(FindObjectsInactive.Include);
    private RoomStateFlags Flags => stateFlags ? stateFlags : stateFlags = FindFirstObjectByType<RoomStateFlags>(FindObjectsInactive.Include);

    private void Reset()
    {
        spawnPoint = transform;
        EnsureSerializedSaveId();
    }

    private void Awake()
    {
        unlockedByItem = startUnlocked;
    }

    private void OnValidate()
    {
        requiredItemQuantity = Mathf.Max(1, requiredItemQuantity);
        EnsureSerializedSaveId();
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        bool unlocked = IsUnlocked();
        bool canUnlockFromInventory = !unlocked && HasRequiredItemInInventory(context);
        bool effectivelyUnlocked = unlocked || canUnlockFromInventory && !RequiresDraggedRequiredItem;
        bool canTraverse = CanTraverseFromThisSide && linkedPortal && linkedPortal.OwnerRoom && linkedPortal.CanReceiveTraversal;

        string label = effectivelyUnlocked ? enterLabel : lockedLabel;

        // Priority above NPC dialogue (default 20) so traversal wins when near a door.
        // Always enable so locked doors show the locked message on click rather than silently ignoring.
        int primaryPriority = effectivelyUnlocked ? 30 : 0;
        actions.Add(new InteractionAction(this, InteractionMode.Primary, label, primaryGlyphId,
            enabled: canTraverse || !effectivelyUnlocked, priority: primaryPriority));

        string inspect = GetInspectText(effectivelyUnlocked);
        if (!string.IsNullOrWhiteSpace(inspect))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        switch (action.Mode)
        {
            case InteractionMode.Primary:
                if (!IsUnlocked())
                {
                    if (HasRequiredItemInInventory(context) && !RequiresDraggedRequiredItem)
                    {
                        UnlockWithRequiredItem(context.Inventory);
                    }
                    else
                    {
                        TryStartLockedQuest();
                        PlayLockedSound();
                        InteractionFeedback.Show(GetLockedFeedback(context), this);
                        return false;
                    }
                }

                if (!CanTraverseFromThisSide || !linkedPortal || !linkedPortal.CanReceiveTraversal)
                {
                    InteractionFeedback.Show(GetInspectText(unlocked: false), this);
                    return false;
                }

                if (!linkedPortal.OwnerRoom)
                {
                    Debug.LogWarning($"[RoomPortal] {name}: linked portal '{linkedPortal.name}' has no owner Room.", this);
                    return false;
                }

                bool traverseSuccess = TransitionService && TransitionService.TryTraverse(this, fadeDuration);
                if (traverseSuccess) PlayEnterSound();
                return traverseSuccess;

            case InteractionMode.Inspect:
                string inspect = GetInspectText(IsEffectivelyUnlocked(context));
                if (string.IsNullOrWhiteSpace(inspect))
                {
                    return false;
                }
                InteractionFeedback.Show(inspect, this);
                return true;
        }

        return false;
    }

    public bool CanTraverseFromThisSide => traversalMode != PortalTraversalMode.ExitOnly;
    public bool CanReceiveTraversal => traversalMode != PortalTraversalMode.EntryOnly;
    public bool IsCurrentlyUnlocked => IsUnlocked();

    public bool TryUseInventoryItem(Inventory inventory, InventoryItemDefinition definition, int quantity)
    {
        if (IsUnlocked()
            || lockMode != PortalLockMode.RequiredItem
            || !requiredItem
            || definition != requiredItem
            || quantity < RequiredItemQuantity
            || !CanTraverseFromThisSide
            || !linkedPortal
            || !linkedPortal.CanReceiveTraversal)
        {
            return false;
        }

        UnlockWithRequiredItem(inventory);
        PlayEnterSound();
        return TransitionService && TransitionService.TryTraverse(this, fadeDuration);
    }

    private bool HasRequiredItemInInventory(in InteractionContext context)
    {
        return lockMode == PortalLockMode.RequiredItem
            && requiredItem
            && context.Inventory
            && context.Inventory.CountItem(requiredItem.ItemId) >= RequiredItemQuantity;
    }

    private bool IsEffectivelyUnlocked(in InteractionContext context)
    {
        return IsUnlocked() || HasRequiredItemInInventory(context) && !RequiresDraggedRequiredItem;
    }

    private bool IsUnlocked()
    {
        return lockMode switch
        {
            PortalLockMode.None => true,
            PortalLockMode.Flag => Flags != null && Flags.HasFlag(requiredFlag),
            PortalLockMode.RequiredItem => unlockedByItem,
            _ => true
        };
    }

    public void RestoreUnlockedByItem(bool value)
    {
        unlockedByItem = startUnlocked || value;
    }

    private string ResolveSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId))
        {
            return saveId;
        }

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name;
        return $"{sceneName}:portal:{GetHierarchyPath(transform)}";
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

    private string GetInspectText(bool unlocked)
    {
        if (!unlocked && !string.IsNullOrWhiteSpace(lockedInspectText))
        {
            return lockedInspectText;
        }
        return inspectText;
    }

    private string GetLockedFeedback(in InteractionContext context)
    {
        if (RequiresDraggedRequiredItem && HasRequiredItemInInventory(context))
        {
            return "The garage key might fit. Drag it from your inventory onto the locked door.";
        }

        return GetInspectText(unlocked: false);
    }

    private void UnlockWithRequiredItem(Inventory inventory)
    {
        if (consumeRequiredItem && inventory)
        {
            inventory.TryRemove(requiredItem, RequiredItemQuantity);
        }

        unlockedByItem = true;
        TryHandInUnlockQuest();
        SaveManager.Instance?.MarkRoomPortalUnlocked(SaveId);
    }

    private bool IsGarageKeyRequirement()
    {
        return lockMode == PortalLockMode.RequiredItem
            && requiredItem
            && requiredItem.ItemId == "rusty_key";
    }

    private void TryHandInUnlockQuest()
    {
        if (questToCompleteOnUnlock == null || QuestController.Instance == null)
        {
            return;
        }

        string questId = questToCompleteOnUnlock.questID;
        if (string.IsNullOrWhiteSpace(questId) || QuestController.Instance.isQuestHandedIn(questId))
        {
            return;
        }

        if (!QuestController.Instance.isQuestActive(questId))
        {
            QuestController.Instance.AcceptQuest(questToCompleteOnUnlock);
        }

        QuestController.Instance.MarkQuestReadyToHandIn(questId);
        QuestController.Instance.CompleteQuest(questId);
    }

    private void TryStartLockedQuest()
    {
        if (questToStartWhenLocked == null || QuestController.Instance == null)
        {
            return;
        }

        string questId = questToStartWhenLocked.questID;
        if (string.IsNullOrWhiteSpace(questId)
            || QuestController.Instance.isQuestActive(questId)
            || QuestController.Instance.isQuestHandedIn(questId))
        {
            return;
        }

        QuestController.Instance.AcceptQuest(questToStartWhenLocked);
    }

    private void PlayEnterSound()
    {
        if (audioSource && enterRoom)
        {
            audioSource.PlayOneShot(enterRoom);
        }
    }

    private void PlayLockedSound()
    {
        if (audioSource && lockedDoor)
        {
            audioSource.PlayOneShot(lockedDoor);
        }
    }

    private void OnDrawGizmos()
    {
        if (!linkedPortal)
        {
            return;
        }

        Gizmos.color = IsUnlocked() ? new Color(0.2f, 0.9f, 0.2f, 0.5f) : new Color(0.9f, 0.3f, 0.2f, 0.5f);
        Vector3 from = transform.position;
        Vector3 to = linkedPortal.transform.position;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}

public enum PortalTraversalMode
{
    EntryOnly = 0,
    ExitOnly = 1,
    Bidirectional = 2
}

public enum PortalLockMode
{
    None = 0,
    Flag = 1,
    RequiredItem = 2
}
