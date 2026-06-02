using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 角色专属被动能力系统。
///
/// 被动能力：
/// 1. 所有 DOT 持续时间延长 20%
/// 2. DOT 伤害可暴击
/// 3. 引爆：按 E 引爆所有敌人 DOT + 屏幕抖动
/// 4. 通过升级解锁 4 种 DOT 子弹：流血/中毒/燃烧/霜冻
///
/// 键位：E=引爆，F=技能，Q=切换技能
/// </summary>
public class MagePassive : MonoBehaviour
{
    [Header("Mage 被动参数")]
    [SerializeField] private float _dotDurationBonus = 0.2f;
    [SerializeField] private float _dotCritMultiplier = 2f;

    [Header("引爆参数")]
    [SerializeField] private float _detonateCooldown = 12f;
    [SerializeField] private float _detonateMultiplier = 3f;
    [SerializeField] private float _detonateRadius = 50f;

    [Header("DOT 增强属性")]
    [SerializeField] private float _corrosionArmorReduction = 0.1f;    // 腐蚀：每次叠加 -10% 护甲
    [SerializeField] private int _curseSpreadTargets = 1;              // 诅咒：死亡时传播目标数
    [SerializeField] private float _dotFrequencyBonus = 0f;            // 痛苦：DOT 间隔缩短比例（累加）
    [SerializeField] private float _dotCritBurstChance = 0f;           // 凋零：DOT 双倍伤害几率
    [SerializeField] private int _erosionTriggerCount = 5;             // 侵蚀：每N次DOT生效触发冲击
    [SerializeField] private float _erosionDamagePercent = 0.5f;       // 侵蚀：冲击伤害比例
    [SerializeField] private float _windVortexChance = 0.2f;           // 风蚀：漩涡触发几率

    [Header("子弹增强属性")]
    [SerializeField] private float _attackSpeedBonus = 0f;             // 急速：攻速加成
    [SerializeField] private float _bulletSpeedBonus = 0f;             // 急速：子弹速度加成
    [SerializeField] private int _bulletCountBonus = 0;                // 弹幕：子弹数量增加
    [SerializeField] private float _ricochetChance = 0f;               // 反弹：反弹几率
    [SerializeField] private int _ricochetMaxBounces = 0;              // 反弹：最大反弹次数（超过100%后）
    [SerializeField] private float _bulletSizeBonus = 0f;              // 共振：碰撞体积加成
    [SerializeField] private float _knockbackBonus = 0f;               // 共振：击退加成

    [Header("运行时状态")]
    [SerializeField] private float _lastDetonateTime = -999f;

    private List<DotGunState> _dotGuns = new List<DotGunState>();

    // ── 公共属性 ──
    public float DetonateCooldown => _detonateCooldown;
    public float DetonateCooldownRemaining => Mathf.Max(0f, _detonateCooldown - (Time.time - _lastDetonateTime));
    public bool DetonateReady => Time.time - _lastDetonateTime >= _detonateCooldown;
    public float DetonateMultiplier { get => _detonateMultiplier; set => _detonateMultiplier = value; }
    public float DetonateCooldownValue { get => _detonateCooldown; set => _detonateCooldown = value; }
    public List<DotGunState> DotGuns => _dotGuns;

    // ── DOT 增强属性访问器 ──
    public float CorrosionArmorReduction { get => _corrosionArmorReduction; set => _corrosionArmorReduction = value; }
    public int CurseSpreadTargets { get => _curseSpreadTargets; set => _curseSpreadTargets = value; }
    public float DotFrequencyBonus { get => _dotFrequencyBonus; set => _dotFrequencyBonus = value; }
    public float DotCritBurstChance { get => _dotCritBurstChance; set => _dotCritBurstChance = value; }
    public int ErosionTriggerCount { get => _erosionTriggerCount; set => _erosionTriggerCount = Mathf.Max(2, value); }
    public float ErosionDamagePercent { get => _erosionDamagePercent; set => _erosionDamagePercent = value; }
    public float WindVortexChance { get => _windVortexChance; set => _windVortexChance = value; }

