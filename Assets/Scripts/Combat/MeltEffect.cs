using UnityEngine;

/// <summary>
/// 灼烧效果 — 元素反应"融化"（霜冻 × 燃烧）产生的 Debuff。
///
/// 效果：
/// - 持续1秒，期间所有 DOT 伤害翻倍
/// - 显示"融化！"文字
/// - 敌人变橙红色
/// </summary>
public class MeltEffect : MonoBehaviour
{
    private float _endTime;
    private float _damageMultiplier = 2f;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private bool _visualApplied;

    public bool IsActive => Time.time < _endTime;
    public float DamageMultiplier => IsActive ? _damageMultiplier : 1f;

    public void Activate(float duration, float multiplier)
    {
        _damageMultiplier = multiplier;
        _endTime = Time.time + duration;
        ApplyVisual();
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _endTime = 0f;
        _visualApplied = false;
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    private void ApplyVisual()
    {
        if (_visualApplied) return;
        if (_sr != null)
        {
            _originalColor = _sr.color;
            _sr.color = new Color(1f, 0.3f, 0f, _sr.color.a);
            _visualApplied = true;
        }
    }

    private void Update()
    {
        if (_visualApplied && Time.time >= _endTime)
        {
            RestoreVisual();
        }
    }

    private void RestoreVisual()
    {
        if (_visualApplied && _sr != null)
        {
            _sr.color = _originalColor;
            _visualApplied = false;
        }
    }

    private void OnDisable()
    {
        RestoreVisual();
    }
}
