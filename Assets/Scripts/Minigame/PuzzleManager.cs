using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class Puzzlemanager : MonoBehaviour
{
    private PuzzlePiece[] puzzlePieces;

    [SerializeField] private string sceneToLoadOnCompletion = "MainMenu";

    [Header("Completion Cutscene (optional)")]
    [SerializeField] private VideoClip cutsceneClip;
    [SerializeField] private string cutsceneSaveId = "TornPhotoCutscene";
    [SerializeField] private bool playOnce = true;

    private bool sceneLoadQueued;

    private void Awake()
    {
        puzzlePieces = FindObjectsByType<PuzzlePiece>(FindObjectsSortMode.None);

        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece != null)
            {
                piece.OnLockedInPlace += HandlePieceLocked;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece != null)
            {
                piece.OnLockedInPlace -= HandlePieceLocked;
            }
        }

        TornPhotoCutscenePlayer player = TornPhotoCutscenePlayer.Instance;
        if (player != null)
        {
            player.Finished -= HandleCutsceneFinished;
        }
    }

    private void HandlePieceLocked(PuzzlePiece piece)
    {
        if (AllPiecesLocked())
        {
            CompletePuzzle();
        }
    }

    private bool AllPiecesLocked()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece == null) continue;

            if (!piece.IsLockedInPlace)
            {
                return false;
            }
        }

        return true;
    }

    private void CompletePuzzle()
    {
        TornPhotoCutscenePlayer player = TornPhotoCutscenePlayer.Instance;
        if (cutsceneClip && player != null)
        {
            player.Finished += HandleCutsceneFinished;
            if (player.Play(cutsceneClip, cutsceneSaveId, playOnce))
            {
                return;
            }

            player.Finished -= HandleCutsceneFinished;
        }

        LoadNextScene();
    }

    private void HandleCutsceneFinished()
    {
        TornPhotoCutscenePlayer player = TornPhotoCutscenePlayer.Instance;
        if (player != null)
        {
            player.Finished -= HandleCutsceneFinished;
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (sceneLoadQueued || string.IsNullOrWhiteSpace(sceneToLoadOnCompletion))
        {
            return;
        }

        sceneLoadQueued = true;
        SceneManager.LoadScene(sceneToLoadOnCompletion);
    }
}
