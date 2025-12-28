using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// Main Canvas driver. Start/Difficulty/Training/Pause/GameOver overlays.
/// ESC/P toggles Pause when no blocking menu (Start/Difficulty/Training/GameOver) is open.
/// Cooperates with PanelAnimator (any of: Open/Close or Show/Hide); otherwise SetActive.
[DefaultExecutionOrder(100)] // make sure Update runs after most gameplay scripts
public class UIController : MonoBehaviour
{
    [Header("Runtime")]
    public ModeRuntime mode;               // GameSystem (ModeRuntime)

    [Header("Top Bar HUD (contains ScoreHUDSimpleV2)")]
    public GameObject topBar;              // Main Canvas/TopBar

    [Header("Modal Root + Panels")]
    public GameObject modalDim;            // Main Canvas/ModalDim
    public GameObject startPanel;          // ModalDim/StartPanel
    public GameObject difficultyPanel;     // ModalDim/DifficultyPanel (Competition)
    public GameObject trainingPanel;       // ModalDim/TrainingPanel   (Training)
    public GameObject pausePanel;          // PausePanel  (sibling of ModalDim)
    public GameObject gameOverPanel;       // ModalDim/GameOverPanel  (or sibling)

    [Header("GameOver texts (optional)")]
    public TMP_Text winnerText;            // GameOverPanel/WinnerText
    public TMP_Text finalText;             // GameOverPanel/FinalText

    [Header("Start buttons (auto-found if empty)")]
    public Button trainingButton;          // StartPanel/TrainingButton
    public Button competitionButton;       // StartPanel/CompetitionButton

    [Header("Competition Back (auto-find)")]
    public Button backButton;              // DifficultyPanel/BackButton

    [Header("Pause buttons (auto-found if empty)")]
    public Button pauseResumeButton;       // PausePanel/Resume
    public Button pauseRestartButton;      // PausePanel/Restart
    public Button pauseMainMenuButton;     // PausePanel/MainMenu

    [Header("GameOver buttons (auto-found if empty)")]
    public Button rematchButton;           // GameOverPanel/RematchButton
    public Button goMainMenuButton;        // GameOverPanel/MainMenuButton

    [Header("Hotkeys")]
    public KeyCode pauseKey1 = KeyCode.Escape;
    public KeyCode pauseKey2 = KeyCode.P;

    [Header("Exporter (optional)")]
    public ScoreExporter exporter;

    // Optional: TopBar/PauseButton (auto-found; will call OnClick_OpenPause)
    private Button topBarPauseButton;

    void Reset()  { AutoFindRefs(); }
    void Awake()
    {
        if (!mode) mode = FindObjectOfType<ModeRuntime>();
        EnsureEventSystem();
        AutoFindRefs();
    }

    void Start()
    {
        // Menus at boot
        Time.timeScale = 0f;

        // Realize panels at least once to ensure CanvasGroup exists
        SafeActivate(startPanel,       true);
        SafeActivate(difficultyPanel,  true);
        SafeActivate(trainingPanel,    true);
        SafeActivate(ResolveGameOverPanel(), true);
        SafeActivate(EnsurePausePanelRef(), true); // ensure we actually have it

        ShowStartMenu();

        AutoBindStartButtons();
        AutoBindPauseButtons();
        AutoBindGameOverButtons();
        BindCompetitionSelectorLink();
        BindTrainingSelectorLink();

        // Optional Pause button on TopBar (if you add one named "PauseButton")
        var t = topBar ? topBar.transform.Find("PauseButton") : null;
        topBarPauseButton = t ? t.GetComponent<Button>() : null;
        if (topBarPauseButton) topBarPauseButton.onClick.AddListener(OnClick_OpenPause);
    }

    void Update()
    {
        // If a compiled build accidentally lost the reference, re-acquire it here too.
        if (!pausePanel) EnsurePausePanelRef();

        // ⛔ DO NOT fire hotkeys while typing into any input field
        if (IsTypingInInput()) return;

        if (Input.GetKeyDown(pauseKey1) || Input.GetKeyDown(pauseKey2))
        {
            // If Pause is already visible -> close. Otherwise open only if no blocking menu.
            if (pausePanel && pausePanel.activeInHierarchy)
            {
                HidePause();
            }
            else if (!IsBlockingMenuOpen())
            {
                ShowPause();
            }
        }
    }

