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
        public bool isSkillUpgrade;
        public GenericUpgradeType genericType;
        public CharacterUpgradeOption customOption;
        public SkillUpgradeSlot skillUpgrade;
        public BuildRoute buildRoute;
        public bool isRecommended;
    }

    /// <summary>
    /// 技能升级槽位
    /// </summary>
    public struct SkillUpgradeSlot
    {
        public BaseSkill skill;
    }

    public struct DotGunConfig
    {
        public StatusEffectType type; public Color color; public float cooldown;
        public int impactDmg; public float dotDps; public float dotDuration;
    }

    private MagePassive _magePassive;
    private MageUpgradeConfig _mageUpgradeConfig;
    private CharacterData _currentCharacter;
    private Dictionary<string, int> _customUpgradeStacks;

    public void Init(MagePassive magePassive, MageUpgradeConfig config, CharacterData character,
        Dictionary<string, int> upgradeStacks)
    {
        _magePassive = magePassive;
        _mageUpgradeConfig = config;
        _currentCharacter = character;
        _customUpgradeStacks = upgradeStacks;
    }

    /// <summary>
    /// 生成 3 个随机升级选项
    /// </summary>
    public UpgradeSlot[] GenerateOptions(WeaponController weaponController)
    {
        if (_currentCharacter == null)
            _currentCharacter = GameSceneBootstrap.CurrentCharacter;

        var allSlots = new List<UpgradeSlot>();

        // 1. 通用升级
        bool useGeneric = _currentCharacter == null || _currentCharacter.useGenericUpgrades;
        if (useGeneric)
        {
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.AttackUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.MaxHpUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.SpeedUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.ArmorUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.MagnetRangeUp });
        }

        // 2. 角色专属升级
        if (_currentCharacter != null && _currentCharacter.customUpgrades != null)
        {
            foreach (var upgrade in _currentCharacter.customUpgrades)
            {
                if (upgrade.maxStacks > 0)
                {
                    int currentStacks = 0;
                    _customUpgradeStacks.TryGetValue(upgrade.upgradeId, out currentStacks);
                    if (currentStacks >= upgrade.maxStacks) continue;
                }

                if (_magePassive == null)
                    _magePassive = GameReferences.Player?.GetComponent<MagePassive>();
                if (IsDotGunUpgrade(upgrade.upgradeId) && _magePassive != null)
                {
                    bool alreadyOwned = false;
                    var dotGuns = _magePassive.DotGuns;
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

                BuildRoute route = BuildPathRecommender.ClassifyUpgradeRoute(upgrade.upgradeId);
                allSlots.Add(new UpgradeSlot { isCustom = true, customOption = upgrade, buildRoute = route });
            }
        }

        // 3. 技能升级
        var skillMgr = GameReferences.Player?.GetComponent<PlayerSkillManager>();
        if (skillMgr != null)
        {
            foreach (var skill in skillMgr.ActiveSkills)
            {
                if (skill == null || skill.Data == null) continue;
                if (skill.CurrentLevel >= skill.Data.maxLevel) continue;
                allSlots.Add(new UpgradeSlot { isSkillUpgrade = true, skillUpgrade = new SkillUpgradeSlot { skill = skill } });
            }
        }

        if (allSlots.Count == 0)
        {
            allSlots.Add(new UpgradeSlot { genericType = GenericUpgradeType.AttackUp });
            allSlots.Add(new UpgradeSlot { genericType = GenericUpgradeType.MaxHpUp });
            allSlots.Add(new UpgradeSlot { genericType = GenericUpgradeType.SpeedUp });
        }

        CalculateRecommendations(allSlots);

        // Fisher-Yates 洗牌
        var arr = allSlots.ToArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = arr[i]; arr[i] = arr[j]; arr[j] = temp;
        }

        // 优先推荐选项
        var result = new UpgradeSlot[3];
        var recommendedList = new List<UpgradeSlot>();
        var normalList = new List<UpgradeSlot>();
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i].isRecommended) recommendedList.Add(arr[i]);
            else normalList.Add(arr[i]);
        }

        int idx = 0;
        if (recommendedList.Count > 0)
        {
            int ri = Random.Range(0, recommendedList.Count);
            result[idx++] = recommendedList[ri];
        }

        var normalArr = normalList.ToArray();
        for (int i = normalArr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = normalArr[i]; normalArr[i] = normalArr[j]; normalArr[j] = temp;
        }
        int ni = 0;
        while (idx < 3)
        {
            if (normalArr.Length > 0)
                result[idx] = normalArr[ni++ % normalArr.Length];
            else if (recommendedList.Count > 0)
                result[idx] = recommendedList[Random.Range(0, recommendedList.Count)];
            idx++;
        }

        return result;
    }

    /// <summary>
    /// 根据 upgradeId 分类 Build 路线
    /// </summary>
    public BuildRoute ClassifyUpgradeRoute(string upgradeId)
    {
        switch (upgradeId)
        {
            case "bleed": case "poison": case "burn": case "frostbite":
            case "corrosion": case "curse": case "agony": case "wither": case "erosion":
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
        if (_magePassive == null) return;

        int dotGunCount = _magePassive.DotGuns.Count;
        int dotEnhanceTypes = 0;
        if (_customUpgradeStacks.ContainsKey("corrosion")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("curse")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("agony")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("wither")) dotEnhanceTypes++;
        if (_customUpgradeStacks.ContainsKey("erosion")) dotEnhanceTypes++;
        float dotProgress = (dotGunCount / 4f) * 0.5f + (dotEnhanceTypes / 5f) * 0.5f;

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

        if (slot.isSkillUpgrade)
        {
            var skill = slot.skillUpgrade.skill;
            var data = skill.Data;
            int nextLevel = skill.CurrentLevel + 1;
            int newDmg = data.GetDamageAtLevel(nextLevel);
            float newCd = data.GetCooldownAtLevel(nextLevel);
            string desc = $"⬆ Lv.{nextLevel}\n";
            if (newDmg > 0) desc += $"DMG: {data.GetDamageAtLevel(skill.CurrentLevel)} → {newDmg}\n";
            desc += $"CD: {data.GetCooldownAtLevel(skill.CurrentLevel):F1}s → {newCd:F1}s";
            return $"🔮 {data.skillName} {desc}";
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
        return upgradeId == "bleed" || upgradeId == "poison" || upgradeId == "burn" || upgradeId == "frostbite";
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
            default: return null;
        }
    }
}