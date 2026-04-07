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

        public InventoryItemDefinition Definition => definition;
        public int Quantity => quantity;

        public Entry(InventoryItemDefinition definition, int quantity)
        {
            this.definition = definition;
            this.quantity = Mathf.Max(1, quantity);
        }

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
    [SerializeField] private int selectedIndex = -1;

    public int Capacity => capacity;
    public int Count => entries.Count;
    public int SelectedIndex => IsValidIndex(selectedIndex) ? selectedIndex : -1;
    public bool HasSelection => SelectedIndex >= 0;
    public bool IsFull => entries.Count >= capacity;
    public IReadOnlyList<Entry> Entries => entries;
    public InventoryItemDefinition SelectedItem => TryGetSelectedEntry(out Entry entry) ? entry.Definition : null;

    public event Action Changed;

    private void OnValidate()
    {
        capacity = Mathf.Max(1, capacity);
        if (entries.Count > capacity)
        {
            entries.RemoveRange(capacity, entries.Count - capacity);
        }

        selectedIndex = entries.Count == 0
            ? -1
            : Mathf.Clamp(selectedIndex, -1, entries.Count - 1);
    }

    public bool Contains(InventoryItemDefinition definition)
    {
        return FindEntryIndex(definition) >= 0;
    }

    public bool TryGetEntry(int index, out Entry entry)
    {
        if (!IsValidIndex(index))
        {
            entry = default;
            return false;
        }

        entry = entries[index];
        return true;
    }

    public bool TryGetSelectedEntry(out Entry entry)
    {
        return TryGetEntry(SelectedIndex, out entry);
    }

    public bool TryAdd(InventoryItemDefinition definition, int quantity = 1)
    {
        if (!definition || quantity <= 0)
        {
            return false;
        }

        int existingIndex = FindEntryIndex(definition);
        if (existingIndex >= 0)
        {
            entries[existingIndex] = entries[existingIndex].Add(quantity);
            NotifyChanged();
            return true;
        }

        if (IsFull)
        {
            return false;
        }

        entries.Add(new Entry(definition, quantity));
        NotifyChanged();
        return true;
    }

    public bool TryRemove(InventoryItemDefinition definition, int quantity = 1)
    {
        if (!definition || quantity <= 0)
        {
            return false;
        }

        int entryIndex = FindEntryIndex(definition);
        if (entryIndex < 0)
        {
            return false;
        }

        Entry updatedEntry = entries[entryIndex].Remove(quantity);
        if (updatedEntry.Quantity <= 0)
        {
            entries.RemoveAt(entryIndex);
            SyncSelectionAfterRemoval(entryIndex);
        }
        else
        {
            entries[entryIndex] = updatedEntry;
        }

        NotifyChanged();
        return true;
    }

    public bool Select(int index, bool toggle = true)
    {
        if (index < 0)
        {
            return ClearSelection();
        }

        if (!IsValidIndex(index))
        {
            return false;
        }

        if (toggle && selectedIndex == index)
        {
            return ClearSelection();
        }

        if (selectedIndex == index)
        {
            return false;
        }

        selectedIndex = index;
        NotifyChanged();
        return true;
    }

    public bool ClearSelection()
    {
        if (selectedIndex < 0)
        {
            return false;
        }

        selectedIndex = -1;
        NotifyChanged();
        return true;
    }

    public bool Swap(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
        {
            return false;
        }

        (entries[fromIndex], entries[toIndex]) = (entries[toIndex], entries[fromIndex]);
        SyncSelectionAfterSwap(fromIndex, toIndex);
        NotifyChanged();
        return true;
    }

    public bool Move(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
        {
            return false;
        }

        Entry entry = entries[fromIndex];
        entries.RemoveAt(fromIndex);
        entries.Insert(toIndex, entry);
        SyncSelectionAfterMove(fromIndex, toIndex);
        NotifyChanged();
        return true;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < entries.Count;
    }

    private int FindEntryIndex(InventoryItemDefinition definition)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Definition == definition)
            {
                return i;
            }
        }

        return -1;
    }

    private void SyncSelectionAfterRemoval(int removedIndex)
    {
        if (selectedIndex == removedIndex)
        {
            selectedIndex = -1;
            return;
        }

        if (selectedIndex > removedIndex)
        {
            selectedIndex--;
        }
    }

    private void SyncSelectionAfterSwap(int fromIndex, int toIndex)
    {
        if (selectedIndex == fromIndex)
        {
            selectedIndex = toIndex;
            return;
        }

        if (selectedIndex == toIndex)
        {
            selectedIndex = fromIndex;
        }
    }

    private void SyncSelectionAfterMove(int fromIndex, int toIndex)
    {
        if (selectedIndex == fromIndex)
        {
            selectedIndex = toIndex;
            return;
        }

        if (fromIndex < toIndex && selectedIndex > fromIndex && selectedIndex <= toIndex)
        {
            selectedIndex--;
            return;
        }

        if (toIndex < fromIndex && selectedIndex >= toIndex && selectedIndex < fromIndex)
        {
            selectedIndex++;
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
