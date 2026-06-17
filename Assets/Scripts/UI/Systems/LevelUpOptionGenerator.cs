using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 升级选项生成器 — 负责生成升级选项、Build路线分类、推荐计算
/// 从 LevelUpUI 拆分而来
/// </summary>
public class LevelUpOptionGenerator
{
    /// <summary>
    /// 通用升级选项类型
    /// </summary>
    public enum GenericUpgradeType
    {
        AttackUp, MaxHpUp, SpeedUp, ArmorUp, MagnetRangeUp,
        WeaponDamageUp, WeaponPierceUp, WeaponCooldownDown, WeaponRangeUp
    }

    /// <summary>
    /// #7 Build 路线分类
    /// </summary>
    public enum BuildRoute
    {
        None, DotType, Detonate, Bullet
    }

    /// <summary>
    /// 升级选项槽位
    /// </summary>
    public struct UpgradeSlot
    {
        public bool isCustom;
        public GenericUpgradeType genericType;
        public CharacterUpgradeOption customOption;
        public BuildRoute buildRoute;
        public bool isRecommended;
    }

    /// <summary>
    /// 技能升级槽位
    /// </summary>
    public struct DotGunConfig
    {
        public StatusEffectType type; public Color color; public float cooldown;
        public int impactDmg; public float dotDps; public float dotDuration;
    }

    private ICharacterPassive _characterPassive;
    private IDotCharacterPassive _dotCharacterPassive;
    private MageUpgradeConfig _mageUpgradeConfig;
    private CharacterData _currentCharacter;
    private Dictionary<string, int> _customUpgradeStacks;

    public void Init(ICharacterPassive characterPassive, MageUpgradeConfig config, CharacterData character,
        Dictionary<string, int> upgradeStacks)
    {
        _characterPassive = characterPassive;
        _dotCharacterPassive = characterPassive as IDotCharacterPassive;
        _mageUpgradeConfig = config;
        _currentCharacter = character;
        _customUpgradeStacks = upgradeStacks;
    }

    /// <summary>
    /// 生成 3 个随机升级选项（按稀有度加权）
    /// </summary>
    public UpgradeSlot[] GenerateOptions(WeaponController weaponController)
    {
        if (_currentCharacter == null)
            _currentCharacter = GameSceneBootstrap.CurrentCharacter;

        var allSlots = new List<UpgradeSlot>();

        GenerateUpgradeOptions(allSlots);
        GenerateDotOptions(allSlots);
        GenerateWeaponOptions(allSlots);

        if (allSlots.Count == 0)
        {
            allSlots.Add(new UpgradeSlot { genericType = GenericUpgradeType.AttackUp });
            allSlots.Add(new UpgradeSlot { genericType = GenericUpgradeType.MaxHpUp });
            allSlots.Add(new UpgradeSlot { genericType = GenericUpgradeType.SpeedUp });
        }

        CalculateRecommendations(allSlots);

        // 按稀有度加权选择 3 个
        var result = new UpgradeSlot[3];
        var used = new HashSet<int>();
        for (int slot = 0; slot < 3 && slot < allSlots.Count; slot++)
        {
            int selected = WeightedSelect(allSlots, used);
            if (selected >= 0)
            {
                used.Add(selected);
                result[slot] = allSlots[selected];
            }
        }

        // 填充空位
        for (int i = 0; i < 3; i++)
            if (result[i].customOption.upgradeId == null && result[i].genericType == 0)
                result[i] = allSlots[0];

        return result;
    }

    private static int WeightedSelect(List<UpgradeSlot> slots, HashSet<int> used)
    {
        float totalWeight = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            if (used.Contains(i)) continue;
            totalWeight += GetRarityWeight(slots[i].customOption.rarity);
        }

