using UnityEngine;

/// Drives a root-level replicated paddle from PaddleSampleHub (single UDP stream).
/// When the stream is stale, smoothly falls back toward the avatar hand.
[DefaultExecutionOrder(80)]
public class PaddleDriver : MonoBehaviour
{
    public enum Side { Left, Right }

    [Header("Identity")]
    public Side side = Side.Left;

    [Header("Inputs")]
    public PaddleSampleHub hub;           // drag from GameSystem
    public Transform handFallback;        // avatar hand bone (the one that holds paddle)

    [Header("Calibration Offsets")]
    public Vector3 posOffset = Vector3.zero;      // meters
    public Vector3 rotOffsetEuler = Vector3.zero; // degrees

    [Header("Smoothing")]
    [Range(0f, 1f)] public float posLerpAlpha = 0.18f;
    [Range(0f, 1f)] public float rotLerpAlpha = 0.18f;
    [Tooltip("If distance to target exceeds this, snap to target (meters).")]
    public float maxTeleportDistance = 2.0f;

    [Header("Staleness & Fallback")]
    [Tooltip("No sample newer than this → stream considered stale (ms).")]
    public int sampleTimeoutMs = 140;
    [Tooltip("When stale, glide toward hand for this duration before fully binding back (ms).")]
    public int graceToFallbackMs = 600;

    [Header("Debug")]
    public bool debugGizmos = false;

    Quaternion RotOffset => Quaternion.Euler(rotOffsetEuler);

    Vector3 _targetPos;
    Quaternion _targetRot;
    bool _hadLiveOnce;

    void Reset()
    {
        // Try to auto-locate hub
        if (!hub) hub = FindObjectOfType<PaddleSampleHub>();
    }

    void Start()
    {
        _targetPos = transform.position;
        _targetRot = transform.rotation;
    }

    void Update()
    {
        if (!hub)
        {
            // fallback only
            if (handFallback) ApplyToward(handFallback.position, handFallback.rotation);
            return;
        }

        if (hub.TryGet(side == Side.Left ? PaddleSampleHub.Side.Left : PaddleSampleHub.Side.Right,
                       out Vector3 livePos, out Quaternion liveRot, out float ageSec))
        {
            float ageMs = ageSec * 1000f;
            bool isFresh = ageMs <= sampleTimeoutMs;

            if (isFresh)
            {
                _hadLiveOnce = true;
                Vector3 p = livePos + posOffset;
                Quaternion r = liveRot * RotOffset;
                MoveSmooth(p, r);
            }
            else
            {
                // stale: glide toward hand over grace window if we ever had live data
                if (handFallback)
                {
                    if (_hadLiveOnce && ageMs < (sampleTimeoutMs + graceToFallbackMs))
                    {
                        // blend target between last live target and hand target by staleness
                        float k = Mathf.InverseLerp(sampleTimeoutMs, sampleTimeoutMs + graceToFallbackMs, ageMs);
                        Vector3 p = Vector3.Lerp(_targetPos, handFallback.position, k);
                        Quaternion r = Quaternion.Slerp(_targetRot, handFallback.rotation, k);
                        MoveSmooth(p, r);
                    }
                    else
                    {
                        // fully fallback
                        ApplyToward(handFallback.position, handFallback.rotation);
                    }
                }
            }
        }
        else
        {
            // no sample ever: stick to hand
            if (handFallback) ApplyToward(handFallback.position, handFallback.rotation);
        }
    }

    void MoveSmooth(Vector3 targetPos, Quaternion targetRot)
    {
        // snap if huge jump (e.g., calibration jump)
        if (Vector3.Distance(transform.position, targetPos) > maxTeleportDistance)
        {
            transform.SetPositionAndRotation(targetPos, targetRot);
            _targetPos = targetPos; _targetRot = targetRot;
            return;
        }

        _targetPos = Vector3.Lerp(transform.position, targetPos, posLerpAlpha);
        _targetRot = Quaternion.Slerp(transform.rotation, targetRot, rotLerpAlpha);
        transform.SetPositionAndRotation(_targetPos, _targetRot);
    }

    void ApplyToward(Vector3 p, Quaternion r)
    {
        _targetPos = Vector3.Lerp(transform.position, p, posLerpAlpha);
        _targetRot = Quaternion.Slerp(transform.rotation, r, rotLerpAlpha);
        transform.SetPositionAndRotation(_targetPos, _targetRot);
    }

    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        Gizmos.color = (side == Side.Left) ? Color.cyan : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
}
