using UnityEngine;

/// <summary>
/// 技能操作提示和冷却显示 — 左下角 IMGUI 绘制。
/// </summary>
public class SkillHUD : MonoBehaviour
{
    [Header("外观")]
    [SerializeField] private float _barWidth = 280f;
    [SerializeField] private float _barHeight = 14f;
    [SerializeField] private float _marginLeft = 24f;
    [SerializeField] private float _marginBottom = 40f;

    private PlayerSkillManager _skillManager;
    private Texture2D _bgTex;
    private Texture2D _cdTex;
    private Texture2D _readyTex;

    private void Start()
    {
        _skillManager = GameReferences.Player?.GetComponent<PlayerSkillManager>();
        _bgTex = UIColorTheme.MakeTexture(UIColorTheme.DarkBackground);
        _readyTex = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan);
    }

    private void OnGUI()
    {
        GUIScaleHelper.BeginScale();

        float x = _marginLeft;
        float y = Screen.height - _marginBottom;

        // ── 操作提示 ──
        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = UIColorTheme.TextSecondary }
        };
        GUI.color = UIColorTheme.TextSecondary;
        GUI.Label(new Rect(x, y - 72, _barWidth, 24), "[E] 使用技能   [Q] 切换技能", hintStyle);

        if (_skillManager != null)
        {
            var skill = _skillManager.CurrentActiveSkill;
            if (skill != null)
            {
                var nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = UIColorTheme.AccentCyan }
                };
                GUI.color = UIColorTheme.AccentCyan;
                string keyHint = _skillManager.ActiveSkills.Count > 1
                    ? $" [{_skillManager.CurrentActiveIndex + 1}/{_skillManager.ActiveSkills.Count}]"
                    : "";
                GUI.Label(new Rect(x, y - 50, _barWidth, 26), $"{skill.Data.skillName}{keyHint}", nameStyle);

                // ── 冷却条 ──
                GUI.color = UIColorTheme.DarkBackground;
                GUI.DrawTexture(new Rect(x, y - 24, _barWidth, _barHeight), _bgTex);

                float cdPercent = 1f - skill.CooldownPercent;
                cdPercent = Mathf.Clamp01(cdPercent);

                if (cdPercent < 1f)
                {
                    GUI.color = UIColorTheme.AccentMagenta;
                    if (_cdTex == null) _cdTex = UIColorTheme.MakeTexture(UIColorTheme.AccentMagenta);
                    GUI.DrawTexture(new Rect(x, y - 24, _barWidth * cdPercent, _barHeight), _cdTex);

                    var cdStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = UIColorTheme.AccentMagenta }
                    };
                    GUI.Label(new Rect(x, y - 24, _barWidth, _barHeight),
                        $"CD {skill.CooldownRemaining:F1}s", cdStyle);
                }
                else
                {
                    GUI.color = UIColorTheme.AccentCyan;
                    GUI.DrawTexture(new Rect(x, y - 24, _barWidth, _barHeight), _readyTex);

                    var readyStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(new Rect(x, y - 24, _barWidth, _barHeight), "READY", readyStyle);
                }

                // ── 边框 ──
                GUI.color = UIColorTheme.PanelBackground;
                GUI.DrawTexture(new Rect(x - 1, y - 25, _barWidth + 2, 1), _bgTex);
                GUI.DrawTexture(new Rect(x - 1, y - 11, _barWidth + 2, 1), _bgTex);
                GUI.DrawTexture(new Rect(x - 1, y - 25, 1, _barHeight + 2), _bgTex);
                GUI.DrawTexture(new Rect(x + _barWidth, y - 25, 1, _barHeight + 2), _bgTex);
            }
            else
            {
                GUI.color = UIColorTheme.TextSecondary;
                var emptyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    normal = { textColor = UIColorTheme.TextSecondary }
                };
                GUI.Label(new Rect(x, y - 50, _barWidth, 26), "No skill equipped", emptyStyle);
            }
        }

        GUI.color = Color.white;
        GUIScaleHelper.EndScale();
    }
}