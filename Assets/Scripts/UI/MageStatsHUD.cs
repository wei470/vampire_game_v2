#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 角色专属 HUD 统计面板。
///
/// 功能：
/// 1. 非暂停时：左下角简化版（当前总 DPS + 引爆 CD）
/// 2. 暂停时：展开完整统计面板（已解锁 DOT 子弹、各 DOT DPS、暴击率、引爆参数、里程碑进度等）
///
/// 由 GameSceneBootstrap 自动创建，仅 Mage 角色显示。
/// </summary>
public class MageStatsHUD : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // 配置
    // ════════════════════════════════════════════════════════════════

    [Header("面板外观")]
    [SerializeField] private float _panelWidth = 320f;
    [SerializeField] private float _panelPadding = 16f;
    [SerializeField] private float _lineHeight = 22f;
    [SerializeField] private float _headerHeight = 28f;

    [Header("位置")]
    [SerializeField] private float _marginBottom = 200f;   // 距底部（避让 SkillHUD）
    [SerializeField] private float _marginLeft = 24f;

    // ════════════════════════════════════════════════════════════════
    // 缓存引用
    // ════════════════════════════════════════════════════════════════

    private MagePassive _magePassive;
    private SpawnManager _spawnManager;
    private bool _isMage;

    // 纹理缓存
    private Texture2D _panelBgTex;
    private Texture2D _headerBgTex;
    private Texture2D _dividerTex;
    private Texture2D _progressBgTex;
    private Texture2D _progressFillTex;
    private Texture2D _milestoneBgTex;
    private Texture2D _milestoneFillTex;

    // DOT 子弹颜色纹理
    private Texture2D _bleedTex;
    private Texture2D _poisonTex;
    private Texture2D _burnTex;
    private Texture2D _frostTex;

    // ════════════════════════════════════════════════════════════════
    // 颜色方案（紫色系 Mage 主题）
    // ════════════════════════════════════════════════════════════════

    private static readonly Color PanelBg = new Color(0.08f, 0.05f, 0.15f, 0.92f);
    private static readonly Color HeaderBg = new Color(0.25f, 0.1f, 0.4f, 0.95f);
    private static readonly Color DividerColor = new Color(0.4f, 0.2f, 0.6f, 0.6f);
    private static readonly Color TextPrimary = new Color(0.95f, 0.9f, 1f);
    private static readonly Color TextSecondary = new Color(0.7f, 0.6f, 0.8f);
    private static readonly Color AccentPurple = new Color(0.8f, 0.3f, 1f);
    private static readonly Color AccentGold = new Color(1f, 0.85f, 0f);
    private static readonly Color ProgressBg = new Color(0.15f, 0.08f, 0.25f);
    private static readonly Color ProgressFill = new Color(0.5f, 0.2f, 0.8f);
    private static readonly Color MilestoneComplete = new Color(1f, 0.85f, 0f);
    private static readonly Color MilestoneIncomplete = new Color(0.3f, 0.2f, 0.4f);

    // DOT 颜色
    private static readonly Color BleedColor = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color PoisonColor = new Color(0.2f, 0.85f, 0.3f);
    private static readonly Color BurnColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color FrostColor = new Color(0.4f, 0.75f, 1f);

    // ════════════════════════════════════════════════════════════════
    // 样式缓存（避免每帧 new）
    // ════════════════════════════════════════════════════════════════

    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _valueStyle;
    private GUIStyle _smallStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _milestoneLabelStyle;
    private bool _stylesInitialized;

    // DPS 计算缓存
    private float _cachedTotalDps;
    private float _lastDpsCalcTime;
    private const float DPS_CALC_INTERVAL = 0.5f; // 每 0.5 秒刷新一次 DPS

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    private void Start()
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            _magePassive = player.GetComponent<MagePassive>();
            _isMage = _magePassive != null;
        }
        _spawnManager = GameReferences.SpawnManager;

        // 创建纹理
        _panelBgTex = MakeTex(PanelBg);
        _headerBgTex = MakeTex(HeaderBg);
        _dividerTex = MakeTex(DividerColor);
        _progressBgTex = MakeTex(ProgressBg);
        _progressFillTex = MakeTex(ProgressFill);
        _milestoneBgTex = MakeTex(MilestoneIncomplete);
        _milestoneFillTex = MakeTex(MilestoneComplete);

        _bleedTex = MakeTex(BleedColor);
        _poisonTex = MakeTex(PoisonColor);
        _burnTex = MakeTex(BurnColor);
        _frostTex = MakeTex(FrostColor);
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = AccentPurple }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = TextSecondary }
        };

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = TextPrimary }
        };

        _smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = TextSecondary }
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = AccentGold }
        };

        _milestoneLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = AccentGold }
        };
    }

    private void OnGUI()
    {
        if (!_isMage || _magePassive == null) return;

        InitStyles();
        GUIScaleHelper.BeginScale();

        bool isPaused = GameManager.Instance != null &&
                        GameManager.Instance.CurrentState == GameManager.GameState.Paused;

        if (isPaused)
            DrawFullPanel();
        else
            DrawCompactBar();

        GUIScaleHelper.EndScale();
    }

    // ════════════════════════════════════════════════════════════════
    // 简化版（非暂停时）
    // ════════════════════════════════════════════════════════════════

    private void DrawCompactBar()
    {
        float x = _marginLeft;
        float y = Screen.height - _marginBottom;
        float w = 280f;
        float h = 42f;

        // 背景
        GUI.color = PanelBg;
        GUI.DrawTexture(new Rect(x, y, w, h), _panelBgTex);

        // 标题
        GUI.color = AccentPurple;
        GUI.Label(new Rect(x + 8, y + 2, 80, 18), "⚔ MAGE", _smallStyle);

        // DPS
        float totalDps = CalculateCurrentDPS();
        GUI.color = TextPrimary;
        GUI.Label(new Rect(x + 80, y + 2, 100, 18), $"DPS: {totalDps:F1}", _smallStyle);

        // 引爆 CD
        float cdRemain = _magePassive.DetonateCooldownRemaining;
        bool ready = _magePassive.DetonateReady;
        if (ready)
        {
            GUI.color = AccentGold;
            GUI.Label(new Rect(x + 180, y + 2, 90, 18), "[E] READY", _smallStyle);
        }
        else
        {
            float cdPct = 1f - Mathf.Clamp01(cdRemain / _magePassive.DetonateCooldown);
            GUI.color = AccentPurple;
            GUI.Label(new Rect(x + 180, y + 2, 90, 18), $"[E] {cdRemain:F1}s", _smallStyle);
        }

        // DOT 子弹图标条
        DrawDotGunIcons(x + 8, y + 20, w - 16);

        // 边框
        GUI.color = DividerColor;
        DrawBorder(x, y, w, h);

        GUI.color = Color.white;
    }

    /// <summary>
    /// 简化版底部的 DOT 子弹小图标条
    /// </summary>
    private void DrawDotGunIcons(float x, float y, float maxW)
    {
        var guns = _magePassive.DotGuns;
        if (guns == null || guns.Count == 0) return;

        float iconSize = 14f;
        float curX = x;

        for (int i = 0; i < guns.Count; i++)
        {
            var gun = guns[i];
            if (curX + iconSize + 50 > x + maxW) break; // 防止溢出

            // DOT 类型色块
            Texture2D tex = GetDotTexture(gun.effectType);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(curX, y, iconSize, iconSize), tex);

            // 等级标签
            GUI.color = TextSecondary;
            GUI.Label(new Rect(curX + iconSize + 2, y - 1, 30, iconSize), $"Lv{gun.upgradeLevel}", _smallStyle);

            curX += iconSize + 38;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 完整面板（暂停时）
    // ════════════════════════════════════════════════════════════════

    private void DrawFullPanel()
    {
        float x = _marginLeft;
        float baseY = Screen.height - _marginBottom;

        // 计算面板高度（动态）
        var guns = _magePassive.DotGuns;
        int dotGunCount = guns != null ? guns.Count : 0;
        int lineCount = 6 + dotGunCount + 5; // 标题 + DOT列表 + 基础属性 + 增强 + 里程碑
        float panelH = _panelPadding * 2 + _headerHeight + lineCount * _lineHeight + 20;
        float panelW = _panelWidth;

        // 向上偏移确保不被底部 UI 遮挡
        float y = baseY - panelH;

        // 面板背景
        GUI.color = PanelBg;
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), _panelBgTex);

        float cy = y + _panelPadding;

        // ── 标题栏 ──
        GUI.color = HeaderBg;
        GUI.DrawTexture(new Rect(x, y, panelW, _headerHeight + _panelPadding), _headerBgTex);
        GUI.color = AccentPurple;
        GUI.Label(new Rect(x, cy, panelW, _headerHeight), "⚔ MAGE STATS ⚔", _headerStyle);
        cy += _headerHeight + 6;

        // ── 已解锁 DOT 子弹列表 ──
        cy = DrawSectionHeader(x, cy, panelW, "DOT GUNS");
        if (guns != null)
        {
            for (int i = 0; i < guns.Count; i++)
            {
                cy = DrawDotGunRow(x + 8, cy, panelW - 16, guns[i]);
            }
        }
        if (guns == null || guns.Count == 0)
        {
            GUI.color = TextSecondary;
            GUI.Label(new Rect(x + 12, cy, panelW, _lineHeight), "  (none)", _smallStyle);
            cy += _lineHeight;
        }

        // 分隔线
        cy = DrawDivider(x, cy, panelW);

        // ── 总览统计 ──
        cy = DrawSectionHeader(x, cy, panelW, "OVERVIEW");
        float totalDps = CalculateCurrentDPS();
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Total DPS", $"{totalDps:F1}", AccentGold);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Crit Chance", $"{_magePassive.GetDotCritChance() * 100:F1}%",
            Color.Lerp(TextPrimary, Color.red, 0.3f));
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Crit Multiplier", $"{_magePassive.GetDotCritMultiplier():F1}x", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "DOT Duration +", $"{(_magePassive.GetDotDurationMultiplier() - 1f) * 100:F0}%", TextPrimary);

        // 分隔线
        cy = DrawDivider(x, cy, panelW);

        // ── 引爆参数 ──
        cy = DrawSectionHeader(x, cy, panelW, "DETONATE [E]");
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Damage", $"x{_magePassive.DetonateMultiplier:F1}", AccentPurple);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Cooldown", $"{_magePassive.DetonateCooldown:F1}s", TextPrimary);
        float cdRemain = _magePassive.DetonateCooldownRemaining;
        bool ready = _magePassive.DetonateReady;
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Status",
            ready ? "READY" : $"{cdRemain:F1}s",
            ready ? AccentGold : AccentPurple);

        // 分隔线
        cy = DrawDivider(x, cy, panelW);

        // ── DOT 增强属性 ──
        cy = DrawSectionHeader(x, cy, panelW, "DOT ENHANCEMENTS");
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Frequency +", $"{_magePassive.DotFrequencyBonus * 100:F0}%", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Corrosion", $"{_magePassive.CorrosionArmorReduction * 100:F0}%/stack", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Curse Spread", $"{_magePassive.CurseSpreadTargets} targets", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Wither Burst", $"{_magePassive.DotCritBurstChance * 100:F0}%", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Erosion", $"every {_magePassive.ErosionTriggerCount} ticks ({_magePassive.ErosionDamagePercent * 100:F0}%)", TextPrimary);

        // ── 子弹增强属性 ──
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Attack Speed +", $"{_magePassive.AttackSpeedBonus * 100:F0}%", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Bullet Speed +", $"{_magePassive.BulletSpeedBonus * 100:F0}%", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Bullet Count", $"{1 + _magePassive.BulletCountBonus}", TextPrimary);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Ricochet", $"{_magePassive.RicochetChance * 100:F0}%", TextPrimary);

        // 分隔线
        cy = DrawDivider(x, cy, panelW);

        // ── 里程碑进度 ──
        cy = DrawSectionHeader(x, cy, panelW, "MILESTONES");
        cy = DrawMilestone(x + 8, cy, panelW - 16,
            "★ Element Master",
            "All 4 DOT types unlocked → +20% DOT damage",
            guns != null ? guns.Count : 0, 4,
            _magePassive.GetDotDamageMultiplier() > 1f);

        cy = DrawMilestone(x + 8, cy, panelW - 16,
            "★ Chain Detonate",
            "Detonate hits 10+ enemies → x2 DOT for 3s",
            0, 1,
            _magePassive.IsChainDetonateActive);

        // 链式引爆剩余时间
        if (_magePassive.IsChainDetonateActive)
        {
            GUI.color = AccentGold;
            GUI.Label(new Rect(x + 20, cy, panelW, _lineHeight),
                $"  Active: x2 DOT damage", _smallStyle);
            cy += _lineHeight;
        }

        // ── 边框 ──
        GUI.color = DividerColor;
        DrawBorder(x, y, panelW, panelH);

        GUI.color = Color.white;
    }

    // ════════════════════════════════════════════════════════════════
    // 绘制辅助方法
    // ════════════════════════════════════════════════════════════════

    private float DrawSectionHeader(float x, float y, float w, string title)
    {
        GUI.color = AccentPurple;
        GUI.Label(new Rect(x + 4, y, w, _headerHeight - 4), title, _titleStyle);
        return y + _headerHeight;
    }

    private float DrawDotGunRow(float x, float y, float w, MagePassive.DotGunState gun)
    {
        float iconSize = 16f;

        // DOT 类型色块图标
        Texture2D tex = GetDotTexture(gun.effectType);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x, y + 2, iconSize, iconSize), tex);

        // DOT 类型名称
        string typeName = GetDotTypeName(gun.effectType);
        GUI.color = GetDotColor(gun.effectType);
        GUI.Label(new Rect(x + iconSize + 4, y, 100, _lineHeight), typeName, _labelStyle);

        // DPS 和等级
        GUI.color = TextPrimary;
        GUI.Label(new Rect(x + 110, y, 60, _lineHeight), $"DPS:{gun.dotDps:F1}", _valueStyle);

        GUI.color = AccentGold;
        GUI.Label(new Rect(x + 175, y, 50, _lineHeight), $"Lv{gun.upgradeLevel}", _valueStyle);

        // 冷却
        GUI.color = TextSecondary;
        float effectiveCd = gun.cooldown * _magePassive.GetAttackSpeedMultiplier();
        GUI.Label(new Rect(x + 225, y, 60, _lineHeight), $"CD:{effectiveCd:F2}s", _valueStyle);

        return y + _lineHeight;
    }

    private float DrawStatRow(float x, float y, float w, string label, string value, Color valueColor)
    {
        GUI.color = TextSecondary;
        GUI.Label(new Rect(x, y, w * 0.55f, _lineHeight), label, _labelStyle);

        GUI.color = valueColor;
        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, _lineHeight), value, _valueStyle);

        return y + _lineHeight;
    }

    private float DrawDivider(float x, float y, float w)
    {
        GUI.color = DividerColor;
        GUI.DrawTexture(new Rect(x + 8, y + _lineHeight / 2 - 1, w - 16, 1), _dividerTex);
        return y + _lineHeight;
    }

    private float DrawMilestone(float x, float y, float w, string title, string desc, int current, int target, bool completed)
    {
        // 图标（完成/未完成）
        GUI.color = completed ? MilestoneComplete : MilestoneIncomplete;
        GUI.DrawTexture(new Rect(x, y + 2, 12, 12), completed ? _milestoneFillTex : _milestoneBgTex);

        // 标题
        GUI.color = completed ? MilestoneComplete : TextPrimary;
        GUI.Label(new Rect(x + 16, y, w, _lineHeight), title, _milestoneLabelStyle);

        // 进度条
        float barX = x + 16;
        float barY = y + _lineHeight - 2;
        float barW = w - 32;
        float barH = 8f;

        GUI.color = ProgressBg;
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), _progressBgTex);

        float pct = Mathf.Clamp01((float)current / Mathf.Max(1, target));
        GUI.color = completed ? MilestoneComplete : ProgressFill;
        GUI.DrawTexture(new Rect(barX, barY, barW * pct, barH), completed ? _milestoneFillTex : _progressFillTex);

        // 描述
        GUI.color = TextSecondary;
        GUI.Label(new Rect(x + 16, y + _lineHeight + 4, w, _lineHeight), desc, _smallStyle);

        return y + _lineHeight * 2 + 6;
    }

    private void DrawBorder(float x, float y, float w, float h)
    {
        GUI.DrawTexture(new Rect(x, y, w, 1), _dividerTex);
        GUI.DrawTexture(new Rect(x, y + h - 1, w, 1), _dividerTex);
        GUI.DrawTexture(new Rect(x, y, 1, h), _dividerTex);
        GUI.DrawTexture(new Rect(x + w - 1, y, 1, h), _dividerTex);
    }

    // ════════════════════════════════════════════════════════════════
    // DPS 计算
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 计算当前所有 DOT 子弹的理论总 DPS（每秒伤害）
    /// </summary>
    private float CalculateCurrentDPS()
    {
        // 缓存 DPS 计算，避免每帧开销
        if (Time.time - _lastDpsCalcTime < DPS_CALC_INTERVAL)
            return _cachedTotalDps;

        _lastDpsCalcTime = Time.time;
        _cachedTotalDps = 0f;

        var guns = _magePassive?.DotGuns;
        if (guns == null) return 0f;

        float dmgMult = _magePassive.GetDotDamageMultiplier();
        float durationMult = _magePassive.GetDotDurationMultiplier();
        float attackSpeedMult = _magePassive.GetAttackSpeedMultiplier();
        float critChance = _magePassive.GetDotCritChance();
        float critMult = _magePassive.GetDotCritMultiplier();

        for (int i = 0; i < guns.Count; i++)
        {
            var gun = guns[i];
            // 有效 DPS = 基础 DPS × 伤害倍率 × 持续时间倍率 × 暴击期望
            float critExpected = 1f + critChance * (critMult - 1f);
            float effectiveDps = gun.dotDps * dmgMult * critExpected;

            // 考虑射击频率：每次命中施加 DOT，假设持续覆盖
            // DPS 反映的是 DOT 效果本身的 DPS，不需要乘以射击频率
            _cachedTotalDps += effectiveDps;
        }

        return _cachedTotalDps;
    }

    // ════════════════════════════════════════════════════════════════
    // 辅助方法
    // ════════════════════════════════════════════════════════════════

    private Texture2D GetDotTexture(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return _bleedTex;
            case StatusEffectType.Poison: return _poisonTex;
            case StatusEffectType.Burn: return _burnTex;
            case StatusEffectType.Frostbite: return _frostTex;
            default: return _poisonTex;
        }
    }

    private Color GetDotColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return BleedColor;
            case StatusEffectType.Poison: return PoisonColor;
            case StatusEffectType.Burn: return BurnColor;
            case StatusEffectType.Frostbite: return FrostColor;
            default: return TextPrimary;
        }
    }

    private string GetDotTypeName(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return "🔴 Bleed";
            case StatusEffectType.Poison: return "🟢 Poison";
            case StatusEffectType.Burn: return "🟠 Burn";
            case StatusEffectType.Frostbite: return "🔵 Frost";
            default: return type.ToString();
        }
    }

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return tex;
    }

    /// <summary>
    /// 重置状态（场景重置时调用）
    /// </summary>
    public void ResetState()
    {
        _cachedTotalDps = 0f;
        _lastDpsCalcTime = 0f;
    }
}