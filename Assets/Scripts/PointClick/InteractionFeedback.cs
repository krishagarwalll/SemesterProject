using UnityEngine;

public static class InteractionFeedback
{
    public static void Show(string message, Object context = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Debug.Log(message, context);
    }
}
