using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 2;
    public string sceneName = "";
    public float playerX = 0f;
    public float playerY = 0f;
    public int progressionIndex = 0;
    public List<SavedInventoryItem> inventoryItems = new();
    public List<SavedQuest> activeQuests = new();
    public List<string> handedInQuestIds = new();
    public List<string> pickedUpWorldItemIds = new();
    public List<SavedWorldItem> worldItems = new();
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
