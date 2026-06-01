#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

public class AchievementUI : MonoBehaviour
{
    public class Achievement
    {
        public string id; public string name; public string description;
        public bool unlocked; public float unlockTime;
        public Achievement(string id, string name, string desc) { this.id = id; this.name = name; this.description = desc; this.unlocked = false; }
    }

    private static readonly Achievement[] ALL_ACHIEVEMENTS = new Achievement[]
    {
        new("first_blood","First Blood","Kill your first enemy"),new("kill_100","Warrior","Kill 100 enemies"),new("kill_500","Genocide","Kill 500 enemies"),
        new("wave_5","Survivor","Reach wave 5"),new("wave_10","Veteran","Reach wave 10"),new("wave_20","Legendary","Reach wave 20"),new("wave_50","Immortal","Reach wave 50"),
        new("boss_first","Boss Slayer","Defeat your first Boss"),new("boss_5","Boss Hunter","Defeat 5 Bosses"),new("coin_1000","Rich","Collect 1000 coins"),
        new("combo_10","Chain Killer","Reach 10x combo"),new("combo_50","Unstoppable","Reach 50x combo"),new("no_hit_wave","Untouchable","Complete a wave without damage"),new("level_max","Max Level","Reach level 20"),
    };

    private List<Achievement> _achievements = new List<Achievement>();
    private Dictionary<string, Achievement> _achievementMap = new Dictionary<string, Achievement>();
    private bool _showPanel = false; private Vector2 _scrollPos;
    private float _lastUnlockDisplayTime; private string _lastUnlockName = ""; private float _unlockDisplayDuration = 3f;
    private int _killsThisGame, _currentWave, _currentCombo, _bossesKilled, _totalCoins, _currentLevel;
    private bool _tookDamageThisWave;
    private Texture2D _overlayTex, _panelBgTex;

    private void Awake() { foreach (var a in ALL_ACHIEVEMENTS) { _achievements.Add(a); _achievementMap[a.id] = a; } }
    private void OnEnable() { EventManager.OnEnemyKilled += OnEnemyKilled; EventManager.OnWaveStart += OnWaveStart; EventManager.OnPlayerDamaged += OnPlayerDamaged; EventManager.OnComboChanged += OnComboChanged; EventManager.OnCoinChanged += OnCoinChanged; EventManager.OnLevelUp += OnLevelUp; }
    private void OnDisable() { EventManager.OnEnemyKilled -= OnEnemyKilled; EventManager.OnWaveStart -= OnWaveStart; EventManager.OnPlayerDamaged -= OnPlayerDamaged; EventManager.OnComboChanged -= OnComboChanged; EventManager.OnCoinChanged -= OnCoinChanged; EventManager.OnLevelUp -= OnLevelUp; }

    private void OnEnemyKilled(Vector3 pos, int xp, int coin) { _killsThisGame++; _totalCoins += coin; Try("first_blood", _killsThisGame >= 1); Try("kill_100", _killsThisGame >= 100); Try("kill_500", _killsThisGame >= 500); }
    private void OnWaveStart(int w) { if (_currentWave > 0 && !_tookDamageThisWave) Try("no_hit_wave", true); _currentWave = w; _tookDamageThisWave = false; Try("wave_5", w >= 5); Try("wave_10", w >= 10); Try("wave_20", w >= 20); Try("wave_50", w >= 50); }
    private void OnPlayerDamaged(int hp, int maxHp) { _tookDamageThisWave = true; }
    private void OnComboChanged(int c) { _currentCombo = c; Try("combo_10", c >= 10); Try("combo_50", c >= 50); }
    private void OnCoinChanged(int t) { _totalCoins = t; Try("coin_1000", t >= 1000); }
    private void OnLevelUp(int l) { _currentLevel = l; Try("level_max", l >= 20); }
    public void OnBossDefeated() { _bossesKilled++; Try("boss_first", _bossesKilled >= 1); Try("boss_5", _bossesKilled >= 5); }

    private void Try(string id, bool cond)
    {
        if (!cond) return; if (!_achievementMap.TryGetValue(id, out var a)) return; if (a.unlocked) return;
        a.unlocked = true; a.unlockTime = Time.unscaledTime; _lastUnlockName = a.name; _lastUnlockDisplayTime = Time.unscaledTime;
    }

    public void DrawAchievementUI()
    {
        InitTextures();
        if (Time.unscaledTime - _lastUnlockDisplayTime < _unlockDisplayDuration && !string.IsNullOrEmpty(_lastUnlockName)) DrawUnlockToast();
        if (_showPanel) DrawAchievementPanel();
    }

    private void InitTextures() { if (_overlayTex == null) _overlayTex = UIColorTheme.MakeTexture(UIColorTheme.OverlayDark); if (_panelBgTex == null) _panelBgTex = UIColorTheme.MakeTexture(UIColorTheme.PanelBackground); }

    private void DrawUnlockToast()
    {
        float alpha = 1f - (Time.unscaledTime - _lastUnlockDisplayTime) / _unlockDisplayDuration;
        GUI.color = new Color(UIColorTheme.AccentCyan.r, UIColorTheme.AccentCyan.g, UIColorTheme.AccentCyan.b, alpha);
        float w = 300, h = 50, x = (Screen.width - w) / 2f, y = 100 - (1f - alpha) * 30f;
        GUI.Box(new Rect(x, y, w, h), "");
        var style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(x, y, w, h), $"🏆 {_lastUnlockName}", style);
        GUI.color = Color.white;
    }

    private void DrawAchievementPanel()
    {
        float w = 500, h = 400, x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;
        GUI.color = UIColorTheme.OverlayDark; GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex); GUI.color = Color.white;
        GUI.color = UIColorTheme.PanelBackground; GUI.DrawTexture(new Rect(x, y, w, h), _panelBgTex); GUI.color = Color.white;
        GUI.Box(new Rect(x, y, w, h), "");

        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(x, y + 10, w, 30), "🏆 Achievements", titleStyle); GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(x + 10, y + 50, w - 20, h - 100));
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);
        int unlocked = 0;
        foreach (var a in _achievements) { GUI.color = a.unlocked ? UIColorTheme.AccentCyan : UIColorTheme.TextSecondary; string s = a.unlocked ? "✅" : "🔒"; GUILayout.Label($"{s}  {a.name}  —  {a.description}"); if (a.unlocked) unlocked++; }
        GUILayout.EndScrollView(); GUILayout.EndArea();

        var statStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(x, y + h - 40, w, 30), $"{unlocked}/{_achievements.Count} Unlocked", statStyle); GUI.color = Color.white;
        if (GUI.Button(new Rect(x + w - 80, y + 10, 60, 25), "Close")) _showPanel = false;
    }

    public void TogglePanel() { _showPanel = !_showPanel; }
    public int GetUnlockedCount() { int c = 0; foreach (var a in _achievements) if (a.unlocked) c++; return c; }
}