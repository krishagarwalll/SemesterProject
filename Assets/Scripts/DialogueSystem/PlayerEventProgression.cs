using UnityEngine;

public class PlayerEventProgression : MonoBehaviour
{
    public int progressionIndex = 0;

    public int CurrentStage => progressionIndex;

    public void IncrementProgression() => progressionIndex++;

    public bool HasReachedStage(int stage) => progressionIndex >= stage;

    public void AdvanceToStage(int stage) => progressionIndex = Mathf.Max(progressionIndex, stage);
}
