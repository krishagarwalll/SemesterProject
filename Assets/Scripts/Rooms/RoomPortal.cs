using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class RoomPortal : MonoBehaviour, IInteractionActionProvider
{
    [FieldHeader("Connection")]
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
    [SerializeField] private bool consumeRequiredItem;
    [ConditionalField("lockMode", (int)PortalLockMode.RequiredItem)]
    [Tooltip("Skip the item check — portal starts pre-unlocked. Useful for testing or one-time events.")]
    [SerializeField] private bool startUnlocked;
    [ConditionalField("lockMode", (int)PortalLockMode.None, invertEnumMatch: true)]
    [SerializeField, TextArea] private string lockedInspectText;

    [FieldHeader("Transition")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

    [Header("Quest")]
    [Tooltip("Optional. When the lock is opened by consuming the required item, this quest is handed in immediately.")]
    [SerializeField] private Quest questToCompleteOnUnlock;

    [FieldHeader("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterRoom;
    [SerializeField] private AudioClip lockedDoor;

    private RoomTransitionService transitionService;
    private RoomStateFlags stateFlags;
    private bool unlockedByItem;

    public RoomPortal LinkedPortal => linkedPortal;
    public Room OwnerRoom => GetComponentInParent<Room>(true);
    public Transform SpawnPoint => spawnPoint ? spawnPoint : transform;

    private RoomTransitionService TransitionService => transitionService ? transitionService : transitionService = FindFirstObjectByType<RoomTransitionService>(FindObjectsInactive.Include);
    private RoomStateFlags Flags => stateFlags ? stateFlags : stateFlags = FindFirstObjectByType<RoomStateFlags>(FindObjectsInactive.Include);

    private void Reset()
    {
        spawnPoint = transform;
    }

    private void Awake()
    {
        unlockedByItem = startUnlocked;
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        bool unlocked = IsUnlocked();
        bool canUnlockFromInventory = !unlocked && HasRequiredItemInInventory(context);
        bool effectivelyUnlocked = unlocked || canUnlockFromInventory;
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
                    if (lockMode == PortalLockMode.RequiredItem && requiredItem && context.Inventory && context.Inventory.Contains(requiredItem))
                    {
                        if (consumeRequiredItem)
                        {
                            context.Inventory.TryRemove(requiredItem);
                        }
                        unlockedByItem = true;
                        TryHandInUnlockQuest();
                    }
                    else
                    {
                        PlayLockedSound();
                        InteractionFeedback.Show(GetInspectText(unlocked: false), this);
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

    private bool HasRequiredItemInInventory(in InteractionContext context)
    {
        return lockMode == PortalLockMode.RequiredItem
            && requiredItem
            && context.Inventory
            && context.Inventory.Contains(requiredItem);
    }

    private bool IsEffectivelyUnlocked(in InteractionContext context)
    {
        return IsUnlocked() || HasRequiredItemInInventory(context);
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

    private string GetInspectText(bool unlocked)
    {
        if (!unlocked && !string.IsNullOrWhiteSpace(lockedInspectText))
        {
            return lockedInspectText;
        }
        return inspectText;
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
