using UnityEngine;
using System;

/// <summary>
/// 永久升级商店 — 管理商店升级、里程碑、永久加成计算。
/// 从 SaveManager 中提取，减少主文件行数。
///
/// 职责：
/// - 商店升级配置 + 购买/查询逻辑
/// - 里程碑配置 + 领取/查询逻辑
/// - 永久加成/乘算计算
/// - 局内被动技能加成（临时）
/// </summary>
public static class PermanentUpgradeStore
{
    // ============================================================
    // 配置常量
    // ============================================================

    public static readonly ShopUpgradeDef[] SHOP_UPGRADES = new ShopUpgradeDef[]
    {
        new ShopUpgradeDef("Max HP +50",  "Start with 50 more HP",          "max_hp",          50f,  1.0f,  5),
        new ShopUpgradeDef("ATK +2",      "Start with 2 more damage",       "attack_damage",   2f,   1.0f,  5),
        new ShopUpgradeDef("Speed +10%",  "Move 10% faster",                "speed",           0f,   1.10f, 5),
        new ShopUpgradeDef("HP Regen +0.5/s", "Regen 0.5 HP per second",    "hp_regen",        0.5f, 1.0f,  5),
        new ShopUpgradeDef("XP Boost +10%", "Gain 10% more XP",            "xp_multiplier",   0.1f, 1.0f,  10),
        new ShopUpgradeDef("Weaken Enemies", "Enemies -5% HP/DMG/SPD",     "weaken_enemies",  0f,   0.95f, 12),
        new ShopUpgradeDef("Crit Chance +3%", "3% more crit chance",       "crit_chance",     0.03f, 1.0f, 15),
        new ShopUpgradeDef("Lifesteal +1%", "Heal 1% of damage dealt",     "lifesteal",       0.01f, 1.0f, 10),
        new ShopUpgradeDef("Magnet +20%", "Pickup range +20% per lvl",     "magnet_radius",   0f,   1.20f, 8),
        new ShopUpgradeDef("Pierce +1",   "Attacks pierce 1 more enemy",   "pierce",          1f,   1.0f,  15),
        new ShopUpgradeDef("Dodge +2%",   "2% chance to dodge attacks",    "dodge_chance",    0.02f, 1.0f, 12),
        // #34 新增商店商品
        new ShopUpgradeDef("Gold Interest", "+2% gold interest/wave",       "gold_interest",   0.02f, 1.0f, 8),
        new ShopUpgradeDef("Revive +1",    "Extra revive per game",         "revive_count",    1f,   1.0f, 3, 2),
        new ShopUpgradeDef("Skill CD -5%","Reduce skill cooldowns by 5%",   "skill_cooldown",  0.05f, 1.0f, 10),
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
        new MilestoneDef("games_5",   "Regular",           "Play 5 games",        "totalGames",  5,    "+5% XP",             "xp_multiplier",  0.05f),
        new MilestoneDef("games_20",  "Addicted",          "Play 20 games",       "totalGames",  20,   "+0.5 HP Regen/s",    "hp_regen",       0.5f),
        new MilestoneDef("games_50",  "Dedicated",         "Play 50 games",       "totalGames",  50,   "+20% XP, +1 Armor",  "xp_multiplier",  0.20f),
    };

    // ============================================================
    // 升级操作
    // ============================================================

    public static int GetUpgradeLevel(SaveGameData data, string attr)
    {
        return data.upgrades.Get(attr, 0);
    }

