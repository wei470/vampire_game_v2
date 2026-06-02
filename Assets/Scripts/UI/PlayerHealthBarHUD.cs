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
    private Texture2D _xpFillTex;

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

        // ── 经验条（白色，生命条上方，与 Lv. 字条同高，与生命条同宽）──
        var lvSystem = _player.GetComponent<PlayerLevelSystem>();
        if (lvSystem != null)
        {
            float xpBarY = y - 26f;
            float xpBarH = 24f;
            float xpPercent = lvSystem.ExpProgress;
            xpPercent = Mathf.Clamp01(xpPercent);

            // 经验条背景
            GUI.color = new Color(0.25f, 0.25f, 0.25f, 0.8f);
            GUI.DrawTexture(new Rect(x, xpBarY, _barWidth, xpBarH), _bgTex);

            // 经验条填充（白色）
            GUI.color = Color.white;
            if (_xpFillTex == null) _xpFillTex = UIColorTheme.MakeTexture(Color.white);
            GUI.DrawTexture(new Rect(x, xpBarY, _barWidth * xpPercent, xpBarH), _xpFillTex);

            // 经验条边框
            GUI.color = UIColorTheme.PanelBackground;
            GUI.DrawTexture(new Rect(x - 1, xpBarY - 1, _barWidth + 2, 1), _bgTex);
            GUI.DrawTexture(new Rect(x - 1, xpBarY + xpBarH, _barWidth + 2, 1), _bgTex);
            GUI.DrawTexture(new Rect(x - 1, xpBarY - 1, 1, xpBarH + 2), _bgTex);
            GUI.DrawTexture(new Rect(x + _barWidth, xpBarY - 1, 1, xpBarH + 2), _bgTex);

            // ── 等级文字（叠加在经验条上方）──
            GUI.color = UIColorTheme.AccentCyan;
            var lvStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UIColorTheme.AccentCyan }
            };
            GUI.Label(new Rect(x, xpBarY, _barWidth, xpBarH), $"Lv.{lvSystem.Level}", lvStyle);
        }

        // ── 金币计数（生命值下方）──
        GUI.color = UIColorTheme.GoldText;
        var coinStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = UIColorTheme.GoldText }
        };
        int totalCoins = Coin.TotalCoins;
        GUI.Label(new Rect(x, y + _barHeight + 4, _barWidth, 20), $"🪙 {totalCoins}", coinStyle);

        GUI.color = Color.white;
        GUIScaleHelper.EndScale();
    }
}
