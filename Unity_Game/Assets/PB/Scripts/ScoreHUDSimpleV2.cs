using UnityEngine;
using TMPro;

public class ScoreHUDSimpleV2 : MonoBehaviour
{
    [Header("Runtime")]
    public ModeRuntime mode;                 // Drag your GameSystem (has ModeRuntime)

    [Header("Texts")]
    public TextMeshProUGUI scoreText;        // TopBar/ScoreText
    public TextMeshProUGUI serverText;       // TopBar/ServerText

    [Header("Options")]
    public bool showNamesInScore = true;
    public string defaultLeftName  = "Left";
    public string defaultRightName = "Right";

    int _lastL = -1, _lastR = -1;
    bool _lastServerRight;

    string LeftName  => (mode && !string.IsNullOrWhiteSpace(mode.leftPlayerName))  ? mode.leftPlayerName  : defaultLeftName;
    string RightName => (mode && !string.IsNullOrWhiteSpace(mode.rightPlayerName)) ? mode.rightPlayerName : defaultRightName;

    void Awake()
    {
        if (!scoreText || !serverText)
            Debug.LogWarning("ScoreHUDSimpleV2: assign ScoreText and ServerText in the inspector.");
    }

    // Call from ScoreManager whenever score/server changes, or call manually after Start
    public void SetScore(int l, int r)
    {
        _lastL = l; _lastR = r;
        if (!scoreText) return;

        if (showNamesInScore)
            scoreText.text = $"{LeftName} {l} : {r} {RightName}";
        else
            scoreText.text = $"{l} : {r}";
    }

    public void SetServer(bool serverIsRight)
    {
        _lastServerRight = serverIsRight;
        if (!serverText) return;

        serverText.text = $"Server: {(serverIsRight ? RightName : LeftName)}";
    }

    // Helper for name changes
    public void RefreshNames()
    {
        SetScore(_lastL, _lastR);
        SetServer(_lastServerRight);
    }
}
