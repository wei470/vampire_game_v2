using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DPS 追踪器 — 实时统计伤害输出，显示 DPS/总伤/峰值/命中数。
/// 挂在场景中，由 GameSceneBootstrap 在 DPS Test 模式创建。
/// </summary>
public class DpsTracker : MonoBehaviour
{
    public static DpsTracker Instance { get; private set; }

    // ── 统计数据 ──
    private float _totalDamage;
    private float _peakDps;
    private float _peakSingleHit;
    private int _hitCount;
    private float _combatStartTime;
    private bool _tracking;

    // ── 滑动窗口 DPS（1秒） ──
    private float[] _damageWindow = new float[60]; // 每帧记录
    private int _windowIndex;
    private float _windowTotal;

    // ── 显示 ──
    private bool _visible = true;
    private GUIStyle _labelStyle;
    private GUIStyle _valueStyle;
    private GUIStyle _headerStyle;

    private void Awake()
    {
        Instance = this;
        Reset();
    }

    public void Reset()
    {
        _totalDamage = 0;
        _peakDps = 0;
        _peakSingleHit = 0;
        _hitCount = 0;
        _combatStartTime = Time.time;
        _tracking = true;
        _windowIndex = 0;
        _windowTotal = 0;
        for (int i = 0; i < _damageWindow.Length; i++) _damageWindow[i] = 0;
    }

    /// <summary>
    /// 由 Damageable.OnDamaged 事件调用
    /// </summary>
    public void RecordDamage(float damage)
    {
        if (!_tracking || damage <= 0) return;

        _totalDamage += damage;
        _hitCount++;
        if (damage > _peakSingleHit) _peakSingleHit = damage;

        // 滑动窗口
        _windowTotal -= _damageWindow[_windowIndex];
        _windowTotal += damage;
        _damageWindow[_windowIndex] = damage;
        _windowIndex = (_windowIndex + 1) % _damageWindow.Length;

        float currentDps = _windowTotal;
        if (currentDps > _peakDps) _peakDps = currentDps;
    }

    /// <summary>
    /// 木桩重生时调用（可选重置或累计）
    /// </summary>
    public void OnDummyRespawn()
    {
        // 不重置统计，持续累计
    }

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.rKey.wasPressedThisFrame) Reset();
            if (kb.tabKey.wasPressedThisFrame) _visible = !_visible;
        }
    }

    public float CurrentDps => _windowTotal;
    public float TotalDamage => _totalDamage;
    public float PeakDps => _peakDps;
    public float PeakSingleHit => _peakSingleHit;
    public int HitCount => _hitCount;
    public float Elapsed => Time.time - _combatStartTime;

    // ═══ HUD 显示 ═══

    private void OnGUI()
    {
        if (!_visible) return;

        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.3f, 0.3f) }
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        float x = 10;
        float y = 10;
        float w = 320;
        float lineH = 28;

        // 背景
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(x, y, w, lineH * 8 + 10), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 标题
        GUI.Label(new Rect(x + 10, y, w, lineH), "⚔ DPS TEST", _headerStyle);
        y += lineH + 4;

        // DPS（当前）
        DrawRow(x, ref y, w, lineH, "DPS:", $"{CurrentDps:F1}", new Color(1f, 0.9f, 0.3f));

        // DPS（峰值）
        DrawRow(x, ref y, w, lineH, "峰值 DPS:", $"{PeakDps:F1}", new Color(1f, 0.6f, 0.2f));

        // 总伤害
        DrawRow(x, ref y, w, lineH, "总伤害:", FormatDamage(TotalDamage), new Color(1f, 0.3f, 0.3f));

        // 最高单次
        DrawRow(x, ref y, w, lineH, "最高单次:", $"{PeakSingleHit:F1}", new Color(0.9f, 0.5f, 1f));

        // 命中次数
        DrawRow(x, ref y, w, lineH, "命中次数:", $"{HitCount}", Color.white);

        // 平均伤害
        float avg = _hitCount > 0 ? _totalDamage / _hitCount : 0;
        DrawRow(x, ref y, w, lineH, "平均伤害:", $"{avg:F1}", new Color(0.5f, 0.9f, 0.5f));

        // 时间
        DrawRow(x, ref y, w, lineH, "经过时间:", $"{Elapsed:F1}s", new Color(0.7f, 0.7f, 0.7f));

        // 操作提示
        y += 4;
        GUI.Label(new Rect(x + 10, y, w, lineH), "R 重置 | Tab 隐藏/显示", _labelStyle);
    }

    private void DrawRow(float x, ref float y, float w, float h, string label, string value, Color valueColor)
    {
        GUI.Label(new Rect(x + 10, y, 120, h), label, _labelStyle);
        GUI.color = valueColor;
        GUI.Label(new Rect(x + 130, y, 180, h), value, _valueStyle);
        GUI.color = Color.white;
        y += h;
    }

    private static string FormatDamage(float dmg)
    {
        if (dmg >= 1e9) return $"{dmg / 1e9:F2}B";
        if (dmg >= 1e6) return $"{dmg / 1e6:F2}M";
        if (dmg >= 1e3) return $"{dmg / 1e3:F1}K";
        return $"{dmg:F1}";
    }

    // ═══ 注册受伤事件 ═══

    private void OnEnable()
    {
        EventManager.OnDamage += OnDamageEvent;
    }

    private void OnDisable()
    {
        EventManager.OnDamage -= OnDamageEvent;
    }

    private void OnDamageEvent(GameObject target, float damage, Vector3 sourcePos)
    {
        // 只统计对木桩的伤害
        if (target != null && target.GetComponent<TrainingDummy>() != null)
            RecordDamage(damage);
    }
}
