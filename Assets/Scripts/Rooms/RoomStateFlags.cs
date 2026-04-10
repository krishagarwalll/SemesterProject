using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RoomStateFlags : MonoBehaviour
{
    [SerializeField] private List<string> startingFlags = new();

    private readonly HashSet<string> flags = new();

    private void Awake()
    {
        flags.Clear();
        for (int i = 0; i < startingFlags.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(startingFlags[i]))
            {
                flags.Add(startingFlags[i]);
            }
        }
    }

    public bool HasFlag(string flagId)
    {
        return !string.IsNullOrWhiteSpace(flagId) && flags.Contains(flagId);
    }

    public void SetFlag(string flagId, bool value = true)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        if (value)
        {
            flags.Add(flagId);
        }
        else
        {
            flags.Remove(flagId);
        }
    }
}
