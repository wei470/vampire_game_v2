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

    public void ClearAllDotGuns()
    {
        _dotGuns.Clear();
        _elementMasterTriggered = false;
    }

    private void Update()
    {
        _detonateSystem.UpdateChargeInput();

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
        // 直接从 InputSystem 读取鼠标坐标并转换为世界坐标（避免依赖缓存导致帧延迟/偏移）
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            var cam = GameReferences.MainCamera;
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                Vector3 screenPos = mouse.position.ReadValue();
                // 使用相机到 z=0 平面的距离
                screenPos.z = Mathf.Abs(cam.transform.position.z);
                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
                Vector2 dir = ((Vector2)worldPos - (Vector2)transform.position);
                if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            }
        }

        // 备用：朝最近的敌人方向
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr != null && spawnMgr.ActiveEnemies != null)
        {
            float minDist = float.MaxValue;
            Vector2 nearest = Vector2.zero;
            for (int i = 0; i < spawnMgr.ActiveEnemies.Count; i++)
            {
                var e = spawnMgr.ActiveEnemies[i];
                if (e == null) continue;
                var eb = e.GetComponent<EnemyBase>();
                if (eb == null || !eb.Alive) continue;
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < minDist) { minDist = d; nearest = e.transform.position; }
            }
            if (minDist < float.MaxValue)
                return (nearest - (Vector2)transform.position).normalized;
        }

        return (Vector2)transform.right;
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

    // ═══ 升级应用（委托给 MageUpgradeApplier）═══

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