        float roll = Random.Range(0f, totalWeight);
        float cum = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            if (used.Contains(i)) continue;
            cum += GetRarityWeight(slots[i].customOption.rarity);
            if (roll <= cum) return i;
        }

        for (int i = 0; i < slots.Count; i++)
            if (!used.Contains(i)) return i;
        return -1;
    }

    private static float GetRarityWeight(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 40f;
            case UpgradeRarity.Uncommon: return 30f;
            case UpgradeRarity.Rare: return 20f;
            case UpgradeRarity.Epic: return 10f;
            default: return 40f;
        }
    }

    /// <summary>
    /// 生成通用升级选项（攻/血/速/甲/磁铁）
    /// </summary>
    private void GenerateUpgradeOptions(List<UpgradeSlot> allSlots)
    {
        bool useGeneric = _currentCharacter == null || _currentCharacter.useGenericUpgrades;
        if (!useGeneric) return;

        allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.AttackUp });
        allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.MaxHpUp });
        allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.SpeedUp });
        allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.ArmorUp });
        allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.MagnetRangeUp });
    }

    /// <summary>
    /// 生成 DOT 类型选项（DOT枪解锁 + DOT增强 + 子弹强化）
    /// </summary>
    private void GenerateDotOptions(List<UpgradeSlot> allSlots)
    {
        if (_currentCharacter == null || _currentCharacter.customUpgrades == null) return;

        foreach (var upgrade in _currentCharacter.customUpgrades)
        {
            if (upgrade.maxStacks > 0)
            {
                int currentStacks = 0;
                _customUpgradeStacks.TryGetValue(upgrade.upgradeId, out currentStacks);
                if (currentStacks >= upgrade.maxStacks) continue;
            }

            if (_characterPassive == null)
                _characterPassive = GameReferences.CharacterPassive;
            if (_dotCharacterPassive == null)
                _dotCharacterPassive = _characterPassive as IDotCharacterPassive;
            if (IsDotGunUpgrade(upgrade.upgradeId) && _dotCharacterPassive != null)
            {
                bool alreadyOwned = false;
                var dotGuns = _dotCharacterPassive.DotGuns;
                var dotGunConfig = GetDotGunForUpgrade(upgrade.upgradeId);
                if (dotGunConfig.HasValue)
                {
                    foreach (var gun in dotGuns)
                    {
                        if (gun.effectType == dotGunConfig.Value.type) { alreadyOwned = true; break; }
                    }
                }
                if (alreadyOwned) continue;
            }

            var requiredType = GetRequiredDotGunType(upgrade.upgradeId);
            if (requiredType.HasValue && _dotCharacterPassive != null)
            {
                bool hasRequiredGun = false;
                foreach (var gun in _dotCharacterPassive.DotGuns)
                {
                    if (gun.effectType == requiredType.Value) { hasRequiredGun = true; break; }
                }
                if (!hasRequiredGun) continue;
            }

            BuildRoute route = BuildPathRecommender.ClassifyUpgradeRoute(upgrade.upgradeId);
            allSlots.Add(new UpgradeSlot { isCustom = true, customOption = upgrade, buildRoute = route });
        }
    }

    /// <summary>
    /// 生成武器/技能升级选项
    /// </summary>
    private void GenerateWeaponOptions(List<UpgradeSlot> allSlots)
    {
    }

    /// <summary>
    /// 根据 upgradeId 分类 Build 路线
    /// </summary>
    public BuildRoute ClassifyUpgradeRoute(string upgradeId)
    {
        switch (upgradeId)
        {
            case "bleed": case "poison": case "burn": case "frostbite":
            case "static": case "dark": case "light": case "wind":
            case "corrosion": case "curse": case "agony": case "wither": case "erosion":
            case "frost_winter": case "frost_deep_winter": case "frost_snowy_day": case "frost_frozen_hands":
            case "frost_ice_blade": case "frost_cold_bullet": case "frost_cold_embrace":
            case "burn_firmament": case "paralysis":
            case "wind_typhoon": case "wind_tornado": case "wind_wild": case "wind_storm": case "wind_swift":
                return BuildRoute.DotType;
            case "radiate": case "contaminate":
                return BuildRoute.Detonate;
            case "haste": case "barrage": case "ricochet":
                return BuildRoute.Bullet;
        }
        return BuildRoute.None;
    }

    /// <summary>
    /// 计算 Build 路线完成度并标记推荐选项
    /// </summary>
    public void CalculateRecommendations(List<UpgradeSlot> slots)
    {
        if (_dotCharacterPassive == null) return;

        int dotGunCount = _dotCharacterPassive.DotGuns.Count;
        int dotEnhanceTypes = 0;
        if (_customUpgradeStacks.ContainsKey("corrosion")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("curse")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("agony")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("wither")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("erosion")) dotEnhanceTypes++;
        float dotProgress = (dotGunCount / 7f) * 0.5f + (dotEnhanceTypes / 5f) * 0.5f;

        int detCount = 0;
        if (_customUpgradeStacks.ContainsKey("radiate")) detCount++;
        if (_customUpgradeStacks.ContainsKey("contaminate")) detCount++;
        float detProgress = detCount / 2f;

        int bulletCount = 0;
        if (_customUpgradeStacks.ContainsKey("haste")) bulletCount++;
        if (_customUpgradeStacks.ContainsKey("barrage")) bulletCount++;
        if (_customUpgradeStacks.ContainsKey("ricochet")) bulletCount++;
        float bulletProgress = bulletCount / 3f;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.buildRoute == BuildRoute.None) continue;
            bool recommend = false;
            switch (slot.buildRoute)
            {
                case BuildRoute.DotType: recommend = dotProgress > 0.5f; break;
                case BuildRoute.Detonate: recommend = detProgress > 0.5f; break;
                case BuildRoute.Bullet: recommend = bulletProgress > 0.5f; break;
            }
            if (recommend) { slot.isRecommended = true; slots[i] = slot; }
        }

        DebugHelper.Log($"[LevelUpOptionGenerator] DOT:{dotProgress:P0} Det:{detProgress:P0} Bullet:{bulletProgress:P0}");
    }

    /// <summary>
    /// 获取选项描述文字
    /// </summary>
    public string GetSlotDescription(UpgradeSlot slot)
    {
        if (slot.isCustom)
        {
            var opt = slot.customOption;
            int stacks = 0;
            _customUpgradeStacks.TryGetValue(opt.upgradeId, out stacks);
            string stackText = opt.maxStacks > 0 ? $" [{stacks}/{opt.maxStacks}]" : "";
            string routeTag = GetBuildRouteTag(slot.buildRoute, slot.isRecommended);
            string tagPrefix = !string.IsNullOrEmpty(routeTag) ? $"{routeTag}\n" : "";
            return $"{tagPrefix}{opt.upgradeName}{stackText}\n{opt.description}";
        }

        return GetGenericDescription(slot.genericType);
    }

    public string GetBuildRouteTag(BuildRoute route, bool isRecommended)
    {
        string tag = "";
        switch (route) { case BuildRoute.DotType: tag = "[DOT]"; break; case BuildRoute.Detonate: tag = "[DETO]"; break; case BuildRoute.Bullet: tag = "[BULLET]"; break; }
        if (isRecommended && route != BuildRoute.None) return $"💡{tag} 推荐";
        return tag;
    }

    public string GetGenericDescription(GenericUpgradeType type)
    {
        switch (type)
        {
            case GenericUpgradeType.AttackUp: return "+ATK\n攻击力 +15%";
            case GenericUpgradeType.MaxHpUp: return "+HP\n最大生命 +20%";
            case GenericUpgradeType.SpeedUp: return "+Speed\n移动速度 +10%";
            case GenericUpgradeType.ArmorUp: return "+Armor\n护甲 +3";
            case GenericUpgradeType.MagnetRangeUp: return "+Magnet\n拾取范围 +30%";
            default: return "???";
        }
    }

    public bool IsDotGunUpgrade(string upgradeId)
    {
        if (_mageUpgradeConfig != null)
            return _mageUpgradeConfig.IsDotGunUpgrade(upgradeId);
        return upgradeId == "bleed" || upgradeId == "poison" || upgradeId == "burn" || upgradeId == "frostbite"
            || upgradeId == "static" || upgradeId == "dark" || upgradeId == "light" || upgradeId == "wind";
    }

    /// <summary>
    /// 获取升级所需的DOT枪类型（null表示不需要特定DOT枪）
    /// 子弹强化必须在对应子弹解锁后才能选择
    /// </summary>
    public StatusEffectType? GetRequiredDotGunType(string upgradeId)
    {
        switch (upgradeId)
        {
            // 子弹增强需要对应子弹
            case "shadow_link": return StatusEffectType.Dark;
            case "light_judgment": return StatusEffectType.Light;
            case "static_field": return StatusEffectType.Static;
            case "frost_explosion": return StatusEffectType.Frostbite;
            case "frost_winter": case "frost_deep_winter": case "frost_snowy_day": case "frost_frozen_hands":
            case "frost_ice_blade": case "frost_cold_bullet": case "frost_cold_embrace":
                return StatusEffectType.Frostbite;
            case "burn_firmament": return StatusEffectType.Burn;
            case "paralysis": return StatusEffectType.Static;
            case "wind_typhoon": case "wind_tornado": case "wind_wild": case "wind_storm": case "wind_swift":
                return StatusEffectType.WindErosion;
            // DOT增强需要至少1种DOT子弹
            case "corrosion": case "curse": case "agony": case "wither": case "erosion":
                return StatusEffectType.Poison; // 占位，实际只需检查有DOT枪
            // 引爆增强需要至少1种DOT子弹
            case "radiate": case "contaminate":
                return StatusEffectType.Poison; // 占位
            default: return null;
        }
    }

    public DotGunConfig? GetDotGunForUpgrade(string upgradeId)
    {
        if (_mageUpgradeConfig != null)
        {
            var entry = _mageUpgradeConfig.GetDotGunEntry(upgradeId);
            if (entry.HasValue)
            {
                var e = entry.Value;
                return new DotGunConfig { type = e.effectType, color = e.color, cooldown = e.cooldown, impactDmg = e.impactDmg, dotDps = e.dotDps, dotDuration = e.dotDuration };
            }
            return null;
        }
        switch (upgradeId)
        {
            case "bleed": return new DotGunConfig { type = StatusEffectType.Bleed, cooldown = 1.0f, impactDmg = 3, dotDps = 2f, dotDuration = 4f };
            case "poison": return new DotGunConfig { type = StatusEffectType.Poison, cooldown = 2.0f, dotDps = 3f, dotDuration = 5f };
            case "burn": return new DotGunConfig { type = StatusEffectType.Burn, cooldown = 0.2f, impactDmg = 2, dotDps = 2f, dotDuration = 3f };
            case "frostbite": return new DotGunConfig { type = StatusEffectType.Frostbite, cooldown = 2.0f, impactDmg = 6, dotDps = 2f, dotDuration = 3f };
            case "static": return new DotGunConfig { type = StatusEffectType.Static, cooldown = 1.0f, impactDmg = 5 };
            case "dark": return new DotGunConfig { type = StatusEffectType.Dark, cooldown = 3.0f };
            case "light": return new DotGunConfig { type = StatusEffectType.Light, cooldown = 5.0f };
            default: return null;
        }
    }
}