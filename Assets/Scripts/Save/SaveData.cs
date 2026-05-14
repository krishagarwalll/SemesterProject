using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 3;
    public string sceneName = "";
    public float playerX = 0f;
    public float playerY = 0f;
    public int progressionIndex = 0;
    public List<SavedInventoryItem> inventoryItems = new();
    public List<SavedQuest> activeQuests = new();
    public List<string> handedInQuestIds = new();
    public List<string> pickedUpWorldItemIds = new();
    public List<SavedWorldItem> worldItems = new();
    public List<string> completedCutsceneIds = new();
    public List<SavedRoomPortal> roomPortals = new();
    public List<string> roomFlags = new();
    public List<SavedDialogueLineState> dialogueLineStates = new();
    public List<string> triggeredSceneEventIds = new();
}

[Serializable]
public class SavedInventoryItem
{
    public int slotIndex = -1;
    public string itemId = "";
    public int quantity = 1;
}

[Serializable]
public class SavedQuest
{
    public string questId = "";
    public bool readyToHandIn = false;
    public List<int> objectiveAmounts = new();
}

[Serializable]
public class SavedWorldItem
{
    public string saveId = "";
    public bool active = true;
    public float x;
    public float y;
    public float z;
    public float rotationZ;
}

[Serializable]
public class SavedRoomPortal
{
    public string saveId = "";
    public bool unlockedByItem;
}

[Serializable]
public class SavedDialogueLineState
{
    public string saveId = "";
    public int currentDialogueIndex;
}