    public static bool CanBuyUpgrade(SaveGameData data, int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= SHOP_UPGRADES.Length) return false;
        var upgrade = SHOP_UPGRADES[upgradeIndex];
        int level = GetUpgradeLevel(data, upgrade.attr);
        if (level >= upgrade.maxLevel) return false;
        return data.coins >= upgrade.cost;
    }

    public static bool BuyUpgrade(SaveGameData data, int upgradeIndex)
    {
        if (!CanBuyUpgrade(data, upgradeIndex)) return false;
        var upgrade = SHOP_UPGRADES[upgradeIndex];
        data.coins -= upgrade.cost;
        int newLevel = GetUpgradeLevel(data, upgrade.attr) + 1;
        data.upgrades.Set(upgrade.attr, newLevel);
        DebugHelper.Log($"[PermanentUpgradeStore] Bought {upgrade.name} Lv.{newLevel}, remaining: {data.coins}");
        return true;
    }

    // ============================================================
    // 里程碑
    // ============================================================

    public static bool IsMilestoneClaimed(SaveGameData data, string id)
    {
        if (data.claimedMilestones == null) return false;
        foreach (string s in data.claimedMilestones)
            if (s == id) return true;
        return false;
    }

    public static int GetStatForMilestone(SaveGameData data, string stat)
    {
        switch (stat)
        {
            case "highestWave": return data.highestWave;
            case "totalKills": return data.totalKills;
            case "totalGames": return data.totalGames;
            default: return 0;
        }
    }

    public static bool ClaimMilestone(SaveGameData data, string milestoneId)
    {
        foreach (var ms in MILESTONES)
        {
            if (ms.id == milestoneId)
            {
                if (IsMilestoneClaimed(data, ms.id)) return false;
                int statVal = GetStatForMilestone(data, ms.stat);
                if (statVal < ms.threshold) return false;
                string[] newClaimed = new string[data.claimedMilestones.Length + 1];
                Array.Copy(data.claimedMilestones, newClaimed, data.claimedMilestones.Length);
                newClaimed[data.claimedMilestones.Length] = ms.id;
                data.claimedMilestones = newClaimed;
                DebugHelper.Log($"[PermanentUpgradeStore] Claimed milestone: {ms.name}");
                return true;
            }
        }
        return false;
    }

    // ============================================================
    // 永久加成计算
    // ============================================================

    public static float GetPermanentBonus(SaveGameData data, string attr)
    {
        float total = 0f;
        foreach (var upgrade in SHOP_UPGRADES)
        {
            if (upgrade.attr != attr) continue;
            int level = GetUpgradeLevel(data, upgrade.attr);
            if (level <= 0) continue;
            if (upgrade.amount != 0f) total += upgrade.amount * level;
        }
        foreach (var ms in MILESTONES)
        {
            if (ms.effectAttr == attr && IsMilestoneClaimed(data, ms.id))
                total += ms.effectValue;
        }
        return total;
    }

    public static float GetPermanentMultiplier(SaveGameData data, string attr)
    {
        float total = 1f;
        foreach (var upgrade in SHOP_UPGRADES)
        {
            if (upgrade.attr != attr) continue;
            int level = GetUpgradeLevel(data, upgrade.attr);
            if (level <= 0) continue;
            if (upgrade.multiplier != 1f && upgrade.multiplier > 0f)
                total *= Mathf.Pow(upgrade.multiplier, level);
        }
        return total;
    }

    // ============================================================
    // 局内被动技能加成（临时，不持久化到存档）
    // ============================================================

    /// <summary>
    /// 添加局内被动技能加成（存储在 upgrades 字典中，以 "passive_" 前缀区分）
    /// </summary>
    public static void AddPermanentBonus(SaveGameData data, string key, float value)
    {
        string fullKey = "passive_" + key;
        int current = data.upgrades.Get(fullKey, 0);
        data.upgrades.Set(fullKey, current + Mathf.RoundToInt(value * 1000f));
        DebugHelper.Log($"[PermanentUpgradeStore] Added passive bonus: {fullKey} +{value} (stored as {Mathf.RoundToInt(value * 1000f)})");
    }

    /// <summary>
    /// 获取局内被动技能加成
    /// </summary>
    public static float GetPassiveBonus(SaveGameData data, string key)
    {
        string fullKey = "passive_" + key;
        return data.upgrades.Get(fullKey, 0) / 1000f;
    }

    /// <summary>
    /// 清除所有局内被动加成（游戏结束时调用）
    /// </summary>
    public static void ClearPassiveBonuses(SaveGameData data)
    {
        var keys = data.upgrades.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null && keys[i].StartsWith("passive_"))
            {
                data.upgrades.Set(keys[i], 0);
            }
        }
        DebugHelper.Log("[PermanentUpgradeStore] All passive bonuses cleared");
    }
}