    // =================== Screens ===================
    public void ShowStartMenu()
    {
        SetOverlay(true);
        ShowPanel(startPanel);
        HidePanel(difficultyPanel);
        HidePanel(trainingPanel);
        HidePanel(EnsurePausePanelRef());
        HidePanel(ResolveGameOverPanel());
        SafeActivate(topBar, true);
        Time.timeScale = 0f;
    }

    public void ShowDifficulty()
    {
        SetOverlay(true);
        HidePanel(startPanel);
        HidePanel(trainingPanel);
        ShowPanel(difficultyPanel);
        HidePanel(EnsurePausePanelRef());
        HidePanel(ResolveGameOverPanel());
        SafeActivate(topBar, true);
        Time.timeScale = 0f;
    }

    public void ShowTrainingPanel()
    {
        SetOverlay(true);
        HidePanel(startPanel);
        HidePanel(difficultyPanel);
        ShowPanel(trainingPanel);
        HidePanel(EnsurePausePanelRef());
        HidePanel(ResolveGameOverPanel());
        SafeActivate(topBar, true);
        Time.timeScale = 0f;

        EnsureInteractive(trainingPanel);          // if cloned, make buttons clickable
    }

    public void HideAllModals()
    {
        SetOverlay(false);
        HidePanel(startPanel);
        HidePanel(difficultyPanel);
        HidePanel(trainingPanel);
        HidePanel(EnsurePausePanelRef());
        HidePanel(ResolveGameOverPanel());
        Time.timeScale = 1f;
    }

    // =================== Pause ===================
    public void OnClick_OpenPause() => ShowPause();

    public void ShowPause()
    {
        var pp = EnsurePausePanelRef();
        if (!pp) return;

        SetOverlay(true);
        ShowPanel(pp);
        EnsureInteractive(pp);
        Time.timeScale = 0f;
    }

    public void HidePause()
    {
        var pp = EnsurePausePanelRef();
        if (!pp) return;

        SetOverlay(false);
        HidePanel(pp);
        Time.timeScale = 1f;
    }

    // =================== Game Over (called by ScoreManager) ===================
    public void ShowGameOverPanelWithResult(int winner01, int leftScore, int rightScore)
    {
        var gop = ResolveGameOverPanel();
        SetOverlay(true);
        ShowPanel(gop);

        string leftName  = mode ? mode.leftPlayerName  : "Left";
        string rightName = mode ? mode.rightPlayerName : "Right";
        string winnerName = (winner01 == 0) ? leftName : rightName;

        if (winnerText) winnerText.text = $"Winner: {winnerName}";
        if (finalText)  finalText.text  = $"{leftName} {leftScore} – {rightScore} {rightName}";

        // Write TXT result (optional)
        if (exporter)
        {
            var leftDiff  = mode ? mode.leftDifficulty.ToString()  : "-";
            var rightDiff = mode ? mode.rightDifficulty.ToString() : "-";
            exporter.ExportFromParams(
                leftScore, rightScore,
                leftName, rightName, leftDiff, rightDiff,
                mode ? mode.currentMode.ToString() : "Unknown",
                mode ? FindObjectOfType<ScoreManager>().rallyScoring : true,
                FindObjectOfType<ScoreManager>().targetPoints,
                FindObjectOfType<ScoreManager>().winByTwo,
                FindObjectOfType<ScoreManager>().startingServerSide,
                FindObjectOfType<ScoreManager>().CurrentServerSide
            );
        }

        Time.timeScale = 0f;
    }
    public void HideGameOverPanel()
    {
        HidePanel(ResolveGameOverPanel());
        SetOverlay(false);
        Time.timeScale = 1f;
    }

    // =================== Buttons (Start screen) ===================
    public void OnClick_Training()     => ShowTrainingPanel();
    public void OnClick_Competition()  => ShowDifficulty();
    public void OnClick_BackToStart()  => ShowStartMenu();

    // ===== Training panel entry points (used by TrainingSelector) =====
    public void OnClick_TrainingStart()
    {
        if (mode) mode.StartTraining();
        HideAllModals();
        SyncTopBarWithMode();  // hide TopBar in Training
    }
    public void OnClick_TrainingBack() => ShowStartMenu();

    // =================== Buttons (Pause/GameOver) ===================
    public void OnClick_PauseResume()  => HidePause();

