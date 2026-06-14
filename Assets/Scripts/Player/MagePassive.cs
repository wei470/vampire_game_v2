using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 角色专属被动能力系统
/// 
/// 职责拆分：
/// - DetonateSystem: 引爆系统（蓄力/连锁/余烬/碎裂）
/// - MageUpgradeApplier: 升级效果应用 + 协同 + 进化 + 里程碑
/// - MagePassive: DOT枪管理 + 属性访问 + 子弹发射
/// </summary>
public partial class MagePassive : CharacterPassiveBase, IDotCharacterPassive
{
    // ── IDotCharacterPassive 实现 ──
    public override string CharacterId => "mage";
    public override string DisplayName => "DOT 法师";
    public DetonateSystem GetDetonateSystem() => _detonateSystem;

    [Header("Mage 被动参数")]
    [SerializeField] private float _dotDurationBonus = 0.2f;
    [SerializeField] private float _dotCritMultiplier = 2f;

    [Header("DOT 增强属性")]
    [SerializeField] private float _corrosionArmorReduction = 0.1f;
    [SerializeField] private int _erosionArmorPenetration = 0;
    [SerializeField] private int _curseSpreadTargets = 1;

    [Header("P1 深度玩法属性")]
    [SerializeField] private float _pandemicBonus = 0f;
    [SerializeField] private float _dualWieldBonus = 0f;
    [SerializeField] private float _chargeSpeedBonus = 0f;
    [SerializeField] private float _chargeDamageBonus = 0f;

    [Header("P2 协同/趣味属性")]
    [SerializeField] private float _toxicologyCritBonus = 0f;
    [SerializeField] private float _lightJudgmentBonus = 0f;
    [SerializeField] private int _staticFieldStacks = 0;
    [SerializeField] private float _frostExplosionPct = 0f;

    [Header("子弹增强扩展属性")]
    [SerializeField] private float _ammoSpeedBonus = 0f;
    [SerializeField] private float _ammoRangeBonus = 0f;

    [Header("生存向属性")]
    [SerializeField] private float _elementalShieldHp = 0f;

    [Header("P3 终极/高级属性")]
    [SerializeField] private float _doomsdayThreshold = 0f;
    [SerializeField] private int _shatterBoostFragments = 0;
    [SerializeField] private float _shatterBoostDmg = 0f;

    private List<DotGunState> _dotGuns = new List<DotGunState>();

    // ── 里程碑系统 ──
    private bool _elementMasterTriggered = false;
    public bool ElementMasterTriggered { get => _elementMasterTriggered; set => _elementMasterTriggered = value; }

    // ── 协同系统 ──
    private HashSet<string> _activeSynergies = new HashSet<string>();

    // ── DOT 进化系统 ──
    private HashSet<StatusEffectType> _evolvedTypes = new HashSet<StatusEffectType>();

    // ── 拆分组件 ──
    private DetonateSystem _detonateSystem;

    // ── 公共属性（委托到 DetonateSystem）──
    public float DetonateCooldown => _detonateSystem != null ? _detonateSystem.DetonateCooldown : 12f;
    public float DetonateCooldownRemaining => _detonateSystem != null ? _detonateSystem.DetonateCooldownRemaining : 0f;
    public bool DetonateReady => _detonateSystem != null ? _detonateSystem.DetonateReady : true;
    public float DetonateMultiplier { get => _detonateSystem != null ? _detonateSystem.DetonateMultiplier : 3f; set { if (_detonateSystem != null) _detonateSystem.DetonateMultiplier = value; } }
    public float DetonateCooldownValue { get => _detonateSystem != null ? _detonateSystem.DetonateCooldown : 12f; set { if (_detonateSystem != null) _detonateSystem.DetonateCooldownValue = value; } }
    public float DetonateCooldownReduction { get; set; } = 0f;
    public bool IsCharging => _detonateSystem != null && _detonateSystem.IsCharging;
    public float ChargeProgress => _detonateSystem != null ? _detonateSystem.ChargeProgress : 0f;
    public float ChargeMultiplier => _detonateSystem != null ? _detonateSystem.ChargeMultiplier : 1f;
    public bool IsChainDetonateActive => _detonateSystem != null && _detonateSystem.IsChainDetonateActive;
    public int LastDetonateEnemyCount => _detonateSystem != null ? _detonateSystem.LastDetonateEnemyCount : 0;
    public List<DotGunState> DotGuns => _dotGuns;
    public HashSet<string> ActiveSynergies => _activeSynergies;
    public bool IsEvolved(StatusEffectType type) => _evolvedTypes.Contains(type);
    public HashSet<StatusEffectType> EvolvedTypes => _evolvedTypes;

