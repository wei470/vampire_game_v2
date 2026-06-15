using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色升级配置 — 通用升级配置结构 + 配置加载器 + 角色配置接口。
/// 每个角色继承此类，定义自己的 gunEntries + upgradeEntries。
/// </summary>

#region 角色配置接口

public interface ICharacterConfig
{
    string CharacterId { get; }
    CharacterUpgradeOption[] GetUpgradeOptions();
    DotGunEntry[] GetGunEntries();
}

#endregion

#region 数据结构

[System.Serializable]
public struct DotGunEntry
{
    public string upgradeId;
    public StatusEffectType effectType;
    public string displayName;
    [TextArea(2, 3)] public string description;
    public Color color;
    public float cooldown;
    public int impactDmg;
    public float dotDps;
    public float dotDuration;
}

[System.Serializable]
public struct UpgradeEntry
{
    public string upgradeId;
    public string upgradeName;
    [TextArea(2, 3)] public string description;
    public CharacterUpgradeOption.UpgradeCategory category;
    public UpgradeRarity rarity;
    public float value1;
    public float value2;
    public float value3;
    public int maxStacks;
}

public enum UpgradeRarity
{
    Common,     // 灰色 40%
    Uncommon,   // 绿色 30%
    Rare,       // 蓝色 20%
    Epic        // 紫色 10%
}

#endregion

#region 配置基类

[CreateAssetMenu(fileName = "CharacterUpgradeConfig", menuName = "VampireGame/Character Upgrade Config")]
public class CharacterUpgradeConfig : ScriptableObject, ICharacterConfig
{
    [Header("角色信息")]
    public string characterId = "";
    public string displayName = "";
    public string description = "";
    public string passiveDescription = "";
    public Color characterColor = Color.white;

    [Header("子弹枪配置")]
    public DotGunEntry[] dotGunEntries = new DotGunEntry[0];

    [Header("升级选项配置")]
    public UpgradeEntry[] upgradeEntries = new UpgradeEntry[0];

    // ── ICharacterConfig 实现 ──
    string ICharacterConfig.CharacterId => characterId;
    CharacterUpgradeOption[] ICharacterConfig.GetUpgradeOptions() => BuildCustomUpgrades();
    DotGunEntry[] ICharacterConfig.GetGunEntries() => dotGunEntries;

    // ═══ 运行时查询 API ═══

    public DotGunEntry? GetDotGunEntry(string upgradeId)
    {
        if (dotGunEntries == null) return null;
        for (int i = 0; i < dotGunEntries.Length; i++)
            if (dotGunEntries[i].upgradeId == upgradeId) return dotGunEntries[i];
        return null;
    }

    public UpgradeEntry? GetUpgradeEntry(string upgradeId)
    {
        if (upgradeEntries == null) return null;
        for (int i = 0; i < upgradeEntries.Length; i++)
            if (upgradeEntries[i].upgradeId == upgradeId) return upgradeEntries[i];
        return null;
    }

    public bool IsDotGunUpgrade(string upgradeId) => GetDotGunEntry(upgradeId).HasValue;

    public CharacterUpgradeOption[] BuildCustomUpgrades()
    {
        var list = new List<CharacterUpgradeOption>(32);
        if (dotGunEntries != null)
            for (int i = 0; i < dotGunEntries.Length; i++)
            {
                var dg = dotGunEntries[i];
                list.Add(new CharacterUpgradeOption
                {
                    upgradeId = dg.upgradeId, upgradeName = dg.displayName,
                    description = dg.description, category = CharacterUpgradeOption.UpgradeCategory.DotType,
                    value1 = 0f, value2 = 0f, value3 = 0f, maxStacks = 0
                });
            }
        if (upgradeEntries != null)
            for (int i = 0; i < upgradeEntries.Length; i++)
            {
                var ue = upgradeEntries[i];
                list.Add(new CharacterUpgradeOption
                {
                    upgradeId = ue.upgradeId, upgradeName = ue.upgradeName,
                    description = ue.description, category = ue.category,
                    rarity = ue.rarity,
                    value1 = ue.value1, value2 = ue.value2, value3 = ue.value3, maxStacks = ue.maxStacks
                });
            }
        return list.ToArray();
    }
}

#endregion

#region 配置加载器

public static class CharacterConfigLoader
{
    private static readonly Dictionary<string, ICharacterConfig> _cache = new();

    public static ICharacterConfig Load(string characterId)
    {
        if (_cache.TryGetValue(characterId, out var cached)) return cached;
        var config = Resources.Load<CharacterUpgradeConfig>($"Configs/{characterId}UpgradeConfig");
        if (config != null) { _cache[characterId] = config; return config; }
        DebugHelper.LogWarning($"[CharacterConfigLoader] No config found for '{characterId}'");
        return null;
    }

    public static void ClearCache() { _cache.Clear(); }
}

#endregion
