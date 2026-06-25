using UnityEngine;

[DisallowMultipleComponent]
public sealed class PauseTransitionDriver : MonoBehaviour
{
    private void Update()
    {
        PauseService.TickTransitions(Time.unscaledDeltaTime);
    }
}
