using UnityEngine;

public class TargetSlot : MonoBehaviour
{
    [SerializeField] private PuzzlePiece correctPiece;
    [SerializeField] private MinigameEX minigame;

    [SerializeField] private float snapDistance = 0.5f;

    private void OnTriggerStay2D(Collider2D other)
    {
        PuzzlePiece piece = other.GetComponent<PuzzlePiece>();

        if (piece == null) return;
        if (piece != correctPiece) return;

        float dist = Vector2.Distance(piece.transform.position, transform.position);

        if (dist <= snapDistance)
        {
            SnapPiece(piece);
            Complete();
        }
    }

    void SnapPiece(PuzzlePiece piece)
    {
        piece.transform.position = transform.position;

        var drag = piece.GetComponent<WorldPuzzleDragObject>();
        if (drag != null)
            drag.enabled = false;
    }

    void Complete()
    {
        if (minigame != null)
            minigame.CompleteMinigame();
    }
}