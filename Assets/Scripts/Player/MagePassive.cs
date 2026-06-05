using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 角色专属被动能力系统（~550行）
/// 
/// 职责拆分：
/// - DetonateSystem: 引爆系统（蓄力/连锁/余烬/碎裂）
/// - MagePassive: DOT枪管理 + 升级 + 协同 + 进化 + 子弹发射
/// </summary>
public class MagePassive : MonoBehaviour
{
    [Header("Mage 被动参数")]
    [SerializeField] private float _dotDurationBonus = 0.2f;
    [SerializeField] private float _dotCritMultiplier = 2f;

    [Header("DOT 增强属性")]
    [SerializeField] private float _corrosionArmorReduction = 0.1f;
    [SerializeField] private int _curseSpreadTargets = 1;
    [SerializeField] private float _dotFrequencyBonus = 0f;
    [SerializeField] private float _dotCritBurstChance = 0f;
    [SerializeField] private int _erosionTriggerCount = 5;
    [SerializeField] private float _erosionDamagePercent = 0f;

    [Header("子弹增强属性")]
    [SerializeField] private float _attackSpeedBonus = 0f;
    [SerializeField] private float _bulletSpeedBonus = 0f;
    [SerializeField] private int _bulletCountBonus = 0;
    [SerializeField] private float _ricochetChance = 0f;
    [SerializeField] private int _ricochetMaxBounces = 0;
    [SerializeField] private float _bulletSizeBonus = 0f;
    [SerializeField] private float _knockbackBonus = 0f;

    private List<DotGunState> _dotGuns = new List<DotGunState>();
    private WeaponController _weaponController;

    // ── 里程碑系统 ──
    private bool _elementMasterTriggered = false;

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
    public int ErosionTriggerCount { get => _erosionTriggerCount; set => _erosionTriggerCount = Mathf.Max(2, value); }
    public float ErosionDamagePercent { get => _erosionDamagePercent; set => _erosionDamagePercent = value; }
    public float AttackSpeedBonus { get => _attackSpeedBonus; set => _attackSpeedBonus = value; }
    public float BulletSpeedBonus { get => _bulletSpeedBonus; set => _bulletSpeedBonus = value; }
    public int BulletCountBonus { get => _bulletCountBonus; set => _bulletCountBonus = value; }
    public float RicochetChance { get => _ricochetChance; set => _ricochetChance = value; }
    public int RicochetMaxBounces { get => _ricochetMaxBounces; set => _ricochetMaxBounces = value; }
    public float BulletSizeBonus { get => _bulletSizeBonus; set => _bulletSizeBonus = value; }
    public float KnockbackBonus { get => _knockbackBonus; set => _knockbackBonus = value; }

    public float GetDotDurationMultiplier() => 1f + _dotDurationBonus;
    public float DotDurationMultiplier => GetDotDurationMultiplier();
    public float CritMultiplier => _dotCritMultiplier;
    public float CritChance => 0.05f;

    public float GetDotDamageMultiplier()
    {
        float mult = 1f;
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
        // 添加引爆系统组件
        _detonateSystem = gameObject.AddComponent<DetonateSystem>();
        _detonateSystem.Init(this);
        // Mage 默认自带毒子弹
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
                    TriggerEvolution(type);
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
        CheckMilestones();
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

    public void ClearAllDotGuns()
    {
        _dotGuns.Clear();
        _elementMasterTriggered = false;
    }

    private void Update()
    {
        // 蓄力输入委托给 DetonateSystem
        _detonateSystem.UpdateChargeInput();

        // 自动发射 DOT 子弹
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        Vector2 fireDir = GetFireDirection();
        if (fireDir.sqrMagnitude < 0.01f) return;

        float dmgMult = _weaponController != null ? _weaponController.DamageMultiplier : 1f;
        float attackSpeedMult = GetAttackSpeedMultiplier();

        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            float effectiveCooldown = gun.cooldown * attackSpeedMult;
            if (Time.time - gun.lastFireTime >= effectiveCooldown)
            {
                gun.lastFireTime = Time.time;
                _dotGuns[i] = gun;
                SpawnDotBullet(gun, fireDir, dmgMult);
            }
        }
    }