    public void OnClick_PauseRestart()
    {
        HidePause();
        if (mode) mode.RestartCurrentMatch();
        SyncTopBarWithMode();
    }

    public void OnClick_PauseMainMenu()
    {
        HidePause();
        ShowStartMenu();
    }

    public void OnClick_Rematch()
    {
        HideGameOverPanel();
        if (mode) mode.RestartCurrentMatch();
        SyncTopBarWithMode();
    }

    public void OnClick_GameOverMainMenu()
    {
        HideGameOverPanel();
        ShowStartMenu();
    }

    // =================== Helpers ===================
    public void SyncTopBarWithMode()
    {
        if (!topBar || !mode) return;
        SafeActivate(topBar, mode.currentMode == ModeRuntime.GameMode.Competition);
    }

    // Open Pause only when no blocking menu is open (ignores Pause itself)
    bool IsBlockingMenuOpen()
    {
        bool blocking =
            (startPanel       && startPanel.activeInHierarchy) ||
            (difficultyPanel  && difficultyPanel.activeInHierarchy) ||
            (trainingPanel    && trainingPanel.activeInHierarchy) ||
            (ResolveGameOverPanel() && ResolveGameOverPanel().activeInHierarchy);
        return blocking;
    }

    // ===== Panel show/hide that cooperates with PanelAnimator by name =====
    void ShowPanel(GameObject go)
    {
        if (!go) return;

        // ensure active so animator can run
        if (!go.activeSelf) go.SetActive(true);

        EnsureInteractive(go);

        // Try both method name pairs; ignore if they don't exist
        var pa = go.GetComponent<PanelAnimator>();
        if (pa)
        {
            pa.SendMessage("Open", SendMessageOptions.DontRequireReceiver);
            pa.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
        }

        go.transform.SetAsLastSibling();
    }

