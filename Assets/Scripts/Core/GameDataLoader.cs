#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 游戏数据加载器 — 负责加载角色/武器/技能数据和 Mage 升级配置
/// 从 GameSceneBootstrap 拆分而来，职责单一：只负责数据加载
/// </summary>
public class GameDataLoader
{
    // ── 加载的数据 ──
    public CharacterData[] Characters { get; private set; }
    public WeaponData[] Weapons { get; private set; }
    public SkillData[] Skills { get; private set; }
    public MageUpgradeConfig MageUpgradeConfig { get; private set; }

    /// <summary>
    /// 加载所有选择数据（角色/武器/技能/Mage升级配置）
    /// </summary>
    public void LoadSelectionData()
    {
        // ── 加载角色（从 ScriptableObjects/Characters/*.asset）──
        var charNames = new string[] {
            "Char_warrior", "Char_mage", "Char_ranger", "Char_vampire",
            "Char_assassin", "Char_paladin", "Char_necromancer", "Char_berserker"
        };
        var charList = new List<CharacterData>();
        foreach (var name in charNames)
        {
            var c = LoadAsset<CharacterData>($"Assets/ScriptableObjects/Characters/{name}.asset");
            if (c != null) charList.Add(c);
        }
        Characters = charList.Count > 0 ? charList.ToArray() : new CharacterData[0];

        // ── 加载武器（运行时创建，因为 Weapons 目录为空）──
        Weapons = CreateDefaultWeapons();

        // ── 加载技能（从 ScriptableObjects/Skills/*.asset）──
        var skillNames = new string[] {
            "Skill_WindWave", "Skill_Berserk", "Skill_TheWorld", "Skill_Teleport",
            "Skill_DeathAura", "Skill_LightningStorm", "Skill_GravityWell", "Skill_FrostNova"
        };
        var skillList = new List<SkillData>();
        foreach (var name in skillNames)
        {
            var s = LoadAsset<SkillData>($"Assets/ScriptableObjects/Skills/{name}.asset");
            if (s != null) skillList.Add(s);
        }
        Skills = skillList.Count > 0 ? skillList.ToArray() : new SkillData[0];

        // ── #38 加载 MageUpgradeConfig ──
        MageUpgradeConfig = LoadAsset<MageUpgradeConfig>("Assets/ScriptableObjects/Config/MageUpgradeConfig.asset");
        if (MageUpgradeConfig == null)
        {
            // 运行时创建默认配置（与编辑器中的 .asset 一致）
            MageUpgradeConfig = ScriptableObject.CreateInstance<MageUpgradeConfig>();
            DebugHelper.Log("[GameDataLoader] MageUpgradeConfig: runtime default created");
        }

        // ── 为 Mage 角色运行时注入专属升级和描述 ──
        foreach (var c in Characters)
        {
            if (c != null && (c.characterId == "mage" || c.characterName.ToLower().Contains("mage")))
            {
                // 从配置读取描述和颜色
                c.description = MageUpgradeConfig.description;
                c.passiveDescription = MageUpgradeConfig.passiveDescription;
                c.characterColor = MageUpgradeConfig.characterColor;

                // 使用配置生成升级选项（替代硬编码的 CreateMageUpgrades）
                if (c.customUpgrades == null || c.customUpgrades.Length == 0)
                {
                    c.customUpgrades = MageUpgradeConfig.BuildCustomUpgrades();
                    c.useGenericUpgrades = false; // Mage 只用专属升级
                    DebugHelper.Log($"[GameDataLoader] Injected {c.customUpgrades.Length} Mage custom upgrades from config");
                }
            }
        }

        // ── 确保至少有选项 ──
        if (Characters.Length == 0) Characters = new CharacterData[] { CreateDefaultCharacter() };
        if (Weapons.Length == 0) Weapons = CreateDefaultWeapons();
        if (Skills.Length == 0) Skills = new SkillData[] { CreateDefaultSkill() };

        DebugHelper.Log($"[GameDataLoader] Loaded: {Characters.Length} characters, {Weapons.Length} weapons, {Skills.Length} skills");
    }

    /// <summary>
    /// 创建默认武器数据（运行时）
    /// </summary>
    public WeaponData[] CreateDefaultWeapons()
    {
        var weapons = new WeaponData[8];

        weapons[0] = CreateWeapon("Bullet", "Rapid fire bullets", WeaponData.ProjectileType.Bullet,
            damage: 15, cooldown: 0.4f, speed: 14f, pierce: 1, color: new Color(0.3f, 0.8f, 1f));

        weapons[1] = CreateWeapon("Lightning", "Chain lightning that jumps between enemies", WeaponData.ProjectileType.ChainLightning,
            damage: 25, cooldown: 1.2f, speed: 0f, pierce: 1, color: new Color(0.5f, 0.5f, 1f));

        weapons[2] = CreateWeapon("Shockwave", "Expanding ring of damage", WeaponData.ProjectileType.Shockwave,
            damage: 20, cooldown: 1.5f, speed: 8f, pierce: 99, color: new Color(1f, 0.7f, 0.3f));

        weapons[3] = CreateWeapon("Homing Missile", "Tracking missiles", WeaponData.ProjectileType.HomingMissile,
            damage: 30, cooldown: 0.8f, speed: 10f, pierce: 1, color: new Color(1f, 0.4f, 0.4f));

        weapons[4] = CreateWeapon("Mine Trap", "Explosive mines on the ground", WeaponData.ProjectileType.MineTrap,
            damage: 40, cooldown: 2f, speed: 0f, pierce: 99, color: new Color(0.8f, 0.2f, 0.2f));

        weapons[5] = CreateWeapon("Flamethrower", "Continuous fire stream", WeaponData.ProjectileType.Flamethrower,
            damage: 8, cooldown: 0.1f, speed: 10f, pierce: 3, color: new Color(1f, 0.5f, 0f));

        weapons[6] = CreateWeapon("Frost Orb", "Slow and damage enemies in area", WeaponData.ProjectileType.FrostOrb,
            damage: 15, cooldown: 1.8f, speed: 6f, pierce: 99, color: new Color(0.5f, 0.8f, 1f));

        weapons[7] = CreateWeapon("Venom Dart", "Poison darts with DOT", WeaponData.ProjectileType.VenomDart,
            damage: 12, cooldown: 0.6f, speed: 12f, pierce: 2, color: new Color(0.3f, 0.8f, 0.3f));

        return weapons;
    }

    private WeaponData CreateWeapon(string name, string desc, WeaponData.ProjectileType type,
        int damage, float cooldown, float speed, int pierce, Color color)
    {
        var w = ScriptableObject.CreateInstance<WeaponData>();
        w.weaponName = name;
        w.description = desc;
        w.projectileType = type;
        w.baseDamage = damage;
        w.cooldown = cooldown;
        w.projectileSpeed = speed;
        w.pierce = pierce;
        w.projectileColor = color;
        return w;
    }

    private CharacterData CreateDefaultCharacter()
    {
        var c = ScriptableObject.CreateInstance<CharacterData>();
        c.characterName = "Default";
        c.description = "Default character";
        c.maxHP = 100;
        c.moveSpeed = 20f;
        c.armor = 0;
        c.attackDamage = 10;
        c.characterColor = Color.blue;
        return c;
    }

    private SkillData CreateDefaultSkill()
    {
        var s = ScriptableObject.CreateInstance<SkillData>();
        s.skillName = "Default Skill";
        s.description = "Default skill";
        s.baseDamage = 10;
        s.cooldown = 10f;
        return s;
    }

    private T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
        return null;
#endif
    }
}