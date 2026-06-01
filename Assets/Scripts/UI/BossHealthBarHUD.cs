using UnityEngine;

/// <summary>
/// Boss 血量条状显示 — 中上方 IMGUI 绘制。
/// 只在 Boss 存活时显示，跟随 BossEnemy 的 HP 实时更新。
/// </summary>
public class BossHealthBarHUD : MonoBehaviour
{
    [Header("外观")]
    [SerializeField] private float _barWidth = 500f;
    [SerializeField] private float _barHeight = 16f;

    private Texture2D _bgTex;
    private Texture2D _fillTex;
    private BossEnemy _cachedBoss;

    private void Start()
    {
        _bgTex = UIColorTheme.MakeTexture(UIColorTheme.DarkBackground);
    }

    private void OnGUI()
    {
        GUIScaleHelper.BeginScale();
        // 查找场景中的 Boss
        if (_cachedBoss == null)
        {
            _cachedBoss = FindAnyObjectByType<BossEnemy>();
            if (_cachedBoss == null) { GUIScaleHelper.EndScale(); return; }
        }

        var dmg = _cachedBoss.GetComponent<Damageable>();
        if (dmg == null || !dmg.IsAlive)
        {
            _cachedBoss = null;
            GUIScaleHelper.EndScale();
            return;
        }

        float percent = dmg.HpPercent;
        percent = Mathf.Clamp01(percent);

        float x = (Screen.width - _barWidth) / 2f;
        float y = 14f;

        // ── 背景条 ──
        GUI.color = UIColorTheme.DarkBackground;
        GUI.DrawTexture(new Rect(x, y, _barWidth, _barHeight), _bgTex);

        // ── 填充条：荧光青 → 洋红 → 亮粉 ──
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
        GUI.color = UIColorTheme.AccentPink;
        GUI.DrawTexture(new Rect(x - 1, y - 1, _barWidth + 2, 1), _bgTex);
        GUI.DrawTexture(new Rect(x - 1, y + _barHeight, _barWidth + 2, 1), _bgTex);
        GUI.DrawTexture(new Rect(x - 1, y - 1, 1, _barHeight + 2), _bgTex);
        GUI.DrawTexture(new Rect(x + _barWidth, y - 1, 1, _barHeight + 2), _bgTex);

        // ── Boss 名称 + HP 文字 ──
        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = UIColorTheme.AccentPink }
        };
        GUI.Label(new Rect(x, y - 4, _barWidth, _barHeight), $"★ {_cachedBoss.name}  {dmg.CurrentHp}/{dmg.MaxHp} ★", style);

        GUI.color = Color.white;
        GUIScaleHelper.EndScale();
    }

    private void OnDestroy()
    {
        if (_bgTex != null) Destroy(_bgTex);
        if (_fillTex != null) Destroy(_fillTex);
    }
}