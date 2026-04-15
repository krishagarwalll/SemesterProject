using System;
using UnityEngine;

public static class MemoryFragmentService
{
    public const int Total = 3;

    public static int Count { get; private set; }
    public static bool HasAll => Count >= Total;

    public static event Action<int> FragmentUnlocked;
    
    public static bool Has(int fragmentNumber) => fragmentNumber >= 1 && Count >= fragmentNumber;
    
    public static bool UnlockNext()
    {
        if (Count >= Total) return false;
        Count++;
        FragmentUnlocked?.Invoke(Count);
        return true;
    }
    
    public static void RestoreFromSave(int count)
    {
        Count = Mathf.Clamp(count, 0, Total);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Count = 0;
        FragmentUnlocked = null;
    }
}
