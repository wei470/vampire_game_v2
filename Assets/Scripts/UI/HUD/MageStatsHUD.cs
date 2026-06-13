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
/// 绘制逻辑委托给 MageStatsHUDRenderer（静态类）
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

    private ICharacterPassive _passive;
    private MagePassive _magePassive;
    private bool _isMage;

    // 纹理缓存
    private Texture2D _panelBgTex;
    private Texture2D _headerBgTex;
    private Texture2D _dividerTex;
    private Texture2D _progressBgTex;
    private Texture2D _progressFillTex;
    private Texture2D _milestoneBgTex;
    private Texture2D _milestoneFillTex;

    // DOT 子弹颜色纹理（数组：[0]=Bleed, [1]=Poison, [2]=Burn, [3]=Frost）
    private Texture2D[] _dotTextures;

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

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    private void Start()
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            _passive = player.GetComponent<ICharacterPassive>();
            _magePassive = _passive as MagePassive;
            _isMage = _magePassive != null;
        }

        // 创建纹理
        _panelBgTex = MageStatsDataCollector.MakeTex(MageStatsHUDRenderer.PanelBg);
        _headerBgTex = MageStatsDataCollector.MakeTex(MageStatsHUDRenderer.HeaderBg);
        _dividerTex = MageStatsDataCollector.MakeTex(MageStatsHUDRenderer.DividerColor);
        _progressBgTex = MageStatsDataCollector.MakeTex(MageStatsHUDRenderer.ProgressBg);
        _progressFillTex = MageStatsDataCollector.MakeTex(MageStatsHUDRenderer.ProgressFill);
        _milestoneBgTex = MageStatsDataCollector.MakeTex(MageStatsHUDRenderer.MilestoneIncomplete);
        _milestoneFillTex = MageStatsDataCollector.MakeTex(MageStatsHUDRenderer.MilestoneComplete);

        _dotTextures = new Texture2D[]
        {
            MageStatsDataCollector.MakeTex(MageStatsDataCollector.BleedColor),
            MageStatsDataCollector.MakeTex(MageStatsDataCollector.PoisonColor),
            MageStatsDataCollector.MakeTex(MageStatsDataCollector.BurnColor),
            MageStatsDataCollector.MakeTex(MageStatsDataCollector.FrostColor)
        };
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
            normal = { textColor = MageStatsHUDRenderer.AccentPurple }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = MageStatsHUDRenderer.TextSecondary }
        };

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = MageStatsHUDRenderer.TextPrimary }
        };

        _smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = MageStatsHUDRenderer.TextSecondary }
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = MageStatsHUDRenderer.AccentGold }
        };

        _milestoneLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = MageStatsHUDRenderer.AccentGold }
        };
    }

    private void OnGUI()
    {
        if (!_isMage || _passive == null) return;

        InitStyles();
        GUIScaleHelper.BeginScale();

        bool isPaused = GameManager.Instance != null &&
                        GameManager.Instance.CurrentState == GameManager.GameState.Paused;

        if (isPaused)
        {
            MageStatsHUDRenderer.DrawFullPanel(_passive, _marginLeft, _marginBottom,
                _panelWidth, _panelPadding, _lineHeight, _headerHeight,
                _panelBgTex, _headerBgTex, _dividerTex,
                _progressBgTex, _progressFillTex,
                _milestoneBgTex, _milestoneFillTex,
                _dotTextures,
                _headerStyle, _labelStyle, _valueStyle,
                _smallStyle, _titleStyle, _milestoneLabelStyle);
        }
        else
        {
            MageStatsHUDRenderer.DrawCompactBar(_passive, _marginLeft, _marginBottom,
                _panelBgTex, _dividerTex, _dotTextures, _smallStyle);
        }

        GUIScaleHelper.EndScale();
    }

    /// <summary>
    /// 重置状态（场景重置时调用）
    /// </summary>
    public void ResetState()
    {
        MageStatsDataCollector.ResetCache();
    }
}