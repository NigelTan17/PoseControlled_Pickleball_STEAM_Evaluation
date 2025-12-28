using UnityEngine;

public class ModeRuntime : MonoBehaviour
{
    public enum GameMode   { Training, Competition }
    public enum Difficulty { Easy, Medium, Hard }

    [Header("Core")]
    public ScoreManager scoreManager;
    public PB.Scripts.PickleBallController ball;

    [Header("Players & Cameras")]
    public GameObject qinL;
    public GameObject qinR;
    public Camera leftCam;
    public Camera rightCam;

    [Header("Deflector (Training)")]
    public GameObject deflectorWallGO;
    public PB.Scripts.DeflectorWall deflectorWall;
    public Transform qinRRootForWall; // kept for backward-compat (used if neither paddle is set)

    [Header("Paddles")]
    public GameObject leftChildPaddle;
    public GameObject rightChildPaddle;
    public GameObject leftDetachedPaddle;
    public GameObject rightDetachedPaddle;

    [Header("Easy helpers")]
    public PaddleEasyHit leftEasyHit;
    public PaddleEasyHit rightEasyHit;

    [Header("Match Targets")]
    public int trainingTargetPoints = 9999;
    public int competitionTargetPoints = 11;
    public bool competitionWinByTwo = true;

    [Header("Runtime (Read Only)")]
    public GameMode currentMode = GameMode.Competition;
    public Difficulty leftDifficulty  = Difficulty.Medium;
    public Difficulty rightDifficulty = Difficulty.Medium;

    [Header("Player Names")]
    public string leftPlayerName  = "Left";
    public string rightPlayerName = "Right";

    // ---------- Difficulty / Names ----------
    public void SetLeftDifficulty_Easy()   { leftDifficulty  = Difficulty.Easy;   ApplyLeftDifficulty();  }
    public void SetLeftDifficulty_Med()    { leftDifficulty  = Difficulty.Medium; ApplyLeftDifficulty();  }
    public void SetLeftDifficulty_Hard()   { leftDifficulty  = Difficulty.Hard;   ApplyLeftDifficulty();  }

    public void SetRightDifficulty_Easy()  { rightDifficulty = Difficulty.Easy;   ApplyRightDifficulty(); }
    public void SetRightDifficulty_Med()   { rightDifficulty = Difficulty.Medium; ApplyRightDifficulty(); }
    public void SetRightDifficulty_Hard()  { rightDifficulty = Difficulty.Hard;   ApplyRightDifficulty(); }

    public void SetLeftPlayerName(string n)
    {
        if (!string.IsNullOrWhiteSpace(n)) leftPlayerName = n;
        if (scoreManager) scoreManager.RefreshHUDImmediate();
    }
    public void SetRightPlayerName(string n)
    {
        if (!string.IsNullOrWhiteSpace(n)) rightPlayerName = n;
        if (scoreManager) scoreManager.RefreshHUDImmediate();
    }

    // ---------- Modes ----------
    public void StartTraining()
    {
        currentMode = GameMode.Training;

        // Cameras: Right full-screen
        if (leftCam)  leftCam.enabled = false;
        if (rightCam) { rightCam.enabled = true; rightCam.rect = new Rect(0, 0, 1, 1); }

        // Players
        if (qinL) qinL.SetActive(false);
        if (qinR) qinR.SetActive(true);

        // Deflector on
        if (deflectorWallGO) deflectorWallGO.SetActive(true);

        // Paddles
        ApplyLeftOffForTraining(); // left side off in training
        ApplyRightDifficulty();    // right side as chosen

        // Rules + server lock + reset score
        if (scoreManager)
        {
            scoreManager.SetRallyScoring(true);
            scoreManager.SetTargetAndWinByTwo(trainingTargetPoints, false);
            scoreManager.SetServerLock(true, +1);              // always Right in Training
            scoreManager.ResetMatchToStartServer(1);           // 0:0, server Right
        }

        // Ball behavior: always serve Right in training
        if (ball)
        {
            ball.SetAlwaysServeRight(true);
            ball.ForceResetToServerSide(+1);
        }

        // Route current active paddles to ball + deflector
        ApplyActivePaddles();
    }

