using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SaveManager : MonoBehaviour
{
    public const string SaveKey = "GameSave_v1";

    [SerializeField] private Quest[] allQuests;
    [SerializeField] private InventoryItemDefinition[] allItemDefinitions;

    public static SaveManager Instance { get; private set; }
    public static event Action SaveWritten;

    private ISaveStorage storage;
    private SaveData pendingSceneLoadData;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        storage = new PlayerPrefsSaveStorage();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    // ── Public API ───────────────────────────────────────────────

    public bool HasSave() => storage.Exists(SaveKey);

    public bool TryGetSaveJson(out string json) => storage.TryRead(SaveKey, out json);

    public bool TryGetSaveData(out SaveData data)
    {
        data = null;
        if (!storage.TryRead(SaveKey, out string json))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
            EnsureLists(data);
            return data != null;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[SaveManager] Save data could not be read: {exception.Message}");
            return false;
        }
    }

    public bool OverwriteSaveJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[SaveManager] Empty save JSON was not written.");
            return false;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
            {
                Debug.LogWarning("[SaveManager] Save JSON is invalid.");
                return false;
            }

            EnsureLists(data);
            WriteSave(data);
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[SaveManager] Save JSON could not be parsed: {exception.Message}");
            return false;
        }
    }

    public void Save()
    {
        WriteSave(GatherCurrentState());
    }

    public void SaveWithPlayerPosition(Vector3 position)
    {
        SaveData data = GatherCurrentState();
        data.playerX = position.x;
        data.playerY = position.y;
        WriteSave(data);
    }

    private void WriteSave(SaveData data)
    {
        try
        {
            storage.Write(SaveKey, JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log("[SaveManager] Game saved.");
            SaveWritten?.Invoke();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[SaveManager] Failed to save: {exception.Message}");
        }
    }

    public void DeleteSave()
    {
        storage.Delete(SaveKey);
        Debug.Log("[SaveManager] Save deleted.");
    }

    public bool IsCutsceneCompleted(string cutsceneId)
    {
        return ContainsSavedId(cutsceneId, data => data.completedCutsceneIds);
    }

    public void MarkCutsceneCompleted(string cutsceneId, bool writeImmediately = true)
    {
        AddSavedId(cutsceneId, data => data.completedCutsceneIds, writeImmediately);
    }

    public bool IsSceneEventTriggered(string eventId)
    {
        return ContainsSavedId(eventId, data => data.triggeredSceneEventIds);
    }

    public void MarkSceneEventTriggered(string eventId, bool writeImmediately = true)
    {
        AddSavedId(eventId, data => data.triggeredSceneEventIds, writeImmediately);
    }

    public void MarkRoomPortalUnlocked(string portalId, bool writeImmediately = true)
    {
        if (string.IsNullOrWhiteSpace(portalId))
        {
            return;
        }

        SaveData data = ReadSaveOrGatherCurrent();
        EnsureLists(data);
        SavedRoomPortal portal = data.roomPortals.Find(p => p != null && p.saveId == portalId);
        if (portal == null)
        {
            portal = new SavedRoomPortal { saveId = portalId };
            data.roomPortals.Add(portal);
        }

        portal.unlockedByItem = true;
        if (writeImmediately)
        {
            WriteSave(data);
        }
    }

    public bool MarkQuestHandedInInSave(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return false;
        if (!storage.TryRead(SaveKey, out string json)) return false;

        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(json); }
        catch { return false; }
        if (data == null) return false;

        if (data.activeQuests != null)
        {
            data.activeQuests.RemoveAll(q => q != null && q.questId == questId);
        }
        if (data.handedInQuestIds == null)
        {
            data.handedInQuestIds = new List<string>();
        }
        if (!data.handedInQuestIds.Contains(questId))
        {
            data.handedInQuestIds.Add(questId);
        }

        WriteSave(data);
        return true;
    }

    public void LoadAndApply()
    {
        if (!storage.TryRead(SaveKey, out string json))
        {
            Debug.LogWarning("[SaveManager] No save data found.");
            return;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[SaveManager] Save data could not be read: {exception.Message}");
            return;
        }

        if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
        {
            Debug.LogWarning("[SaveManager] Save data is invalid.");
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (data.sceneName != currentScene)
        {
            pendingSceneLoadData = data;
            PauseService.ClearAll();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(data.sceneName);
            return;
        }

        ApplySaveData(data);
    }

    // Call this after a scene has been loaded if the scene was different from the save
    public void ApplySaveData(SaveData data)
    {
        EnsureLists(data);
        RestoreCutscenes(data);
        RestorePlayerPosition(data);
        RestoreProgression(data);
        RestoreInventory(data);
        RestoreQuests(data);
        RestoreRoomState(data);
        RestoreDialogueLineStates(data);
        RestoreSceneEvents(data);
        RestoreWorldItems(data);
    }

    // ── Gathering ────────────────────────────────────────────────

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingSceneLoadData == null || pendingSceneLoadData.sceneName != scene.name)
        {
            return;
        }

        SaveData data = pendingSceneLoadData;
        pendingSceneLoadData = null;
        ApplySaveData(data);
    }

    private SaveData GatherCurrentState()
    {
        var data = ReadExistingSaveOrNew();
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
            for (int i = 0; i < inventory.Entries.Count; i++)
            {
                var entry = inventory.Entries[i];
                if (entry.IsOccupied)
                {
                    data.inventoryItems.Add(new SavedInventoryItem
                    {
                        slotIndex = i,
                        itemId = entry.Definition.ItemId,
                        quantity = entry.Quantity
                    });
                }
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
            if (string.IsNullOrEmpty(item.SaveId))
            {
                continue;
            }

            bool active = item.gameObject.activeSelf;
            if (!active)
            {
                data.pickedUpWorldItemIds.Add(item.SaveId);
            }

            Transform root = item.RootTransform ? item.RootTransform : item.transform;
            data.worldItems.Add(new SavedWorldItem
            {
                saveId = item.SaveId,
                active = active,
                x = root.position.x,
                y = root.position.y,
                z = root.position.z,
                rotationZ = root.rotation.eulerAngles.z
            });
        }

        var roomFlags = FindFirstObjectByType<RoomStateFlags>(FindObjectsInactive.Include);
        if (roomFlags)
        {
            data.roomFlags = roomFlags.Snapshot();
        }

        SaveRoomPortalState(data);
        SaveDialogueLineStates(data);
        SaveSceneEventStates(data);

        return data;
    }

    // ── Restoration ──────────────────────────────────────────────

    private void RestorePlayerPosition(SaveData data)
    {
        var player = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Exclude);
        if (player)
            player.TryWarp(new Vector3(data.playerX, data.playerY, player.transform.position.z));

        var roomTransition = FindFirstObjectByType<RoomTransitionService>(FindObjectsInactive.Exclude);
        if (roomTransition)
            roomTransition.RefreshActiveRoom();
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
        if (!inventory) return;

        var defLookup = BuildItemLookup();
        inventory.Clear();
        if (data.inventoryItems == null)
        {
            return;
        }

        foreach (var saved in data.inventoryItems)
        {
                if (!defLookup.TryGetValue(saved.itemId, out InventoryItemDefinition def))
                {
                    continue;
                }

                if (saved.slotIndex >= 0 && inventory.TryStoreExact(saved.slotIndex, def, saved.quantity))
                {
                    continue;
                }

                inventory.TryAdd(def, saved.quantity);
        }
    }

    private void RestoreQuests(SaveData data)
    {
        if (QuestController.Instance == null) return;

        var questLookup = BuildQuestLookup();
        QuestController.Instance.ClearRuntimeState();

        if (data.activeQuests != null)
        {
            foreach (var saved in data.activeQuests)
            {
                if (!questLookup.TryGetValue(saved.questId, out Quest quest)) continue;
                QuestController.Instance.RestoreQuestProgress(quest, saved.objectiveAmounts, saved.readyToHandIn);
            }
        }

        if (data.handedInQuestIds == null)
        {
            return;
        }

        foreach (var handedInId in data.handedInQuestIds)
        {
            QuestController.Instance.MarkQuestHandedIn(handedInId);
        }
    }

    private void RestoreWorldItems(SaveData data)
    {
        var worldItems = FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Dictionary<string, SavedWorldItem> savedWorldItems = new();
        if (data.worldItems != null)
        {
            for (int i = 0; i < data.worldItems.Count; i++)
            {
                SavedWorldItem saved = data.worldItems[i];
                if (saved != null && !string.IsNullOrWhiteSpace(saved.saveId))
                {
                    savedWorldItems[saved.saveId] = saved;
                }
            }
        }

        HashSet<string> pickedUpIds = data.pickedUpWorldItemIds == null
            ? new HashSet<string>()
            : new HashSet<string>(data.pickedUpWorldItemIds);

        foreach (var item in worldItems)
        {
            if (string.IsNullOrEmpty(item.SaveId))
            {
                continue;
            }

            if (savedWorldItems.TryGetValue(item.SaveId, out SavedWorldItem saved))
            {
                item.gameObject.SetActive(saved.active);
                if (saved.active)
                {
                    item.SeedPlacementPose(new Vector3(saved.x, saved.y, saved.z), Quaternion.Euler(0f, 0f, saved.rotationZ));
                }
                continue;
            }

            if (pickedUpIds.Contains(item.SaveId))
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private void RestoreCutscenes(SaveData data)
    {
        CutsceneController[] cutscenes = FindObjectsByType<CutsceneController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HashSet<string> completedIds = data.completedCutsceneIds == null
            ? new HashSet<string>()
            : new HashSet<string>(data.completedCutsceneIds);

        for (int i = 0; i < cutscenes.Length; i++)
        {
            if (cutscenes[i] && completedIds.Contains(cutscenes[i].SaveId))
            {
                cutscenes[i].RestoreCompleted();
            }
        }
    }

    private void RestoreRoomState(SaveData data)
    {
        RoomStateFlags flags = FindFirstObjectByType<RoomStateFlags>(FindObjectsInactive.Include);
        if (flags)
        {
            flags.Restore(data.roomFlags);
        }

        Dictionary<string, SavedRoomPortal> savedPortals = new();
        if (data.roomPortals != null)
        {
            for (int i = 0; i < data.roomPortals.Count; i++)
            {
                SavedRoomPortal saved = data.roomPortals[i];
                if (saved != null && !string.IsNullOrWhiteSpace(saved.saveId))
                {
                    savedPortals[saved.saveId] = saved;
                }
            }
        }

        RoomPortal[] portals = FindObjectsByType<RoomPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < portals.Length; i++)
        {
            RoomPortal portal = portals[i];
            if (!portal || string.IsNullOrWhiteSpace(portal.SaveId))
            {
                continue;
            }

            if (savedPortals.TryGetValue(portal.SaveId, out SavedRoomPortal saved))
            {
                portal.RestoreUnlockedByItem(saved.unlockedByItem);
            }
        }
    }

    private void RestoreDialogueLineStates(SaveData data)
    {
        Dictionary<string, int> savedIndexes = new();
        if (data.dialogueLineStates != null)
        {
            for (int i = 0; i < data.dialogueLineStates.Count; i++)
            {
                SavedDialogueLineState saved = data.dialogueLineStates[i];
                if (saved != null && !string.IsNullOrWhiteSpace(saved.saveId))
                {
                    savedIndexes[saved.saveId] = saved.currentDialogueIndex;
                }
            }
        }

        DialogueLines[] dialogueLines = FindObjectsByType<DialogueLines>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            DialogueLines dialogue = dialogueLines[i];
            if (dialogue && savedIndexes.TryGetValue(dialogue.SaveId, out int index))
            {
                dialogue.RestoreDialogueIndex(index);
            }
        }
    }

    private void RestoreSceneEvents(SaveData data)
    {
        HashSet<string> triggeredIds = data.triggeredSceneEventIds == null
            ? new HashSet<string>()
            : new HashSet<string>(data.triggeredSceneEventIds);

        ForestGhostWaveTrigger[] triggers = FindObjectsByType<ForestGhostWaveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < triggers.Length; i++)
        {
            ForestGhostWaveTrigger trigger = triggers[i];
            if (trigger && triggeredIds.Contains(trigger.SaveId))
            {
                trigger.RestoreTriggered(true);
            }
        }
    }

    private void SaveRoomPortalState(SaveData data)
    {
        RoomPortal[] portals = FindObjectsByType<RoomPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < portals.Length; i++)
        {
            RoomPortal portal = portals[i];
            if (!portal || string.IsNullOrWhiteSpace(portal.SaveId))
            {
                continue;
            }

            data.roomPortals.RemoveAll(saved => saved != null && saved.saveId == portal.SaveId);
            data.roomPortals.Add(new SavedRoomPortal
            {
                saveId = portal.SaveId,
                unlockedByItem = portal.WasUnlockedByItem
            });
        }
    }

    private void SaveDialogueLineStates(SaveData data)
    {
        DialogueLines[] dialogueLines = FindObjectsByType<DialogueLines>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            DialogueLines dialogue = dialogueLines[i];
            if (!dialogue || string.IsNullOrWhiteSpace(dialogue.SaveId))
            {
                continue;
            }

            data.dialogueLineStates.RemoveAll(saved => saved != null && saved.saveId == dialogue.SaveId);
            data.dialogueLineStates.Add(new SavedDialogueLineState
            {
                saveId = dialogue.SaveId,
                currentDialogueIndex = dialogue.currentDialogueIndex
            });
        }
    }

    private void SaveSceneEventStates(SaveData data)
    {
        ForestGhostWaveTrigger[] triggers = FindObjectsByType<ForestGhostWaveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < triggers.Length; i++)
        {
            ForestGhostWaveTrigger trigger = triggers[i];
            if (!trigger || string.IsNullOrWhiteSpace(trigger.SaveId) || !trigger.HasFired)
            {
                continue;
            }

            if (!data.triggeredSceneEventIds.Contains(trigger.SaveId))
            {
                data.triggeredSceneEventIds.Add(trigger.SaveId);
            }
        }
    }

    private bool ContainsSavedId(string id, Func<SaveData, List<string>> listSelector)
    {
        if (string.IsNullOrWhiteSpace(id) || !TryGetSaveData(out SaveData data))
        {
            return false;
        }

        List<string> ids = listSelector(data);
        return ids != null && ids.Contains(id);
    }

    private void AddSavedId(string id, Func<SaveData, List<string>> listSelector, bool writeImmediately)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        SaveData data = ReadSaveOrGatherCurrent();
        EnsureLists(data);
        List<string> ids = listSelector(data);
        if (!ids.Contains(id))
        {
            ids.Add(id);
        }

        if (writeImmediately)
        {
            WriteSave(data);
        }
    }

    private SaveData ReadSaveOrGatherCurrent()
    {
        return TryGetSaveData(out SaveData data) ? data : GatherCurrentState();
    }

    private SaveData ReadExistingSaveOrNew()
    {
        SaveData data = TryGetSaveData(out SaveData existing) ? existing : new SaveData();
        EnsureLists(data);
        return data;
    }

    private static void EnsureLists(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.inventoryItems ??= new List<SavedInventoryItem>();
        data.activeQuests ??= new List<SavedQuest>();
        data.handedInQuestIds ??= new List<string>();
        data.pickedUpWorldItemIds ??= new List<string>();
        data.worldItems ??= new List<SavedWorldItem>();
        data.completedCutsceneIds ??= new List<string>();
        data.roomPortals ??= new List<SavedRoomPortal>();
        data.roomFlags ??= new List<string>();
        data.dialogueLineStates ??= new List<SavedDialogueLineState>();
        data.triggeredSceneEventIds ??= new List<string>();
    }

    // ── Lookup builders ──────────────────────────────────────────

    private Dictionary<string, InventoryItemDefinition> BuildItemLookup()
    {
        var lookup = new Dictionary<string, InventoryItemDefinition>();
        AddSceneItemDefinitions(lookup);
        AddInventoryItemDefinitions(lookup);
        AddResourceItemDefinitions(lookup);
        AddConfiguredItemDefinitions(lookup);
        return lookup;
    }

    private void AddSceneItemDefinitions(Dictionary<string, InventoryItemDefinition> lookup)
    {
        PickupItem[] pickupItems = FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < pickupItems.Length; i++)
        {
            AddItemDefinition(lookup, pickupItems[i] ? pickupItems[i].ItemDefinition : null);
        }
    }

    private void AddInventoryItemDefinitions(Dictionary<string, InventoryItemDefinition> lookup)
    {
        Inventory inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (!inventory)
        {
            return;
        }

        foreach (Inventory.Entry entry in inventory.Entries)
        {
            if (entry.IsOccupied)
            {
                AddItemDefinition(lookup, entry.Definition);
            }
        }
    }

    private void AddResourceItemDefinitions(Dictionary<string, InventoryItemDefinition> lookup)
    {
        InventoryItemDefinition[] definitions = Resources.LoadAll<InventoryItemDefinition>(string.Empty);
        for (int i = 0; i < definitions.Length; i++)
        {
            AddItemDefinition(lookup, definitions[i]);
        }
    }

    private void AddConfiguredItemDefinitions(Dictionary<string, InventoryItemDefinition> lookup)
    {
        if (allItemDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < allItemDefinitions.Length; i++)
        {
            AddItemDefinition(lookup, allItemDefinitions[i]);
        }
    }

    private static void AddItemDefinition(Dictionary<string, InventoryItemDefinition> lookup, InventoryItemDefinition definition)
    {
        if (definition && !lookup.ContainsKey(definition.ItemId))
        {
            lookup[definition.ItemId] = definition;
        }
    }

    private Dictionary<string, Quest> BuildQuestLookup()
    {
        var lookup = new Dictionary<string, Quest>();
        AddQuestsToLookup(lookup, Resources.LoadAll<Quest>(string.Empty));
        AddQuestsToLookup(lookup, allQuests);
        return lookup;
    }

    private static void AddQuestsToLookup(Dictionary<string, Quest> lookup, Quest[] quests)
    {
        if (quests == null)
        {
            return;
        }

        foreach (var quest in quests)
        {
            if (quest && !lookup.ContainsKey(quest.questID))
            {
                lookup[quest.questID] = quest;
            }
        }
    }
}
