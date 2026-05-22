using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(PickupItem))]
public class MemoryFragmentCutscene : MonoBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private VideoClip cutsceneClip;
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
        if (!cutsceneClip)
        {
            return;
        }

        MemoryFragmentCutscenePlayer player = MemoryFragmentCutscenePlayer.Instance;
        if (player)
        {
            player.Play(cutsceneClip, ResolveSaveId(), playOnce);
        }
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
