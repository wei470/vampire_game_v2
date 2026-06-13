using UnityEngine;

/// <summary>
/// 装备稀有度
/// </summary>
public enum EquipmentRarity
{
    Common,     // 白色
    Uncommon,   // 绿色
    Rare,       // 蓝色
    Epic,       // 紫色
    Legendary   // 橙色
}

/// <summary>
/// 装备槽位
/// </summary>
public enum EquipmentSlot
{
    Weapon,     // 武器
    Armor,      // 护甲
    Accessory,  // 饰品
    Relic       // 圣物
}

/// <summary>
/// 装备接口 — 所有装备实现此接口
/// </summary>
public interface IEquipment
{
    string EquipmentId { get; }
    string DisplayName { get; }
    string Description { get; }
    EquipmentRarity Rarity { get; }
    EquipmentSlot Slot { get; }
    void OnEquip(ICharacterPassive character);
    void OnUnequip(ICharacterPassive character);
}

/// <summary>
/// 装备词缀
/// </summary>
[System.Serializable]
public class EquipmentAffix
{
    public string affixId;
    public string displayName;
    public float value;
    public string description;
}

/// <summary>
/// 装备实例数据（运行时）
/// </summary>
[System.Serializable]
public class EquipmentInstance
{
    public string equipmentId;
    public EquipmentRarity rarity;
    public EquipmentSlot slot;
    public EquipmentAffix[] affixes;
    public string displayName;
    public string description;
}
