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
    }

    [SerializeField, Min(1)] private int capacity = 16;
    [SerializeField] private List<Entry> entries = new();

    public IReadOnlyList<Entry> Entries => entries;
    public event Action Changed;

    public bool Contains(InventoryItemDefinition definition)
    {
        return FindEntryIndex(definition) >= 0;
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
            Changed?.Invoke();
            return true;
        }

        if (entries.Count >= capacity)
        {
            return false;
        }

        entries.Add(new Entry(definition, quantity));
        Changed?.Invoke();
        return true;
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
}
