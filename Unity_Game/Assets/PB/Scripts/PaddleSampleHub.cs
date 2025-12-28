using UnityEngine;
using System;
using PB.Scripts; // your UDPReceive is in PB.Scripts

/// Central parser for paddle samples appended to the existing body CSV.
/// Body = first 99 floats (33 points * xyz), then zero or more blocks:
///   PADL, x, y, z, qx, qy, qz, qw, t
///   PADR, x, y, z, qx, qy, qz, qw, t
public class PaddleSampleHub : MonoBehaviour
{
    public enum Side { Left, Right }

    [Header("Source (same UDP data as BodyDriver)")]
    public UDPReceive udp;               // drag your existing UDPReceive (port 5052)
    public bool logPackets = false;

    [Header("Parsing")]
    [Tooltip("Body floats count (33 * 3). We ignore anything before this index.")]
    public int bodyFloatCount = 99;

    struct Sample
    {
        public Vector3 pos;
        public Quaternion rot;
        public double senderTime;           // from packet
        public float localArrival;          // Time.realtimeSinceStartup when parsed
        public bool valid;
    }

    private Sample _left, _right;
    private string _lastParsed;             // avoid reparsing same frame/string

    void Update()
    {
        if (udp == null) return;
        string s = udp.data;
        if (string.IsNullOrEmpty(s)) return;

        // If UDPReceive writes the same string instance many frames, skip reparsing.
        if (ReferenceEquals(s, _lastParsed)) return;
        _lastParsed = s;

        ParsePacket(s);
    }

    void ParsePacket(string s)
    {
        var tok = s.Split(',');
        if (tok.Length <= bodyFloatCount) return;

        int i = bodyFloatCount;
        while (i < tok.Length)
        {
            string tag = tok[i].Trim();

            if (tag.Equals("PADL", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("L", StringComparison.OrdinalIgnoreCase))
            {
                if (TryReadBlock(tok, i + 1, out Sample smp, out int consumed))
                {
                    _left = smp; _left.valid = true;
                    if (logPackets) Debug.Log($"[Hub] PADL pos={_left.pos} ts={_left.senderTime}");
                    i = i + 1 + consumed;
                    continue;
                }
            }
            else if (tag.Equals("PADR", StringComparison.OrdinalIgnoreCase) ||
                     tag.Equals("R", StringComparison.OrdinalIgnoreCase))
            {
                if (TryReadBlock(tok, i + 1, out Sample smp, out int consumed))
                {
                    _right = smp; _right.valid = true;
                    if (logPackets) Debug.Log($"[Hub] PADR pos={_right.pos} ts={_right.senderTime}");
                    i = i + 1 + consumed;
                    continue;
                }
            }

            // Unknown token or trailing garbage — advance safely
            i++;
        }
    }

    // Reads exactly: x,y,z,qx,qy,qz,qw,t  (8 tokens)
    static bool TryReadBlock(string[] tok, int start, out Sample smp, out int consumed)
    {
        smp = default;
        consumed = 0;
        const int need = 8;
        if (start + need > tok.Length) return false;

        // Predeclare locals so older compilers don't complain about "unassigned variable".
        float x = 0, y = 0, z = 0, qx = 0, qy = 0, qz = 0, qw = 1;
        double t = 0;

        bool ok =
            float.TryParse(tok[start + 0], out x) &&
            float.TryParse(tok[start + 1], out y) &&
            float.TryParse(tok[start + 2], out z) &&
            float.TryParse(tok[start + 3], out qx) &&
            float.TryParse(tok[start + 4], out qy) &&
            float.TryParse(tok[start + 5], out qz) &&
            float.TryParse(tok[start + 6], out qw) &&
            double.TryParse(tok[start + 7], out t);

        if (!ok) return false;

        smp.pos = new Vector3(x, y, z);
        smp.rot = new Quaternion(qx, qy, qz, qw);
        smp.senderTime = t;
        smp.localArrival = Time.realtimeSinceStartup;
        consumed = need;
        return true;
    }

    /// Returns true if we have any sample for the side. ageSec is how long since arrival.
    public bool TryGet(Side side, out Vector3 pos, out Quaternion rot, out float ageSec)
    {
        var now = Time.realtimeSinceStartup;
        if (side == Side.Left && _left.valid)
        {
            pos = _left.pos; rot = _left.rot; ageSec = now - _left.localArrival; return true;
        }
        if (side == Side.Right && _right.valid)
        {
            pos = _right.pos; rot = _right.rot; ageSec = now - _right.localArrival; return true;
        }
        pos = default; rot = default; ageSec = float.PositiveInfinity; return false;
    }
}
