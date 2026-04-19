using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class RoomPortal : MonoBehaviour, IInteractionActionProvider
{
    [SerializeField] private RoomPortal linkedPortal;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PortalTraversalMode traversalMode = PortalTraversalMode.Bidirectional;
    [SerializeField] private PortalLockMode lockMode;
    [SerializeField] private string requiredFlag;
    [SerializeField] private InventoryItemDefinition requiredItem;
    [SerializeField] private bool consumeRequiredItem;
    [SerializeField] private string enterLabel = "Enter";
    [SerializeField] private string lockedLabel = "Locked";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string primaryGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";
    [SerializeField, TextArea] private string lockedInspectText;
    [SerializeField, TextArea] private string inspectText;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterRoom;
    [SerializeField] private AudioClip lockedDoor;



    private RoomTransitionService transitionService;
    private RoomStateFlags stateFlags;
    [SerializeField] private bool startUnlocked;
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

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        bool unlocked = IsUnlocked();
        bool canUnlockFromInventory = !unlocked && HasRequiredItemInInventory(context);
        bool effectivelyUnlocked = unlocked || canUnlockFromInventory;
        bool canTraverse = CanTraverseFromThisSide && linkedPortal && linkedPortal.OwnerRoom && linkedPortal.CanReceiveTraversal;
        string label = effectivelyUnlocked ? enterLabel : lockedLabel;
        actions.Add(new InteractionAction(this, InteractionMode.Primary, label, primaryGlyphId, effectivelyUnlocked && canTraverse));

        if (!unlocked && lockMode == PortalLockMode.RequiredItem && context.SelectedItem && requiredItem == context.SelectedItem)
        {
            actions.Add(new InteractionAction(this, InteractionMode.UseSelectedItem, enterLabel, primaryGlyphId, canTraverse, requiresApproach: false, priority: 10));
        }

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

                //return TransitionService && TransitionService.TryTraverse(this, fadeDuration);
                {
                    bool success = TransitionService && TransitionService.TryTraverse(this, fadeDuration);

                    if (success)
                    {
                        PlayEnterSound();
                    }

                    return success;
                }



            case InteractionMode.UseSelectedItem:
                if (lockMode != PortalLockMode.RequiredItem || context.SelectedItem != requiredItem || !CanTraverseFromThisSide || !linkedPortal || !linkedPortal.CanReceiveTraversal)
                {
                    return false;
                }

                if (consumeRequiredItem && context.Inventory)
                {
                    context.Inventory.TryRemove(requiredItem);
                }

                unlockedByItem = true;
                //return TransitionService && TransitionService.TryTraverse(this, fadeDuration);
                {
                    bool success = TransitionService && TransitionService.TryTraverse(this, fadeDuration);

                    if (success)
                    {
                        PlayEnterSound();
                    }

                    return success;
                }


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

    private void PlayEnterSound()
    {
        if (audioSource != null && enterRoom != null)
        {
            audioSource.PlayOneShot(enterRoom);
        }
    }

    private void PlayLockedSound()
    {
        if (audioSource != null && lockedDoor != null)
        {
            audioSource.PlayOneShot(lockedDoor);
        }
    }

    public bool CanTraverseFromThisSide => traversalMode != PortalTraversalMode.ExitOnly;
    public bool CanReceiveTraversal => traversalMode != PortalTraversalMode.EntryOnly;

    private bool IsUnlocked()
    {
        return lockMode switch
        {
            PortalLockMode.None => true,
            PortalLockMode.Flag => Flags && Flags.HasFlag(requiredFlag),
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
