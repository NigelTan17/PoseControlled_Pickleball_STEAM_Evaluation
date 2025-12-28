// BallIgnoreBodies.cs — v2.3 (multi-ball-collider, toggle-able, debug)
// Attach to the BALL. Drag 琴 L / 琴 R into Player Roots.
// It ignores EVERY collider under those roots EXCEPT real paddles.

using System.Collections.Generic;
using UnityEngine;

public class BallIgnoreBodies : MonoBehaviour
{
    [Header("Players to ignore (不要让身体碰撞球)")]
    public Transform[] playerRoots;   // e.g., 琴 L, 琴 R

    [Header("Identify real paddles (保留拍子碰撞)")]
    [Tooltip("真正拍子的 Tag")]
    public string paddleTag = "Paddle";
    [Tooltip("若拍子物体上挂有名为 'PaddleHitGate' 的脚本，也视为拍子（可选）")]
    public bool detectByPaddleGate = true;

    [Header("Extra whitelist / blacklist")]
    public Collider[] paddleWhitelist;   // 永远当作拍子（不忽略）
    public Collider[] alwaysIgnore;      // 永远忽略

    [Header("Mode toggle (开=身体可撞球；关=只有拍子有效)")]
    public bool allowBodyHits = false;

    [Header("Debug")]
    public bool logAtStart = true;
    public bool logUnexpectedCollisions = true;

    // ---- internals ----
    private readonly List<Collider> _ballCols = new();        // ALL colliders on ball
    private readonly List<Collider> _bodyCols = new();        // colliders to ignore
    private readonly HashSet<(Collider,Collider)> _ignored = new(); // pairs we ignored

    void Awake()
    {
        CacheBallColliders();
        RebuildBodyList();
    }

    void OnValidate()
    {
        CacheBallColliders();
        RebuildBodyList();
        ApplyIgnoreState();
    }

    void Start()
    {
        // One more pass next frame in case rig enables colliders during Start
        Invoke(nameof(ApplyIgnoreState), 0.02f);
        if (logAtStart)
            Debug.Log($"[BallIgnoreBodies] ballCols={_ballCols.Count}, bodyCols={_bodyCols.Count}, allowBodyHits={allowBodyHits}");
    }

    void OnEnable()  => ApplyIgnoreState();
    void OnDisable() => RestoreAll();
    void OnDestroy() => RestoreAll();

    public void SetAllowBodyHits(bool allow)
    {
        allowBodyHits = allow;
        ApplyIgnoreState();
    }

    // ---------------- helpers ----------------
    private void CacheBallColliders()
    {
        _ballCols.Clear();
        GetComponents<Collider>(_ballCols);     // ALL colliders on the ball
        if (_ballCols.Count == 0) Debug.LogWarning("[BallIgnoreBodies] No collider on ball.");
    }

    private void RebuildBodyList()
    {
        _bodyCols.Clear();
        if (playerRoots == null) return;

        var wl = new HashSet<Collider>();
        if (paddleWhitelist != null) foreach (var c in paddleWhitelist) if (c) wl.Add(c);

        foreach (var root in playerRoots)
        {
            if (!root) continue;

            var cols = root.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                if (!c) continue;
                if (c.isTrigger) continue;             // triggers don't push the ball
                if (wl.Contains(c)) continue;          // whitelisted = real paddle

                // real paddle if has tag or a component named "PaddleHitGate"
                bool isPaddle = (paddleTag.Length > 0 && c.CompareTag(paddleTag));
                if (!isPaddle && detectByPaddleGate)
                {
                    var monos = c.GetComponentsInParent<MonoBehaviour>(true);
                    for (int i = 0; i < monos.Length; i++)
                    {
                        var mb = monos[i];
                        if (mb && mb.GetType().Name == "PaddleHitGate") { isPaddle = true; break; }
                    }
                }
                if (isPaddle) continue;

                _bodyCols.Add(c);
            }
        }

        if (alwaysIgnore != null)
            foreach (var c in alwaysIgnore) if (c && !_bodyCols.Contains(c)) _bodyCols.Add(c);
    }

    private void ApplyIgnoreState()
    {
        if (_ballCols.Count == 0 || _bodyCols.Count == 0) return;

        if (allowBodyHits)
        {
            // restore all pairs
            foreach (var bc in _ballCols)
                foreach (var hc in _bodyCols)
                    SafeSetIgnore(bc, hc, false);
        }
        else
        {
            // ignore all pairs
            foreach (var bc in _ballCols)
                foreach (var hc in _bodyCols)
                    SafeSetIgnore(bc, hc, true);
        }
    }

    private void RestoreAll()
    {
        foreach (var pair in _ignored)
            if (pair.Item1 && pair.Item2) Physics.IgnoreCollision(pair.Item1, pair.Item2, false);
        _ignored.Clear();
    }

    private void SafeSetIgnore(Collider a, Collider b, bool ignore)
    {
        if (!a || !b) return;
        Physics.IgnoreCollision(a, b, ignore);
        var key = (a, b);
        if (ignore) _ignored.Add(key); else _ignored.Remove(key);
    }

    // optional: log if we still collide with a body (helps pinpoint a missed collider)
    void OnCollisionEnter(Collision c)
    {
        if (!logUnexpectedCollisions) return;
        if (!BelongsToAnyRoot(c.collider.transform)) return;

        // check whether this collider is tagged as a paddle (whitelisted)
        bool taggedPaddle = paddleTag.Length > 0 && c.collider.CompareTag(paddleTag);
        if (!taggedPaddle)
            Debug.LogWarning($"[BallIgnoreBodies] Unexpected body collision from: {c.collider.name} (layer {LayerMask.LayerToName(c.collider.gameObject.layer)})");
    }

    private bool BelongsToAnyRoot(Transform t)
    {
        if (playerRoots == null) return false;
        foreach (var r in playerRoots) if (r && (t == r || t.IsChildOf(r))) return true;
        return false;
    }
}
