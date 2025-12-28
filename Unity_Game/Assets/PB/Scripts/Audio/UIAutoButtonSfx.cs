using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public class UIAutoButtonSfx : MonoBehaviour
{
    public string clickKey = "UIClick";
    public bool includeToggles = true, includeDropdowns = true, includeSliders = true;

    void OnEnable(){ Rescan(); }
    [ContextMenu("Rescan Now")] public void Rescan()
    {
        if (!AudioController.Instance) return;

        foreach (var b in GetComponentsInChildren<Button>(true))
            Ensure(b.gameObject, () => b.onClick.AddListener(()=> AudioController.Instance.PlaySFX2D(clickKey,1f)));

        if (includeToggles)
            foreach (var t in GetComponentsInChildren<Toggle>(true))
                Ensure(t.gameObject, () => t.onValueChanged.AddListener(_=> AudioController.Instance.PlaySFX2D(clickKey,0.9f)));

        if (includeDropdowns)
            foreach (var d in GetComponentsInChildren<Dropdown>(true))
                Ensure(d.gameObject, () => d.onValueChanged.AddListener(_=> AudioController.Instance.PlaySFX2D(clickKey,0.9f)));

        if (includeSliders)
            foreach (var s in GetComponentsInChildren<Slider>(true))
                Ensure(s.gameObject, () => s.onValueChanged.AddListener(_=> AudioController.Instance.PlaySFX2D(clickKey,0.6f)));
    }

    // Mark each control once so we don't double-subscribe
    void Ensure(GameObject go, System.Action add)
    {
        if (go.GetComponent<_UISfxTag>() == null) go.AddComponent<_UISfxTag>();
        if (!go.GetComponent<_UISfxTag>().added) { add(); go.GetComponent<_UISfxTag>().added = true; }
    }
}
class _UISfxTag : MonoBehaviour { public bool added; }
