using UnityEngine;
using PB.Scripts;

/// <summary>
/// Easy 模式：隐形大球拍（只存在于球拍上；Ball 不关心模式）
/// 作用：当球接近并且拍在“逼近球”时，向球发起一次虚拟击球请求。
/// 额外：为了有一致的手感，这里在成功虚拟击球时触发一次 BallHit SFX。
/// </summary>
public class PaddleEasyHit : MonoBehaviour
{
    public PickleBallController ball;

    [Tooltip("隐形半径（米）")]
    public float radius = 1.8f;

    [Tooltip("最小逼近速度（m/s）")]
    public float minClosingSpeed = 1.0f;

    private Vector3 _prevPos;

    private void Start()
    {
        _prevPos = transform.position;
    }

    private void FixedUpdate()
    {
        if (!ball) return;

        Vector3 pos = transform.position;
        Vector3 vel = (pos - _prevPos) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
        _prevPos = pos;

        // 在半径内才判定
        if ((ball.transform.position - pos).sqrMagnitude > radius * radius) return;

        // 请求“虚拟击球”；Ball 内部会做冷却/合法性判断
        if (ball.TryVirtualHit(pos, vel, minClosingSpeed))
        {
            // 立即播拍击 SFX（Easy 模式是虚拟碰撞，BallSFXLite听不到碰撞）
            AudioController.Instance?.PlaySFX2D("BallHit");
        }
    }

#if UNITY_EDITOR
    // 可视化半径（编辑器）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
