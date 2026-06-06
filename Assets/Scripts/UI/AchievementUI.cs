#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

public class AchievementUI : MonoBehaviour
{
    public class Achievement
    {
        public string id; public string name; public string description;
        public bool unlocked; public float unlockTime;
        public string bonusKey; public float bonusValue; // #35 永久加成
        public Achievement(string id, string name, string desc, string bonusKey = "", float bonusValue = 0f)
        { this.id = id; this.name = name; this.description = desc; this.unlocked = false; this.bonusKey = bonusKey; this.bonusValue = bonusValue; }
    }

    private static readonly Achievement[] ALL_ACHIEVEMENTS = new Achievement[]
    {
        // ── 基础成就 ──
        new("first_blood","First Blood","Kill your first enemy"),
        new("kill_100","Warrior","Kill 100 enemies"),
        new("kill_500","Genocide","Kill 500 enemies"),
        new("kill_2000","Apocalypse","Kill 2000 enemies","max_hp_bonus", 10f),
        new("wave_5","Survivor","Reach wave 5"),
        new("wave_10","Veteran","Reach wave 10"),
        new("wave_20","Legendary","Reach wave 20","crit_chance", 0.01f),
        new("wave_50","Immortal","Reach wave 50","crit_chance", 0.02f),
        new("boss_first","Boss Slayer","Defeat your first Boss"),
        new("boss_5","Boss Hunter","Defeat 5 Bosses","damage_bonus", 0.05f),
        new("coin_1000","Rich","Collect 1000 coins"),
        new("coin_5000","Tycoon","Collect 5000 coins","coin_bonus", 0.1f),
        new("combo_10","Chain Killer","Reach 10x combo"),
        new("combo_50","Unstoppable","Reach 50x combo","crit_chance", 0.01f),
        new("no_hit_wave","Untouchable","Complete a wave without damage","max_hp_bonus", 5f),
        new("level_max","Max Level","Reach level 20"),
        new("survive_10min","Endurance","Survive 10 minutes","max_hp_bonus", 5f),
        new("boss_no_hit","Flawless Boss","Defeat a Boss without taking damage","damage_bonus", 0.03f),
        // ── DOT 专属成就（#35 新增）──
        new("element_master","Element Master","Collect all 4 DOT bullet types","dot_damage_bonus", 0.05f),
        new("detonate_20","Cataclysm","Detonate hitting 20+ enemies","detonate_bonus", 0.1f),
        new("dot_100k","Toxic Cloud","Deal 100k total DOT damage","dot_damage_bonus", 0.03f),
        new("poison_50","Plague Bearer","Stack 50 poison on a single enemy","dot_damage_bonus", 0.02f),
        new("combo_3_types","Trinity","Have 3 DOT combo effects active simultaneously","dot_damage_bonus", 0.03f),
        new("chain_detonate","Chain Reaction","Trigger Chain Detonate milestone","detonate_bonus", 0.05f),
    };

    private List<Achievement> _achievements = new List<Achievement>();
    private Dictionary<string, Achievement> _achievementMap = new Dictionary<string, Achievement>();
    private bool _showPanel = false; private Vector2 _scrollPos;
    private float _unlockDisplayDuration = 4f;
    private int _killsThisGame, _currentWave, _currentCombo, _bossesKilled, _totalCoins, _currentLevel;
    private bool _tookDamageThisWave;
    private Texture2D _overlayTex, _panelBgTex;

    // 通知队列系统（委托给 AchievementNotificationRenderer）
    private Queue<NotificationEntry> _notificationQueue = new Queue<NotificationEntry>();
    private NotificationEntry? _currentNotification;
    private Texture2D _notifBgTex;
    private Texture2D _notifBorderTex;

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

    // #38 永久加成上限控制
    private const float MAX_SINGLE_BONUS = 0.20f;  // 单个加成上限 20%
    private const float MAX_TOTAL_BONUS = 1.00f;   // 总加成上限 100%

    private void Try(string id, bool cond)
    {
        if (!cond) return; if (!_achievementMap.TryGetValue(id, out var a)) return; if (a.unlocked) return;
        a.unlocked = true; a.unlockTime = Time.unscaledTime;

        // #29 构建通知文本（#38 增强：显示具体永久加成数值）
        string bonusText = AchievementNotificationRenderer.FormatBonusText(a.bonusKey, a.bonusValue);

        _notificationQueue.Enqueue(new NotificationEntry
        {
            name = a.name,
            description = a.description,
            bonusText = bonusText,
            showTime = 0f
        });

        // 播放成就音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        DebugHelper.Log($"[Achievement] Unlocked: {a.name} — {a.description}");
    }

    public void DrawAchievementUI()
    {
        InitTextures();
        AchievementNotificationRenderer.UpdateNotificationQueue(_notificationQueue, ref _currentNotification);
        if (_currentNotification.HasValue) AchievementNotificationRenderer.DrawNotificationCard(_currentNotification.Value, _notifBgTex, _notifBorderTex);
        if (_showPanel) DrawAchievementPanel();
    }

