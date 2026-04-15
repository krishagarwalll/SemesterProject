using UnityEngine;

public static class Gameplay2DSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyLayerRules()
    {
        Ignore("Character", "RoomProp");
        Ignore("Character", "WorldItem");
        Ignore("WorldItem", "RoomProp");
    }

    private static void Ignore(string leftLayerName, string rightLayerName)
    {
        int left = LayerMask.NameToLayer(leftLayerName);
        int right = LayerMask.NameToLayer(rightLayerName);
        if (left < 0 || right < 0)
        {
            return;
        }

        Physics2D.IgnoreLayerCollision(left, right, true);
        Physics.IgnoreLayerCollision(left, right, true);
    }
}
