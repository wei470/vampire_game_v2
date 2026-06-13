using UnityEngine;

/// <summary>
/// 技能操作提示和冷却显示 — 左下角 IMGUI 绘制。
/// Mage 角色额外显示引爆(E技能)冷却条。
/// </summary>
public class SkillHUD : MonoBehaviour
{
    [Header("外观")]
    [SerializeField] private float _barWidth = 280f;
    [SerializeField] private float _barHeight = 14f;
    [SerializeField] private float _marginLeft = 24f;
    [SerializeField] private float _marginBottom = 40f;
    [SerializeField] private float _detonateBarGap = 6f;  // 引爆条与技能条之间的间距

    private PlayerSkillManager _skillManager;
    private MagePassive _magePassive;
    private bool _isMage;
    private Texture2D _bgTex;
    private Texture2D _cdTex;
    private Texture2D _readyTex;
    private Texture2D _detonateCdTex;
    private Texture2D _detonateReadyTex;

    private GUIStyle _hintStyle;
    private GUIStyle _skillNameStyle;
    private GUIStyle _cdTextStyle;
    private GUIStyle _readyTextStyle;
    private GUIStyle _emptySkillStyle;
    private GUIStyle _detonateLabelStyle;
    private GUIStyle _detonateReadyTextStyle;
    private GUIStyle _detonateCdTextStyle;
    private bool _stylesInit;

