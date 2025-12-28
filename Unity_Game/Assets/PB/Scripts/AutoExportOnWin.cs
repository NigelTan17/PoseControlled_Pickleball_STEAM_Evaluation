using UnityEngine;

/// Exports once per finished match and arms again when scores reset/drop.
/// Also ensures an export happens when the application quits (last match result).
public class AutoExportOnWin : MonoBehaviour
{
    [Header("Refs")]
    public ScoreManager score;     // GameSystem (Score Manager)
    public ModeRuntime mode;       // GameSystem (Mode Runtime)
    public ScoreExporter exporter; // GameSystem (Score Exporter)

    private bool exportedThisMatch = false;
    private int lastL = -1, lastR = -1;

    void Reset()
    {
        score    = FindObjectOfType<ScoreManager>();
        mode     = FindObjectOfType<ModeRuntime>();
        exporter = FindObjectOfType<ScoreExporter>();
    }

    void Update()
    {
        if (!score || !exporter) return;

        int l = score.LeftScore;
        int r = score.RightScore;

        int winner = GetWinner(l, r, score.targetPoints, score.winByTwo);
        if (winner != -1 && !exportedThisMatch)
        {
            DoExport(l, r);
            exportedThisMatch = true;
        }

        // Re-arm when scores reset to 0:0 or total drops (restart/rematch/main menu)
        int totalNow = l + r;
        int totalPrev = Mathf.Max(0, lastL) + Mathf.Max(0, lastR);
        bool scoresReset = (l == 0 && r == 0) && (lastL != 0 || lastR != 0);
        bool totalDropped = totalNow < totalPrev;

        if (exportedThisMatch && (scoresReset || totalDropped))
            exportedThisMatch = false;

        lastL = l; lastR = r;
    }

    void OnApplicationQuit()
    {
        // If the last match finished but we didn’t catch it (or user closes right away),
        // write once more on quit using current state.
        if (exporter && score)
        {
            int winner = GetWinner(score.LeftScore, score.RightScore, score.targetPoints, score.winByTwo);
            if (winner != -1 && !exportedThisMatch)
                DoExport(score.LeftScore, score.RightScore);
        }
    }

    void DoExport(int l, int r)
    {
        string leftName   = mode ? mode.leftPlayerName  : "Left";
        string rightName  = mode ? mode.rightPlayerName : "Right";
        string leftDiff   = mode ? mode.leftDifficulty.ToString()  : "-";
        string rightDiff  = mode ? mode.rightDifficulty.ToString() : "-";
        string modeName   = mode ? mode.currentMode.ToString()     : "Unknown";

        exporter.ExportFromParams(
            l, r,
            leftName, rightName, leftDiff, rightDiff,
            modeName, score.rallyScoring, score.targetPoints, score.winByTwo,
            score.startingServerSide, score.CurrentServerSide
        );
    }

    static int GetWinner(int l, int r, int target, bool byTwo)
    {
        if (byTwo)
        {
            if (l >= target && l >= r + 2) return 0;
            if (r >= target && r >= l + 2) return 1;
        }
        else
        {
            if (l >= target && l > r) return 0;
            if (r >= target && r > l) return 1;
        }
        return -1;
    }
}
