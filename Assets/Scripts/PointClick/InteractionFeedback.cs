using UnityEngine;

public static class InteractionFeedback
{
    public static event System.Action<string, Object> MessageRequested;

    public static void Show(string message, Object context = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        MessageRequested?.Invoke(message, context);
        Debug.Log(message, context);
    }
}