    private void EnsureStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;
        _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.TextSecondary } };
        _skillNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.GetAccentColor() } };
        _cdTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter, normal = { textColor = UIColorTheme.TextPrimary } };
        _readyTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        _emptySkillStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = UIColorTheme.TextSecondary } };
        _detonateLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.9f, 0.5f, 1f) } };
        _detonateReadyTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        _detonateCdTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, normal = { textColor = UIColorTheme.TextPrimary } };
    }

    private void Start()
    {
        _skillManager = GameReferences.Player?.GetComponent<PlayerSkillManager>();
        _bgTex = UIColorTheme.MakeTexture(UIColorTheme.DarkBackground);

        // 检测是否是 Mage 角色
        var player = GameReferences.Player;
        if (player != null)
        {
            _magePassive = player.GetComponent<MagePassive>();
            _isMage = _magePassive != null;
        }

        // #10 使用角色专属主题色
        _readyTex = UIColorTheme.MakeTexture(UIColorTheme.GetAccentColor());
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUIScaleHelper.BeginScale();

        float x = _marginLeft;
        float y = Screen.height - _marginBottom;

        // ── 操作提示 ──
        var hintStyle = _hintStyle;
        GUI.color = UIColorTheme.TextSecondary;

        if (_skillManager != null)
        {
            var skill = _skillManager.CurrentActiveSkill;
            if (skill != null)
            {
                var nameStyle = _skillNameStyle;
                GUI.color = UIColorTheme.GetAccentColor();
                string keyHint = _skillManager.ActiveSkills.Count > 1
                    ? $" [{_skillManager.CurrentActiveIndex + 1}/{_skillManager.ActiveSkills.Count}]"
                    : "";
                GUI.Label(new Rect(x, y - 50, _barWidth, 26), $"{skill.Data.skillName}{keyHint}", nameStyle);

                // ── 冷却条（使用后清空，随时间填充至满表示就绪）──
                GUI.color = UIColorTheme.DarkBackground;
                GUI.DrawTexture(new Rect(x, y - 24, _barWidth, _barHeight), _bgTex);

                // 冷却进度 = 已冷却比例（刚用时=0，冷却完=1）
                float cdProgress = 1f - Mathf.Clamp01(skill.CooldownPercent);
                cdProgress = Mathf.Clamp01(cdProgress);

                if (skill.IsOnCooldown)
                {
                    GUI.color = UIColorTheme.AccentMagenta;
                    if (_cdTex == null) _cdTex = UIColorTheme.MakeTexture(UIColorTheme.AccentMagenta);
                    // 冷却条从左侧开始填充（刚用时空，逐渐填满）
                    GUI.DrawTexture(new Rect(x, y - 24, _barWidth * cdProgress, _barHeight), _cdTex);

                    GUI.Label(new Rect(x, y - 24, _barWidth, _barHeight),
                        $"CD {skill.CooldownRemaining:F1}s", _cdTextStyle);
                }
                else
                {
                    GUI.color = UIColorTheme.GetAccentColor();
                    GUI.DrawTexture(new Rect(x, y - 24, _barWidth, _barHeight), _readyTex);

                    GUI.Label(new Rect(x, y - 24, _barWidth, _barHeight), "READY", _readyTextStyle);
                }

                // ── 边框 ──
                GUI.color = UIColorTheme.PanelBackground;
                GUI.DrawTexture(new Rect(x - 1, y - 25, _barWidth + 2, 1), _bgTex);
                GUI.DrawTexture(new Rect(x - 1, y - 11, _barWidth + 2, 1), _bgTex);
                GUI.DrawTexture(new Rect(x - 1, y - 25, 1, _barHeight + 2), _bgTex);
                GUI.DrawTexture(new Rect(x + _barWidth, y - 25, 1, _barHeight + 2), _bgTex);

                // ── Mage 引爆 CD 条（仅 Mage 角色显示，在技能条下方）──
                if (_isMage && _magePassive != null)
                {
                    DrawDetonateBar(x, y - 24 + _barHeight + _detonateBarGap);
                }
            }
            else
            {
                GUI.color = UIColorTheme.TextSecondary;
                GUI.Label(new Rect(x, y - 50, _barWidth, 26), "No skill equipped", _emptySkillStyle);
            }
        }

        GUI.color = Color.white;
        GUIScaleHelper.EndScale();
    }

    /// <summary>
    /// 绘制 Mage 引爆(E技能)冷却条。
    /// </summary>
    /// <param name="x">左上角 x 坐标</param>
    /// <param name="barY">条顶部 y 坐标</param>
    private void DrawDetonateBar(float x, float barY)
    {
        // 延迟创建纹理
        if (_detonateCdTex == null)
            _detonateCdTex = UIColorTheme.MakeTexture(new Color(0.8f, 0.2f, 1f)); // 紫色
        if (_detonateReadyTex == null)
            _detonateReadyTex = UIColorTheme.MakeTexture(new Color(0.6f, 0.1f, 0.8f)); // 深紫色

        // 标签 "[E] 引爆"
        GUI.Label(new Rect(x, barY, _barWidth, _barHeight), "[E] 引爆", _detonateLabelStyle);

        // 背景
        GUI.color = UIColorTheme.DarkBackground;
        GUI.DrawTexture(new Rect(x, barY, _barWidth, _barHeight), _bgTex);

        if (_magePassive.DetonateReady)
        {
            // 就绪状态 — 深紫色满条
            GUI.color = new Color(0.6f, 0.1f, 0.8f);
            GUI.DrawTexture(new Rect(x, barY, _barWidth, _barHeight), _detonateReadyTex);

            GUI.Label(new Rect(x, barY, _barWidth, _barHeight), "DETONATE READY [E]", _detonateReadyTextStyle);
        }
        else
        {
            // 冷却中 — 紫色进度条从左填充
            float cdPercent = 1f - Mathf.Clamp01(_magePassive.DetonateCooldownRemaining / _magePassive.DetonateCooldown);
            GUI.color = new Color(0.8f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(x, barY, _barWidth * cdPercent, _barHeight), _detonateCdTex);

            GUI.Label(new Rect(x, barY, _barWidth, _barHeight),
                $"[E] {_magePassive.DetonateCooldownRemaining:F1}s", _detonateCdTextStyle);
        }

        // 边框
        GUI.color = UIColorTheme.PanelBackground;
        GUI.DrawTexture(new Rect(x - 1, barY - 1, _barWidth + 2, 1), _bgTex);
        GUI.DrawTexture(new Rect(x - 1, barY + _barHeight, _barWidth + 2, 1), _bgTex);
        GUI.DrawTexture(new Rect(x - 1, barY - 1, 1, _barHeight + 2), _bgTex);
        GUI.DrawTexture(new Rect(x + _barWidth, barY - 1, 1, _barHeight + 2), _bgTex);
    }
}
