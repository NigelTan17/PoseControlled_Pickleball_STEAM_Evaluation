using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public enum Mode { Easy, Medium, Hard }
    public Mode startMode = Mode.Easy;

    [Tooltip("把两只球拍的 PaddleEasyHit 拖进来（或用 FindObjectsOfType 自动找）")]
    public PaddleEasyHit[] easyHitComponents;

    private void Start()
    {
        ApplyMode(startMode);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) ApplyMode(Mode.Easy);
        if (Input.GetKeyDown(KeyCode.F2)) ApplyMode(Mode.Medium);
        if (Input.GetKeyDown(KeyCode.F3)) ApplyMode(Mode.Hard);
    }

    public void ApplyMode(Mode mode)
    {
        bool easyOn = (mode == Mode.Easy);
        foreach (var comp in easyHitComponents)
            if (comp) comp.enabled = easyOn;

        Debug.Log($"Mode set to: {mode}");
    }
}