    // ── DOT 增强属性访问器 ──
    public float CorrosionArmorReduction { get => _corrosionArmorReduction; set => _corrosionArmorReduction = value; }
    public int ErosionArmorPenetration { get => _erosionArmorPenetration; set => _erosionArmorPenetration = value; }
    public int CurseSpreadTargets { get => _curseSpreadTargets; set => _curseSpreadTargets = value; }

    // ── P1 深度玩法属性访问器 ──
    public float PandemicBonus { get => _pandemicBonus; set => _pandemicBonus = value; }
    public float DualWieldBonus { get => _dualWieldBonus; set => _dualWieldBonus = value; }
    public float ChargeSpeedBonus { get => _chargeSpeedBonus; set => _chargeSpeedBonus = value; }
    public float ChargeDamageBonus { get => _chargeDamageBonus; set => _chargeDamageBonus = value; }

    // ── P2 协同/趣味属性访问器 ──
    public float ToxicologyCritBonus { get => _toxicologyCritBonus; set => _toxicologyCritBonus = value; }
    public float LightJudgmentBonus { get => _lightJudgmentBonus; set => _lightJudgmentBonus = value; }
    public int StaticFieldStacks { get => _staticFieldStacks; set => _staticFieldStacks = value; }
    public float FrostExplosionPct { get => _frostExplosionPct; set => _frostExplosionPct = Mathf.Min(0.10f, value); }

    // ── 子弹增强扩展访问器 ──
    public float AmmoSpeedBonus { get => _ammoSpeedBonus; set => _ammoSpeedBonus = value; }
    public float AmmoRangeBonus { get => _ammoRangeBonus; set => _ammoRangeBonus = value; }

    // ── 生存向属性访问器 ──
    public float ElementalShieldHp { get => _elementalShieldHp; set => _elementalShieldHp = value; }

    // ── P3 终极/高级属性访问器 ──
    public float DoomsdayThreshold { get => _doomsdayThreshold; set => _doomsdayThreshold = Mathf.Min(0.30f, value); }
    public int ShatterBoostFragments { get => _shatterBoostFragments; set => _shatterBoostFragments = value; }
    public float ShatterBoostDmg { get => _shatterBoostDmg; set => _shatterBoostDmg = value; }

    // ── 综合查询方法 ──
    public float GetTotalDotCritChance() => GetDotCritChance() + _toxicologyCritBonus;
    public int GetTotalPenetrate() => _penetrateCount;
    public float GetDualWieldMultiplier() => Mathf.Max(0.2f, 1f - _attackSpeedBonus - _dualWieldBonus);

    // ── 进化系统新增属性 ──
    public override float DotDamageMultiplier { get; set; } = 0f;
    public override float CritChanceBonus { get; set; }

    public float GetDotDurationMultiplier() => 1f + _dotDurationBonus;
    public float DotDurationMultiplier => GetDotDurationMultiplier();
    public float CritMultiplier => _dotCritMultiplier;
    public float CritChance => GetDotCritChance();

    public float GetDotDamageMultiplier()
    {
        float mult = 1f + DotDamageMultiplier;
        if (_elementMasterTriggered) mult += 0.2f;
        if (_detonateSystem != null && _detonateSystem.IsChainDetonateActive) mult *= 2f;
        return mult;
    }

