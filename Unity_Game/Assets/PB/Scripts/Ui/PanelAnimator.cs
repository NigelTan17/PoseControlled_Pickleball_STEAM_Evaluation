using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.18f;
    [SerializeField, Range(0.7f, 1f)] private float popScaleFrom = 0.92f;
    [SerializeField] private bool startHidden = true;

    private CanvasGroup _cg;
    private Vector3 _baseScale = Vector3.one;
    private Coroutine _running;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

        _baseScale = (transform.localScale == Vector3.zero) ? Vector3.one : transform.localScale;

        if (startHidden) HideImmediate(); else ShowImmediate();
    }

    // ---------- Public API ----------
    public void Open()
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(CoOpen());
    }

    public void Close()
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(CoClose());
    }

    public void ShowImmediate()
    {
        if (_running != null) StopCoroutine(_running);
        _cg.alpha = 1f;
        _cg.interactable = true;
        _cg.blocksRaycasts = true;
        transform.localScale = _baseScale;
        // NOTE: we do NOT SetActive(true/false) here; object stays active always.
    }

    public void HideImmediate()
    {
        if (_running != null) StopCoroutine(_running);
        _cg.alpha = 0f;
        _cg.interactable = false;
        _cg.blocksRaycasts = false;
        transform.localScale = _baseScale * popScaleFrom;
        // NOTE: we do NOT deactivate the GameObject; that was causing panels to vanish.
    }

    // ---------- Coroutines ----------
    private IEnumerator CoOpen()
    {
        _cg.interactable = false;
        _cg.blocksRaycasts = false;
        _cg.alpha = 0f;
        transform.localScale = _baseScale * popScaleFrom;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            float e = 1f - Mathf.Pow(1f - k, 3f);   // ease-out cubic
            _cg.alpha = e;
            transform.localScale = Vector3.Lerp(_baseScale * popScaleFrom, _baseScale, e);
            yield return null;
        }

        ShowImmediate();
        _running = null;
    }

    private IEnumerator CoClose()
    {
        _cg.interactable = false;
        _cg.blocksRaycasts = false;

        float t = 0f;
        float a0 = _cg.alpha;
        Vector3 s0 = transform.localScale;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            _cg.alpha = Mathf.Lerp(a0, 0f, e);
            transform.localScale = Vector3.Lerp(s0, _baseScale * popScaleFrom, e);
            yield return null;
        }

        HideImmediate();
        _running = null;
    }
}
