using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class OpenMiniGame : MonoBehaviour, IInteractionActionProvider
{
    [SerializeField] private string sceneName = "RippedUpLetterMiniGame";
    [SerializeField] private string interactionLabel = "Start";
    [SerializeField] private string glyphId = "Primary";
    [SerializeField] private bool requiresApproach;

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        actions.Add(new InteractionAction(
            this,
            InteractionMode.Primary,
            interactionLabel,
            glyphId,
            enabled: !string.IsNullOrWhiteSpace(sceneName),
            requiresApproach: requiresApproach));
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        if (action.Mode != InteractionMode.Primary)
        {
            return false;
        }

        OpenScene();
        return true;
    }

    public void OpenScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"[{nameof(OpenMiniGame)}] No scene name is configured on '{name}'.", this);
            return;
        }

        PauseService.ClearAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        StartCoroutine(ScreenFade.FadeOutThenLoad(this, sceneName));
    }
}
