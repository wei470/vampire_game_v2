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
public partial class MagePassive : MonoBehaviour
{
    [Header("Mage 被动参数")]
    [SerializeField] private float _dotDurationBonus = 0.2f;
    [SerializeField] private float _dotCritMultiplier = 2f;

    [Header("DOT 增强属性")]
    [SerializeField] private float _corrosionArmorReduction = 0.1f;
    [SerializeField] private int _curseSpreadTargets = 1;
    [SerializeField] private float _dotFrequencyBonus = 0f;
    [SerializeField] private float _dotCritBurstChance = 0f;

    [Header("子弹增强属性")]
    [SerializeField] private float _attackSpeedBonus = 0f;
    [SerializeField] private float _bulletSpeedBonus = 0f;
    [SerializeField] private int _bulletCountBonus = 0;
    [SerializeField] private float _ricochetChance = 0f;
    [SerializeField] private int _ricochetMaxBounces = 0;
    [SerializeField] private float _bulletSizeBonus = 0f;
    [SerializeField] private float _knockbackBonus = 0f;

    [Header("P0 新增强化属性")]
    [SerializeField] private float _dotSaturationBonus = 0f;   // 饱和：每种DOT加成
    [SerializeField] private float _detonateExtraPerDot = 0f;  // 元素引爆：每种DOT额外伤害
    [SerializeField] private float _dotLifestealPerTick = 0f;  // 吸血法术：每次DOT回复

    [Header("P1 深度玩法属性")]
    [SerializeField] private float _pandemicBonus = 0f;        // [已弃用]蔓延：传播效率加成
    [SerializeField] private int _chainReactionCount = 0;      // 连锁反应：二次引爆次数
    [SerializeField] private float _dualWieldBonus = 0f;       // [已弃用]双持：射速加成
    [SerializeField] private float _chargeSpeedBonus = 0f;     // 引爆蓄力速度加成
    [SerializeField] private float _chargeDamageBonus = 0f;    // 引爆蓄力伤害加成

    [Header("P2 协同/趣味属性")]
    [SerializeField] private float _resonanceChance = 0f;      // 共鸣：不消耗持续时间几率
    [SerializeField] private float _toxicologyCritBonus = 0f;  // [已弃用]剧毒天赋：DOT暴击率加成
    [SerializeField] private float _corruptTouchDebuff = 0f;   // 腐化之触：攻击力降低
    [SerializeField] private float _elementalStormDmg = 0f;    // 元素风暴：全局被动伤害
    [SerializeField] private float _elementalStormInterval = 0f; // 元素风暴：触发间隔
    [SerializeField] private float _shadowLinkRangeBonus = 0f; // 暗影链接：范围加成
    [SerializeField] private float _shadowLinkEffBonus = 0f;   // 暗影链接：效率加成
    [SerializeField] private float _lightJudgmentBonus = 0f;   // 光明审判：每层加成提升
    [SerializeField] private int _staticFieldStacks = 0;       // 静电领域：层数
    [SerializeField] private float _frostExplosionPct = 0f;    // 霜爆：最大生命百分比

    [Header("子弹增强扩展属性")]
    [SerializeField] private float _ammoSpeedBonus = 0f;       // 弹药精通：子弹速度加成
    [SerializeField] private float _ammoRangeBonus = 0f;       // 弹药精通：范围加成
    [SerializeField] private float _elementalAffinityBonus = 0f; // 元素亲和：每枪加成
    [SerializeField] private int _penetrateCount = 0;          // 贯穿弹：穿透数

    [Header("生存向属性")]
    [SerializeField] private float _elementalShieldHp = 0f;    // 元素护盾：额外最大生命
    [SerializeField] private float _phaseShiftDuration = 0f;   // 相位移动：无敌持续时间
    [SerializeField] private float _soulSiphonHeal = 0f;       // 灵魂虹吸：回血量
    [SerializeField] private float _soulSiphonSpeedDuration = 0f; // 灵魂虹吸：移速持续

