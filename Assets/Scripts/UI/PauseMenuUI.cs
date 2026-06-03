using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    private bool _isPaused = false;
    private Texture2D _overlayTex;
    private Texture2D _resumeTex;
    private Texture2D _settingsTex;
    private Texture2D _quitTex;
    private Texture2D _hoverTex;
    private Texture2D _statsBgTex;   // #27 统计面板背景
    private Texture2D _dividerTex;   // #27 分隔线
    private Texture2D _borderTex;    // #27 边框

    public bool IsPaused => _isPaused;

    private void OnEnable() { EventManager.OnGameStateChanged += OnGameStateChanged; }
    private void OnDisable() { EventManager.OnGameStateChanged -= OnGameStateChanged; }
    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        _isPaused = (newState == GameManager.GameState.Paused);
    }

    public void ShowPause()
    {
        _isPaused = true;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Paused);
        Time.timeScale = 0f;

        // 播放暂停音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayPause();
    }

    public void ResumeGame()
    {
        _isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        Time.timeScale = 1f;
    }

    public void ReturnToMenu()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        // #37 使用统一的 GameStateResetter 重置所有游戏状态
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        GameStateResetter.FullReset();

        SceneManager.LoadScene("MenuScene");
    }

    private void InitTextures()
    {
        _overlayTex = UIColorTheme.MakeTexture(UIColorTheme.OverlayDark);
        _resumeTex = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan);
        _settingsTex = UIColorTheme.MakeTexture(UIColorTheme.PanelBackground);
        _quitTex = UIColorTheme.MakeTexture(UIColorTheme.AccentMagenta);
        _hoverTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonHover);
    }

    public void DrawPauseMenu()
    {
        if (!_isPaused) return;
        if (_overlayTex == null) InitTextures();

        GUI.color = UIColorTheme.OverlayDark;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);
        GUI.color = Color.white;

        // #27 左侧：按钮面板（居中偏左）
        float btnPanelW = 360;
        float btnPanelH = 380;
        float btnPanelX = Screen.width * 0.28f - btnPanelW / 2f;
        float btnPanelY = (Screen.height - btnPanelH) / 2f;

        GUILayout.BeginArea(new Rect(btnPanelX, btnPanelY, btnPanelW, btnPanelH));

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = UIColorTheme.AccentCyan }
        };
        GUI.color = UIColorTheme.AccentCyan;
        GUILayout.Label("PAUSED", titleStyle);
        GUILayout.Space(25);

        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };

        btnStyle.normal.textColor = UIColorTheme.TextPrimary;
        btnStyle.normal.background = _resumeTex;
        btnStyle.hover.textColor = UIColorTheme.AccentCyan;
        btnStyle.hover.background = _hoverTex;
        GUI.color = Color.white;
        if (GUILayout.Button("▶ Resume", btnStyle, GUILayout.Height(50))) ResumeGame();
        GUILayout.Space(8);

        btnStyle.normal.textColor = UIColorTheme.TextSecondary;
        btnStyle.normal.background = _settingsTex;
        btnStyle.hover.textColor = UIColorTheme.AccentCyan;
        btnStyle.hover.background = _hoverTex;
        GUI.enabled = false;
        if (GUILayout.Button("⚙ Settings (Coming Soon)", btnStyle, GUILayout.Height(50))) { }
        GUI.enabled = true;
        GUILayout.Space(8);

        btnStyle.normal.textColor = UIColorTheme.TextPrimary;
        btnStyle.normal.background = _quitTex;
        btnStyle.hover.textColor = UIColorTheme.AccentPink;
        btnStyle.hover.background = _hoverTex;
        if (GUILayout.Button("✕ Return to Menu", btnStyle, GUILayout.Height(50))) ReturnToMenu();
        GUILayout.Space(15);

        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = UIColorTheme.TextSecondary }
        };
        GUI.color = UIColorTheme.TextSecondary;
        GUILayout.Label("Press ESC to resume", hintStyle);
        GUILayout.EndArea();

        // #27 右侧：实时统计面板
        DrawStatsPanel();
    }

    /// <summary>
    /// #27 右侧实时统计面板 — 显示 Build 概览 + 属性 + 波次统计
    /// </summary>
    private void DrawStatsPanel()
    {
        float statsW = 420;
        float statsH = 500;
        float statsX = Screen.width * 0.68f - statsW / 2f;
        float statsY = (Screen.height - statsH) / 2f;

        // 统计面板背景
        if (_statsBgTex == null) _statsBgTex = UIColorTheme.MakeTexture(new Color(0.05f, 0.08f, 0.15f, 0.9f));
        GUI.DrawTexture(new Rect(statsX, statsY, statsW, statsH), _statsBgTex);

        // 边框
        DrawBorderRect(new Rect(statsX, statsY, statsW, statsH), UIColorTheme.AccentCyan, 1);

        float x = statsX + 15;
        float y = statsY + 10;
        float w = statsW - 30;

        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = UIColorTheme.AccentCyan }
        };
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = UIColorTheme.TextSecondary }
        };
        var valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = UIColorTheme.TextPrimary }
        };
        var sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            normal = { textColor = UIColorTheme.AccentCyan }
        };

        // ── 标题 ──
        GUI.Label(new Rect(x, y, w, 28), "📊 GAME STATS", headerStyle);
        y += 32;
        DrawDivider(x, y, w);
        y += 8;

        // ── 波次信息 ──
        GUI.Label(new Rect(x, y, w, 22), "── WAVE INFO ──", sectionStyle);
        y += 24;

        var spawnMgr = GameReferences.SpawnManager;
        int wave = spawnMgr != null ? spawnMgr.CurrentWave : 0;
        int alive = spawnMgr != null ? spawnMgr.EnemiesAlive : 0;

        DrawStatRow(x, y, w, labelStyle, valueStyle, "Wave", $"{wave}");
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Enemies Alive", $"{alive}");
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Time", FormatTime(Time.timeSinceLevelLoad));
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Coins", $"{Coin.TotalCoins}");
        y += 28;
        DrawDivider(x, y, w);
        y += 8;

        // ── 玩家属性 ──
        GUI.Label(new Rect(x, y, w, 22), "── PLAYER ──", sectionStyle);
        y += 24;

        var player = GameReferences.Player;
        if (player != null)
        {
            var dmg = player.Damageable;
            if (dmg != null)
            {
                DrawStatRow(x, y, w, labelStyle, valueStyle, "HP", $"{dmg.CurrentHp}/{dmg.MaxHp}");
                y += 22;
                DrawStatRow(x, y, w, labelStyle, valueStyle, "Armor", $"{dmg.Armor}");
                y += 22;
            }
            DrawStatRow(x, y, w, labelStyle, valueStyle, "Move Speed", $"{player.MoveSpeed:F1}");
            y += 22;

            // 武器属性
            var wc = player.GetComponent<WeaponController>();
            if (wc != null && wc.enabled && wc.CurrentWeapon != null)
            {
                DrawStatRow(x, y, w, labelStyle, valueStyle, "Weapon", wc.CurrentWeapon.weaponName);
                y += 22;
                DrawStatRow(x, y, w, labelStyle, valueStyle, "DMG Mult", $"×{wc.DamageMultiplier:F2}");
                y += 22;
            }
        }
        y += 6;
        DrawDivider(x, y, w);
        y += 8;

        // ── Mage 专属：DOT Build 概览 ──
        var magePassive = player?.GetComponent<MagePassive>();
        if (magePassive != null)
        {
            GUI.Label(new Rect(x, y, w, 22), "── MAGE DOT BUILD ──", sectionStyle);
            y += 24;

            var dotGuns = magePassive.DotGuns;
            if (dotGuns != null && dotGuns.Count > 0)
            {
                foreach (var gun in dotGuns)
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(gun.color);
                    string name = gun.effectType.ToString();
                    DrawStatRow(x, y, w, labelStyle, valueStyle, name, $"DPS:{gun.dotDps:F1} Lv:{gun.upgradeLevel}");
                    y += 22;
                }
            }
            else
            {
                GUI.Label(new Rect(x, y, w, 20), "  No DOT guns unlocked", labelStyle);
                y += 22;
            }

            y += 6;
            DrawStatRow(x, y, w, labelStyle, valueStyle, "Crit Rate", $"{magePassive.CritChance * 100:F0}%");
            y += 22;
            DrawStatRow(x, y, w, labelStyle, valueStyle, "Crit Mult", $"×{magePassive.CritMultiplier:F1}");
            y += 22;
            DrawStatRow(x, y, w, labelStyle, valueStyle, "DOT Duration+", $"+{(magePassive.DotDurationMultiplier - 1f) * 100:F0}%");
            y += 22;
            DrawStatRow(x, y, w, labelStyle, valueStyle, "Detonate Mult", $"×{magePassive.DetonateMultiplier:F1}");
            y += 22;
        }

        GUI.color = Color.white;
    }

    private void DrawStatRow(float x, float y, float w, GUIStyle labelStyle, GUIStyle valueStyle, string label, string value)
    {
        GUI.Label(new Rect(x, y, w * 0.55f, 20), label, labelStyle);
        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, 20), value, valueStyle);
    }

    private void DrawDivider(float x, float y, float w)
    {
        if (_dividerTex == null) _dividerTex = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan);
        Color c = UIColorTheme.AccentCyan;
        GUI.color = new Color(c.r, c.g, c.b, 0.3f);
        GUI.DrawTexture(new Rect(x, y, w, 1), _dividerTex);
        GUI.color = Color.white;
    }

    private void DrawBorderRect(Rect r, Color c, float thickness)
    {
        if (_borderTex == null) _borderTex = UIColorTheme.MakeTexture(Color.white);
        GUI.color = new Color(c.r, c.g, c.b, 0.5f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), _borderTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), _borderTex);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), _borderTex);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), _borderTex);
        GUI.color = Color.white;
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }
}