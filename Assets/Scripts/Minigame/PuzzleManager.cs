using UnityEngine;
using UnityEngine.SceneManagement;

public class Puzzlemanager : MonoBehaviour
{
    private PuzzlePiece[] puzzlePieces;
    private LineRenderer[] slotBorders;

    [SerializeField] private string sceneToLoadOnCompletion = "MainMenu";

    [Header("Completion Cutscene (optional)")]
    [SerializeField] private string cutsceneFileName = "ending-cutscene.mp4";
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

    private void Start()
    {
        ArrangePiecesIntoIncorrectSlots();
        CreateSlotBorders();
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
        if (AllPiecesCorrectlyPlaced())
        {
            CompletePuzzle();
        }
    }

    private bool AllPiecesCorrectlyPlaced()
    {
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece == null) continue;

            if (!piece.IsCorrectlyPlaced)
            {
                return false;
            }
        }

        return true;
    }

    private void ArrangePiecesIntoIncorrectSlots()
    {
        if (puzzlePieces == null || puzzlePieces.Length <= 1)
        {
            return;
        }

        Transform[] slots = new Transform[puzzlePieces.Length];
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            slots[i] = puzzlePieces[i] ? puzzlePieces[i].CorrectSlot : null;
        }

        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            PuzzlePiece piece = puzzlePieces[i];
            if (!piece) continue;

            Transform shiftedSlot = slots[(i + 1) % slots.Length];
            if (shiftedSlot == piece.CorrectSlot && slots.Length > 2)
            {
                shiftedSlot = slots[(i + 2) % slots.Length];
            }

            piece.PlaceInSlotAsUnlocked(shiftedSlot);
        }
    }

    private void CreateSlotBorders()
    {
        if (puzzlePieces == null) return;

        slotBorders = new LineRenderer[puzzlePieces.Length];
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            Transform slot = puzzlePieces[i] ? puzzlePieces[i].CorrectSlot : null;
            if (!slot) continue;

            GameObject borderObject = new($"SlotBorder_{i}");
            borderObject.transform.SetParent(slot, false);
            borderObject.transform.localPosition = Vector3.zero;

            LineRenderer line = borderObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 4;
            line.widthMultiplier = 0.035f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = new Color(1f, 1f, 1f, 0.65f);
            float half = 0.55f;
            line.SetPosition(0, new Vector3(-half, -half, 0f));
            line.SetPosition(1, new Vector3(-half, half, 0f));
            line.SetPosition(2, new Vector3(half, half, 0f));
            line.SetPosition(3, new Vector3(half, -half, 0f));
            slotBorders[i] = line;
        }
    }

    private void CompletePuzzle()
    {
        TornPhotoCutscenePlayer player = TornPhotoCutscenePlayer.Instance;
        if (!string.IsNullOrWhiteSpace(cutsceneFileName) && player != null)
        {
            player.Finished += HandleCutsceneFinished;
            if (player.Play(cutsceneFileName, cutsceneSaveId, playOnce))
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
        StartCoroutine(ScreenFade.FadeOutThenLoad(this, sceneToLoadOnCompletion));
    }
}