    [Header("P3 终极/高级属性")]
    [SerializeField] private bool _eternalAgonyActive = false;  // 永恒痛苦：是否激活
    [SerializeField] private float _doomsdayThreshold = 0f;    // 末日审判：HP阈值
    [SerializeField] private float _annihilationZoneDmg = 0f;  // 湮灭领域：每秒伤害
    [SerializeField] private float _annihilationZoneDuration = 0f; // 湮灭领域：持续时间
    [SerializeField] private float _emberBoostBonus = 0f;      // 余烬强化：伤害加成
    [SerializeField] private float _emberBoostDuration = 0f;   // 余烬强化：持续加成
    [SerializeField] private int _shatterBoostFragments = 0;   // [已弃用]碎裂强化：碎片数量
    [SerializeField] private float _shatterBoostDmg = 0f;      // [已弃用]碎裂强化：碎片伤害

    private List<DotGunState> _dotGuns = new List<DotGunState>();
    private WeaponController _weaponController;

    // ── 里程碑系统 ──
    private bool _elementMasterTriggered = false;
    public bool ElementMasterTriggered { get => _elementMasterTriggered; set => _elementMasterTriggered = value; }

    // ── 协同系统 ──
    private HashSet<string> _activeSynergies = new HashSet<string>();

    // ── 元素融合系统 ──
    private HashSet<string> _completedFusions = new HashSet<string>();
    private float _fusionDpsMultiplier = 1f;

    /// <summary>已完成的融合列表（供 LevelUpUI 查询）</summary>
    public HashSet<string> CompletedFusions => _completedFusions;

    /// <summary>融合 DPS 倍率</summary>
    public float FusionDpsMultiplier => _fusionDpsMultiplier;

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
    public int CurseSpreadTargets { get => _curseSpreadTargets; set => _curseSpreadTargets = value; }
    public float DotFrequencyBonus { get => _dotFrequencyBonus; set => _dotFrequencyBonus = value; }
    public float DotCritBurstChance { get => _dotCritBurstChance; set => _dotCritBurstChance = value; }
    public float AttackSpeedBonus { get => _attackSpeedBonus; set => _attackSpeedBonus = value; }
    public float BulletSpeedBonus { get => _bulletSpeedBonus; set => _bulletSpeedBonus = value; }
    public int BulletCountBonus { get => _bulletCountBonus; set => _bulletCountBonus = value; }
    public float RicochetChance { get => _ricochetChance; set => _ricochetChance = value; }
    public int RicochetMaxBounces { get => _ricochetMaxBounces; set => _ricochetMaxBounces = value; }
    /// <summary>贯穿弹：穿透敌人数量（贯穿弹升级）</summary>
    public int PiercingBonus { get; set; } = 0;
    public float BulletSizeBonus { get => _bulletSizeBonus; set => _bulletSizeBonus = value; }
    public float KnockbackBonus { get => _knockbackBonus; set => _knockbackBonus = value; }
    public float DotSaturationBonus { get => _dotSaturationBonus; set => _dotSaturationBonus = value; }
    public float DetonateExtraPerDot { get => _detonateExtraPerDot; set => _detonateExtraPerDot = value; }
    public float DotLifestealPerTick { get => _dotLifestealPerTick; set => _dotLifestealPerTick = value; }

    // ── P1 深度玩法属性访问器 ──
    public float PandemicBonus { get => _pandemicBonus; set => _pandemicBonus = value; }
    public int ChainReactionCount { get => _chainReactionCount; set => _chainReactionCount = value; }
    public float DualWieldBonus { get => _dualWieldBonus; set => _dualWieldBonus = value; }
    public float ChargeSpeedBonus { get => _chargeSpeedBonus; set => _chargeSpeedBonus = value; }
    public float ChargeDamageBonus { get => _chargeDamageBonus; set => _chargeDamageBonus = value; }