    public void StartCompetition()
    {
        currentMode = GameMode.Competition;

        // Cameras: split
        if (leftCam)  { leftCam.enabled = true;  leftCam.rect  = new Rect(0f, 0f, 0.5f, 1f); }
        if (rightCam) { rightCam.enabled = true; rightCam.rect = new Rect(0.5f, 0f, 0.5f, 1f); }

        // Players
        if (qinL) qinL.SetActive(true);
        if (qinR) qinR.SetActive(true);

        // Deflector off
        if (deflectorWallGO) deflectorWallGO.SetActive(false);

        // Paddles
        ApplyLeftDifficulty();
        ApplyRightDifficulty();

        // Competition = rally scoring ON
        if (scoreManager)
        {
            scoreManager.SetServerLock(false, +1);
            scoreManager.SetRallyScoring(true);
            scoreManager.SetTargetAndWinByTwo(competitionTargetPoints, competitionWinByTwo);
            scoreManager.ResetMatchToStartServer(scoreManager.startingServerSide);
        }

        // Ball: unlock always-right; place to current server
        if (ball && scoreManager)
        {
            ball.SetAlwaysServeRight(false);
            int sgn = (scoreManager.CurrentServerSide == 1) ? +1 : -1;
            ball.ForceResetToServerSide(sgn);
        }

        // Route current active paddles to ball (deflector is off in competition)
        ApplyActivePaddles();
    }

    public void RestartCurrentMatch()
    {
        if (currentMode == GameMode.Training) StartTraining();
        else                                   StartCompetition();
    }

    // ---------- Helpers ----------
    void ApplyLeftOffForTraining()
    {
        if (leftChildPaddle)    leftChildPaddle.SetActive(false);
        if (leftDetachedPaddle) leftDetachedPaddle.SetActive(false);
        if (leftEasyHit)        leftEasyHit.enabled = false;

        ApplyActivePaddles();
    }

    void ApplyLeftDifficulty()
    {
        if (currentMode == GameMode.Training) { ApplyLeftOffForTraining(); return; }
        switch (leftDifficulty)
        {
            case Difficulty.Easy:
                if (leftDetachedPaddle) leftDetachedPaddle.SetActive(false);
                if (leftChildPaddle)    leftChildPaddle.SetActive(true);
                if (leftEasyHit)        leftEasyHit.enabled = true;
                break;
            case Difficulty.Medium:
                if (leftDetachedPaddle) leftDetachedPaddle.SetActive(false);
                if (leftChildPaddle)    leftChildPaddle.SetActive(true);
                if (leftEasyHit)        leftEasyHit.enabled = false;
                break;
            case Difficulty.Hard:
                if (leftChildPaddle)    leftChildPaddle.SetActive(false);
                if (leftDetachedPaddle) leftDetachedPaddle.SetActive(true);
                if (leftEasyHit)        leftEasyHit.enabled = false;
                break;
        }
        ApplyActivePaddles();
    }

    void ApplyRightDifficulty()
    {
        switch (rightDifficulty)
        {
            case Difficulty.Easy:
                if (rightDetachedPaddle) rightDetachedPaddle.SetActive(false);
                if (rightChildPaddle)    rightChildPaddle.SetActive(true);
                if (rightEasyHit)        rightEasyHit.enabled = true;
                break;
            case Difficulty.Medium:
                if (rightDetachedPaddle) rightDetachedPaddle.SetActive(false);
                if (rightChildPaddle)    rightChildPaddle.SetActive(true);
                if (rightEasyHit)        rightEasyHit.enabled = false;
                break;
            case Difficulty.Hard:
                if (rightChildPaddle)    rightChildPaddle.SetActive(false);
                if (rightDetachedPaddle) rightDetachedPaddle.SetActive(true);
                if (rightEasyHit)        rightEasyHit.enabled = false;
                break;
        }
        ApplyActivePaddles();
    }

    public bool IsServerRight() => scoreManager ? (scoreManager.CurrentServerSide == 1) : true;

    // ---------- NEW: route the currently ACTIVE paddles ----------
    GameObject GetActiveLeftPaddle()
    {
        if (leftDetachedPaddle && leftDetachedPaddle.activeInHierarchy) return leftDetachedPaddle;
        if (leftChildPaddle    && leftChildPaddle.activeInHierarchy)    return leftChildPaddle;
        return leftChildPaddle ? leftChildPaddle : leftDetachedPaddle;
    }

    GameObject GetActiveRightPaddle()
    {
        if (rightDetachedPaddle && rightDetachedPaddle.activeInHierarchy) return rightDetachedPaddle;
        if (rightChildPaddle    && rightChildPaddle.activeInHierarchy)    return rightChildPaddle;
        if (qinRRootForWall) return qinRRootForWall.gameObject; // fallback for very old setup
        return rightChildPaddle ? rightChildPaddle : rightDetachedPaddle;
    }

    void ApplyActivePaddles()
    {
        var leftActive  = GetActiveLeftPaddle();
        var rightActive = GetActiveRightPaddle();

        // 1) Tell the Ball which paddles are "the" paddles
        if (ball) ball.SetPaddles(leftActive, rightActive);

        // 2) Training deflector should follow the active Right paddle
        if (deflectorWall && deflectorWallGO && deflectorWallGO.activeInHierarchy)
        {
            var target = rightActive ? rightActive.transform
                                     : (qinRRootForWall ? qinRRootForWall : null);
            if (target) deflectorWall.serverTarget = target;
        }
    }
}
