using UnityEngine;

/// <summary>
/// #23 受伤全屏闪红效果 — 玩家受伤时覆盖整个屏幕的红色闪光，快速消退
/// 复用 DetonateFlashEffect 的设计模式，使用 OnGUI 实现，无需 Canvas
/// </summary>
public class DamageFlashEffect : MonoBehaviour
{
    private float _duration;
    private float _startTime;
    private Color _flashColor;
    private static DamageFlashEffect _activeInstance;

    /// <summary>
    /// 显示全屏闪红效果
    /// </summary>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="color">闪光颜色（默认红色半透明）</param>
    public static void Show(float duration = 0.1f, Color color = default)
    {
        // 如果已有实例在运行，先销毁
        if (_activeInstance != null)
        {
            Destroy(_activeInstance.gameObject);
        }

        var go = new GameObject("DamageFlash");
        go.transform.SetParent(null);
        DontDestroyOnLoad(go);

        var flash = go.AddComponent<DamageFlashEffect>();
        flash._duration = duration;
        flash._startTime = Time.unscaledTime;
        flash._flashColor = color == default ? new Color(1f, 0f, 0f, 0.3f) : color;
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
        // 快速衰减的 alpha：1 → 0，使用平方曲线让闪红更快消退
        float alpha = (1f - t) * (1f - t);

        // 使用传入的颜色，alpha 按衰减曲线变化
        Color drawColor = new Color(_flashColor.r, _flashColor.g, _flashColor.b, _flashColor.a * alpha);

        // 绘制全屏矩形
        GUI.depth = -999; // 略低于 DetonateFlash 的 -1000
        GUI.color = drawColor;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        if (_activeInstance == this)
            _activeInstance = null;
    }
}