    public float GetDotCritChance()
    {
        float baseCrit = 0.05f;
        baseCrit += SaveManager.Instance?.GetPermanentBonus("crit_chance") ?? 0f;
        baseCrit += _dotGuns.Count * 0.02f;
        return baseCrit;
    }

    private float _lastAttackSpeedMult = 1f;

    public float GetDotCritMultiplier() => _dotCritMultiplier;
    public override float GetAttackSpeedMultiplier() => Mathf.Max(0.2f, 1f - _attackSpeedBonus);

    public float GetChargeMoveSpeedMultiplier()
    {
        return _detonateSystem != null ? _detonateSystem.GetChargeMoveSpeedMultiplier() : 1f;
    }

    public void AddDotDurationBonus(float bonus) { _dotDurationBonus += bonus; }

    public void SyncDotDamageMultiplierToAll()
    {
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;
        float dmgMult = GetDotDamageMultiplier();
        for (int i = 0; i < enemies.Count; i++)
        {
            var sem = enemies[i]?.GetComponent<StatusEffectManager>();
            if (sem != null) sem.DotDamageMultiplier = dmgMult;
        }
    }

    public bool HasPlagueSynergy => _activeSynergies.Contains("plague");
    public bool HasFrozenBladeSynergy => _activeSynergies.Contains("frozen_blade");
    public bool HasBulletStormSynergy => _activeSynergies.Contains("bullet_storm");
    public bool HasJudgmentDaySynergy => _activeSynergies.Contains("judgment_day");

    // ── 升级配置 ──
    private MageUpgradeConfig _upgradeConfig;
    public void SetUpgradeConfig(MageUpgradeConfig config) { _upgradeConfig = config; }
    public MageUpgradeConfig GetUpgradeConfig() => _upgradeConfig;

    protected override void Awake()
    {
        base.Awake();
        _detonateSystem = gameObject.AddComponent<DetonateSystem>();
        _detonateSystem.Init(this);
        UnlockDotGun(StatusEffectType.Poison, new Color(0.1f, 0.8f, 0.2f), 1.5f, 0, 2f, 5f);
    }

    /// <summary>
    /// 解锁/升级 DOT 子弹
    /// </summary>
    public void UnlockDotGun(StatusEffectType type, Color color, float cooldown, int impactDmg, float dotDps, float dotDuration)
    {
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            if (_dotGuns[i].effectType == type)
            {
                var gun = _dotGuns[i];
                gun.dotDps *= 1.15f;
                gun.impactDamage = Mathf.RoundToInt(gun.impactDamage * 1.1f);
                gun.upgradeLevel++;
                DebugHelper.Log($"[MagePassive] Upgraded {type} DOT gun to Lv{gun.upgradeLevel}");
                if (gun.upgradeLevel >= 5 && !_evolvedTypes.Contains(type))
                    MageUpgradeApplier.CheckEvolution(this, type);
                return;
            }
        }

        _dotGuns.Add(new DotGunState
        {
            effectType = type, color = color, cooldown = cooldown,
            impactDamage = impactDmg, dotDps = dotDps, dotDuration = dotDuration,
            upgradeLevel = 1
        });
        DebugHelper.Log($"[MagePassive] Unlocked {type} DOT gun! (color={color})");
        MageUpgradeApplier.CheckMilestones(this);
    }

    public void EnhanceAllDotGuns(float dpsMultiplier)
    {
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            _dotGuns[i].dotDps *= (1f + dpsMultiplier);
        }
    }

    public void ClearAllDotGuns()
    {
        _dotGuns.Clear();
        _elementMasterTriggered = false;
    }

    // ═══ 升级应用（委托给 MageUpgradeApplier）═══
    // Update/GetFireDirection/SpawnDotBullet/ApplyUpgradeVisual/ApplyBulletSizeBonus → MagePassive.Firing.cs

    public override bool ApplyUpgrade(string upgradeId)
    {
        return MageUpgradeApplier.ApplyUpgrade(this, upgradeId);
    }

}
