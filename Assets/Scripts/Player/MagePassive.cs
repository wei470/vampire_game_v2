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

    [Header("中毒专属")]
    [SerializeField] private float _poisonDurationBonus = 0f;
    [SerializeField] private float _poisonDpsBonus = 0f;
    [SerializeField] private float _poisonPoolBonus = 0f;
    [SerializeField] private float _poisonTickReduction = 0f;
    [SerializeField] private float _poisonCritBonus = 0f;
    [SerializeField] private bool _poisonSepsis = false;
    [SerializeField] private bool _poisonPlague = false;
    [SerializeField] private bool _poisonLethal = false;

    [Header("燃烧专属")]
    [SerializeField] private float _burnDurationBonus = 0f;
    [SerializeField] private float _burnRadiusBonus = 0f;
    [SerializeField] private float _burnTickReduction = 0f;
    [SerializeField] private float _burnSlowBonus = 0f;
    [SerializeField] private float _burnCritBonus = 0f;
    [SerializeField] private bool _burnMeltMastery = false;
    [SerializeField] private bool _burnStorm = false;
    [SerializeField] private bool _burnBurst = false;
    [SerializeField] private bool _burnFirmament = false;

    [Header("霜冻专属")]
    [SerializeField] private float _frostDurationBonus = 0f;
    [SerializeField] private float _frostMaxSlowBonus = 0f;
    [SerializeField] private float _frostPerStackSlowBonus = 0f;
    [SerializeField] private float _frostTickReduction = 0f;
    [SerializeField] private float _frostRangeBonus = 0f;
    [SerializeField] private float _frostCritBonus = 0f;
    [SerializeField] private bool _frostFreeze = false;
    [SerializeField] private bool _frostBlizzard = false;
    [SerializeField] private bool _frostAbsolute = false;
    [SerializeField] private bool _snowyDay = false;
    [SerializeField] private bool _frozenHands = false;
    [SerializeField] private bool _iceBlade = false;
    [SerializeField] private bool _coldBullet = false;
    [SerializeField] private bool _coldEmbrace = false;

    [Header("雷电专属")]
    [SerializeField] private float _staticDamageBonus = 0f;
    [SerializeField] private int _staticChainBonus = 0;
    [SerializeField] private float _staticTickReduction = 0f;
    [SerializeField] private float _staticRangeBonus = 0f;
    [SerializeField] private float _staticCritBonus = 0f;
    [SerializeField] private bool _staticOverload = false;
    [SerializeField] private bool _stormMulti = false;
    [SerializeField] private bool _stormChain = false;
    [SerializeField] private bool _paralysis = false;

    [Header("风专属")]
    [SerializeField] private float _windSpeedBonus = 0f;
    [SerializeField] private float _windTickReduction = 0f;
    [SerializeField] private bool _windHurricane = false;
    [SerializeField] private float _windPrecisionAngle = 25f;
    [SerializeField] private float _typhoonKnockbackBonus = 0f;
    [SerializeField] private bool _tornado = false;
    [SerializeField] private bool _wildWind = false;
    [SerializeField] private bool _stormWind = false;
    [SerializeField] private bool _swiftWind = false;

    private List<DotGunState> _dotGuns = new List<DotGunState>();

    // 雷暴延迟发射
    private float _stormMultiDelay = -1f;
    private DotGunState _stormMultiGun;
    private System.Collections.Generic.List<Vector2> _stormMultiDirs = new();
    private float _stormMultiDmgMult;

    // 静电爆炸（万雷齐发）
    private int _lightningBulletCount;
    private bool _lightningExplosionPending;

    // 暴风延迟发射
    private float _stormWindDelay = -1f;
    private DotGunState _stormWindGun;
    private System.Collections.Generic.List<Vector2> _stormWindDirs = new();
    private float _stormWindDmgMult;
    private float _stormWindDelay2 = -1f;
    private DotGunState _stormWindGun2;
    private System.Collections.Generic.List<Vector2> _stormWindDirs2 = new();
    private float _stormWindDmgMult2;

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

    // ── 中毒专属访问器 ──
    public float PoisonDurationBonus { get => _poisonDurationBonus; set => _poisonDurationBonus = value; }
    public float PoisonDpsBonus { get => _poisonDpsBonus; set => _poisonDpsBonus = value; }
    public float PoisonPoolBonus { get => _poisonPoolBonus; set => _poisonPoolBonus = value; }
    public float PoisonTickReduction { get => _poisonTickReduction; set => _poisonTickReduction = value; }
    public float PoisonCritBonus { get => _poisonCritBonus; set => _poisonCritBonus = value; }
    public bool PoisonSepsis { get => _poisonSepsis; set => _poisonSepsis = value; }
    public bool PoisonPlague { get => _poisonPlague; set => _poisonPlague = value; }
    public bool PoisonLethal { get => _poisonLethal; set => _poisonLethal = value; }

    // ── 燃烧专属访问器 ──
    public float BurnDurationBonus { get => _burnDurationBonus; set => _burnDurationBonus = value; }
    public float BurnRadiusBonus { get => _burnRadiusBonus; set => _burnRadiusBonus = value; }
    public float BurnTickReduction { get => _burnTickReduction; set => _burnTickReduction = value; }
    public float BurnSlowBonus { get => _burnSlowBonus; set => _burnSlowBonus = value; }
    public float BurnCritBonus { get => _burnCritBonus; set => _burnCritBonus = value; }
    public bool BurnMeltMastery { get => _burnMeltMastery; set => _burnMeltMastery = value; }
    public bool BurnStorm { get => _burnStorm; set => _burnStorm = value; }
    public bool BurnBurst { get => _burnBurst; set => _burnBurst = value; }
    public bool BurnFirmament { get => _burnFirmament; set => _burnFirmament = value; }

    // ── 霜冻专属访问器 ──
    public float FrostDurationBonus { get => _frostDurationBonus; set => _frostDurationBonus = value; }
    public float FrostMaxSlowBonus { get => _frostMaxSlowBonus; set => _frostMaxSlowBonus = value; }
    public float FrostPerStackSlowBonus { get => _frostPerStackSlowBonus; set => _frostPerStackSlowBonus = value; }
    public float FrostTickReduction { get => _frostTickReduction; set => _frostTickReduction = value; }
    public float FrostRangeBonus { get => _frostRangeBonus; set => _frostRangeBonus = value; }
    public float FrostCritBonus { get => _frostCritBonus; set => _frostCritBonus = value; }
    public bool FrostFreeze { get => _frostFreeze; set => _frostFreeze = value; }
    public bool FrostBlizzard { get => _frostBlizzard; set => _frostBlizzard = value; }
    public bool FrostAbsolute { get => _frostAbsolute; set => _frostAbsolute = value; }
    public bool SnowyDay { get => _snowyDay; set => _snowyDay = value; }
    public bool FrozenHands { get => _frozenHands; set => _frozenHands = value; }
    public bool IceBlade { get => _iceBlade; set => _iceBlade = value; }
    public bool ColdBullet { get => _coldBullet; set => _coldBullet = value; }
    public bool ColdEmbrace { get => _coldEmbrace; set => _coldEmbrace = value; }

    // ── 雷电专属访问器 ──
    public float StaticDamageBonus { get => _staticDamageBonus; set => _staticDamageBonus = value; }
    public int StaticChainBonus { get => _staticChainBonus; set => _staticChainBonus = value; }
    public float StaticTickReduction { get => _staticTickReduction; set => _staticTickReduction = value; }
    public float StaticRangeBonus { get => _staticRangeBonus; set => _staticRangeBonus = value; }
    public float StaticCritBonus { get => _staticCritBonus; set => _staticCritBonus = value; }
    public bool StaticOverload { get => _staticOverload; set => _staticOverload = value; }
    public bool StormMulti { get => _stormMulti; set => _stormMulti = value; }
    public bool StormChain { get => _stormChain; set => _stormChain = value; }
    public bool Paralysis { get => _paralysis; set => _paralysis = value; }

    // ── 风专属访问器 ──
    public float WindSpeedBonus { get => _windSpeedBonus; set => _windSpeedBonus = value; }
    public float WindTickReduction { get => _windTickReduction; set => _windTickReduction = value; }
    public bool WindHurricane { get => _windHurricane; set => _windHurricane = value; }
    public float WindPrecisionAngle { get => _windPrecisionAngle; set => _windPrecisionAngle = value; }
    public float TyphoonKnockbackBonus { get => _typhoonKnockbackBonus; set => _typhoonKnockbackBonus = value; }
    public bool Tornado { get => _tornado; set => _tornado = value; }
    public bool WildWind { get => _wildWind; set => _wildWind = value; }
    public bool StormWind { get => _stormWind; set => _stormWind = value; }
    public bool SwiftWind { get => _swiftWind; set => _swiftWind = value; }
    public bool IsLightningExplosionPending => _lightningExplosionPending;
    public void ConsumeLightningExplosion() { _lightningExplosionPending = false; }

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

    public float GetDotCritMultiplier() => _dotCritMultiplier;
    public override float GetAttackSpeedMultiplier() => Mathf.Max(0.2f, 1f / (1f + Mathf.Max(0f, _attackSpeedBonus)));

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
            upgradeLevel = 1,
            accumulator = _dotGuns.Count * cooldown / (_dotGuns.Count + 1f)
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

    private Dictionary<string, int> _upgradeStacks = new Dictionary<string, int>();
    public Dictionary<string, int> UpgradeStacks => _upgradeStacks;

    public override bool ApplyUpgrade(string upgradeId)
    {
        bool result = MageUpgradeApplier.ApplyUpgrade(this, upgradeId);
        if (result)
        {
            if (!_upgradeStacks.ContainsKey(upgradeId))
                _upgradeStacks[upgradeId] = 0;
            _upgradeStacks[upgradeId]++;
        }
        return result;
    }

}
