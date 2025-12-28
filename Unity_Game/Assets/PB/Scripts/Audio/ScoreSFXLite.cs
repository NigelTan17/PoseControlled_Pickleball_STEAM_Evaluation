using UnityEngine;
using System;
using System.Reflection;

/// Score SFX (compat): beeps when score increases, no edits to ScoreManager needed.
/// It auto-binds to common names (LeftScore/rightScore/etc). GameOver is optional.
public class ScoreSFXLite : MonoBehaviour
{
    public ScoreManager scoreManager;
    [Header("Keys in AudioController")]
    public string pointKey = "Score";
    public string gameOverKey = "GameOver";
    [Tooltip("Try to play GameOver if a flag exists (optional).")]
    public bool enableGameOver = false;

    Func<int> _getLeft, _getRight;
    Func<bool> _getOver; // may be null

    int _lastL, _lastR;
    bool _init, _overPlayed;

    void Start()
    {
        if (!scoreManager) scoreManager = FindObjectOfType<ScoreManager>();
        BindAccessors();
        Snapshot();
    }

    void BindAccessors()
    {
        if (!scoreManager) return;
        var t = scoreManager.GetType();
        _getLeft  = BindInt(t, scoreManager, new[] { "LeftScore", "leftScore", "scoreLeft", "lScore" });
        _getRight = BindInt(t, scoreManager, new[] { "RightScore", "rightScore", "scoreRight", "rScore" });

        if (enableGameOver)
            _getOver = BindBool(t, scoreManager, new[] { "IsGameOver", "GameOver", "gameOver", "isGameOver" });
    }

    Func<int> BindInt(Type t, object inst, string[] names)
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        foreach (var n in names)
        {
            var p = t.GetProperty(n, BF); if (p != null && p.PropertyType == typeof(int)) return () => (int)p.GetValue(inst);
            var f = t.GetField(n, BF);    if (f != null && f.FieldType == typeof(int))   return () => (int)f.GetValue(inst);
            var m = t.GetMethod("get_" + n, BF); if (m != null && m.ReturnType == typeof(int) && m.GetParameters().Length == 0)
                return () => (int)m.Invoke(inst, null);
        }
        // fallback => 0 (still lets us beep on the other side changing)
        return () => 0;
    }

    Func<bool> BindBool(Type t, object inst, string[] names)
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        foreach (var n in names)
        {
            var p = t.GetProperty(n, BF); if (p != null && p.PropertyType == typeof(bool)) return () => (bool)p.GetValue(inst);
            var f = t.GetField(n, BF);    if (f != null && f.FieldType == typeof(bool))   return () => (bool)f.GetValue(inst);
            var m = t.GetMethod("get_" + n, BF); if (m != null && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
                return () => (bool)m.Invoke(inst, null);
        }
        return null;
    }

    void Snapshot()
    {
        if (scoreManager == null) return;
        _lastL = _getLeft != null ? _getLeft() : 0;
        _lastR = _getRight != null ? _getRight() : 0;
        _init = true;
    }

    void Update()
    {
        if (!AudioController.Instance || scoreManager == null || _getLeft == null || _getRight == null) return;

        int l = _getLeft();
        int r = _getRight();

        if (_init)
        {
            if (l > _lastL || r > _lastR)
                AudioController.Instance.PlaySFX2D(pointKey, 1f);

            if (enableGameOver && _getOver != null)
            {
                bool over = _getOver();
                if (over && !_overPlayed)
                {
                    AudioController.Instance.PlaySFX2D(gameOverKey, 1f);
                    _overPlayed = true;
                }
                if (!over) _overPlayed = false;
            }
        }

        _lastL = l; _lastR = r; _init = true;
    }
}