    void HidePanel(GameObject go)
    {
        if (!go) return;

        var pa = go.GetComponent<PanelAnimator>();
        if (pa)
        {
            pa.SendMessage("Close", SendMessageOptions.DontRequireReceiver);
            pa.SendMessage("Hide",  SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            go.SetActive(false);
        }
    }

    static void EnsureInteractive(GameObject root)
    {
        if (!root) return;
        foreach (var g in root.GetComponentsInChildren<CanvasGroup>(true))
        {
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
    }

    static void SafeActivate(GameObject go, bool on) { if (go && go.activeSelf != on) go.SetActive(on); }
    void SetOverlay(bool on) { if (modalDim) modalDim.SetActive(on); }

    // GameOver may be under ModalDim or sibling
    GameObject ResolveGameOverPanel()
    {
        if (gameOverPanel) return gameOverPanel;
        var t = transform.Find("ModalDim/GameOverPanel");
        if (!t) t = transform.Find("GameOverPanel");
        gameOverPanel = t ? t.gameObject : null;

        if (gameOverPanel)
        {
            if (!winnerText)  winnerText  = gameOverPanel.transform.Find("WinnerText")?.GetComponent<TMP_Text>();
            if (!finalText)   finalText   = gameOverPanel.transform.Find("FinalText")?.GetComponent<TMP_Text>();
            if (!rematchButton)     rematchButton     = gameOverPanel.transform.Find("RematchButton")?.GetComponent<Button>();
            if (!goMainMenuButton)  goMainMenuButton  = gameOverPanel.transform.Find("MainMenuButton")?.GetComponent<Button>();
        }
        return gameOverPanel;
    }

    // Always re-acquire PausePanel by name if missing.
    GameObject EnsurePausePanelRef()
    {
        if (pausePanel) return pausePanel;

        var t = transform.Find("PausePanel");                      // sibling of ModalDim (your screenshots)
        if (!t) t = transform.Find("ModalDim/PausePanel");         // (fallback) if someone moved it under ModalDim
        pausePanel = t ? t.gameObject : null;
        return pausePanel;
    }

    void AutoFindRefs()
    {
        if (!topBar)          topBar          = transform.Find("TopBar")?.gameObject;
        if (!modalDim)        modalDim        = transform.Find("ModalDim")?.gameObject;
        if (!startPanel)      startPanel      = transform.Find("ModalDim/StartPanel")?.gameObject;
        if (!difficultyPanel) difficultyPanel = transform.Find("ModalDim/DifficultyPanel")?.gameObject;

        if (!trainingPanel)
        {
            var t = transform.Find("ModalDim/TrainingPanel");
            if (!t) t = transform.Find("TrainingPanel"); // also support sibling layout
            trainingPanel = t ? t.gameObject : null;
        }

        EnsurePausePanelRef(); // set pausePanel if possible now

        // GameOver panel can be under ModalDim or as sibling
        ResolveGameOverPanel();

        if (!trainingButton)
            trainingButton = transform.Find("ModalDim/StartPanel/TrainingButton")?.GetComponent<Button>();
        if (!competitionButton)
            competitionButton = transform.Find("ModalDim/StartPanel/CompetitionButton")?.GetComponent<Button>();
        if (!backButton)
            backButton = transform.Find("ModalDim/DifficultyPanel/BackButton")?.GetComponent<Button>();

        if (!pauseResumeButton)
            pauseResumeButton = transform.Find("PausePanel/Resume")?.GetComponent<Button>();
        if (!pauseRestartButton)
            pauseRestartButton = transform.Find("PausePanel/Restart")?.GetComponent<Button>();
        if (!pauseMainMenuButton)
            pauseMainMenuButton = transform.Find("PausePanel/MainMenu")?.GetComponent<Button>();
    }

    void AutoBindStartButtons()
    {
        if (trainingButton)    trainingButton.onClick.AddListener(OnClick_Training);
        if (competitionButton) competitionButton.onClick.AddListener(OnClick_Competition);
        if (backButton)        backButton.onClick.AddListener(OnClick_BackToStart);
    }

    void AutoBindPauseButtons()
    {
        // Make sure buttons exist even if PausePanel was inactive during Awake
        EnsurePausePanelRef();
        if (!pauseResumeButton)
            pauseResumeButton = transform.Find("PausePanel/Resume")?.GetComponent<Button>();
        if (!pauseRestartButton)
            pauseRestartButton = transform.Find("PausePanel/Restart")?.GetComponent<Button>();
        if (!pauseMainMenuButton)
            pauseMainMenuButton = transform.Find("PausePanel/MainMenu")?.GetComponent<Button>();

        if (pauseResumeButton)   pauseResumeButton.onClick.AddListener(OnClick_PauseResume);
        if (pauseRestartButton)  pauseRestartButton.onClick.AddListener(OnClick_PauseRestart);
        if (pauseMainMenuButton) pauseMainMenuButton.onClick.AddListener(OnClick_PauseMainMenu);
    }

    void AutoBindGameOverButtons()
    {
        var gop = ResolveGameOverPanel();
        if (!gop) return;
        if (!rematchButton)     rematchButton     = gop.transform.Find("RematchButton")?.GetComponent<Button>();
        if (!goMainMenuButton)  goMainMenuButton  = gop.transform.Find("MainMenuButton")?.GetComponent<Button>();
        if (rematchButton)      rematchButton.onClick.AddListener(OnClick_Rematch);
        if (goMainMenuButton)   goMainMenuButton.onClick.AddListener(OnClick_GameOverMainMenu);
    }

    void BindCompetitionSelectorLink()
    {
        if (!difficultyPanel) return;
        var diff = difficultyPanel.GetComponentInChildren<DifficultySelector>(true);
        if (diff)
        {
            if (!diff.ui) diff.ui = this;                // StartMatch closes overlay
            diff.onStartMatch += SyncTopBarWithMode;     // keep topbar state correct
        }
    }

    void BindTrainingSelectorLink()
    {
        if (!trainingPanel) return;
        var selector = trainingPanel.GetComponentInChildren<TrainingSelector>(true);
        if (selector && !selector.ui) selector.ui = this;
    }

    static void EnsureEventSystem()
    {
        if (!Object.FindObjectOfType<EventSystem>())
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(es);
        }
    }

    // ========= INPUT GUARD =========
    // True if the user is typing in TMP_InputField or legacy InputField
    static bool IsTypingInInput()
    {
        if (!EventSystem.current) return false;
        var go = EventSystem.current.currentSelectedGameObject;
        if (!go) return false;

        var tmp = go.GetComponent<TMP_InputField>();
        if (tmp != null && tmp.isFocused) return true;

        var legacy = go.GetComponent<InputField>();
        if (legacy != null && legacy.isFocused) return true;

        return false;
    }
}
