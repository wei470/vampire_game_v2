using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 存档管理器，负责保存/加载玩家永久进度。
/// 对应 Python: playfile.py
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    [Serializable]
    public class ShopUpgradeDef
    {
        public string name;
        public string desc;
        public string attr;
        public float amount;
        public float multiplier;
        public int cost;
        public int maxLevel;

        public ShopUpgradeDef(string name, string desc, string attr, float amount, float multiplier, int cost, int maxLevel = 20)
        {
            this.name = name; this.desc = desc; this.attr = attr;
            this.amount = amount; this.multiplier = multiplier;
            this.cost = cost; this.maxLevel = maxLevel;
        }
    }

    [Serializable]
    public class MilestoneDef
    {
        public string id; public string name; public string desc;
        public string stat; public int threshold; public string rewardDesc;
        public string effectAttr; public float effectValue;

        public MilestoneDef(string id, string name, string desc, string stat, int threshold,
            string rewardDesc, string effectAttr, float effectValue)
        {
            this.id = id; this.name = name; this.desc = desc;
            this.stat = stat; this.threshold = threshold;
            this.rewardDesc = rewardDesc; this.effectAttr = effectAttr; this.effectValue = effectValue;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int coins;
        public SerializableDictionary upgrades = new SerializableDictionary();
        public int highestWave;
        public int totalKills;
        public int totalGames;
        public string[] claimedMilestones = new string[0];
    }

    [Serializable]
    public class SerializableDictionary
    {
        public string[] keys = new string[0];
        public int[] values = new int[0];

        /// <summary>
        /// 运行时 Dictionary 缓存，避免 O(n) 线性搜索
        /// </summary>
        [NonSerialized] private Dictionary<string, int> _dict;
        [NonSerialized] private bool _dirty = false;

        private void EnsureBuilt()
        {
            if (_dict == null) RebuildDict();
        }

        private void RebuildDict()
        {
            _dict = new Dictionary<string, int>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
                _dict[keys[i]] = values[i];
        }

        private void SyncToArrays()
        {
            keys = new string[_dict.Count];
            values = new int[_dict.Count];
            int idx = 0;
            foreach (var kvp in _dict)
            {
                keys[idx] = kvp.Key;
                values[idx] = kvp.Value;
                idx++;
            }
        }

        public int Get(string key, int defaultVal = 0)
        {
            EnsureBuilt();
            return _dict.TryGetValue(key, out var v) ? v : defaultVal;
        }

        public void Set(string key, int value)
        {
            EnsureBuilt();
            _dict[key] = value;
            _dirty = true;
        }

        /// <summary>
        /// 在 Save 前调用，将 Dictionary 同步回数组以便 JSON 序列化
        /// </summary>
        public void Flush()
        {
            if (_dirty && _dict != null)
            {
                SyncToArrays();
                _dirty = false;
            }
        }
    }

    // ============================================================
    // 配置常量
    // ============================================================

    public static readonly ShopUpgradeDef[] SHOP_UPGRADES = new ShopUpgradeDef[]
    {
        new ShopUpgradeDef("Max HP +50",  "Start with 50 more HP",          "max_hp",          50f,  1.0f,  5),
        new ShopUpgradeDef("ATK +2",      "Start with 2 more damage",       "attack_damage",   2f,   1.0f,  5),
        new ShopUpgradeDef("Speed +10%",  "Move 10% faster",                "speed",           0f,   1.10f, 5),
        new ShopUpgradeDef("Armor +1",    "Reduce damage by 1",             "armor",           1f,   1.0f,  8),
        new ShopUpgradeDef("HP Regen +0.5/s", "Regen 0.5 HP per second",    "hp_regen",        0.5f, 1.0f,  5),
        new ShopUpgradeDef("XP Boost +10%", "Gain 10% more XP",            "xp_multiplier",   0.1f, 1.0f,  10),
        new ShopUpgradeDef("Weaken Enemies", "Enemies -5% HP/DMG/SPD",     "weaken_enemies",  0f,   0.95f, 12),
        new ShopUpgradeDef("Crit Chance +3%", "3% more crit chance",       "crit_chance",     0.03f, 1.0f, 15),
        new ShopUpgradeDef("Lifesteal +1%", "Heal 1% of damage dealt",     "lifesteal",       0.01f, 1.0f, 10),
        new ShopUpgradeDef("Magnet +20%", "Pickup range +20% per lvl",     "magnet_radius",   0f,   1.20f, 8),
        new ShopUpgradeDef("Pierce +1",   "Attacks pierce 1 more enemy",   "pierce",          1f,   1.0f,  15),
        new ShopUpgradeDef("Dodge +2%",   "2% chance to dodge attacks",    "dodge_chance",    0.02f, 1.0f, 12),
    };

    public static readonly MilestoneDef[] MILESTONES = new MilestoneDef[]
    {
        new MilestoneDef("wave_5",    "Getting Started",  "Reach Wave 5",        "highestWave", 5,    "+10% XP",           "xp_multiplier",  0.10f),
        new MilestoneDef("wave_10",   "Survivor",          "Reach Wave 10",       "highestWave", 10,   "+25 Max HP",         "max_hp",         25f),
        new MilestoneDef("wave_20",   "Veteran",           "Reach Wave 20",       "highestWave", 20,   "+50 Max HP, +2 ATK", "max_hp",         50f),
        new MilestoneDef("wave_50",   "Legendary",         "Reach Wave 50",       "highestWave", 50,   "+100 Max HP, +5 ATK","max_hp",         100f),
        new MilestoneDef("kills_100", "Slayer",            "Kill 100 enemies",    "totalKills",  100,  "+3 ATK",             "attack_damage",  3f),
        new MilestoneDef("kills_500", "Warlord",           "Kill 500 enemies",    "totalKills",  500,  "+0.3 Speed",         "speed",          0.3f),
        new MilestoneDef("kills_1k",  "Genocide",          "Kill 1000 enemies",   "totalKills",  1000, "+5% Crit Chance",    "crit_chance",    0.05f),
        new MilestoneDef("games_5",   "Regular",           "Play 5 games",        "totalGames",  5,    "+1 Armor",           "armor",          1f),
        new MilestoneDef("games_20",  "Addicted",          "Play 20 games",       "totalGames",  20,   "+0.5 HP Regen/s",    "hp_regen",       0.5f),
        new MilestoneDef("games_50",  "Dedicated",         "Play 50 games",       "totalGames",  50,   "+20% XP, +1 Armor",  "xp_multiplier",  0.20f),
    };

    // ============================================================
    // 运行时状态
    // ============================================================

    private SaveData _data = null;
    private string _savePath = null;

    /// <summary>
    /// 当前存档数据（保证非 null）
    /// </summary>
    public SaveData Data
    {
        get
        {
            if (_data == null) _data = CreateDefaultSave();
            return _data;
        }
    }

    public static event Action<int> OnCoinsChanged;
    public static event Action<SaveData> OnSaveLoaded;

    protected override void Awake()
    {
        base.Awake();
        _savePath = Path.Combine(Application.persistentDataPath, "save_data.json");
        if (_data == null) _data = CreateDefaultSave();
        Load();
    }

    // ============================================================
    // 存/读档
    // ============================================================

    public void Save()
    {
        if (string.IsNullOrEmpty(_savePath))
            _savePath = Path.Combine(Application.persistentDataPath, "save_data.json");
        try
        {
            // 保存前将 Dictionary 同步回数组
            Data.upgrades.Flush();
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(_savePath, json);
            DebugHelper.Log($"[SaveManager] Saved to {_savePath}");
        }
        catch (Exception e)
        {
            DebugHelper.LogError($"[SaveManager] Save failed: {e.Message}");
        }
    }

    public void Load()
    {
        if (_savePath == null)
            _savePath = Path.Combine(Application.persistentDataPath, "save_data.json");

        if (File.Exists(_savePath))
        {
            try
            {
                string json = File.ReadAllText(_savePath);
                _data = JsonUtility.FromJson<SaveData>(json);
                if (_data.upgrades == null) _data.upgrades = new SerializableDictionary();
                if (_data.claimedMilestones == null) _data.claimedMilestones = new string[0];
                DebugHelper.Log($"[SaveManager] Loaded: coins={_data.coins}, waves={_data.highestWave}, kills={_data.totalKills}");
            }
            catch (Exception e)
            {
                DebugHelper.LogWarning($"[SaveManager] Load failed: {e.Message}");
                _data = CreateDefaultSave();
            }
        }
        else
        {
            _data = CreateDefaultSave();
            DebugHelper.Log("[SaveManager] No save file found, created default save");
        }
        OnSaveLoaded?.Invoke(_data);
    }

    private SaveData CreateDefaultSave()
    {
        SaveData data = new SaveData();
        data.coins = 0; data.highestWave = 0; data.totalKills = 0; data.totalGames = 0;
        data.claimedMilestones = new string[0];
        foreach (var upgrade in SHOP_UPGRADES)
            data.upgrades.Set(upgrade.attr, 0);
        return data;
    }

    // ============================================================
    // 金币操作
    // ============================================================

    public int GetCoins() { return Data.coins; }

    public void AddCoins(int amount)
    {
        Data.coins += amount;
        OnCoinsChanged?.Invoke(Data.coins);
    }

    public bool SpendCoins(int amount)
    {
        if (Data.coins < amount) return false;
        Data.coins -= amount;
        OnCoinsChanged?.Invoke(Data.coins);
        return true;
    }

    // ============================================================
    // 升级操作
    // ============================================================

    public int GetUpgradeLevel(string attr) { return Data.upgrades.Get(attr, 0); }

    public bool CanBuyUpgrade(int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= SHOP_UPGRADES.Length) return false;
        var upgrade = SHOP_UPGRADES[upgradeIndex];
        int level = GetUpgradeLevel(upgrade.attr);
        if (level >= upgrade.maxLevel) return false;
        return Data.coins >= upgrade.cost;
    }

    public bool BuyUpgrade(int upgradeIndex)
    {
        if (!CanBuyUpgrade(upgradeIndex)) return false;
        var upgrade = SHOP_UPGRADES[upgradeIndex];
        Data.coins -= upgrade.cost;
        int newLevel = GetUpgradeLevel(upgrade.attr) + 1;
        Data.upgrades.Set(upgrade.attr, newLevel);
        DebugHelper.Log($"[SaveManager] Bought {upgrade.name} Lv.{newLevel}, remaining: {Data.coins}");
        OnCoinsChanged?.Invoke(Data.coins);
        Save();
        return true;
    }

    // ============================================================
    // 统计更新
    // ============================================================

    public void RecordGameEnd(int finalWave, int killsThisGame)
    {
        Data.totalGames++;
        Data.totalKills += killsThisGame;
        if (finalWave > Data.highestWave) Data.highestWave = finalWave;
        Save();
        DebugHelper.Log($"[SaveManager] Game recorded: wave={finalWave}, kills={killsThisGame}, total_games={Data.totalGames}");
    }

    // ============================================================
    // 里程碑
    // ============================================================

    public bool IsMilestoneClaimed(string id)
    {
        if (Data.claimedMilestones == null) return false;
        foreach (string s in Data.claimedMilestones)
            if (s == id) return true;
        return false;
    }

    public int GetStatForMilestone(string stat)
    {
        switch (stat)
        {
            case "highestWave": return Data.highestWave;
            case "totalKills": return Data.totalKills;
            case "totalGames": return Data.totalGames;
            default: return 0;
        }
    }

    public bool ClaimMilestone(string milestoneId)
    {
        foreach (var ms in MILESTONES)
        {
            if (ms.id == milestoneId)
            {
                if (IsMilestoneClaimed(ms.id)) return false;
                int statVal = GetStatForMilestone(ms.stat);
                if (statVal < ms.threshold) return false;
                string[] newClaimed = new string[Data.claimedMilestones.Length + 1];
                Array.Copy(Data.claimedMilestones, newClaimed, Data.claimedMilestones.Length);
                newClaimed[Data.claimedMilestones.Length] = ms.id;
                Data.claimedMilestones = newClaimed;
                Save();
                DebugHelper.Log($"[SaveManager] Claimed milestone: {ms.name}");
                return true;
            }
        }
        return false;
    }

    // ============================================================
    // 永久加成
    // ============================================================

    public float GetPermanentBonus(string attr)
    {
        float total = 0f;
        foreach (var upgrade in SHOP_UPGRADES)
        {
            if (upgrade.attr != attr) continue;
            int level = GetUpgradeLevel(upgrade.attr);
            if (level <= 0) continue;
            if (upgrade.amount != 0f) total += upgrade.amount * level;
        }
        foreach (var ms in MILESTONES)
        {
            if (ms.effectAttr == attr && IsMilestoneClaimed(ms.id))
                total += ms.effectValue;
        }
        return total;
    }

    public float GetPermanentMultiplier(string attr)
    {
        float total = 1f;
        foreach (var upgrade in SHOP_UPGRADES)
        {
            if (upgrade.attr != attr) continue;
            int level = GetUpgradeLevel(upgrade.attr);
            if (level <= 0) continue;
            if (upgrade.multiplier != 1f && upgrade.multiplier > 0f)
                total *= Mathf.Pow(upgrade.multiplier, level);
        }
        return total;
    }

    // ============================================================
    // 被动技能永久加成（局内临时，不持久化到存档）
    // ============================================================

    /// <summary>
    /// 添加局内被动技能加成（存储在 upgrades 字典中，以 "passive_" 前缀区分）
    /// </summary>
    public void AddPermanentBonus(string key, float value)
    {
        string fullKey = "passive_" + key;
        int current = Data.upgrades.Get(fullKey, 0);
        // 将 float 值乘以 1000 存储为 int，读取时除以 1000 还原
        Data.upgrades.Set(fullKey, current + Mathf.RoundToInt(value * 1000f));
        DebugHelper.Log($"[SaveManager] Added passive bonus: {fullKey} +{value} (stored as {Mathf.RoundToInt(value * 1000f)})");
    }

    /// <summary>
    /// 获取局内被动技能加成
    /// </summary>
    public float GetPassiveBonus(string key)
    {
        string fullKey = "passive_" + key;
        return Data.upgrades.Get(fullKey, 0) / 1000f;
    }

    /// <summary>
    /// 清除所有局内被动加成（游戏结束时调用）
    /// </summary>
    public void ClearPassiveBonuses()
    {
        // 清除所有 passive_ 前缀的条目
        var keys = Data.upgrades.keys;
        var values = Data.upgrades.values;
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null && keys[i].StartsWith("passive_"))
            {
                Data.upgrades.Set(keys[i], 0);
            }
        }
        DebugHelper.Log("[SaveManager] All passive bonuses cleared");
    }

    public void ResetData()
    {
        _data = CreateDefaultSave();
        Save();
        OnCoinsChanged?.Invoke(Data.coins);
        DebugHelper.Log("[SaveManager] Save data reset");
    }
}
