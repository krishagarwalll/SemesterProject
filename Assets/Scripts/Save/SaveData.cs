using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string sceneName = "";
    public float playerX = 0f;
    public float playerY = 0f;
    public int progressionIndex = 0;
    public List<SavedInventoryItem> inventoryItems = new();
    public List<SavedQuest> activeQuests = new();
    public List<string> handedInQuestIds = new();
    public List<string> pickedUpWorldItemIds = new();
}

[Serializable]
public class SavedInventoryItem
{
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
