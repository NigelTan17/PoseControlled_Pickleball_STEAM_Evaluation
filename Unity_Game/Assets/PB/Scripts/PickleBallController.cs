using System.Collections;
using UnityEngine;

namespace PB.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class PickleBallController : MonoBehaviour
    {
        [Header("Serve Points / Reset 发球点")]
        [SerializeField] private Transform servePointLeft;
        [SerializeField] private Transform servePointRight;

        [Header("Paddles (用于识别最后击球方)")]
        [SerializeField] private GameObject paddleL;
        [SerializeField] private GameObject paddleR;

        [Header("Scoring / Match Flow")]
        [SerializeField] private ScoreManager scoreManager;

        [Header("Debug")]
        [SerializeField] private bool debugRules = true;

        private Rigidbody _rb;

        [Header("Ball Flight 统一球物理手感")]
        [SerializeField] private float targetSpeed = 46f;
        [SerializeField] private float loft = 1.2f;
        [SerializeField] private float maxUpVel = 12f;
        [SerializeField] private float maxDownVel = 25f;

        [Header("Ground Bounce 地面反弹（更弹）")]
        [SerializeField] private float firstBounceMinUp = 7.0f;
        [SerializeField] private float minBounceUp = 5.0f;
        [Range(0.9f, 1.08f)] [SerializeField] private float restitutionY = 1.02f;
        [Range(0.98f, 1.02f)] [SerializeField] private float keepHorizontal = 1.00f;

        [Header("Aim Assist 瞄准辅助")]
        [Range(0f, 1f)] [SerializeField] private float aimAssistBlend = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float centerXBias   = 0.55f;

        [Header("Hit Assist (QOL)")]
        [Range(0f, 0.9f)][SerializeField] private float minHitUp = 0.22f;
        [Range(0f, 0.9f)][SerializeField] private float minForwardZ = 0.42f;
        [SerializeField] private float nearNetMeters = 1.20f;
        [SerializeField] private float nearNetSpeedBoost = 6f;
        [SerializeField] private float minLaunchSpeedMS = 4f;

        [Header("Serve Safety 发球安全气泡 (Layer)")]
        [SerializeField] private Vector3 serveOffset = new Vector3(0f, 1.2f, 2f);
        [SerializeField] private float serveLiftY = 0.18f;
        [SerializeField] private float serveShieldSeconds = 0.20f;
        [SerializeField] private string ballLayerName = "Ball";
        [SerializeField] private string ballServeLayerName = "BallServe";
        [SerializeField] private string playerHitLayerName = "PlayerHit";
        private int _ballLayer = -1, _ballServeLayer = -1, _playerHitLayer = -1;

        [Header("Net & Orientation 网 & 朝向")]
        [SerializeField] private float netZ = 0f;
        [Tooltip("If ON, any net touch faults the striker. Turn OFF for normal let play.")]
        [SerializeField] private bool resetOnNetHit = false;

        [Tooltip("Tick if the RIGHT player's court lies on positive Z. Untick if Right is on negative Z.")]
        [SerializeField] private bool rightCourtIsPositiveZ = false; // Qin R = −Z, Qin L = +Z

        [Header("Serve Ownership 发球归属")]
        [Tooltip("Training convenience only. Turn OFF in Competition.")]
        [SerializeField] private bool alwaysServeRight = false;

        [Header("Rules")]
        [Tooltip("Two-bounce rule enforcement after serve. Disable for training if you want volleys immediately.")]
        [SerializeField] private bool enforceTwoBounceRule = true;

        private bool _launched;
        private float _prevZ;

        private bool _servePhase;
        private bool _firstGroundAfterServe;
        private int  _servingSide = 1;      // +1 Right, -1 Left (ScoreManager convention)
        private int  _receiverSide => -_servingSide;

        private bool _needBounceOnReceiver;
        private bool _needBounceOnServer;
        private bool _serveReceiverBounceDone;

        private int  _lastGroundSide = 0;      // sign of last ground contact
        private int  _sameSideBounceCount = 0;

        private int  _groundBounceCount = 0;   // since last hit
        private int  _lastHitter = 0;          // ±1; 0 = none yet this rally

        private int _awaitingCrossNetFrom = 0; // requires crossing before landing

        [Header("Hit spam guards")]
        [SerializeField] private float postVirtualIgnoreSeconds = 0.12f;
        [SerializeField] private float postRealHitIgnoreSeconds = 0.08f;
        private float _ignorePaddleUntil = 0f;

        public bool IsInServeShield => gameObject.layer == _ballServeLayer;

        // ---------- Orientation helpers ----------
        private int SideFromZ(float zWorld)
        {
            int raw = (zWorld > netZ) ? +1 : -1;       // +1 = +Z, -1 = -Z
            return rightCourtIsPositiveZ ? raw : -raw; // map to ScoreManager convention
        }
        private bool IsOnServingSide(float zWorld) => SideFromZ(zWorld) == _servingSide;

        private float WorldZForSide(int sideSign)
        {
            float rightZ = rightCourtIsPositiveZ ? +60f : -60f;
            float leftZ  = -rightZ;
            return (sideSign > 0) ? rightZ : leftZ;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 0.05f;
            _rb.isKinematic = false;

            _ballLayer      = LayerMask.NameToLayer(ballLayerName);
            _ballServeLayer = LayerMask.NameToLayer(ballServeLayerName);
            _playerHitLayer = LayerMask.NameToLayer(playerHitLayerName);

            var col = GetComponent<Collider>();
            if (col) col.material = null;
        }

        private void Start()
        {
            if (servePointRight) ResetToServe(servePointRight);
            else if (servePointLeft) ResetToServe(servePointLeft);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && !_launched) LaunchBall();
            if (Input.GetKeyDown(KeyCode.R) && servePointRight) ResetToServe(servePointRight);
            if (Input.GetKeyDown(KeyCode.T) && servePointLeft)  ResetToServe(servePointLeft);
        }

        private void FixedUpdate()
        {
            if (!_launched) return;
            float nowZ = transform.position.z;
            if (Mathf.Sign(_prevZ) != Mathf.Sign(nowZ))
            {
                _servePhase = false;
                if (_awaitingCrossNetFrom != 0)
                {
                    if (debugRules) Debug.Log("[Rally] Crossed net after hit — ok");
                    _awaitingCrossNetFrom = 0;
                }
            }
            _prevZ = nowZ;
        }

        // -------- Easy virtual hit --------
        public bool TryVirtualHit(Vector3 paddlePos, Vector3 paddleVel, float minClosingSpeed)
        {
            if (Time.time < _ignorePaddleUntil) return false;

            Vector3 toBall = (transform.position - paddlePos);
            float dist = toBall.magnitude;
            if (dist <= 1e-4f) return false;

            float closing = Vector3.Dot(paddleVel, toBall.normalized);
            if (closing < minClosingSpeed) return false;

            Vector3 incoming = _rb.linearVelocity.sqrMagnitude > 1e-6f
                ? _rb.linearVelocity
                : (-toBall.normalized * 0.01f);

            int hitterSide = SideFromZ(paddlePos.z);

            DoHit(incoming, toBall.normalized, hitterSide, transform.position);

            _lastHitter = hitterSide;
            _awaitingCrossNetFrom = hitterSide;
            _ignorePaddleUntil = Time.time + postVirtualIgnoreSeconds;
            return true;
        }

        private void OnCollisionEnter(Collision c)
        {
            if (c.gameObject.CompareTag("Ground"))
            {
                HandleGroundBounce(c.contacts[0].point, c.contacts[0].normal);
                return;
            }
            else if (c.gameObject.CompareTag("Net"))
            {
                if (resetOnNetHit)
                {
                    int faultSide = (_lastHitter != 0) ? _lastHitter : _servingSide;
                    int winner = -faultSide;
                    if (debugRules) Debug.Log($"[Rally] Net touch — fault by {(faultSide>0?"Right":"Left")}, winner={(winner>0?"Right":"Left")}");
                    ResetByServeOwnership(winner);
                    return;
                }
            }
            else if (c.gameObject.CompareTag("Paddle"))
            {
                HandlePaddleHit(c);
            }
            else if (c.gameObject.CompareTag("Wall"))
            {
                // ===== NEW: First-bounce OUT rule for walls =====
                // If wall is hit before any bounce since last strike -> OUT on striker.
                // If ball has bounced: award based on where that first bounce happened.
                int striker = (_lastHitter != 0) ? _lastHitter : _servingSide;

                if (_groundBounceCount == 0)
                {
                    // Out before any ground contact
                    int winner = -striker;
                    if (debugRules) Debug.Log("[Out] Hit WALL before first bounce -> winner = " + (winner > 0 ? "Right" : "Left"));
                    ResetByServeOwnership(winner);
                }
                else
                {
                    // Already bounced — give to hitter ONLY if first bounce was on opponent side
                    // (i.e., the last recorded ground side equals opponent of striker)
                    if (_lastGroundSide == -striker)
                    {
                        int winner = striker; // defender failed to return after a valid in-bounds bounce
                        if (debugRules) Debug.Log("[Win] After valid in-bounds bounce, ball hit WALL -> winner (hitter) = " + (winner > 0 ? "Right" : "Left"));
                        ResetByServeOwnership(winner);
                    }
                    else
                    {
                        int winner = -striker; // first bounce was not on opponent court
                        if (debugRules) Debug.Log("[Out] WALL after first bounce BUT first bounce not on opponent side -> winner = " + (winner > 0 ? "Right" : "Left"));
                        ResetByServeOwnership(winner);
                    }
                }
            }
        }

        private void HandleGroundBounce(Vector3 contactPoint, Vector3 groundNormal)
        {
            int side = SideFromZ(contactPoint.z);

            if (_awaitingCrossNetFrom != 0)
            {
                if (side == _awaitingCrossNetFrom)
                {
                    int winner = -_awaitingCrossNetFrom; // hitter failed to cross to opponent
                    if (debugRules) Debug.Log("[Fault] Landed back on hitter side before crossing/landing opponent");
                    ResetByServeOwnership(winner);
                    return;
                }
                else
                {
                    _awaitingCrossNetFrom = 0;
                }
            }

            if (_firstGroundAfterServe && IsOnServingSide(contactPoint.z))
            {
                int winner = -_servingSide;
                if (debugRules) Debug.Log("[Serve] Landed on server side first — fault");
                ResetByServeOwnership(winner);
                return;
            }

            if (enforceTwoBounceRule && (_needBounceOnReceiver || _needBounceOnServer))
            {
                if (_needBounceOnReceiver)
                {
                    if (side == _receiverSide)
                    {
                        _needBounceOnReceiver = false;
                        _serveReceiverBounceDone = true;
                        if (debugRules) Debug.Log("[Serve] Receiver first-bounce completed");
                    }
                }
                else if (_needBounceOnServer)
                {
                    if (side == _servingSide)
                    {
                        _needBounceOnServer = false;
                        if (debugRules) Debug.Log("[Serve] Server first-bounce after return — satisfied");
                    }
                    else
                    {
                        int winner = _servingSide;
                        if (debugRules) Debug.Log("[Serve] Return didn’t reach server side — fault on receiver");
                        ResetByServeOwnership(winner);
                        return;
                    }
                }
            }

            if (_lastGroundSide == side) _sameSideBounceCount++;
            else { _lastGroundSide = side; _sameSideBounceCount = 1; }

            if (_sameSideBounceCount >= 2)
            {
                int winner = -side; // double-bounce -> other side wins
                if (debugRules) Debug.Log("[Rally] Double-bounce — fault");
                ResetByServeOwnership(winner);
                return;
            }

            _firstGroundAfterServe = false;
            _servePhase = false;

            Vector3 v = _rb.linearVelocity;
            Vector3 n = groundNormal;
            Vector3 horiz = Vector3.ProjectOnPlane(v, n) * keepHorizontal;

            float vyDown = Mathf.Max(0f, -Vector3.Dot(v, n));
            float minUp = (_groundBounceCount == 0 ? firstBounceMinUp : minBounceUp);
            float vyUp = Mathf.Max(vyDown * restitutionY, minUp);

            _rb.linearVelocity = horiz + n * vyUp;
            _groundBounceCount++;
            ClampVertical();
        }

        private void HandlePaddleHit(Collision c)
        {
            // Drop serve shield on first paddle hit of the rally
            if (gameObject.layer == _ballServeLayer && _ballLayer != -1)
            {
                if (debugRules) Debug.Log("[Serve] Dropping BallServe layer on first paddle hit");
                gameObject.layer = _ballLayer;
                _ignorePaddleUntil = 0f;
            }

            if (Time.time < _ignorePaddleUntil)
            {
                if (debugRules) Debug.Log("[Guard] Ignored paddle due to cooldown");
                return;
            }

            int hitterSide;
            if (IsFromRoot(paddleL, c))      hitterSide = -1;
            else if (IsFromRoot(paddleR, c)) hitterSide = +1;
            else                              hitterSide = SideFromZ(c.transform.position.z);

            if (enforceTwoBounceRule && (_needBounceOnReceiver || _needBounceOnServer))
            {
                // Only enforce during the early serve sequence
                if (_needBounceOnReceiver && hitterSide == _receiverSide)
                {
                    int winner = _servingSide;
                    if (debugRules) Debug.Log("[Fault] Receiver volleyed the serve");
                    ResetByServeOwnership(winner);
                    return;
                }

                if (!_needBounceOnReceiver && !_needBounceOnServer && _serveReceiverBounceDone && hitterSide == _receiverSide)
                {
                    _needBounceOnServer = true;
                    if (debugRules) Debug.Log("[Serve] Receiver returned — now server must bounce before volley");
                }

                if (_needBounceOnServer && hitterSide == _servingSide)
                {
                    int winner = _receiverSide;
                    if (debugRules) Debug.Log("[Fault] Server volleyed before required bounce");
                    ResetByServeOwnership(winner);
                    return;
                }
            }

            Vector3 incoming = _rb.linearVelocity;
            if (incoming.sqrMagnitude < 1e-6f) incoming = -c.contacts[0].normal * 0.001f;

            DoHit(incoming, c.contacts[0].normal, hitterSide, c.GetContact(0).point);

            _lastHitter = hitterSide;
            _sameSideBounceCount = 0;
            _lastGroundSide = 0;

            _awaitingCrossNetFrom = hitterSide;

            _servePhase = false;
            _groundBounceCount = 0;

            _ignorePaddleUntil = Time.time + postRealHitIgnoreSeconds;
        }

        private static bool IsFromRoot(GameObject root, Collision c)
        {
            if (!root) return false;
            var t = c.collider.transform;
            return t == root.transform || t.IsChildOf(root.transform);
        }

        private void DoHit(Vector3 incoming, Vector3 surfaceNormal, int hitterSide, Vector3 hitPoint)
        {
            Vector3 reflected = Vector3.Reflect(incoming, surfaceNormal).normalized;

            int curSide = SideFromZ(transform.position.z);
            int oppSide = -curSide;
            float zTarget = WorldZForSide(oppSide);

            Vector3 aimDir = (new Vector3(0f, 0f, zTarget) - transform.position).normalized;

            Vector3 dir = Vector3.Lerp(reflected, aimDir, aimAssistBlend);
            dir.x = Mathf.Lerp(dir.x, 0f, centerXBias);

            if (dir.y < minHitUp) dir.y = minHitUp;
            float signToOpp = (zTarget - transform.position.z) >= 0 ? +1f : -1f;
            float fwd = Mathf.Abs(dir.z);
            if (fwd < minForwardZ) dir.z = signToOpp * minForwardZ;
            dir.Normalize();

            float speed = Mathf.Max(targetSpeed, minLaunchSpeedMS);
            Vector3 v = dir * speed + Vector3.up * loft;

            float distToNet = Mathf.Abs(transform.position.z - netZ);
            if (distToNet <= nearNetMeters)
                v += dir * nearNetSpeedBoost;

            if (v.magnitude < minLaunchSpeedMS)
                v = dir * minLaunchSpeedMS + Vector3.up * loft;

            _rb.linearVelocity = v;
            ClampVertical();

            _launched = true;
            _rb.useGravity = true;

            if (debugRules) Debug.Log($"[Hit] side={(hitterSide>0?"Right":"Left")} v={_rb.linearVelocity} at {hitPoint}");
        }

        private void ResetByServeOwnership(int winnerSide)
        {
            if (alwaysServeRight && servePointRight)
            {
                if (scoreManager) scoreManager.SetServerSide(+1);
                ResetToServe(servePointRight);
                return;
            }

            int nextServerSide = _servingSide;
            if (scoreManager)
                nextServerSide = scoreManager.OnRallyEnded(winnerSide);

            Transform nextServePoint = (nextServerSide > 0)
                ? (servePointRight ? servePointRight : servePointLeft)
                : (servePointLeft  ? servePointLeft  : servePointRight);

            ResetToServe(nextServePoint);
        }

        private void ResetToServe(Transform servePoint)
        {
            if (!servePoint) return;

            _servingSide = SideFromZ(servePoint.position.z);

            float opponentZ   = WorldZForSide(-_servingSide);
            float dirSignToOpp = (opponentZ - servePoint.position.z) >= 0f ? +1f : -1f;
            Vector3 forwardFlat = new Vector3(0f, 0f, dirSignToOpp);

            transform.position = servePoint.position
                               + servePoint.right * serveOffset.x
                               + Vector3.up * serveOffset.y
                               + forwardFlat * serveOffset.z
                               + Vector3.up * serveLiftY;

            if (_ballServeLayer != -1 && _ballLayer != -1)
            {
                gameObject.layer = _ballServeLayer;
                StartCoroutine(RestoreBallLayerAfter(serveShieldSeconds));
            }

            _rb.useGravity = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.Sleep();

            _launched = false;
            _prevZ = transform.position.z;

            _servePhase = true;
            _firstGroundAfterServe = true;

            _needBounceOnReceiver = true;
            _needBounceOnServer   = false;
            _serveReceiverBounceDone = false;

            _awaitingCrossNetFrom = 0;

            _groundBounceCount = 0;
            _lastHitter = 0;

            _lastGroundSide = 0;
            _sameSideBounceCount = 0;

            if (debugRules) Debug.Log($"[ResetToServe] {( _servingSide>0 ? "Right" : "Left")} side (world z = {servePoint.position.z:0.00})");
        }

        private IEnumerator RestoreBallLayerAfter(float sec)
        {
            yield return new WaitForSeconds(sec);
            if (_ballLayer != -1) gameObject.layer = _ballLayer;
        }

        private void LaunchBall()
        {
            _launched = true;
            _rb.useGravity = true;

            float opponentZ = WorldZForSide(-_servingSide);
            float forwardSign = (opponentZ - transform.position.z) >= 0f ? +1f : -1f;

            Vector3 forward = new Vector3(0f, 0f, forwardSign);
            Vector3 dir = (forward * Mathf.Cos(12f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(12f * Mathf.Deg2Rad)).normalized;

            _rb.AddForce(dir * 44f * _rb.mass, ForceMode.Impulse);
        }

        private void ClampVertical()
        {
            Vector3 v = _rb.linearVelocity;
            v.y = Mathf.Clamp(v.y, -maxDownVel, maxUpVel);
            _rb.linearVelocity = v;
        }

        // External API used by ModeRuntime
        public void SetAlwaysServeRight(bool value) => alwaysServeRight = value;

        public void SetPaddles(GameObject left, GameObject right)
        {
            if (left)  paddleL = left;
            if (right) paddleR = right;
        }
        public void SetPaddleL(GameObject left) { if (left) paddleL = left; }
        public void SetPaddleR(GameObject right){ if (right) paddleR = right; }

        public void ForceResetToServerSide(int side)
        {
            Transform sp = (side > 0) ? servePointRight : servePointLeft;
            if (sp) ResetToServe(sp);
            if (scoreManager) scoreManager.SetServerSide(side);
        }
    }
}
