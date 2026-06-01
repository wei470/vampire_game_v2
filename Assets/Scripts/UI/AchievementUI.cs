#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 成就 UI — 显示玩家达成的各种成就。
/// 
/// 成就类型：
/// - 击杀类：累计击杀敌人数量
/// - 波次类：达到指定波次
/// - 收集类：累计收集经验和金币
/// - 生存类：单局生存时间
/// - Boss类：击败 Boss
/// 
/// 使用方式：由 GameSceneBootstrap 在 OnGUI 中渲染
/// </summary>
public class AchievementUI : MonoBehaviour
{
    /// <summary>
    /// 成就定义
    /// </summary>
    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        public bool unlocked;
        public float unlockTime;

        public Achievement(string id, string name, string desc)
        {
            this.id = id;
            this.name = name;
            this.description = desc;
            this.unlocked = false;
        }
    }

    // ── 所有成就定义 ──
    private static readonly Achievement[] ALL_ACHIEVEMENTS = new Achievement[]
    {
        new Achievement("first_blood",   "First Blood",       "Kill your first enemy"),
        new Achievement("kill_100",      "Warrior",            "Kill 100 enemies in one game"),
        new Achievement("kill_500",      "Genocide",           "Kill 500 enemies in one game"),
        new Achievement("wave_5",        "Survivor",           "Reach wave 5"),
        new Achievement("wave_10",       "Veteran",            "Reach wave 10"),
        new Achievement("wave_20",       "Legendary",          "Reach wave 20"),
        new Achievement("wave_50",       "Immortal",           "Reach wave 50"),
        new Achievement("boss_first",    "Boss Slayer",        "Defeat your first Boss"),
        new Achievement("boss_5",        "Boss Hunter",        "Defeat 5 Bosses"),
        new Achievement("coin_1000",     "Rich",               "Collect 1000 coins total"),
        new Achievement("combo_10",      "Chain Killer",       "Reach 10x combo"),
        new Achievement("combo_50",      "Unstoppable",        "Reach 50x combo"),
        new Achievement("no_hit_wave",   "Untouchable",        "Complete a wave without taking damage"),
        new Achievement("level_max",     "Max Level",          "Reach level 20"),
    };

    private List<Achievement> _achievements = new List<Achievement>();
    private Dictionary<string, Achievement> _achievementMap = new Dictionary<string, Achievement>();

    private bool _showPanel = false;
    private Vector2 _scrollPos;
    private float _lastUnlockDisplayTime;
    private string _lastUnlockName = "";
    private float _unlockDisplayDuration = 3f;

    // ── 统计追踪 ──
    private int _killsThisGame = 0;
    private int _currentWave = 0;
    private int _currentCombo = 0;
    private bool _tookDamageThisWave = false;
    private int _bossesKilled = 0;
    private int _totalCoins = 0;
    private int _currentLevel = 0;

    private void Awake()
    {
        // 初始化所有成就
        foreach (var a in ALL_ACHIEVEMENTS)
        {
            _achievements.Add(a);
            _achievementMap[a.id] = a;
        }
    }

    private void OnEnable()
    {
        EventManager.OnEnemyKilled += OnEnemyKilled;
        EventManager.OnWaveStart += OnWaveStart;
        EventManager.OnPlayerDamaged += OnPlayerDamaged;
        EventManager.OnComboChanged += OnComboChanged;
        EventManager.OnCoinChanged += OnCoinChanged;
        EventManager.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyKilled -= OnEnemyKilled;
        EventManager.OnWaveStart -= OnWaveStart;
        EventManager.OnPlayerDamaged -= OnPlayerDamaged;
        EventManager.OnComboChanged -= OnComboChanged;
        EventManager.OnCoinChanged -= OnCoinChanged;
        EventManager.OnLevelUp -= OnLevelUp;
    }

    // ── 事件处理 ──

    private void OnEnemyKilled(Vector3 pos, int xp, int coin)
    {
        _killsThisGame++;
        _totalCoins += coin;

        TryUnlock("first_blood", _killsThisGame >= 1);
        TryUnlock("kill_100", _killsThisGame >= 100);
        TryUnlock("kill_500", _killsThisGame >= 500);
    }

    private void OnWaveStart(int waveNum)
    {
        // 上一波没受伤 → 解锁 Untouchable
        if (_currentWave > 0 && !_tookDamageThisWave)
        {
            TryUnlock("no_hit_wave", true);
        }
        _currentWave = waveNum;
        _tookDamageThisWave = false;

        TryUnlock("wave_5", _currentWave >= 5);
        TryUnlock("wave_10", _currentWave >= 10);
        TryUnlock("wave_20", _currentWave >= 20);
        TryUnlock("wave_50", _currentWave >= 50);
    }

    private void OnPlayerDamaged(int hp, int maxHp)
    {
        _tookDamageThisWave = true;
    }

    private void OnComboChanged(int combo)
    {
        _currentCombo = combo;
        TryUnlock("combo_10", _currentCombo >= 10);
        TryUnlock("combo_50", _currentCombo >= 50);
    }

    private void OnCoinChanged(int total)
    {
        _totalCoins = total;
        TryUnlock("coin_1000", _totalCoins >= 1000);
    }

    private void OnLevelUp(int level)
    {
        _currentLevel = level;
        TryUnlock("level_max", _currentLevel >= 20);
    }

    /// <summary>
    /// Boss 被击败时调用（由 BossEnemy 或 EventManager 触发）
    /// </summary>
    public void OnBossDefeated()
    {
        _bossesKilled++;
        TryUnlock("boss_first", _bossesKilled >= 1);
        TryUnlock("boss_5", _bossesKilled >= 5);
    }

    /// <summary>
    /// 尝试解锁成就
    /// </summary>
    private void TryUnlock(string id, bool condition)
    {
        if (!condition) return;
        if (!_achievementMap.TryGetValue(id, out var a)) return;
        if (a.unlocked) return;

        a.unlocked = true;
        a.unlockTime = Time.unscaledTime;
        _lastUnlockName = a.name;
        _lastUnlockDisplayTime = Time.unscaledTime;

        DebugHelper.Log($"[AchievementUI] 🏆 Achievement Unlocked: {a.name} — {a.description}");
    }

    /// <summary>
    /// 渲染成就 UI（在 OnGUI 中调用）
    /// </summary>
    public void DrawAchievementUI()
    {
        // 成就解锁弹窗
        if (Time.unscaledTime - _lastUnlockDisplayTime < _unlockDisplayDuration && !string.IsNullOrEmpty(_lastUnlockName))
        {
            DrawUnlockToast();
        }

        // 成就面板（Tab 键切换）
        if (_showPanel)
        {
            DrawAchievementPanel();
        }
    }

    /// <summary>
    /// 成就解锁弹窗
    /// </summary>
    private void DrawUnlockToast()
    {
        float alpha = 1f - (Time.unscaledTime - _lastUnlockDisplayTime) / _unlockDisplayDuration;
        GUI.color = new Color(1f, 0.85f, 0f, alpha);

        float w = 300, h = 50;
        float x = (Screen.width - w) / 2f;
        float y = 100 - (1f - alpha) * 30f; // 从上往下滑入

        GUI.Box(new Rect(x, y, w, h), "");
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(x, y, w, h), $"🏆 {_lastUnlockName}", style);
        GUI.color = Color.white;
    }

    /// <summary>
    /// 成就面板
    /// </summary>
    private void DrawAchievementPanel()
    {
        float w = 500, h = 400;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        // 遮罩
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Box(new Rect(x, y, w, h), "");

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        GUI.color = new Color(1f, 0.85f, 0f);
        GUI.Label(new Rect(x, y + 10, w, 30), "🏆 Achievements", titleStyle);
        GUI.color = Color.white;

        // 成就列表
        GUILayout.BeginArea(new Rect(x + 10, y + 50, w - 20, h - 100));
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        int unlocked = 0;
        foreach (var a in _achievements)
        {
            GUI.color = a.unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            string status = a.unlocked ? "✅" : "🔒";
            GUILayout.Label($"{status}  {a.name}  —  {a.description}");
            if (a.unlocked) unlocked++;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 统计
        GUI.color = Color.cyan;
        var statStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(x, y + h - 40, w, 30), $"{unlocked}/{_achievements.Count} Unlocked", statStyle);
        GUI.color = Color.white;

        // 关闭按钮
        if (GUI.Button(new Rect(x + w - 80, y + 10, 60, 25), "Close"))
        {
            _showPanel = false;
        }
    }

    /// <summary>
    /// 切换面板显示
    /// </summary>
    public void TogglePanel()
    {
        _showPanel = !_showPanel;
    }

    /// <summary>
    /// 获取解锁数量
    /// </summary>
    public int GetUnlockedCount()
    {
        int count = 0;
        foreach (var a in _achievements)
            if (a.unlocked) count++;
        return count;
    }
}