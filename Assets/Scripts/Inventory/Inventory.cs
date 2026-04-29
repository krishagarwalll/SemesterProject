using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        [SerializeField] private InventoryItemDefinition definition;
        [SerializeField, Min(1)] private int quantity;

        public Entry(InventoryItemDefinition definition, int quantity)
        {
            this.definition = definition;
            this.quantity = Mathf.Max(0, quantity);
        }

        public InventoryItemDefinition Definition => definition;
        public int Quantity => quantity;
        public bool IsOccupied => definition && quantity > 0;

        public Entry Add(int amount)
        {
            return new Entry(definition, quantity + Mathf.Max(1, amount));
        }

        public Entry Remove(int amount)
        {
            return new Entry(definition, Mathf.Max(0, quantity - Mathf.Max(1, amount)));
        }
    }

    [SerializeField, Min(1)] private int capacity = 6;
    [SerializeField] private List<Entry> entries = new();

    public event Action Changed;

    public int Capacity => capacity;
    public int Count => CountOccupiedEntries();
    public bool IsFull => Count >= capacity;
    public IReadOnlyList<Entry> Entries => entries;

    private void Awake()
    {
        EnsureSlotCount();
    }

    private void OnValidate()
    {
        capacity = Mathf.Max(1, capacity);
        EnsureSlotCount();
    }

    public bool Contains(InventoryItemDefinition definition)
    {
        return FindEntryIndex(definition) >= 0;
    }

    public bool TryGetEntry(int index, out Entry entry)
    {
        if (!IsOccupiedIndex(index))
        {
            entry = default;
            return false;
        }

        entry = entries[index];
        return true;
    }

    public bool TryAdd(InventoryItemDefinition definition, int quantity = 1)
    {
        if (!definition || quantity <= 0)
        {
            return false;
        }

        int stackIndex = FindEntryIndex(definition);
        if (stackIndex >= 0)
        {
            entries[stackIndex] = entries[stackIndex].Add(quantity);
            NotifyChanged();
            return true;
        }

        int emptyIndex = FindEmptyIndex();
        if (emptyIndex < 0)
        {
            return false;
        }

        entries[emptyIndex] = new Entry(definition, quantity);
        NotifyChanged();
        return true;
    }

    public bool TryStoreExact(int index, InventoryItemDefinition definition, int quantity = 1)
    {
        if (!definition || quantity <= 0 || !IsValidSlotIndex(index))
        {
            return false;
        }

        if (!IsOccupiedIndex(index))
        {
            entries[index] = new Entry(definition, quantity);
            NotifyChanged();
            return true;
        }

        if (entries[index].Definition != definition)
        {
            return false;
        }

        entries[index] = entries[index].Add(quantity);
        NotifyChanged();
        return true;
    }

    public bool TryStoreAnywhere(InventoryItemDefinition definition, int quantity = 1)
    {
        return TryAdd(definition, quantity);
    }

    public void Clear()
    {
        EnsureSlotCount();
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i] = default;
        }

        NotifyChanged();
    }

    public bool TryInsert(int index, InventoryItemDefinition definition, int quantity = 1)
    {
        if (!definition || quantity <= 0 || !IsValidSlotIndex(index))
        {
            return false;
        }

        if (IsOccupiedIndex(index) && entries[index].Definition == definition)
        {
            entries[index] = entries[index].Add(quantity);
            NotifyChanged();
            return true;
        }

        int targetIndex = IsOccupiedIndex(index) ? FindNearestEmptyIndex(index) : index;
        if (targetIndex < 0)
        {
            return false;
        }

        entries[targetIndex] = new Entry(definition, quantity);
        NotifyChanged();
        return true;
    }

    public bool TryTakeAt(int index, out Entry entry, int quantity = 1)
    {
        entry = default;
        if (!IsOccupiedIndex(index) || quantity <= 0)
        {
            return false;
        }

        Entry current = entries[index];
        if (current.Quantity < quantity)
        {
            return false;
        }

        entry = new Entry(current.Definition, quantity);
        Entry remaining = current.Remove(quantity);
        entries[index] = remaining.IsOccupied ? remaining : default;
        NotifyChanged();
        return true;
    }

    public bool TryRemove(InventoryItemDefinition definition, int quantity = 1)
    {
        int index = FindEntryIndex(definition);
        return index >= 0 && TryTakeAt(index, out _, quantity);
    }

    public bool Swap(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex) || !IsOccupiedIndex(fromIndex) || fromIndex == toIndex)
        {
            return false;
        }

        (entries[fromIndex], entries[toIndex]) = (entries[toIndex], entries[fromIndex]);
        NotifyChanged();
        return true;
    }

    public bool Move(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex) || !IsOccupiedIndex(fromIndex) || fromIndex == toIndex)
        {
            return false;
        }

        if (IsOccupiedIndex(toIndex))
        {
            return Swap(fromIndex, toIndex);
        }

        entries[toIndex] = entries[fromIndex];
        entries[fromIndex] = default;
        NotifyChanged();
        return true;
    }

    private void EnsureSlotCount()
    {
        if (entries.Count > capacity)
        {
            entries.RemoveRange(capacity, entries.Count - capacity);
        }

        while (entries.Count < capacity)
        {
            entries.Add(default);
        }
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < capacity;
    }

    private bool IsOccupiedIndex(int index)
    {
        return IsValidSlotIndex(index) && index < entries.Count && entries[index].IsOccupied;
    }

    private int FindEntryIndex(InventoryItemDefinition definition)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsOccupied && entries[i].Definition == definition)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindEmptyIndex()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (!entries[i].IsOccupied)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindNearestEmptyIndex(int preferredIndex)
    {
        int bestIndex = -1;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsOccupied)
            {
                continue;
            }

            int distance = Mathf.Abs(i - preferredIndex);
            if (distance > bestDistance)
            {
                continue;
            }

            if (distance == bestDistance && bestIndex >= 0 && i > bestIndex)
            {
                continue;
            }

            bestDistance = distance;
            bestIndex = i;
        }

        return bestIndex;
    }

    private int CountOccupiedEntries()
    {
        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsOccupied)
            {
                count++;
            }
        }

        return count;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    // ── Developer query API ──────────────────────────────────────

    public bool HasItem(string itemId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsOccupied && entries[i].Definition.ItemId == itemId) return true;
        }
        return false;
    }

    public bool TryGetItem(string itemId, out Entry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsOccupied && entries[i].Definition.ItemId == itemId) { entry = entries[i]; return true; }
        }
        entry = default;
        return false;
    }

    public int CountItem(string itemId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsOccupied && entries[i].Definition.ItemId == itemId) return entries[i].Quantity;
        }
        return 0;
    }

    public bool TryRemoveItem(string itemId, int count = 1)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (!entries[i].IsOccupied || entries[i].Definition.ItemId != itemId) continue;
            return TryTakeAt(i, out _, count);
        }
        return false;
    }
}