    // ── 子弹增强属性访问器 ──
    public float AttackSpeedBonus { get => _attackSpeedBonus; set => _attackSpeedBonus = value; }
    public float BulletSpeedBonus { get => _bulletSpeedBonus; set => _bulletSpeedBonus = value; }
    public int BulletCountBonus { get => _bulletCountBonus; set => _bulletCountBonus = value; }
    public float RicochetChance { get => _ricochetChance; set => _ricochetChance = value; }
    public int RicochetMaxBounces { get => _ricochetMaxBounces; set => _ricochetMaxBounces = value; }
    public float BulletSizeBonus { get => _bulletSizeBonus; set => _bulletSizeBonus = value; }
    public float KnockbackBonus { get => _knockbackBonus; set => _knockbackBonus = value; }

    public float GetDotDurationMultiplier() => 1f + _dotDurationBonus;

    public float GetDotCritChance()
    {
        float baseCrit = 0.05f;
        baseCrit += SaveManager.Instance?.GetPermanentBonus("crit_chance") ?? 0f;
        return baseCrit;
    }

    public float GetDotCritMultiplier() => _dotCritMultiplier;

    public void AddDotDurationBonus(float bonus)
    {
        _dotDurationBonus += bonus;
        DebugHelper.Log($"[MagePassive] DOT Duration Bonus +{bonus * 100}%, Total: {_dotDurationBonus * 100}%");
    }

    /// <summary>
    /// 获取攻速倍率（急速加成后）
    /// </summary>
    public float GetAttackSpeedMultiplier() => 1f / (1f + _attackSpeedBonus);

    /// <summary>
    /// 获取子弹速度倍率
    /// </summary>
    public float GetBulletSpeedMultiplier() => 1f + _bulletSpeedBonus;

    /// <summary>
    /// 解锁一种 DOT 子弹类型
    /// </summary>
    public void UnlockDotGun(StatusEffectType type, Color color, float cooldown, int impactDmg, float dotDps, float dotDuration)
    {
        foreach (var gun in _dotGuns)
        {
            if (gun.effectType == type)
            {
                gun.dotDps *= 1.15f;
                gun.impactDamage = Mathf.RoundToInt(gun.impactDamage * 1.1f);
                DebugHelper.Log($"[MagePassive] Upgraded {type} DOT gun");
                return;
            }
        }

        _dotGuns.Add(new DotGunState
        {
            effectType = type, color = color, cooldown = cooldown,
            impactDamage = impactDmg, dotDps = dotDps, dotDuration = dotDuration,
            lastFireTime = -999f
        });
        DebugHelper.Log($"[MagePassive] Unlocked {type} DOT gun! (color={color})");
    }

    public void EnhanceAllDotGuns(float dpsMultiplier)
    {
        foreach (var gun in _dotGuns)
            gun.dotDps *= (1f + dpsMultiplier);
    }

    private void Awake()
    {
        // Mage 默认自带毒子弹（可叠加中毒，2 DPS）
        UnlockDotGun(StatusEffectType.Poison, new Color(0.1f, 0.8f, 0.2f), 1.5f, 0, 2f, 5f);
    }

