using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSFXLite : MonoBehaviour
{
    [Header("Enable which sounds")]
    public bool paddleHit = true;
    public bool courtBounce = false;
    public bool netHit = false;
    public bool wallHit = false;

    [Header("Paddle hit playback")]
    public bool paddleHitAs2D = true;     // <— default ON so it's always audible
    [Range(0f, 1f)] public float paddleHit2DVolume = 1f;

    [Header("Keys in AudioController")]
    public string paddleKey = "BallHit";
    public string courtKey  = "BallBounce";
    public string netKey    = "NetHit";
    public string wallKey   = "WallHit";

    [Header("Match by Tag or Layer (either works)")]
    public string paddleTag = "Paddle";
    public string courtTag  = "Court";
    public string netTag    = "Net";
    public string wallTag   = "Wall";
    public LayerMask paddleLayers; // e.g., PlayerHit
    public LayerMask courtLayers;
    public LayerMask netLayers;
    public LayerMask wallLayers;

    [Header("Options")]
    public bool acceptParentTag = true; // walk up parents for tag check
    public bool debug = false;

    [Header("Spam / Loudness (3D only)")]
    public float minInterval = 0.10f;    // min gap between plays (s)
    public float minSpeed = 0.0f;        // keep 0 so triggers still make a sound
    public float maxSpeed = 20f;

    float _lastPlay = -999f;

    void OnCollisionEnter(Collision c)
    {
        Vector3 at = c.GetContact(0).point;
        Handle(c.collider, c.relativeVelocity.magnitude, at);
    }

    void OnCollisionStay(Collision c)
    {
        if (Time.time - _lastPlay > 0.25f)
        {
            Vector3 at = c.GetContact(0).point;
            Handle(c.collider, c.relativeVelocity.magnitude, at);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Handle(other, 0f, transform.position);
    }

    void Handle(Collider col, float speed, Vector3 at)
    {
        if (!AudioController.Instance) return;
        if (Time.time - _lastPlay < minInterval) return;

        string key = null;

        if (paddleHit && IsMatch(col, paddleTag, paddleLayers)) key = paddleKey;
        else if (courtBounce && IsMatch(col, courtTag, courtLayers)) key = courtKey;
        else if (netHit && IsMatch(col, netTag, netLayers)) key = netKey;
        else if (wallHit && IsMatch(col, wallTag, wallLayers)) key = wallKey;

        if (debug)
        {
            string m = key ?? "none";
            Debug.Log($"[BallSFX] hit={col.name} tag={col.tag} layer={LayerMask.LayerToName(col.gameObject.layer)} -> {m}");
        }

        if (string.IsNullOrEmpty(key)) return;

        if (key == paddleKey && paddleHitAs2D)
        {
            // Guaranteed audible paddle click
            AudioController.Instance.PlaySFX2D(key, paddleHit2DVolume, Random.Range(0.97f, 1.05f));
            _lastPlay = Time.time;
            return;
        }

        // 3D path (for court/net/wall, or if you disable 2D for paddle)
        float k = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        float vol = Mathf.Lerp(0.25f, 1f, k);
        float pitch = Random.Range(0.97f, 1.05f);
        AudioController.Instance.PlaySFX3D(key, at, vol, pitch);
        _lastPlay = Time.time;
    }

    bool IsMatch(Collider col, string wantTag, LayerMask layers)
    {
        // Layer wins if set
        if (layers.value != 0)
        {
            if (((1 << col.gameObject.layer) & layers.value) != 0) return true;
            var t = col.transform.parent;
            while (t != null)
            {
                if (((1 << t.gameObject.layer) & layers.value) != 0) return true;
                t = t.parent;
            }
        }

        // Tag checks (self and parents if allowed)
        if (!string.IsNullOrEmpty(wantTag))
        {
            if (col.CompareTag(wantTag)) return true;
            if (acceptParentTag)
            {
                var t = col.transform.parent;
                while (t != null)
                {
                    if (t.CompareTag(wantTag)) return true;
                    t = t.parent;
                }
            }
        }
        return false;
    }
}
