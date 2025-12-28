using UnityEngine;
using UnityEngine.Events;

/// Central scoring + server ownership + HUD + robust GameOver dispatch.
public class ScoreManager : MonoBehaviour
{
    // -------------------- CONFIG --------------------
    [Header("Game Rules")]
    public int  targetPoints = 11;
    public bool winByTwo     = true;

    [Header("Mode")]
    public bool rallyScoring = false;

    [Header("Initial Server")]
    [Range(0,1)] public int startingServerSide = 1; // 0=Left, 1=Right

    [Header("HUD (optional)")]
    public ScoreHUDSimpleV2 hud;

    [Header("UI (optional GameOver)")]
    [Tooltip("If left empty, we will auto-find a UIController in the scene.")]
    public MonoBehaviour ui;   // your UIController; kept generic to avoid hard ref

    [Header("Server Lock (runtime)")]
    public bool serverLockEnabled = false;
    [Range(0,1)] public int serverLockSide01 = 1;

    [Header("Diagnostics")]
    [Tooltip("Flip incoming winner sign once if your scoring is mirrored.")]
    public bool invertWinnerSign = false;
    public bool logEvents = false;

    [Header("Events (optional)")]
    public UnityEvent<int,int,int> OnGameOver; // winner01, L, R

    // -------------------- STATE --------------------
    [Header("Read Only (debug)")]
    [SerializeField] private int  leftScore = 0;
    [SerializeField] private int  rightScore = 0;
    [SerializeField] private int  currentServerSide01 = 1; // 0=Left, 1=Right
    [SerializeField] private bool gameOver = false;

    // Auto-find once after scripts reload
    void Awake()
    {
        if (!ui)
        {
            ui = FindObjectOfType<UIController>();
            if (logEvents)
                Debug.Log(ui ? "[Score] Auto-found UIController." : "[Score] No UIController found to show GameOver.");
        }
    }

    void Start()
    {
        ResetMatchToStartServer(startingServerSide);
    }

    // -------------------- PUBLIC API (legacy-safe) --------------------
    public int LeftScore  => leftScore;
    public int RightScore => rightScore;

    public int CurrentServerSign  => (currentServerSide01 == 1) ? +1 : -1;
    public int CurrentServerSide  => currentServerSide01;          // legacy
    public int CurrentServerSide01 => currentServerSide01;         // explicit

    public void SetRallyScoring(bool on)      => rallyScoring = on; // legacy
    public void RefreshHUDImmediate()         => RefreshHUD();      // legacy
    public void NotifyHUDScoreChanged()       => RefreshHUD();      // legacy
    public void NotifyHUDServerChanged()      => RefreshHUD();      // legacy

    /// Called by gameplay when a rally ends. winnerSign: -1=Left, +1=Right.
    /// Returns next server sign: -1 (Left) / +1 (Right).
    public int OnRallyEnded(int winnerSign)
    {
        if (gameOver) return CurrentServerSign;

        int ws = (winnerSign >= 0) ? +1 : -1;
        if (invertWinnerSign) ws = -ws;

        int winner01 = (ws > 0) ? 1 : 0;
        bool serverScored = (winner01 == currentServerSide01);

        if (rallyScoring)
        {
            AddPoint01(winner01, "Rally scoring");
            currentServerSide01 = winner01;
        }
        else
        {
            if (serverScored)
            {
                AddPoint01(currentServerSide01, "Side-out (server won)");
            }
            else
            {
                currentServerSide01 = 1 - currentServerSide01; // switch serve
                if (logEvents) Debug.Log($"[Serve] Side-out -> switch to {(currentServerSide01==1?"Right":"Left")}");
            }
        }

        if (serverLockEnabled) currentServerSide01 = serverLockSide01;

        RefreshHUD();
        return (currentServerSide01 == 1) ? +1 : -1;
    }

    public void SetServerSide(int sideSign)
    {
        currentServerSide01 = (sideSign > 0) ? 1 : 0;
        if (serverLockEnabled) currentServerSide01 = serverLockSide01;
        if (logEvents) Debug.Log($"[Serve] Forced -> {(currentServerSide01==1?"Right":"Left")}");
        RefreshHUD();
    }

