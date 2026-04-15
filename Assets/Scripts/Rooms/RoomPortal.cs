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

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        bool unlocked = IsUnlocked();
        bool canTraverse = CanTraverseFromThisSide && linkedPortal && linkedPortal.OwnerRoom && linkedPortal.CanReceiveTraversal;
        string label = unlocked ? enterLabel : lockedLabel;
        actions.Add(new InteractionAction(this, InteractionMode.Primary, label, primaryGlyphId, unlocked && canTraverse));

        if (!unlocked && lockMode == PortalLockMode.RequiredItem && context.SelectedItem && requiredItem == context.SelectedItem)
        {
            actions.Add(new InteractionAction(this, InteractionMode.UseSelectedItem, enterLabel, primaryGlyphId, canTraverse, requiresApproach: false, priority: 10));
        }

        string inspect = GetInspectText(unlocked);
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
                if (!IsUnlocked() || !CanTraverseFromThisSide || !linkedPortal || !linkedPortal.CanReceiveTraversal)
                {
                    InteractionFeedback.Show(GetInspectText(unlocked: false), this);
                    return false;
                }

                return TransitionService && TransitionService.TryTraverse(this, fadeDuration);

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
                return TransitionService && TransitionService.TryTraverse(this, fadeDuration);

            case InteractionMode.Inspect:
                string inspect = GetInspectText(IsUnlocked());
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
