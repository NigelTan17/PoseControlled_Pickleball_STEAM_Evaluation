using UnityEngine;

namespace PB.Scripts
{
    public class DeflectorWall : MonoBehaviour
    {
        [Header("Return Target 回传目标(一般填 Qin R 根节点)")]
        public Transform serverTarget;

        [Header("Arc Controls 弧线参数(简单)")]
        public float returnVyMS = 8.5f;       // 上抛速度
        public float minReturnHorizMS = 7.5f; // 水平速度下限
        public float leadSeconds = 0.12f;     // 轻微预判(可为0)

        [Header("Stability 稳定项")]
        public float pushOut = 0.02f;
        public float cooldown = 0.05f;

        float _gy = 9.81f;
        float _nextAllowed = 0f;

        void OnCollisionEnter(Collision c)
        {
            if (Time.time < _nextAllowed) return;
            if (serverTarget == null) return;

            var rb = c.rigidbody;
            if (rb == null) return;

            // 平均法线 + 抗粘连
            Vector3 n = Vector3.zero;
            foreach (var ct in c.contacts) n += ct.normal;
            n.Normalize();
            rb.position += n * pushOut;

            // 目标位置（可选预判）
            Vector3 ballPos = rb.position;
            Vector3 targetPos = serverTarget.position;
            var playerRb = serverTarget.GetComponent<Rigidbody>();
            if (playerRb != null) targetPos += playerRb.linearVelocity * leadSeconds;

            Vector3 toTarget = targetPos - ballPos;
            Vector3 toXZ = new Vector3(toTarget.x, 0f, toTarget.z);
            Vector3 dirXZ = toXZ.sqrMagnitude > 1e-6f ? toXZ.normalized : Vector3.forward;

            // 固定上抛 vy → 航程时间 T
            float vy = Mathf.Max(0.01f, returnVyMS);
            float T = 2f * vy / _gy;

            // 需要的水平速度 = 距离/时间，并保证下限
            float distXZ = toXZ.magnitude;
            float horiz = Mathf.Max(minReturnHorizMS, distXZ / Mathf.Max(0.05f, T));

            rb.linearVelocity = dirXZ * horiz + Vector3.up * vy;

            _nextAllowed = Time.time + cooldown;
        }
    }
}
