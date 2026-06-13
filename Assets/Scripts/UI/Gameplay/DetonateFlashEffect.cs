using UnityEngine;

/// <summary>
/// #17 引爆全屏闪白效果 — 引爆瞬间覆盖整个屏幕的白色闪光，快速消退
/// 使用 OnGUI 实现，无需 Canvas
/// </summary>
public class DetonateFlashEffect : MonoBehaviour
{
    private float _duration;
    private float _startTime;
    private static DetonateFlashEffect _activeInstance;

    /// <summary>
    /// 显示全屏闪白效果
    /// </summary>
    /// <param name="duration">持续时间（秒）</param>
    public static void Show(float duration = 0.15f)
    {
        // 如果已有实例在运行，先销毁
        if (_activeInstance != null)
        {
            Destroy(_activeInstance.gameObject);
        }

        var go = new GameObject("DetonateFlash");
        go.transform.SetParent(null);
        DontDestroyOnLoad(go);

        var flash = go.AddComponent<DetonateFlashEffect>();
        flash._duration = duration;
        flash._startTime = Time.unscaledTime; // 使用 unscaledTime 以便暂停时也能显示
        _activeInstance = flash;
    }

    private void OnGUI()
    {
        float elapsed = Time.unscaledTime - _startTime;
        if (elapsed > _duration)
        {
            _activeInstance = null;
            Destroy(gameObject);
            return;
        }

        float t = elapsed / _duration;
        // 快速衰减的 alpha：1 → 0，使用平方曲线让闪白更快消退
        float alpha = (1f - t) * (1f - t);

        // 白色闪屏，中心更亮
        Color flashColor = new Color(1f, 1f, 1f, alpha * 0.7f);

        // 绘制全屏矩形
        GUI.depth = -1000; // 最顶层
        GUI.color = flashColor;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        if (_activeInstance == this)
            _activeInstance = null;
    }
}