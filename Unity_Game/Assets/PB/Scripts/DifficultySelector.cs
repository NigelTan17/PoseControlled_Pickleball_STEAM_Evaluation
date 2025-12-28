using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DifficultySelector : MonoBehaviour
{
    [Header("Runtime")]
    public ModeRuntime mode;      // GameObject that has ModeRuntime
    public UIController ui;       // Main Canvas UIController
    public event Action onStartMatch;  // fired after StartMatch so UI can sync TopBar

    [Header("Name Inputs")]
    public TMP_InputField leftNameInput;
    public TMP_InputField rightNameInput;

    [Header("Left Choices")]
    public Button LEasyButton;
    public Button LMediumButton;
    public Button LHardButton;

    [Header("Right Choices")]
    public Button REasyButton;
    public Button RMediumButton;
    public Button RHardButton;

    [Header("Start")]
    public Button startMatchButton;

    private ModeRuntime.Difficulty? _left;
    private ModeRuntime.Difficulty? _right;

    void Awake()
    {
        if (!mode) mode = FindObjectOfType<ModeRuntime>();
        if (!ui)   ui   = FindObjectOfType<UIController>();
    }

    void Start()
    {
        if (LEasyButton)   LEasyButton.onClick.AddListener(() => { _left = ModeRuntime.Difficulty.Easy;    mode.SetLeftDifficulty_Easy();  HighlightLeft(_left);  RefreshStart(); });
        if (LMediumButton) LMediumButton.onClick.AddListener(() => { _left = ModeRuntime.Difficulty.Medium;mode.SetLeftDifficulty_Med();   HighlightLeft(_left);  RefreshStart(); });
        if (LHardButton)   LHardButton.onClick.AddListener(() => { _left = ModeRuntime.Difficulty.Hard;    mode.SetLeftDifficulty_Hard();  HighlightLeft(_left);  RefreshStart(); });

        if (REasyButton)   REasyButton.onClick.AddListener(() => { _right = ModeRuntime.Difficulty.Easy;   mode.SetRightDifficulty_Easy(); HighlightRight(_right); RefreshStart(); });
        if (RMediumButton) RMediumButton.onClick.AddListener(() => { _right = ModeRuntime.Difficulty.Medium;mode.SetRightDifficulty_Med();  HighlightRight(_right); RefreshStart(); });
        if (RHardButton)   RHardButton.onClick.AddListener(() => { _right = ModeRuntime.Difficulty.Hard;   mode.SetRightDifficulty_Hard(); HighlightRight(_right); RefreshStart(); });

        if (startMatchButton) startMatchButton.onClick.AddListener(OnStartClicked);

        RefreshStart();
    }

    public void OnStartClicked()
    {
        if (leftNameInput)  mode.SetLeftPlayerName(leftNameInput.text);
        if (rightNameInput) mode.SetRightPlayerName(rightNameInput.text);

        mode.StartCompetition();

        // Close overlay + panels so gameplay is visible
        if (ui) ui.HideAllModals();

        onStartMatch?.Invoke();           // let UI sync TopBar state
    }

    private void RefreshStart()
    {
        if (startMatchButton)
            startMatchButton.interactable = _left.HasValue && _right.HasValue;
    }

    private void HighlightLeft(ModeRuntime.Difficulty? d)
    {
        if (LEasyButton)   LEasyButton.interactable   = d != ModeRuntime.Difficulty.Easy;
        if (LMediumButton) LMediumButton.interactable = d != ModeRuntime.Difficulty.Medium;
        if (LHardButton)   LHardButton.interactable   = d != ModeRuntime.Difficulty.Hard;
    }

    private void HighlightRight(ModeRuntime.Difficulty? d)
    {
        if (REasyButton)   REasyButton.interactable   = d != ModeRuntime.Difficulty.Easy;
        if (RMediumButton) RMediumButton.interactable = d != ModeRuntime.Difficulty.Medium;
        if (RHardButton)   RHardButton.interactable   = d != ModeRuntime.Difficulty.Hard;
    }
}
