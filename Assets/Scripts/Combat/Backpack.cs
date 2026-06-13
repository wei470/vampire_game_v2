using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 背包系统 — 管理玩家装备和物品。
/// </summary>
public static class Backpack
{
    private static List<EquipmentInstance> _inventory = new List<EquipmentInstance>(24);
    private static Dictionary<EquipmentSlot, EquipmentInstance> _equipped = new Dictionary<EquipmentSlot, EquipmentInstance>();

    private const int MAX_SLOTS = 24;

    public static List<EquipmentInstance> Inventory => _inventory;
    public static IReadOnlyDictionary<EquipmentSlot, EquipmentInstance> Equipped => _equipped;

    /// <summary>
    /// 添加装备到背包
    /// </summary>
    public static bool AddItem(EquipmentInstance item)
    {
        if (_inventory.Count >= MAX_SLOTS) return false;
        _inventory.Add(item);
        DebugHelper.Log($"[Backpack] Added: {item.displayName} ({item.rarity})");
        return true;
    }

    /// <summary>
    /// 装备物品
    /// </summary>
    public static bool Equip(EquipmentInstance item, ICharacterPassive character)
    {
        if (!_inventory.Contains(item)) return false;

        // 卸下旧装备
        if (_equipped.TryGetValue(item.slot, out var old))
        {
            var oldEquip = FindEquipment(old.equipmentId);
            oldEquip?.OnUnequip(character);
            _inventory.Add(old);
        }

        // 装备新物品
        _inventory.Remove(item);
        _equipped[item.slot] = item;

        var equip = FindEquipment(item.equipmentId);
        equip?.OnEquip(character);

        DebugHelper.Log($"[Backpack] Equipped: {item.displayName} in {item.slot}");
        return true;
    }

    /// <summary>
    /// 卸下装备
    /// </summary>
    public static bool Unequip(EquipmentSlot slot, ICharacterPassive character)
    {
        if (!_equipped.TryGetValue(slot, out var item)) return false;
        if (_inventory.Count >= MAX_SLOTS) return false;

        var equip = FindEquipment(item.equipmentId);
        equip?.OnUnequip(character);

        _equipped.Remove(slot);
        _inventory.Add(item);

        DebugHelper.Log($"[Backpack] Unequipped: {item.displayName} from {slot}");
        return true;
    }

    /// <summary>
    /// 分解装备获得材料
    /// </summary>
    public static int Dismantle(EquipmentInstance item)
    {
        if (!_inventory.Contains(item)) return 0;
        _inventory.Remove(item);

        int material = item.rarity switch
        {
            EquipmentRarity.Common => 1,
            EquipmentRarity.Uncommon => 3,
            EquipmentRarity.Rare => 10,
            EquipmentRarity.Epic => 30,
            EquipmentRarity.Legendary => 100,
            _ => 1
        };

        DebugHelper.Log($"[Backpack] Dismantled {item.displayName} → {material} materials");
        return material;
    }

    /// <summary>
    /// 清空背包（新游戏时调用）
    /// </summary>
    public static void Clear()
    {
        _inventory.Clear();
        _equipped.Clear();
    }

    private static IEquipment FindEquipment(string equipmentId)
    {
        if (string.IsNullOrEmpty(equipmentId)) return null;
        Debug.LogWarning($"[Backpack] FindEquipment: no IEquipment instance for '{equipmentId}'. Equip/unequip effects not applied — equipment uses affix-based system.");
        return null;
    }
}
