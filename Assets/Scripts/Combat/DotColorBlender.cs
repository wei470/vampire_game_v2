using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT颜色混合器 — 挂在敌人身上，统一管理所有DOT效果的颜色叠加。
/// 
/// 像RGB调色盘一样混合：
/// - 中毒(层数越多越绿)
/// - 霜冻(层数越多越蓝)
/// - 燃烧(层数越多越黄橙)
/// - 雷电/静电(闪烁青色)
/// - 流血(红色)
/// - 黑暗(变黑)
/// 
/// 每个DOT效果通过 RegisterDot/UnregisterDot 注册颜色贡献，
/// 系统根据总权重混合最终颜色。
/// </summary>
public class DotColorBlender : MonoBehaviour
{
    /// <summary>
    /// 单个DOT效果的颜色贡献
    /// </summary>
    public struct DotColorContribution
    {
        public string effectName;
        public Color color;        // 该效果的目标颜色
        public float intensity;    // 0~1，基于层数的强度
        public float pulseSpeed;   // 脉冲速度（0=不脉冲）
    }

    private SpriteRenderer _sr;
    private Color _originalColor;
    private bool _colorCaptured;

    // 所有注册的DOT效果
    private Dictionary<string, DotColorContribution> _contributions
        = new Dictionary<string, DotColorContribution>();

    // 颜色更新频率
    private float _lastColorUpdate;
    private const float COLOR_UPDATE_INTERVAL = 0.05f; // 每0.05秒更新颜色

    // 各元素颜色常量
    public static readonly Color POISON_GREEN = new Color(0.1f, 0.8f, 0.1f);
    public static readonly Color FROST_BLUE = new Color(0.3f, 0.5f, 1f);
    public static readonly Color BURN_ORANGE = new Color(1f, 0.5f, 0f);
    public static readonly Color BURN_YELLOW = new Color(1f, 0.8f, 0f);
    public static readonly Color STATIC_CYAN = new Color(0.3f, 0.8f, 1f);
    public static readonly Color BLEED_RED = new Color(0.9f, 0.1f, 0.1f);
    public static readonly Color DARK_PURPLE = new Color(0.15f, 0f, 0.15f);

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            _originalColor = _sr.color;
            _colorCaptured = true;
        }
    }

    private void OnEnable()
    {
        _contributions.Clear();
        // 恢复原始颜色（对象池回收时可能残留DOT颜色）
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null && _colorCaptured)
        {
            _sr.color = _originalColor;
        }
    }

    /// <summary>
    /// 注册/更新一个DOT效果的颜色贡献
    /// </summary>
    /// <param name="effectName">效果唯一名称（如"poison", "frost"）</param>
    /// <param name="color">该效果的颜色</param>
    /// <param name="intensity">强度0~1，基于层数</param>
    /// <param name="pulseSpeed">脉冲速度（0=不脉冲）</param>
    public void RegisterDot(string effectName, Color color, float intensity, float pulseSpeed = 0f)
    {
        _contributions[effectName] = new DotColorContribution
        {
            effectName = effectName,
            color = color,
            intensity = Mathf.Clamp01(intensity),
            pulseSpeed = pulseSpeed
        };
    }

    /// <summary>
    /// 移除一个DOT效果的颜色贡献
    /// </summary>
    public void UnregisterDot(string effectName)
    {
        _contributions.Remove(effectName);
    }

    /// <summary>
    /// 是否有任何DOT效果在活跃
    /// </summary>
    public bool HasAnyDot => _contributions.Count > 0;

    private void LateUpdate()
    {
        if (_sr == null) return;

        // 降频更新颜色
        if (Time.time - _lastColorUpdate < COLOR_UPDATE_INTERVAL) return;
        _lastColorUpdate = Time.time;

        // 捕获原始颜色
        if (!_colorCaptured)
        {
            _originalColor = _sr.color;
            _colorCaptured = true;
        }

        if (_contributions.Count == 0)
        {
            // 没有DOT效果时，逐渐恢复原始颜色
            _sr.color = Color.Lerp(_sr.color, _originalColor, 0.1f);
            return;
        }

        // 混合所有DOT效果的颜色
        Color blended = BlendColors();
        _sr.color = blended;
    }

    /// <summary>
    /// 混合所有注册的DOT颜色贡献
    /// </summary>
    private Color BlendColors()
    {
        if (!_colorCaptured) _originalColor = _sr.color;

        // 计算总权重
        float totalWeight = 0f;
        foreach (var kvp in _contributions)
        {
            totalWeight += kvp.Value.intensity;
        }

        if (totalWeight <= 0f) return _originalColor;

        // 从原始颜色开始，按权重混合
        Color result = _originalColor;
        float blendFactor = Mathf.Clamp01(totalWeight); // 总混合因子

        // 归一化各效果的贡献
        foreach (var kvp in _contributions)
        {
            var contrib = kvp.Value;
            float normalizedWeight = contrib.intensity / totalWeight;
            
            // 每个效果的颜色贡献强度（基于层数）
            float effectBlend = Mathf.Lerp(0.2f, 0.85f, contrib.intensity);
            
            // 脉冲效果
            if (contrib.pulseSpeed > 0f)
            {
                float pulse = (Mathf.Sin(Time.time * contrib.pulseSpeed) + 1f) * 0.5f;
                effectBlend *= Mathf.Lerp(0.7f, 1f, pulse);
            }

            // 混合：原始颜色 与 效果颜色 之间，按effectBlend插值
            Color effectResult = Color.Lerp(_originalColor, contrib.color, effectBlend);
            
            // 按归一化权重累加到最终结果
            result = Color.Lerp(result, effectResult, normalizedWeight);
        }

        // 总体混合：原始颜色和DOT颜色之间，按总强度混合
        // 低总强度时保持更多原始颜色，高强度时更多DOT颜色
        float overallBlend = Mathf.Lerp(0.3f, 0.9f, Mathf.Clamp01(totalWeight / 3f));
        result = Color.Lerp(_originalColor, result, overallBlend);

        return result;
    }

    /// <summary>
    /// 重置颜色（对象池回收时调用）
    /// </summary>
    public void ResetColor()
    {
        _contributions.Clear();
        if (_sr != null && _colorCaptured)
        {
            _sr.color = _originalColor;
        }
    }

    private void OnDisable()
    {
        _contributions.Clear();
        // 不在OnDisable恢复颜色（可能被其他系统使用）
    }

    /// <summary>
    /// 获取原始颜色（用于其他DOT效果恢复）
    /// </summary>
    public Color OriginalColor => _colorCaptured ? _originalColor : (_sr != null ? _sr.color : Color.white);
}