    // ── 子弹发射 ──

    private Vector2 GetFireDirection()
    {
        if (!GameInputHandler.MouseValid) return (Vector2)transform.right;
        Vector2 mousePos = GameInputHandler.MouseWorldPosition;
        return ((Vector2)mousePos - (Vector2)transform.position).normalized;
    }

    private void SpawnDotBullet(DotGunState gun, Vector2 direction, float dmgMultiplier)
    {
        float durMult = GetDotDurationMultiplier();
        float critChance = GetDotCritChance();
        float bulletSpeedMult = GetBulletSpeedMultiplier();
        int bulletCount = 1 + _bulletCountBonus;
        float spreadAngle = 15f;
        const int MAX_NORMAL = 5;
        int normalCount = Mathf.Min(bulletCount, MAX_NORMAL);
        int homingCount = bulletCount - normalCount;

        for (int b = 0; b < normalCount; b++)
        {
            Vector2 fireDir = direction;
            if (normalCount > 1)
            {
                float angle = (b - (normalCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad), direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)).normalized;
            }
            GameObject bullet = DotBulletFactory.Create(gun.effectType, transform.position, fireDir, gun, bulletSpeedMult, durMult, dmgMultiplier, true, critChance, _dotCritMultiplier);
            ApplyBulletSizeBonus(bullet);
            ApplyUpgradeVisual(bullet, gun);
        }

