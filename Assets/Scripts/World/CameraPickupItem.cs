using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class CameraPickupItem : MonoBehaviour, IInteractionActionProvider
{
    [SerializeField] private string cameraMinigameSceneName = "CameraMinigame";
    [SerializeField] private string pickUpLabel = "Pick Up";

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        actions.Add(new InteractionAction(this, InteractionMode.Primary, pickUpLabel, "Primary", enabled: true, requiresApproach: false));
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        if (action.Mode != InteractionMode.Primary) return false;
        PauseService.ClearAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        StartCoroutine(ScreenFade.FadeOutThenLoad(this, cameraMinigameSceneName));
        return true;
    }
}