    public void SetTargetAndWinByTwo(int points, bool byTwo)
    {
        targetPoints = points; winByTwo = byTwo;
    }

    public void SetServerLock(bool enabled, int sideSign)
    {
        serverLockEnabled = enabled;
        serverLockSide01  = (sideSign > 0) ? 1 : 0;
        if (serverLockEnabled) currentServerSide01 = serverLockSide01;
        RefreshHUD();
    }

    public void ResetMatchToStartServer(int startSide01)
    {
        leftScore = rightScore = 0;
        currentServerSide01 = Mathf.Clamp(startSide01, 0, 1);
        if (serverLockEnabled) currentServerSide01 = serverLockSide01;
        gameOver = false;
        if (logEvents) Debug.Log($"[Score] Reset. Server={(currentServerSide01==1?"Right":"Left")}");
        RefreshHUD();
    }

    // -------------------- INTERNALS --------------------
    private void AddPoint01(int side01, string reason)
    {
        if (gameOver) return;

        if (side01 == 0) leftScore++; else rightScore++;
        if (logEvents) Debug.Log($"[Score] +1 {(side01==1?"Right":"Left")} ({reason}) | L={leftScore} R={rightScore}");

        if (CheckWin(out int winner01))
        {
            gameOver = true;
            if (logEvents) Debug.Log($"[GameOver] Winner={(winner01==1?"Right":"Left")}  Final L={leftScore} R={rightScore}");

            // Keep server on winner (or respect lock)
            currentServerSide01 = serverLockEnabled ? serverLockSide01 : winner01;

            RefreshHUD();
            DispatchGameOver(winner01, leftScore, rightScore);
        }
        else
        {
            RefreshHUD();
        }
    }

    private bool CheckWin(out int winner01)
    {
        winner01 = -1;
        bool Lreach = leftScore  >= targetPoints;
        bool Rreach = rightScore >= targetPoints;

        if (winByTwo)
        {
            if (Lreach && leftScore  >= rightScore + 2) { winner01 = 0; return true; }
            if (Rreach && rightScore >= leftScore  + 2) { winner01 = 1; return true; }
        }
        else
        {
            if (Lreach && leftScore  > rightScore) { winner01 = 0; return true; }
            if (Rreach && rightScore > leftScore)  { winner01 = 1; return true; }
        }
        return false;
    }

    private void DispatchGameOver(int winner01, int L, int R)
    {
        // UnityEvent hook if you want to wire actions in Inspector
        OnGameOver?.Invoke(winner01, L, R);

        // If ui reference got lost, auto-find now
        if (!ui)
        {
            ui = FindObjectOfType<UIController>();
            if (logEvents)
                Debug.Log(ui ? "[Score] (late) Auto-found UIController for GameOver."
                             : "[Score] No UIController in scene to show GameOver.");
        }

        if (ui)
        {
            // Try several names so we hit your method even if it’s renamed
            ui.SendMessage("ShowGameOverPanelWithResult", new object[]{winner01, L, R}, SendMessageOptions.DontRequireReceiver);
            ui.SendMessage("ShowGameOverPanel",          new object[]{winner01, L, R}, SendMessageOptions.DontRequireReceiver);
            ui.SendMessage("GameOver",                   new object[]{winner01, L, R}, SendMessageOptions.DontRequireReceiver);
            ui.SendMessage("OnGameOver",                 new object[]{winner01, L, R}, SendMessageOptions.DontRequireReceiver);
            ui.SendMessage("ShowGameOver",               new object[]{winner01, L, R}, SendMessageOptions.DontRequireReceiver);

            if (logEvents) Debug.Log("[Score] Dispatched GameOver to UI.");
        }
    }

    private void RefreshHUD()
    {
        if (!hud) return;
        hud.SetScore(leftScore, rightScore);
        hud.SetServer(currentServerSide01 == 1); // true = Right serves
    }
}