        if (homingCount > 0)
        {
            float homingSpeed = 10f * bulletSpeedMult;
            int homingDmg = Mathf.Max(1, gun.impactDamage);
            float homingSpread = 30f;
            for (int h = 0; h < homingCount; h++)
            {
                Vector2 hDir = direction;
                if (homingCount > 1)
                {
                    float angle = (h - (homingCount - 1) / 2f) * homingSpread;
                    float rad = angle * Mathf.Deg2Rad;
                    hDir = new Vector2(direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad), direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)).normalized;
                }
                var homing = HomingProjectile.CreateDefault(transform.position, hDir, homingDmg, homingSpeed, 5f, 6f);
                homing.SetDamageMultiplier(dmgMultiplier);
                homing.SetKnockback(_knockbackBonus > 0f ? 3f : 0f);
                var dotHoming = homing.gameObject.AddComponent<DotHomingBullet>();
                dotHoming.Init(gun, durMult, dmgMultiplier, true, critChance, _dotCritMultiplier);
                ApplyBulletSizeBonus(homing.gameObject);
            }
        }
    }

    private void ApplyUpgradeVisual(GameObject bullet, DotGunState gun)
    {
        if (bullet == null || gun.upgradeLevel <= 1) return;
        float scaleBonus = 1f + (gun.upgradeLevel - 1) * 0.1f;
        bullet.transform.localScale *= scaleBonus;
        var sr = bullet.GetComponent<SpriteRenderer>();
        if (sr != null) { float brightness = Mathf.Min(0.3f, (gun.upgradeLevel - 1) * 0.1f); sr.color = Color.Lerp(sr.color, Color.white, brightness); }
        if (gun.upgradeLevel >= 3)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(bullet.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * 1.8f;
            var glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite = sr?.sprite;
            glowSr.color = new Color(gun.color.r, gun.color.g, gun.color.b, 0.25f);
            glowSr.sortingOrder = 14;
        }
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box) box.size *= scaleBonus;
        else if (col != null && col is CircleCollider2D circle) circle.radius *= scaleBonus;
    }

    private void ApplyBulletSizeBonus(GameObject bullet)
    {
        if (bullet == null || _bulletSizeBonus <= 0f) return;
        bullet.transform.localScale *= (1f + _bulletSizeBonus);
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box) box.size *= (1f + _bulletSizeBonus);
        else if (col != null && col is CircleCollider2D circle) circle.radius *= (1f + _bulletSizeBonus);
    }

    // ── 进化系统 ──

    private void TriggerEvolution(StatusEffectType type)
    {
        _evolvedTypes.Add(type);
        var evo = GetEvolutionInfo(type);
        DebugHelper.Log($"[MagePassive] ✦ EVOLVED: {evo.name}!");
        var player = GameReferences.Player;
        if (player != null)
            DamagePopup.Create(player.transform.position + Vector3.up * 3f, 0, new Color(1f, 0.85f, 0f), false, $"✦ EVOLVED: {evo.name}!");
        if (SFXManager.Instance != null) SFXManager.Instance.PlayLevelUp();
        ApplyEvolutionBonus(type);
    }

    private (string name, string description) GetEvolutionInfo(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return ("血之狂潮 (Blood Tide)", "流血 DPS x2");
            case StatusEffectType.Poison: return ("瘟疫之源 (Plague Source)", "中毒子弹 DPS +50%");
            case StatusEffectType.Burn: return ("地狱之火 (Hellfire)", "燃烧 DPS +80%");
            case StatusEffectType.Frostbite: return ("绝对零度 (Absolute Zero)", "霜冻 DPS +100%");
            default: return ("Unknown", "");
        }
    }

    private void ApplyEvolutionBonus(StatusEffectType type)
    {
        float mult = 1f;
        switch (type)
        {
            case StatusEffectType.Bleed: mult = 2f; break;
            case StatusEffectType.Poison: mult = 1.5f; break;
            case StatusEffectType.Burn: mult = 1.8f; break;
            case StatusEffectType.Frostbite: mult = 2f; break;
        }
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            if (gun.effectType == type) { gun.dotDps *= mult; _dotGuns[i] = gun; break; }
        }
    }

    // ── 里程碑 ──

    private void CheckMilestones()
    {
        if (!_elementMasterTriggered && _dotGuns.Count >= 4)
        {
            _elementMasterTriggered = true;
            DebugHelper.Log("[MagePassive] ★ MILESTONE: Element Master! All DOT damage +20%");
            var player = GameReferences.Player;
            if (player != null) DamagePopup.Create(player.transform.position + Vector3.up * 2f, 0, new Color(1f, 0.85f, 0f), false, "★ ELEMENT MASTER");
            SyncDotDamageMultiplierToAll();
        }
    }

    // ── 协同系统 ──

    private enum SynergyType { Plague, FrozenBlade, BulletStorm, JudgmentDay }

    private void CheckSynergies()
    {
        bool hasPoison = false, hasBurn = false, hasCorrosion = false;
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            if (_dotGuns[i].effectType == StatusEffectType.Poison) hasPoison = true;
            if (_dotGuns[i].effectType == StatusEffectType.Burn) hasBurn = true;
        }
        hasCorrosion = _corrosionArmorReduction > 0.101f;
        TryActivateSynergy("plague", SynergyType.Plague, hasPoison && hasBurn && hasCorrosion);

        bool hasBleed = false, hasFrost = false, hasCurse = false;
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            if (_dotGuns[i].effectType == StatusEffectType.Bleed) hasBleed = true;
            if (_dotGuns[i].effectType == StatusEffectType.Frostbite) hasFrost = true;
        }
        hasCurse = _curseSpreadTargets > 1;
        TryActivateSynergy("frozen_blade", SynergyType.FrozenBlade, hasBleed && hasFrost && hasCurse);

        bool hasBarrage = _bulletCountBonus > 0;
        bool hasHaste = _attackSpeedBonus > 0.001f;
        bool hasRicochet = _ricochetChance > 0.001f;
        TryActivateSynergy("bullet_storm", SynergyType.BulletStorm, hasBarrage && hasHaste && hasRicochet);

        bool hasRadiate = _detonateSystem != null && _detonateSystem.DetonateMultiplier > 3.01f;
        bool hasContaminate = _detonateSystem != null && _detonateSystem.DetonateCooldown < 11.99f;
        bool hasErosion = _erosionDamagePercent > 0.001f;
        TryActivateSynergy("judgment_day", SynergyType.JudgmentDay, hasRadiate && hasContaminate && hasErosion);
    }

    private void TryActivateSynergy(string synergyId, SynergyType type, bool conditionMet)
    {
        if (!conditionMet || _activeSynergies.Contains(synergyId)) return;
        _activeSynergies.Add(synergyId);
        var player = GameReferences.Player;
        if (player != null)
        {
            string name = GetSynergyName(type);
            Color color = GetSynergyColor(type);
            DamagePopup.Create(player.transform.position + Vector3.up * 2.5f, 0, color, false, $"✦ SYNERGY: {name}!");
        }
        if (SFXManager.Instance != null) SFXManager.Instance.PlayLevelUp();
        DebugHelper.Log($"[MagePassive] ✦ SYNERGY ACTIVATED: {GetSynergyName(type)}!");
    }

    private string GetSynergyName(SynergyType type)
    {
        switch (type) { case SynergyType.Plague: return "瘟疫"; case SynergyType.FrozenBlade: return "冰封血刃"; case SynergyType.BulletStorm: return "弹雨风暴"; case SynergyType.JudgmentDay: return "末日审判"; default: return "Unknown"; }
    }

    private Color GetSynergyColor(SynergyType type)
    {
        switch (type) { case SynergyType.Plague: return new Color(0.1f, 0.9f, 0.3f); case SynergyType.FrozenBlade: return new Color(0.4f, 0.7f, 1f); case SynergyType.BulletStorm: return new Color(1f, 0.9f, 0.2f); case SynergyType.JudgmentDay: return new Color(1f, 0.3f, 0.1f); default: return Color.white; }
    }

    private void ActivateJudgmentDay() { _judgmentDayEndTime = Time.time + 3f; }

    // ═══ 统一升级应用接口 ═══

    public bool ApplyUpgrade(string upgradeId)
    {
        if (_upgradeConfig == null) { DebugHelper.LogError("[MagePassive] MageUpgradeConfig not set!"); return false; }

        var dotGunEntry = _upgradeConfig.GetDotGunEntry(upgradeId);
        if (dotGunEntry.HasValue)
        {
            var dg = dotGunEntry.Value;
            UnlockDotGun(dg.effectType, dg.color, dg.cooldown, dg.impactDmg, dg.dotDps, dg.dotDuration);
            CheckSynergies();
            return true;
        }

        var upgradeEntry = _upgradeConfig.GetUpgradeEntry(upgradeId);
        if (!upgradeEntry.HasValue) { DebugHelper.LogWarning($"[MagePassive] Unknown upgradeId '{upgradeId}'"); return false; }

        var ue = upgradeEntry.Value;
        switch (ue.category)
        {
            case CharacterUpgradeOption.UpgradeCategory.ArmorReduction: _corrosionArmorReduction += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DotSpread: _curseSpreadTargets += (int)ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DotFrequency: _dotFrequencyBonus += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DotCritBurst: _dotCritBurstChance += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier: if (_detonateSystem != null) _detonateSystem.DetonateMultiplier += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DetonateAbility: if (_detonateSystem != null) _detonateSystem.DetonateCooldownValue *= (1f - ue.value1); break;
            case CharacterUpgradeOption.UpgradeCategory.DotTrigger: _erosionTriggerCount = Mathf.Max(2, _erosionTriggerCount - (int)ue.value1); _erosionDamagePercent += ue.value2; break;
            case CharacterUpgradeOption.UpgradeCategory.AttackSpeed: _attackSpeedBonus += ue.value1; _bulletSpeedBonus += ue.value2; break;
            case CharacterUpgradeOption.UpgradeCategory.BulletCount: _bulletCountBonus += (int)ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.Ricochet: _ricochetChance += ue.value1; if (_ricochetChance > 1f) { _ricochetMaxBounces += 1; _ricochetChance -= 1f; } break;
            case CharacterUpgradeOption.UpgradeCategory.BulletSize: _bulletSizeBonus += ue.value1; _knockbackBonus += ue.value2; break;
        }

        CheckSynergies();
        return true;
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