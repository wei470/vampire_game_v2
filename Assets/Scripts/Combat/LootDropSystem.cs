using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 掉落系统 — 敌人死亡时根据概率和品质掉落装备。
/// </summary>
public static class LootDropSystem
{
    private static readonly EquipmentAffix[] _allAffixes = new EquipmentAffix[]
    {
        new EquipmentAffix { affixId = "dot_damage", displayName = "火焰之", value = 0.15f, description = "DOT伤害+{0}%" },
        new EquipmentAffix { affixId = "crit_chance", displayName = "暴击之", value = 0.05f, description = "暴击率+{0}%" },
        new EquipmentAffix { affixId = "attack_speed", displayName = "迅捷之", value = 0.10f, description = "攻速+{0}%" },
        new EquipmentAffix { affixId = "lifesteal", displayName = "吸血之", value = 0.03f, description = "伤害回复{0}%" },
        new EquipmentAffix { affixId = "max_hp", displayName = "坚韧之", value = 30f, description = "最大HP+{0}" },
        new EquipmentAffix { affixId = "luck", displayName = "幸运之", value = 0.15f, description = "掉落率+{0}%" },
    };

    /// <summary>
    /// 敌人死亡时调用 — 判断是否掉落装备
    /// </summary>
    /// <returns>掉落的装备实例，null=无掉落</returns>
    public static EquipmentInstance TryDrop(bool isElite, bool isBoss, int wave)
    {
        float dropChance = GetDropChance(isElite, isBoss);
        dropChance *= DifficultyManager.GetDropMult();

        if (Random.value > dropChance) return null;

        EquipmentRarity rarity = RollRarity(isElite, isBoss, wave);
        EquipmentSlot slot = (EquipmentSlot)Random.Range(0, 4);
        int affixCount = rarity switch
        {
            EquipmentRarity.Common => 1,
            EquipmentRarity.Uncommon => 1,
            EquipmentRarity.Rare => 2,
            EquipmentRarity.Epic => 2,
            EquipmentRarity.Legendary => 3,
            _ => 1
        };

        var affixes = RollAffixes(affixCount, rarity);
        string name = GenerateName(rarity, affixes);

        return new EquipmentInstance
        {
            equipmentId = $"equip_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}",
            rarity = rarity,
            slot = slot,
            affixes = affixes,
            displayName = name,
            description = GenerateDescription(affixes)
        };
    }

    private static float GetDropChance(bool isElite, bool isBoss)
    {
        if (isBoss) return 1f;
        if (isElite) return 0.15f;
        return 0.01f;
    }

    private static EquipmentRarity RollRarity(bool isElite, bool isBoss, int wave)
    {
        float roll = Random.value;
        if (isBoss)
        {
            if (roll < 0.3f) return EquipmentRarity.Epic;
            if (roll < 0.8f) return EquipmentRarity.Rare;
            return EquipmentRarity.Uncommon;
        }
        if (isElite)
        {
            if (roll < 0.1f) return EquipmentRarity.Rare;
            if (roll < 0.5f) return EquipmentRarity.Uncommon;
            return EquipmentRarity.Common;
        }
        if (roll < 0.05f) return EquipmentRarity.Uncommon;
        return EquipmentRarity.Common;
    }

    private static EquipmentAffix[] RollAffixes(int count, EquipmentRarity rarity)
    {
        var result = new EquipmentAffix[count];
        var available = new List<EquipmentAffix>(_allAffixes);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int idx = Random.Range(0, available.Count);
            var baseAffix = available[idx];
            available.RemoveAt(idx);

            float rarityMult = rarity switch
            {
                EquipmentRarity.Uncommon => 1.2f,
                EquipmentRarity.Rare => 1.5f,
                EquipmentRarity.Epic => 2f,
                EquipmentRarity.Legendary => 3f,
                _ => 1f
            };

            result[i] = new EquipmentAffix
            {
                affixId = baseAffix.affixId,
                displayName = baseAffix.displayName,
                value = baseAffix.value * rarityMult,
                description = baseAffix.description
            };
        }
        return result;
    }

    private static string GenerateName(EquipmentRarity rarity, EquipmentAffix[] affixes)
    {
        string prefix = affixes.Length > 0 ? affixes[0].displayName : "";
        string baseName = rarity switch
        {
            EquipmentRarity.Common => "普通装备",
            EquipmentRarity.Uncommon => "精良装备",
            EquipmentRarity.Rare => "稀有装备",
            EquipmentRarity.Epic => "史诗装备",
            EquipmentRarity.Legendary => "传说装备",
            _ => "装备"
        };
        return $"{prefix}{baseName}";
    }

    private static string GenerateDescription(EquipmentAffix[] affixes)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var affix in affixes)
        {
            sb.AppendLine(string.Format(affix.description, (affix.value * 100).ToString("F0")));
        }
        return sb.ToString().TrimEnd();
    }
}
