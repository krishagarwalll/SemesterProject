using UnityEngine;

public class BowlMinigameTrigger : MonoBehaviour
{
    //place on bowl in kitchen
    private InteractionTarget target;

    private void Awake()
    {
        target = GetComponent<InteractionTarget>();
    }

    public void TriggerMinigame()
    {
        Debug.Log("BOWL MINIGAME TRIGGERED");

        if (FindKey.Instance != null)
        {
            FindKey.Instance.Open();
        }
    }
}