    private void Update()
    {
        // 引爆快捷键：E
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            Detonate();

        // 自动发射所有就绪的 DOT 子弹
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            return;

        Vector2 fireDir = GetFireDirection();
        if (fireDir.sqrMagnitude < 0.01f) return;

        float dmgMult = 1f;
        var wc = GetComponent<WeaponController>();
        if (wc != null) dmgMult = wc.DamageMultiplier;

        // 应用攻速加成
        float attackSpeedMult = GetAttackSpeedMultiplier();

        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            float effectiveCooldown = gun.cooldown * attackSpeedMult;
            if (Time.time - gun.lastFireTime >= effectiveCooldown)
            {
                gun.lastFireTime = Time.time;
                SpawnDotBullet(gun, fireDir, dmgMult);
            }
        }
    }

    private Vector2 GetFireDirection()
    {
        if (!GameInputHandler.MouseValid) return (Vector2)transform.right;
        Vector2 mousePos = GameInputHandler.MouseWorldPosition;
        Vector2 pos = transform.position;
        return (mousePos - pos).normalized;
    }

    private void SpawnDotBullet(DotGunState gun, Vector2 direction, float dmgMultiplier)
    {
        float durMult = GetDotDurationMultiplier();
        float critChance = GetDotCritChance();
        bool canCrit = true;

        // 子弹数量加成：默认1发，加上 BulletCountBonus
        int bulletCount = 1 + _bulletCountBonus;
        float spreadAngle = 15f; // 每发子弹散射角度

        for (int b = 0; b < bulletCount; b++)
        {
            // 计算散射方向
            Vector2 fireDir = direction;
            if (bulletCount > 1)
            {
                float angle = (b - (bulletCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(
                    direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad),
                    direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)
                ).normalized;
            }

            switch (gun.effectType)
            {
                case StatusEffectType.Bleed:
                    BleedBullet.Create(transform.position, fireDir, 14f, gun.impactDamage,
                        gun.dotDps, gun.dotDuration * durMult, dmgMultiplier,
                        canCrit, critChance, _dotCritMultiplier);
                    break;

                case StatusEffectType.Poison:
                    PoisonBullet.Create(transform.position, fireDir, 14f,
                        gun.dotDps, gun.dotDuration * durMult, dmgMultiplier,
                        canCrit, critChance, _dotCritMultiplier);
                    break;

                case StatusEffectType.Burn:
                    BurnBullet.Create(transform.position, fireDir, 12f, gun.impactDamage,
                        gun.dotDps, gun.dotDuration * durMult, dmgMultiplier,
                        canCrit, critChance, _dotCritMultiplier);
                    break;

                case StatusEffectType.Frostbite:
                    FrostBullet.Create(transform.position, fireDir, 20f, gun.impactDamage,
                        gun.dotDps, 1f, 0.3f, dmgMultiplier,
                        canCrit, critChance, _dotCritMultiplier);
                    break;
            }
        }
    }

    /// <summary>
    /// 引爆技能 + 屏幕抖动
    /// </summary>
    public bool Detonate()
    {
        if (!DetonateReady)
        {
            DebugHelper.Log("[MagePassive] Detonate on cooldown");
            return false;
        }

        _lastDetonateTime = Time.time;
        float critChance = GetDotCritChance();
        float critMult = GetDotCritMultiplier();
        int totalDamage = 0;
        int enemiesHit = 0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _detonateRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var d = hit.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;

            bool hadEffect = false;

            // 引爆 StatusEffectManager DOT
            var sem = hit.GetComponent<StatusEffectManager>();
            if (sem != null && sem.HasAnyDot)
            {
                int dmg = sem.Detonate(_detonateMultiplier, critChance, critMult);
                if (dmg > 0) { totalDamage += dmg; hadEffect = true; }
            }

            // 引爆 Bleed
            var bleed = hit.GetComponent<BleedEffect>();
            if (bleed != null)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.2f * _detonateMultiplier);
                d.TakeDamage(extra); totalDamage += extra; hadEffect = true;
            }

            // 引爆 Burn
            var burn = hit.GetComponent<BurnStackEffect>();
            if (burn != null)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.15f * _detonateMultiplier);
                d.TakeDamage(extra); totalDamage += extra; hadEffect = true;
            }

            // 引爆 Poison
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison != null)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.15f * _detonateMultiplier);
                d.TakeDamage(extra); totalDamage += extra; hadEffect = true;
            }

            if (hadEffect)
            {
                enemiesHit++;
                CombatManager.CreateExplosionEffect(hit.transform.position, 2f, new Color(1f, 0.3f, 0.8f), 0.5f);
            }
        }

        // 屏幕抖动
        var cam = Camera.main;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null)
                shake.Shake(1.5f, 0.5f);
        }

        DebugHelper.Log($"[MagePassive] DETONATE! Hit {enemiesHit} enemies for {totalDamage} total damage!");
        return enemiesHit > 0;
    }

    public class DotGunState
    {
        public StatusEffectType effectType;
        public Color color;
        public float cooldown;
        public int impactDamage;
        public float dotDps;
        public float dotDuration;
        public float lastFireTime;
    }
}