    // ── P2 协同/趣味属性访问器 ──
    public float ResonanceChance { get => _resonanceChance; set => _resonanceChance = Mathf.Clamp01(value); }
    public float ToxicologyCritBonus { get => _toxicologyCritBonus; set => _toxicologyCritBonus = value; }
    public float CorruptTouchDebuff { get => _corruptTouchDebuff; set => _corruptTouchDebuff = Mathf.Min(0.5f, value); }
    public float ElementalStormDmg { get => _elementalStormDmg; set => _elementalStormDmg = value; }
    public float ElementalStormInterval { get => _elementalStormInterval; set => _elementalStormInterval = Mathf.Max(1f, value); }
    public float ShadowLinkRangeBonus { get => _shadowLinkRangeBonus; set => _shadowLinkRangeBonus = value; }
    public float ShadowLinkEffBonus { get => _shadowLinkEffBonus; set => _shadowLinkEffBonus = Mathf.Min(0.5f, value); }
    public float LightJudgmentBonus { get => _lightJudgmentBonus; set => _lightJudgmentBonus = value; }
    public int StaticFieldStacks { get => _staticFieldStacks; set => _staticFieldStacks = value; }
    public float FrostExplosionPct { get => _frostExplosionPct; set => _frostExplosionPct = Mathf.Min(0.10f, value); }

    // ── 子弹增强扩展访问器 ──
    public float AmmoSpeedBonus { get => _ammoSpeedBonus; set => _ammoSpeedBonus = value; }
    public float AmmoRangeBonus { get => _ammoRangeBonus; set => _ammoRangeBonus = value; }
    public float ElementalAffinityBonus { get => _elementalAffinityBonus; set => _elementalAffinityBonus = value; }
    public int PenetrateCount { get => _penetrateCount; set => _penetrateCount = value; }

    // ── 生存向属性访问器 ──
    public float ElementalShieldHp { get => _elementalShieldHp; set => _elementalShieldHp = value; }
    public float PhaseShiftDuration { get => _phaseShiftDuration; set => _phaseShiftDuration = value; }
    public float SoulSiphonHeal { get => _soulSiphonHeal; set => _soulSiphonHeal = value; }
    public float SoulSiphonSpeedDuration { get => _soulSiphonSpeedDuration; set => _soulSiphonSpeedDuration = value; }

    // ── P3 终极/高级属性访问器 ──
    public bool EternalAgonyActive { get => _eternalAgonyActive; set => _eternalAgonyActive = value; }
    public float DoomsdayThreshold { get => _doomsdayThreshold; set => _doomsdayThreshold = Mathf.Min(0.30f, value); }
    public float AnnihilationZoneDmg { get => _annihilationZoneDmg; set => _annihilationZoneDmg = value; }
    public float AnnihilationZoneDuration { get => _annihilationZoneDuration; set => _annihilationZoneDuration = value; }
    public float EmberBoostBonus { get => _emberBoostBonus; set => _emberBoostBonus = value; }
    public float EmberBoostDuration { get => _emberBoostDuration; set => _emberBoostDuration = value; }
    public int ShatterBoostFragments { get => _shatterBoostFragments; set => _shatterBoostFragments = value; }
    public float ShatterBoostDmg { get => _shatterBoostDmg; set => _shatterBoostDmg = value; }

    // ── 综合查询方法 ──
    /// <summary>获取DOT暴击总几率（含凋零+剧毒天赋）</summary>
    public float GetTotalDotCritChance() => GetDotCritChance() + _toxicologyCritBonus;
    /// <summary>获取元素亲和提供的额外伤害倍率</summary>
    public float GetElementalAffinityMultiplier() => 1f + _dotGuns.Count * _elementalAffinityBonus;
    /// <summary>获取贯穿数</summary>
    public int GetTotalPenetrate() => _penetrateCount;
    /// <summary>获取双持后攻速倍率（叠加原有急速）</summary>
    public float GetDualWieldMultiplier() => Mathf.Max(0.2f, 1f - _attackSpeedBonus - _dualWieldBonus);

    // ── 进化系统新增属性 ──
    /// <summary>元素融合是否由进化系统解锁</summary>
    public bool FusionUnlockedByEvolution { get; set; }
    /// <summary>进化系统提供的DOT伤害额外加成</summary>
    public float DotDamageMultiplier { get; set; } = 0f;
    /// <summary>进化系统提供的暴击率额外加成</summary>
    public float CritChanceBonus { get; set; }

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

