using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogueLines : MonoBehaviour
{
    [SerializeField] private string saveId;
    [TextArea(3,10)]
    [SerializeField] private String[] dialogues;

    public int currentDialogueIndex = 0;
    public string SaveId => ResolveSaveId();
    
    public string GetCurrentDialogue()
    {
        if (currentDialogueIndex < dialogues.Length)
        {
            return dialogues[currentDialogueIndex];
        }

        return "No dialogue available";
    }

    public void AdvanceDialogue()
    {
        currentDialogueIndex++;
    }

    public void RestoreDialogueIndex(int index)
    {
        currentDialogueIndex = Mathf.Clamp(index, 0, dialogues != null ? dialogues.Length : 0);
    }

    private void Reset()
    {
        EnsureSerializedSaveId();
    }

    private void OnValidate()
    {
        EnsureSerializedSaveId();
    }

    private string ResolveSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId))
        {
            return saveId;
        }

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name;
        return $"{sceneName}:dialogue:{GetHierarchyPath(transform)}";
    }

    private void EnsureSerializedSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId) || Application.isPlaying)
        {
            return;
        }

        saveId = Guid.NewGuid().ToString("N");
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (!current)
        {
            return string.Empty;
        }

        string path = current.name;
        while (current.parent)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