    private void InitTextures()
    {
        if (_overlayTex == null) _overlayTex = UIColorTheme.MakeTexture(UIColorTheme.OverlayDark);
        if (_panelBgTex == null) _panelBgTex = UIColorTheme.MakeTexture(UIColorTheme.PanelBackground);
        if (_notifBgTex == null) _notifBgTex = UIColorTheme.MakeTexture(new Color(0.08f, 0.12f, 0.2f, 0.95f));
        if (_notifBorderTex == null) _notifBorderTex = UIColorTheme.MakeTexture(new Color(1f, 0.85f, 0.2f, 0.8f));
    }

    // 通知绘制和加成格式化已委托给 AchievementNotificationRenderer

    /// <summary>
    /// #38 获取所有已解锁成就的永久加成汇总
    /// </summary>
    private Dictionary<string, float> GetBonusSummary()
    {
        var summary = new Dictionary<string, float>();
        foreach (var a in _achievements)
        {
            if (!a.unlocked || string.IsNullOrEmpty(a.bonusKey) || a.bonusValue <= 0f) continue;
            if (!summary.ContainsKey(a.bonusKey))
                summary[a.bonusKey] = 0f;
            summary[a.bonusKey] += a.bonusValue;
        }
        return summary;
    }

    /// <summary>
    /// #38 获取某个加成的当前总值（含上限控制）
    /// </summary>
    public float GetCappedBonus(string bonusKey)
    {
        float total = 0f;
        foreach (var a in _achievements)
        {
            if (!a.unlocked || a.bonusKey != bonusKey) continue;
            total += a.bonusValue;
        }
        // 单个加成上限
        return Mathf.Min(total, MAX_SINGLE_BONUS);
    }

    private void DrawAchievementPanel()
    {
        float w = 520, h = 480, x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;
        GUI.color = UIColorTheme.OverlayDark; GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex); GUI.color = Color.white;
        GUI.color = UIColorTheme.PanelBackground; GUI.DrawTexture(new Rect(x, y, w, h), _panelBgTex); GUI.color = Color.white;
        GUI.Box(new Rect(x, y, w, h), "");

        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(x, y + 10, w, 30), "🏆 Achievements", titleStyle); GUI.color = Color.white;

        int unlocked = 0;
        foreach (var a in _achievements) { if (a.unlocked) unlocked++; }

        // 成就列表
        float listHeight = h - 180f;
        GUILayout.BeginArea(new Rect(x + 10, y + 50, w - 20, listHeight));
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);
        foreach (var a in _achievements)
        {
            GUI.color = a.unlocked ? UIColorTheme.AccentCyan : UIColorTheme.TextSecondary;
            string s = a.unlocked ? "✅" : "🔒";
            string bonus = a.unlocked ? AchievementNotificationRenderer.FormatBonusText(a.bonusKey, a.bonusValue) : "";
            string line = $"{s}  {a.name}  —  {a.description}";
            if (!string.IsNullOrEmpty(bonus)) line += $"\n      ✨ {bonus}";
            GUILayout.Label(line);
        }
        GUILayout.EndScrollView(); GUILayout.EndArea();

        // #38 永久加成总览面板
        float bonusY = y + listHeight + 55;
        GUI.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        var bonusTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.2f, 1f, 0.4f) } };
        GUI.Label(new Rect(x + 15, bonusY, w - 30, 20), "✨ 永久加成总览", bonusTitleStyle);

        var bonusSummary = GetBonusSummary();
        float totalBonus = 0f;
        float bx = x + 15;
        float by = bonusY + 22;
        var bonusLineStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.8f, 0.9f, 0.7f) } };

        foreach (var kvp in bonusSummary)
        {
            float capped = Mathf.Min(kvp.Value, MAX_SINGLE_BONUS);
            totalBonus += capped;
                string display = AchievementNotificationRenderer.FormatBonusText(kvp.Key, capped);
            if (!string.IsNullOrEmpty(display))
            {
                GUI.Label(new Rect(bx, by, w / 2f - 20, 16), $"• {display}", bonusLineStyle);
                by += 16;
            }
        }

        if (bonusSummary.Count == 0)
        {
            GUI.Label(new Rect(bx, by, w - 30, 16), "暂无解锁加成", bonusLineStyle);
        }
        else
        {
            // 总加成显示
            var totalStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.3f, 1f, 0.6f) } };
            totalBonus = Mathf.Min(totalBonus, MAX_TOTAL_BONUS);
            GUI.Label(new Rect(bx, by + 4, w - 30, 16), $"总计加成: {totalBonus * 100:F1}% (上限 {MAX_TOTAL_BONUS * 100:F0}%)", totalStyle);
        }

        // 统计和关闭按钮
        var statStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(x, y + h - 40, w, 30), $"{unlocked}/{_achievements.Count} Unlocked", statStyle); GUI.color = Color.white;
        if (GUI.Button(new Rect(x + w - 80, y + 10, 60, 25), "Close")) _showPanel = false;
    }

    public void TogglePanel() { _showPanel = !_showPanel; }
    public int GetUnlockedCount() { int c = 0; foreach (var a in _achievements) if (a.unlocked) c++; return c; }
}