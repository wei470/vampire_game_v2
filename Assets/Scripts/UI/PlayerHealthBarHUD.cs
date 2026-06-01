using UnityEngine;

/// <summary>
/// 玩家血量条状显示 — 左上角 IMGUI 绘制。
/// </summary>
public class PlayerHealthBarHUD : MonoBehaviour
{
    [Header("外观")]
    [SerializeField] private float _barWidth = 380f;
    [SerializeField] private float _barHeight = 24f;
    [SerializeField] private float _marginLeft = 24f;
    [SerializeField] private float _marginTop = 20f;

    private PlayerController _player;
    private Texture2D _bgTex;
    private Texture2D _fillTex;

    private void Start()
    {
        _player = GameReferences.Player;
        _bgTex = UIColorTheme.MakeTexture(UIColorTheme.DarkBackground);
    }

    private void OnGUI()
    {
        GUIScaleHelper.BeginScale();
        if (_player == null) { GUIScaleHelper.EndScale(); return; }

        float percent = _player.HpPercent;
        percent = Mathf.Clamp01(percent);

        float x = _marginLeft;
        float y = _marginTop;

        // ── 背景条 ──
        GUI.color = UIColorTheme.DarkBackground;
        GUI.DrawTexture(new Rect(x, y, _barWidth, _barHeight), _bgTex);

        // ── 填充条 ──
        Color fillColor;
        if (percent > 0.6f)
            fillColor = Color.Lerp(UIColorTheme.AccentMagenta, UIColorTheme.AccentCyan, (percent - 0.6f) / 0.4f);
        else if (percent > 0.3f)
            fillColor = Color.Lerp(UIColorTheme.AccentPink, UIColorTheme.AccentMagenta, (percent - 0.3f) / 0.3f);
        else
            fillColor = UIColorTheme.AccentPink;

        if (_fillTex == null) _fillTex = UIColorTheme.MakeTexture(fillColor);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, y, _barWidth * percent, _barHeight), _fillTex);

        // ── 边框 ──
        GUI.color = UIColorTheme.PanelBackground;
        GUI.DrawTexture(new Rect(x - 1, y - 1, _barWidth + 2, 1), _bgTex);
        GUI.DrawTexture(new Rect(x - 1, y + _barHeight, _barWidth + 2, 1), _bgTex);
        GUI.DrawTexture(new Rect(x - 1, y - 1, 1, _barHeight + 2), _bgTex);
        GUI.DrawTexture(new Rect(x + _barWidth, y - 1, 1, _barHeight + 2), _bgTex);

        // ── 血量文字 ──
        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        var dmg = _player.Damageable;
        string hpText = dmg != null ? $"{dmg.CurrentHp} / {dmg.MaxHp}" : "";
        GUI.Label(new Rect(x, y - 2, _barWidth, _barHeight), hpText, style);

        // ── 等级文字 ──
        var lvSystem = _player.GetComponent<PlayerLevelSystem>();
        if (lvSystem != null)
        {
            GUI.color = UIColorTheme.AccentCyan;
            var lvStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = UIColorTheme.AccentCyan }
            };
            GUI.Label(new Rect(x, y - 26, _barWidth, 24), $"Lv.{lvSystem.Level}", lvStyle);
        }

        GUI.color = Color.white;
        GUIScaleHelper.EndScale();
    }
}