    public float GetDotCritMultiplier() => _dotCritMultiplier;
    public float GetAttackSpeedMultiplier() => Mathf.Max(0.2f, 1f - _attackSpeedBonus);
    public float GetBulletSpeedMultiplier() => 1f + _bulletSpeedBonus;

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

    public float GetDotFrequencyMultiplier()
    {
        float mult = 1f - _dotFrequencyBonus;
        if (_activeSynergies.Contains("judgment_day") && Time.time < _judgmentDayEndTime)
            mult *= 0.5f;
        return Mathf.Max(0.1f, mult);
    }

    private float _judgmentDayEndTime = 0f;
    public bool HasPlagueSynergy => _activeSynergies.Contains("plague");
    public bool HasFrozenBladeSynergy => _activeSynergies.Contains("frozen_blade");
    public bool HasBulletStormSynergy => _activeSynergies.Contains("bullet_storm");
    public bool HasJudgmentDaySynergy => _activeSynergies.Contains("judgment_day");

    // ── 升级配置 ──
    private MageUpgradeConfig _upgradeConfig;
    public void SetUpgradeConfig(MageUpgradeConfig config) { _upgradeConfig = config; }
    public MageUpgradeConfig GetUpgradeConfig() => _upgradeConfig;

    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
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
                _dotGuns[i] = gun;
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
            lastFireTime = Time.time, upgradeLevel = 1
        });
        DebugHelper.Log($"[MagePassive] Unlocked {type} DOT gun! (color={color})");
        MageUpgradeApplier.CheckMilestones(this);
    }

    public void EnhanceAllDotGuns(float dpsMultiplier)
    {
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            gun.dotDps *= (1f + dpsMultiplier);
            _dotGuns[i] = gun;
        }
    }

    /// <summary>
    /// 应用元素融合：移除两种原始 DOT，添加融合子弹
    /// </summary>
    public bool ApplyFusion(DotFusionSystem.FusionDef fusion)
    {
        if (_completedFusions.Contains(fusion.fusionId)) return false;

        // 移除两种原始 DOT
        _dotGuns.RemoveAll(g => g.effectType == fusion.required1 || g.effectType == fusion.required2);

        // 添加融合子弹（使用第一种原材料的 effectType 作为载体，避免污染其他 DOT 类型的识别）
        _dotGuns.Add(new DotGunState
        {
            effectType = fusion.required1,
            color = fusion.fusionColor,
            cooldown = fusion.cooldown,
            impactDamage = fusion.impactDmg,
            dotDps = fusion.dotDps,
            dotDuration = fusion.dotDuration,
            lastFireTime = Time.time,
            upgradeLevel = 3 // 融合子弹直接为 Lv3
        });

        _completedFusions.Add(fusion.fusionId);

        // DPS 倍率提升
        _fusionDpsMultiplier *= 1.15f;

        DebugHelper.Log($"[MagePassive] Fusion applied: {fusion.displayName} ({fusion.fusionId})");
        return true;
    }

    /// <summary>
    /// 获取当前可用的融合选项（供 LevelUpUI 查询）
    /// </summary>
    public List<DotFusionSystem.FusionDef> GetAvailableFusions()
    {
        return DotFusionSystem.GetAvailableFusions(_dotGuns, _completedFusions);
    }

    public void ClearAllDotGuns()
    {
        _dotGuns.Clear();
        _elementMasterTriggered = false;
        _completedFusions.Clear();
        _fusionDpsMultiplier = 1f;
    }

    // ═══ 升级应用（委托给 MageUpgradeApplier）═══
    // Update/GetFireDirection/SpawnDotBullet/ApplyUpgradeVisual/ApplyBulletSizeBonus → MagePassive.Firing.cs

    public bool ApplyUpgrade(string upgradeId)
    {
        return MageUpgradeApplier.ApplyUpgrade(this, upgradeId);
    }

    public struct DotGunState
    {
        public StatusEffectType effectType;
        public Color color;
        public float cooldown;
        public int impactDamage;
        public float dotDps;
        public float dotDuration;
        public float lastFireTime;
        public int upgradeLevel;
    }
}