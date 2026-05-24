using UnityEngine;
using UnityEngine.SceneManagement;

public class Puzzlemanager : MonoBehaviour
{
    private PuzzlePiece[] puzzlePieces;

    [SerializeField] private string sceneToLoadOnCompletion = "Sprint3";

    private void Awake()
    {
        puzzlePieces = FindObjectsByType<PuzzlePiece>(FindObjectsSortMode.None);

        Debug.Log($"Puzzle manager found {puzzlePieces.Length} puzzle pieces.");

        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece != null)
            {
                piece.OnLockedInPlace += HandlePieceLocked;
                Debug.Log($"Subscribed to {piece.name}");
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
    }

    private void HandlePieceLocked(PuzzlePiece piece)
    {
        Debug.Log($"{piece.name} locked. Checking puzzle completion...");

        if (AllPiecesLocked())
        {
            CompletePuzzle();
        }
        else
        {
            Debug.Log("Puzzle is not complete yet.");
        }
    }

    private bool AllPiecesLocked()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece == null) continue;

            Debug.Log($"{piece.name} locked state: {piece.IsLockedInPlace}");

            if (!piece.IsLockedInPlace)
            {
                return false;
            }
        }

        return true;
    }

    private void CompletePuzzle()
    {
        Debug.Log("Puzzle complete!");

        SceneManager.LoadScene(sceneToLoadOnCompletion);
    }
}