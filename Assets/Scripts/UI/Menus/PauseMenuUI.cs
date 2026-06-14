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
    private Texture2D _statsBgTex;
    private Texture2D _dividerTex;
    private Texture2D _borderTex;

    private GUIStyle _pauseTitleStyle;
    private GUIStyle _pauseBtnStyle;
    private GUIStyle _pauseHintStyle;
    private GUIStyle _statsHeaderStyle;
    private GUIStyle _statsLabelStyle;
    private GUIStyle _statsValueStyle;
    private GUIStyle _statsSectionStyle;
    private bool _stylesInit;

    private void EnsureStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;
        _pauseTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        _pauseBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold, hover = { background = _hoverTex } };
        _pauseHintStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = UIColorTheme.TextSecondary } };
        _statsHeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = UIColorTheme.AccentCyan } };
        _statsLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = UIColorTheme.TextSecondary } };
        _statsValueStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.TextPrimary } };
        _statsSectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
    }

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
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayPause();
    }

    public void ResumeGame()
    {
        _isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        Time.timeScale = 1f;
    }

    private bool _isReturning = false;

    public void ReturnToMenu()
    {
        if (_isReturning) return;
        _isReturning = true;
        _isPaused = false;

        Time.timeScale = 1f;

        EventManager.ClearAll();
        GameReferences.Reset();
        GameSceneBootstrap.ResetCharacter();
        DotEffectRegistry.ClearAll();
        CurseSpreadSystem.ResetStaticState();
        CharacterFactory.Clear();
        CharacterConfigLoader.ClearCache();
        DotBulletBase.ActiveDotBullets.Clear();
        SimpleBullet.ActiveBullets.Clear();

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
        EnsureStyles();

        GUI.color = UIColorTheme.OverlayDark;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);
        GUI.color = Color.white;

        float btnPanelW = 360;
        float btnPanelH = 380;
        float btnPanelX = Screen.width * 0.28f - btnPanelW / 2f;
        float btnPanelY = (Screen.height - btnPanelH) / 2f;

        GUILayout.BeginArea(new Rect(btnPanelX, btnPanelY, btnPanelW, btnPanelH));

        var titleStyle = _pauseTitleStyle;
        GUI.color = UIColorTheme.AccentCyan;
        GUILayout.Label("PAUSED", titleStyle);
        GUILayout.Space(25);

        var btnStyle = _pauseBtnStyle;

        btnStyle.normal.textColor = UIColorTheme.TextPrimary;
        btnStyle.normal.background = _resumeTex;
        btnStyle.hover.textColor = UIColorTheme.AccentCyan;
        GUI.color = Color.white;
        if (GUILayout.Button("▶ Resume", btnStyle, GUILayout.Height(50))) ResumeGame();
        GUILayout.Space(8);

        btnStyle.normal.textColor = UIColorTheme.TextSecondary;
        btnStyle.normal.background = _settingsTex;
        btnStyle.hover.textColor = UIColorTheme.AccentCyan;
        GUI.enabled = false;
        if (GUILayout.Button("⚙ Settings (Coming Soon)", btnStyle, GUILayout.Height(50))) { }
        GUI.enabled = true;
        GUILayout.Space(8);

        btnStyle.normal.textColor = UIColorTheme.TextPrimary;
        btnStyle.normal.background = _quitTex;
        btnStyle.hover.textColor = UIColorTheme.AccentPink;
        if (GUILayout.Button("✕ Return to Menu", btnStyle, GUILayout.Height(50))) ReturnToMenu();
        GUILayout.Space(15);

        GUI.color = UIColorTheme.TextSecondary;
        GUILayout.Label("Press ESC to resume", _pauseHintStyle);
        GUILayout.EndArea();

        DrawStatsPanel();
    }

    private void DrawStatsPanel()
    {
        float statsW = 420;
        float statsH = 500;
        float statsX = Screen.width * 0.68f - statsW / 2f;
        float statsY = (Screen.height - statsH) / 2f;

        if (_statsBgTex == null) _statsBgTex = UIColorTheme.MakeTexture(new Color(0.05f, 0.08f, 0.15f, 0.9f));
        GUI.DrawTexture(new Rect(statsX, statsY, statsW, statsH), _statsBgTex);

        DrawBorderRect(new Rect(statsX, statsY, statsW, statsH), UIColorTheme.AccentCyan, 1);

        float x = statsX + 15;
        float y = statsY + 10;
        float w = statsW - 30;

        var headerStyle = _statsHeaderStyle;
        var labelStyle = _statsLabelStyle;
        var valueStyle = _statsValueStyle;
        var sectionStyle = _statsSectionStyle;

        GUI.Label(new Rect(x, y, w, 28), "📊 GAME STATS", headerStyle);
        y += 32;
        DrawDivider(x, y, w);
        y += 8;

        DrawStats(ref y, x, w, labelStyle, valueStyle, sectionStyle);
        DrawDamage(ref y, x, w, labelStyle, valueStyle, sectionStyle);
        DrawUpgrades(ref y, x, w, labelStyle, valueStyle, sectionStyle);

        GUI.color = Color.white;
    }

    private void DrawStats(ref float y, float x, float w, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle sectionStyle)
    {
        GUI.Label(new Rect(x, y, w, 22), "── WAVE INFO ──", sectionStyle);
        y += 24;

        var spawnMgr = GameReferences.SpawnManager;
        int wave = spawnMgr != null ? spawnMgr.CurrentWave : 0;
        int alive = spawnMgr != null ? spawnMgr.EnemiesAlive : 0;

        DrawStatRow(x, y, w, labelStyle, valueStyle, "Wave", $"{wave}");
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Enemies Alive", $"{alive}");
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Time", UIFormatUtils.FormatTime(Time.timeSinceLevelLoad));
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Coins", $"{Coin.TotalCoins}");
        y += 28;
        DrawDivider(x, y, w);
        y += 8;
    }

    private void DrawDamage(ref float y, float x, float w, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle sectionStyle)
    {
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
    }

    private void DrawUpgrades(ref float y, float x, float w, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle sectionStyle)
    {
        var player = GameReferences.Player;
        var dotPassive = GameReferences.DotCharacterPassive;
        if (dotPassive == null) return;

        GUI.Label(new Rect(x, y, w, 22), "── MAGE DOT BUILD ──", sectionStyle);
        y += 24;

        var dotGuns = dotPassive.DotGuns;
        if (dotGuns != null && dotGuns.Count > 0)
        {
            foreach (var gun in dotGuns)
            {
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
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Crit Rate", $"{dotPassive.GetDotCritChance() * 100:F0}%");
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "Crit Mult", $"×{dotPassive.GetDotCritMultiplier():F1}");
        y += 22;
        DrawStatRow(x, y, w, labelStyle, valueStyle, "DOT Duration+", $"+{(dotPassive.GetDotDurationMultiplier() - 1f) * 100:F0}%");
        y += 22;

        var det = dotPassive.GetDetonateSystem();
        if (det != null)
        {
            DrawStatRow(x, y, w, labelStyle, valueStyle, "Detonate Mult", $"×{det.DetonateMultiplier:F1}");
            y += 22;
        }
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

}
