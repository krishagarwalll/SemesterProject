using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItem", menuName = "How To Get To Heaven/Inventory Item")]
public class InventoryItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string description;
    [SerializeField] private GameObject worldPrefab;
    [SerializeField] private string primaryLabel = "Use";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private bool canPlaceBackIntoWorld = true;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public GameObject WorldPrefab => worldPrefab;
    public string PrimaryLabel => string.IsNullOrWhiteSpace(primaryLabel) ? "Use" : primaryLabel;
    public string InspectLabel => string.IsNullOrWhiteSpace(inspectLabel) ? "Inspect" : inspectLabel;
    public bool CanPlaceBackIntoWorld => canPlaceBackIntoWorld && worldPrefab;
}
