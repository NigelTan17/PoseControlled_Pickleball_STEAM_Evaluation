using UnityEngine;
using System.Reflection;

/// Plays music (key "Match") ONLY during gameplay (not Start/Difficulty/Training/Pause/GameOver).
public class MatchMusicAuto : MonoBehaviour
{
    public UIController ui;
    public string matchMusicKey = "Match";
    public float fadeIn = 0.8f, fadeOut = 0.5f;

    MethodInfo _isBlocking;
    FieldInfo _pausePanelField;
    bool _playing;

    void Start()
    {
        if (!ui) ui = FindObjectOfType<UIController>();
        if (ui)
        {
            var t = typeof(UIController);
            _isBlocking    = t.GetMethod("IsBlockingMenuOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            _pausePanelField = t.GetField("pausePanel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    void Update()
    {
        if (!AudioController.Instance || !ui || _isBlocking == null) return;

        bool blocking = (bool)_isBlocking.Invoke(ui, null);
        var pausePanel = _pausePanelField?.GetValue(ui) as GameObject;
        bool paused = pausePanel && pausePanel.activeInHierarchy;

        bool inMenuOrPaused = blocking || paused;

        if (!inMenuOrPaused && !_playing)
        { AudioController.Instance.PlayMusic(matchMusicKey, fadeIn); _playing = true; }
        else if (inMenuOrPaused && _playing)
        { AudioController.Instance.StopMusic(fadeOut); _playing = false; }
    }
}
