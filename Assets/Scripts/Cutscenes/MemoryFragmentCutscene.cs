using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PickupItem))]
public class MemoryFragmentCutscene : MonoBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private string cutsceneFileName;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private string cutsceneSaveId;
    [SerializeField] private PickupItem pickupItem;

    private PickupItem PickupItem => pickupItem ? pickupItem : pickupItem = GetComponent<PickupItem>();

    private void Reset()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    private void Awake()
    {
        if (PickupItem)
        {
            PickupItem.StoredToInventory += HandleStoredToInventory;
        }
    }

    private void OnDestroy()
    {
        if (PickupItem)
        {
            PickupItem.StoredToInventory -= HandleStoredToInventory;
        }
    }

    private void HandleStoredToInventory()
    {
        if (string.IsNullOrWhiteSpace(cutsceneFileName))
        {
            Debug.LogWarning($"[{nameof(MemoryFragmentCutscene)}] '{name}' has no cutscene file name.", this);
            return;
        }

        MemoryFragmentCutscenePlayer player = MemoryFragmentCutscenePlayer.Instance;
        if (!player)
        {
            Debug.LogError(
                $"[{nameof(MemoryFragmentCutscene)}] No {nameof(MemoryFragmentCutscenePlayer)} is available for '{name}'.",
                this);
            return;
        }

        player.Play(cutsceneFileName, ResolveSaveId(), playOnce);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryAutoStoreFromContact(other);

    private void OnCollisionEnter2D(Collision2D collision) => TryAutoStoreFromContact(collision.collider);

    private void TryAutoStoreFromContact(Collider2D other)
    {
        if (!other || !other.GetComponentInParent<PoptropicaController>())
        {
            return;
        }

        Inventory inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        PickupItem?.TryStoreWithAnimation(inventory);
    }

    private string ResolveSaveId()
    {
        if (!string.IsNullOrWhiteSpace(cutsceneSaveId))
        {
            return cutsceneSaveId;
        }

        if (PickupItem && !string.IsNullOrWhiteSpace(PickupItem.SaveId))
        {
            return $"MemoryFragmentCutscene:{PickupItem.SaveId}";
        }

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name;
        return $"MemoryFragmentCutscene:{sceneName}:{name}";
    }
}
