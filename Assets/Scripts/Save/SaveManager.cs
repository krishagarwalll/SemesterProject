using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SaveManager : MonoBehaviour
{
    private const string SaveKey = "GameSave_v1";

    [SerializeField] private Quest[] allQuests;
    [SerializeField] private InventoryItemDefinition[] allItemDefinitions;

    public static SaveManager Instance { get; private set; }

    private ISaveStorage storage;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        storage = Application.platform == RuntimePlatform.WebGLPlayer
            ? (ISaveStorage)new PlayerPrefsSaveStorage()
            : new FileSaveStorage();
    }

    // ── Public API ───────────────────────────────────────────────

    public bool HasSave() => storage.Exists(SaveKey);

    public void Save()
    {
        SaveData data = GatherCurrentState();
        storage.Write(SaveKey, JsonUtility.ToJson(data, prettyPrint: true));
        Debug.Log("[SaveManager] Game saved.");
    }

    public void DeleteSave()
    {
        storage.Delete(SaveKey);
        Debug.Log("[SaveManager] Save deleted.");
    }

    public void LoadAndApply()
    {
        if (!storage.TryRead(SaveKey, out string json))
        {
            Debug.LogWarning("[SaveManager] No save data found.");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (data.sceneName != currentScene)
        {
            SceneManager.LoadScene(data.sceneName);
            // Apply the rest after the scene loads — wire this up via OnSceneLoaded if needed
            return;
        }

        ApplySaveData(data);
    }

    // Call this after a scene has been loaded if the scene was different from the save
    public void ApplySaveData(SaveData data)
    {
        RestorePlayerPosition(data);
        RestoreProgression(data);
        RestoreInventory(data);
        RestoreQuests(data);
        RestorePickedUpWorldItems(data);
    }

    // ── Gathering ────────────────────────────────────────────────

    private SaveData GatherCurrentState()
    {
        var data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name;

        var player = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Exclude);
        if (player)
        {
            data.playerX = player.transform.position.x;
            data.playerY = player.transform.position.y;
        }

        var progression = FindFirstObjectByType<PlayerEventProgression>(FindObjectsInactive.Exclude);
        if (progression)
            data.progressionIndex = progression.progressionIndex;

        var inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Exclude);
        if (inventory)
        {
            foreach (var entry in inventory.Entries)
            {
                if (entry.IsOccupied)
                    data.inventoryItems.Add(new SavedInventoryItem { itemId = entry.Definition.ItemId, quantity = entry.Quantity });
            }
        }

        if (QuestController.Instance != null)
        {
            foreach (var qp in QuestController.Instance.activateQuests)
            {
                var saved = new SavedQuest { questId = qp.QuestID, readyToHandIn = qp.readyToHandIn };
                foreach (var obj in qp.objectives)
                    saved.objectiveAmounts.Add(obj.currentAmount);
                data.activeQuests.Add(saved);
            }
            data.handedInQuestIds = new List<string>(QuestController.Instance.handInQuestIDs);
        }

        var worldItems = FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in worldItems)
        {
            if (!string.IsNullOrEmpty(item.SaveId) && !item.gameObject.activeSelf)
                data.pickedUpWorldItemIds.Add(item.SaveId);
        }

        return data;
    }

    // ── Restoration ──────────────────────────────────────────────

    private void RestorePlayerPosition(SaveData data)
    {
        var player = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Exclude);
        if (player)
            player.TryWarp(new Vector3(data.playerX, data.playerY, player.transform.position.z));
    }

    private void RestoreProgression(SaveData data)
    {
        var progression = FindFirstObjectByType<PlayerEventProgression>(FindObjectsInactive.Exclude);
        if (progression)
            progression.progressionIndex = data.progressionIndex;
    }

    private void RestoreInventory(SaveData data)
    {
        var inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Exclude);
        if (!inventory || allItemDefinitions == null) return;

        var defLookup = BuildItemLookup();
        foreach (var saved in data.inventoryItems)
        {
            if (defLookup.TryGetValue(saved.itemId, out InventoryItemDefinition def))
                inventory.TryAdd(def, saved.quantity);
        }
    }

    private void RestoreQuests(SaveData data)
    {
        if (QuestController.Instance == null || allQuests == null) return;

        var questLookup = BuildQuestLookup();
        foreach (var saved in data.activeQuests)
        {
            if (!questLookup.TryGetValue(saved.questId, out Quest quest)) continue;
            QuestController.Instance.AcceptQuest(quest);

            if (saved.readyToHandIn)
                QuestController.Instance.MarkQuestReadyToHandIn(saved.questId);
        }

        foreach (var handedInId in data.handedInQuestIds)
        {
            if (!QuestController.Instance.handInQuestIDs.Contains(handedInId))
                QuestController.Instance.handInQuestIDs.Add(handedInId);
        }
    }

    private void RestorePickedUpWorldItems(SaveData data)
    {
        if (data.pickedUpWorldItemIds == null || data.pickedUpWorldItemIds.Count == 0) return;
        var worldItems = FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in worldItems)
        {
            if (!string.IsNullOrEmpty(item.SaveId) && data.pickedUpWorldItemIds.Contains(item.SaveId))
                item.gameObject.SetActive(false);
        }
    }

    // ── Lookup builders ──────────────────────────────────────────

    private Dictionary<string, InventoryItemDefinition> BuildItemLookup()
    {
        var lookup = new Dictionary<string, InventoryItemDefinition>();
        if (allItemDefinitions == null) return lookup;
        foreach (var def in allItemDefinitions)
        {
            if (def && !lookup.ContainsKey(def.ItemId))
                lookup[def.ItemId] = def;
        }
        return lookup;
    }

    private Dictionary<string, Quest> BuildQuestLookup()
    {
        var lookup = new Dictionary<string, Quest>();
        if (allQuests == null) return lookup;
        foreach (var quest in allQuests)
        {
            if (quest && !lookup.ContainsKey(quest.questID))
                lookup[quest.questID] = quest;
        }
        return lookup;
    }
}
