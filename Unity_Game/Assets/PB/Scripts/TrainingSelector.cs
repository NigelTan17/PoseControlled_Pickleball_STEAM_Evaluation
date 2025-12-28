using UnityEngine;
using UnityEngine.UI;

/// Single-side training difficulty picker. Auto-finds buttons by name and wires them.
public class TrainingSelector : MonoBehaviour
{
    [Header("Refs")]
    public ModeRuntime mode;      // GameSystem (ModeRuntime) or auto-find
    public UIController ui;       // Canvas (UIController) or auto-find

    [Header("Right Difficulty Buttons (optional – auto-found by name)")]
    public Button REasyButton;       // "EasyButton"        or "TrainPanel/EasyButton"
    public Button RMediumButton;     // "MediumButton"      or "TrainPanel/MediumButton"
    public Button RHardButton;       // "HardButton"        or "TrainPanel/HardButton"
    public Button StartButton;       // "StartMatchButton"
    public Button BackButton;        // "BackButton"

    private ModeRuntime.Difficulty _rightDiff = ModeRuntime.Difficulty.Medium;

    void Awake()
    {
        if (!mode) mode = FindObjectOfType<ModeRuntime>();
        if (!ui)   ui   = FindObjectOfType<UIController>();

        AutoFindButtonsIfMissing();
        AutoBind();
    }

    void OnEnable()
    {
        ApplyRightDifficulty(_rightDiff); // default Medium visual
        EnsureButtonsInteractable();
    }

    // ---- Difficulty selection (Right only) ----
    public void ChooseRightEasy()   => ApplyRightDifficulty(ModeRuntime.Difficulty.Easy);
    public void ChooseRightMedium() => ApplyRightDifficulty(ModeRuntime.Difficulty.Medium);
    public void ChooseRightHard()   => ApplyRightDifficulty(ModeRuntime.Difficulty.Hard);

    void ApplyRightDifficulty(ModeRuntime.Difficulty d)
    {
        _rightDiff = d;

        if (mode)
        {
            switch (d)
            {
                case ModeRuntime.Difficulty.Easy:   mode.SetRightDifficulty_Easy(); break;
                case ModeRuntime.Difficulty.Medium: mode.SetRightDifficulty_Med();  break;
                case ModeRuntime.Difficulty.Hard:   mode.SetRightDifficulty_Hard(); break;
            }
        }

        // Visual: selected difficulty button is non-interactable (highlighted)
        if (REasyButton)   REasyButton.interactable   = d != ModeRuntime.Difficulty.Easy;
        if (RMediumButton) RMediumButton.interactable = d != ModeRuntime.Difficulty.Medium;
        if (RHardButton)   RHardButton.interactable   = d != ModeRuntime.Difficulty.Hard;
    }

    // ---- Start/Back ----
    public void OnClick_Start()
    {
        if (ui) ui.OnClick_TrainingStart(); // UIController starts training & closes overlay
    }

    public void OnClick_Back()
    {
        if (ui) ui.OnClick_TrainingBack();
    }

    // ---------- Utilities ----------
    void AutoFindButtonsIfMissing()
    {
        if (!REasyButton)
        {
            var t = transform.Find("TrainPanel/EasyButton");
            if (!t) t = transform.Find("EasyButton");
            REasyButton = t ? t.GetComponent<Button>() : null;
        }
        if (!RMediumButton)
        {
            var t = transform.Find("TrainPanel/MediumButton");
            if (!t) t = transform.Find("MediumButton");
            RMediumButton = t ? t.GetComponent<Button>() : null;
        }
        if (!RHardButton)
        {
            var t = transform.Find("TrainPanel/HardButton");
            if (!t) t = transform.Find("HardButton");
            RHardButton = t ? t.GetComponent<Button>() : null;
        }
        if (!StartButton)
        {
            var t = transform.Find("StartMatchButton");
            StartButton = t ? t.GetComponent<Button>() : null;
        }
        if (!BackButton)
        {
            var t = transform.Find("BackButton");
            BackButton = t ? t.GetComponent<Button>() : null;
        }
    }

    void AutoBind()
    {
        if (REasyButton)   REasyButton.onClick.AddListener(ChooseRightEasy);
        if (RMediumButton) RMediumButton.onClick.AddListener(ChooseRightMedium);
        if (RHardButton)   RHardButton.onClick.AddListener(ChooseRightHard);
        if (StartButton)   StartButton.onClick.AddListener(OnClick_Start);
        if (BackButton)    BackButton.onClick.AddListener(OnClick_Back);
    }

    void EnsureButtonsInteractable()
    {
        // If this panel was cloned, a CanvasGroup may be non-interactable
        var groups = GetComponentsInChildren<CanvasGroup>(true);
        foreach (var g in groups)
        {
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
        if (StartButton) StartButton.interactable = true;
        if (BackButton)  BackButton.interactable  = true;
    }
}
