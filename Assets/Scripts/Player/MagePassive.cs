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
    [SerializeField] private float _erosionDamagePercent = 0f;         // 侵蚀：冲击伤害比例（默认0，选了侵蚀升级后才生效）
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
    private WeaponController _weaponController; // 缓存引用，避免每帧 GetComponent

    // ── #26 里程碑系统 ──
    private bool _elementMasterTriggered = false;  // 集齐4种DOT子弹
    private float _chainDetonateEndTime = 0f;       // 连锁引爆结束时间
    private int _lastDetonateEnemyCount = 0;        // 上次引爆命中敌人数量

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
    // ── 子弹增强属性访问器 ──
    public float AttackSpeedBonus { get => _attackSpeedBonus; set => _attackSpeedBonus = value; }
    public float BulletSpeedBonus { get => _bulletSpeedBonus; set => _bulletSpeedBonus = value; }
    public int BulletCountBonus { get => _bulletCountBonus; set => _bulletCountBonus = value; }
    public float RicochetChance { get => _ricochetChance; set => _ricochetChance = value; }
    public int RicochetMaxBounces { get => _ricochetMaxBounces; set => _ricochetMaxBounces = value; }
    public float BulletSizeBonus { get => _bulletSizeBonus; set => _bulletSizeBonus = value; }
    public float KnockbackBonus { get => _knockbackBonus; set => _knockbackBonus = value; }

    public float GetDotDurationMultiplier() => 1f + _dotDurationBonus;

    /// <summary>
    /// #26 获取 DOT 伤害倍率（含里程碑加成）
    /// 元素大师：集齐4种DOT子弹 → 全DOT伤害 +20%
    /// 连锁引爆：引爆命中>10敌人后3秒内 → DOT伤害翻倍
    /// </summary>
    public float GetDotDamageMultiplier()
    {
        float mult = 1f;
        // 元素大师：全 DOT 伤害 +20%
        if (_elementMasterTriggered)
            mult += 0.2f;
        // 连锁引爆：引爆后 3 秒内 DOT 伤害翻倍
        if (Time.time < _chainDetonateEndTime)
            mult *= 2f;
        return mult;
    }

    /// <summary>
    /// #26 连锁引爆是否激活（供 StatusEffectManager 查询）
    /// </summary>
    public bool IsChainDetonateActive => Time.time < _chainDetonateEndTime;

    /// <summary>
    /// #26 检查里程碑触发
    /// </summary>
    private void CheckMilestones()
    {
        // 元素大师：集齐 4 种不同 DOT 子弹类型
        if (!_elementMasterTriggered && _dotGuns.Count >= 4)
        {
            _elementMasterTriggered = true;
            DebugHelper.Log("[MagePassive] ★ MILESTONE: Element Master! All DOT damage +20%");

            // 显示里程碑弹字
            var player = GameReferences.Player;
            if (player != null)
                DamagePopup.Create(player.transform.position + Vector3.up * 2f,
                    0, new Color(1f, 0.85f, 0f), false, "★ ELEMENT MASTER");

            // 通知 StatusEffectManager 更新伤害倍率
            SyncDotDamageMultiplierToAll();
        }
    }

    /// <summary>
    /// #26 同步 DOT 伤害倍率到所有活跃敌人的 StatusEffectManager
    /// </summary>
    private void SyncDotDamageMultiplierToAll()
    {
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;
        float dmgMult = GetDotDamageMultiplier();
        for (int i = 0; i < enemies.Count; i++)
        {
            var sem = enemies[i]?.GetComponent<StatusEffectManager>();
            if (sem != null)
                sem.DotDamageMultiplier = dmgMult;
        }
    }

    public float GetDotCritChance()
    {
        float baseCrit = 0.05f;
        baseCrit += SaveManager.Instance?.GetPermanentBonus("crit_chance") ?? 0f;
        // #23 每解锁一种 DOT 子弹类型，暴击率 +2%
        baseCrit += _dotGuns.Count * 0.02f;
        return baseCrit;
    }

    public float GetDotCritMultiplier() => _dotCritMultiplier;

    public void AddDotDurationBonus(float bonus)
    {
        _dotDurationBonus += bonus;
        DebugHelper.Log($"[MagePassive] DOT Duration Bonus +{bonus * 100}%, Total: {_dotDurationBonus * 100}%");
    }

    /// <summary>
    /// 获取攻速倍率（急速加成后）— 25% 急速 = 冷却缩短至 80%
    /// </summary>
    public float GetAttackSpeedMultiplier() => Mathf.Max(0.2f, 1f - _attackSpeedBonus);

    /// <summary>
    /// 获取子弹速度倍率
    /// </summary>
    public float GetBulletSpeedMultiplier() => 1f + _bulletSpeedBonus;

    /// <summary>
    /// 解锁一种 DOT 子弹类型
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
                gun.upgradeLevel++; // #20 升级等级+1
                _dotGuns[i] = gun; // struct 需要重新赋值回 List
                DebugHelper.Log($"[MagePassive] Upgraded {type} DOT gun to Lv{gun.upgradeLevel}");
                return;
            }
        }

        _dotGuns.Add(new DotGunState
        {
            effectType = type, color = color, cooldown = cooldown,
            impactDamage = impactDmg, dotDps = dotDps, dotDuration = dotDuration,
            lastFireTime = -999f,
            upgradeLevel = 1 // #20 初始等级 1
        });
        DebugHelper.Log($"[MagePassive] Unlocked {type} DOT gun! (color={color})");

        // #26 解锁新子弹后检查里程碑
        CheckMilestones();
    }

    public void EnhanceAllDotGuns(float dpsMultiplier)
    {
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            gun.dotDps *= (1f + dpsMultiplier);
            _dotGuns[i] = gun; // struct 需要重新赋值回 List
        }
    }

    private void Awake()
    {
        // 缓存 WeaponController 引用，避免 Update 中每帧 GetComponent
        _weaponController = GetComponent<WeaponController>();

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
        if (_weaponController != null) dmgMult = _weaponController.DamageMultiplier;

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
        float bulletSpeedMult = GetBulletSpeedMultiplier();
        bool canCrit = true;

        // 子弹数量加成：默认1发，加上 BulletCountBonus
        int bulletCount = 1 + _bulletCountBonus;
        float spreadAngle = 15f; // 每发子弹散射角度

        // #15 弹幕>5时，前5发为普通散射，超出部分转为追踪弹
        const int MAX_NORMAL = 5;
        int normalCount = Mathf.Min(bulletCount, MAX_NORMAL);
        int homingCount = bulletCount - normalCount;

        // ── 普通散射子弹 ──
        for (int b = 0; b < normalCount; b++)
        {
            Vector2 fireDir = direction;
            if (normalCount > 1)
            {
                float angle = (b - (normalCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(
                    direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad),
                    direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)
                ).normalized;
            }

            // #7 使用 DotBulletFactory 工厂创建子弹，解耦新增类型与 MagePassive
            GameObject bullet = DotBulletFactory.Create(gun.effectType,
                transform.position, fireDir, gun, bulletSpeedMult, durMult, dmgMultiplier,
                canCrit, critChance, _dotCritMultiplier);

            // #13 共振(Resonance)：子弹碰撞体积
            ApplyBulletSizeBonus(bullet);

            // #20 DOT子弹升级视觉变化
            ApplyUpgradeVisual(bullet, gun);
        }

        // ── #15 追踪弹（弹幕>5时超出部分）──
        if (homingCount > 0)
        {
            float homingSpeed = 10f * bulletSpeedMult; // 略慢于普通子弹
            int homingDmg = Mathf.Max(1, gun.impactDamage);
            float homingSpread = 30f; // 追踪弹散射角度更大

            for (int h = 0; h < homingCount; h++)
            {
                Vector2 hDir = direction;
                if (homingCount > 1)
                {
                    float angle = (h - (homingCount - 1) / 2f) * homingSpread;
                    float rad = angle * Mathf.Deg2Rad;
                    hDir = new Vector2(
                        direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad),
                        direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)
                    ).normalized;
                }

                var homing = HomingProjectile.CreateDefault(
                    transform.position, hDir, homingDmg, homingSpeed, 5f, 6f);
                homing.SetDamageMultiplier(dmgMultiplier);
                homing.SetKnockback(_knockbackBonus > 0f ? 3f : 0f);

                // 为追踪弹附加 DOT 效果（通过 DotHomingBullet 桥接组件）
                var dotHoming = homing.gameObject.AddComponent<DotHomingBullet>();
                dotHoming.Init(gun, durMult, dmgMultiplier, canCrit, critChance, _dotCritMultiplier);

                // #13 共振：追踪弹也受体积加成
                ApplyBulletSizeBonus(homing.gameObject);
            }
        }
    }

    /// <summary>
    /// #20 DOT子弹升级视觉变化：每次升级子弹增大10%，颜色更亮，3级+添加尾迹
    /// </summary>
    private void ApplyUpgradeVisual(GameObject bullet, DotGunState gun)
    {
        if (bullet == null || gun.upgradeLevel <= 1) return;

        // 每级增大10%
        float scaleBonus = 1f + (gun.upgradeLevel - 1) * 0.1f;
        bullet.transform.localScale *= scaleBonus;

        // 颜色更亮（level 2+ 亮度递增）
        var sr = bullet.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 提高亮度：向白色靠近
            float brightness = Mathf.Min(0.3f, (gun.upgradeLevel - 1) * 0.1f);
            sr.color = Color.Lerp(sr.color, Color.white, brightness);
        }

        // 3级+添加简单发光子物体
        if (gun.upgradeLevel >= 3)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(bullet.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * 1.8f;

            var glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite = sr?.sprite;
            glowSr.color = new Color(gun.color.r, gun.color.g, gun.color.b, 0.25f);
            glowSr.sortingOrder = 14; // 子弹下方
        }

        // 缩放碰撞体
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box)
            box.size *= scaleBonus;
        else if (col != null && col is CircleCollider2D circle)
            circle.radius *= scaleBonus;
    }

    /// <summary>
    /// #13 共振(Resonance)：应用子弹碰撞体积加成
    /// </summary>
    private void ApplyBulletSizeBonus(GameObject bullet)
    {
        if (bullet == null || _bulletSizeBonus <= 0f) return;
        bullet.transform.localScale *= (1f + _bulletSizeBonus);
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box)
            box.size *= (1f + _bulletSizeBonus);
        else if (col != null && col is CircleCollider2D circle)
            circle.radius *= (1f + _bulletSizeBonus);
    }

    /// <summary>
    /// 引爆技能 + 屏幕抖动
    /// 优化：直接遍历 SpawnManager.ActiveEnemies，避免 Physics2D.OverlapCircleAll 全图扫描
    /// #22 增强：燃烧余烬 + 霜冻碎裂 + 层数联动
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
        float radiusSqr = _detonateRadius * _detonateRadius;

        // #22 收集全局引爆信息
        bool anyBurn = false, anyFrost = false;
        int maxBurnStacks = 0, maxFrostStacks = 0;

        // 直接遍历 SpawnManager 的活跃敌人列表，无需物理查询
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies == null || enemies.Count == 0) return false;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            // 距离粗筛（平方距离，避免开方）
            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > radiusSqr) continue;

            var d = enemy.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;

            bool hadEffect = false;
            int enemyDmg = 0;

            // 引爆 StatusEffectManager DOT（#22 使用 DetonateResult）
            var sem = enemy.GetComponent<StatusEffectManager>();
            if (sem != null && sem.HasAnyDot)
            {
                StatusEffectManager.DetonateResult detResult;
                int dmg = sem.Detonate(_detonateMultiplier, critChance, critMult, out detResult);
                if (dmg > 0) { totalDamage += dmg; enemyDmg += dmg; hadEffect = true; }

                // #22 收集全局燃烧/霜冻信息
                if (detResult.hadBurn)
                {
                    anyBurn = true;
                    maxBurnStacks = Mathf.Max(maxBurnStacks, detResult.burnStacks);
                }
                if (detResult.hadFrost)
                {
                    anyFrost = true;
                    maxFrostStacks = Mathf.Max(maxFrostStacks, detResult.frostStacks);
                }
            }

            // 引爆 Bleed
            var bleed = enemy.GetComponent<BleedEffect>();
            if (bleed != null)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.2f * _detonateMultiplier);
                d.TakeDamage(extra); totalDamage += extra; enemyDmg += extra; hadEffect = true;
            }

            // 引爆 Burn
            var burn = enemy.GetComponent<BurnStackEffect>();
            if (burn != null)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.15f * _detonateMultiplier);
                d.TakeDamage(extra); totalDamage += extra; enemyDmg += extra; hadEffect = true;
                anyBurn = true;
                maxBurnStacks = Mathf.Max(maxBurnStacks, burn.StackCount);
            }

            // 引爆 Poison
            var poison = enemy.GetComponent<PoisonStackEffect>();
            if (poison != null)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.15f * _detonateMultiplier);
                d.TakeDamage(extra); totalDamage += extra; enemyDmg += extra; hadEffect = true;
            }

            if (hadEffect)
            {
                enemiesHit++;
                CombatManager.CreateExplosionEffect(enemy.transform.position, 3f, new Color(1f, 0.3f, 0.8f), 0.6f);

                // #17 每个被引爆的敌人头顶显示紫色伤害弹字
                DamagePopup.Create(enemy.transform.position, enemyDmg,
                    new Color(1f, 0.3f, 0.8f), false);
            }
        }

        // #22 燃烧余烬效果：燃烧层数>10时，爆炸后在敌人位置留下火焰区域
        if (anyBurn && maxBurnStacks > 10)
        {
            SpawnEmberFireZones(enemies, radiusSqr, maxBurnStacks);
        }

        // #22 霜冻碎裂效果：霜冻引爆时，对周围敌人造成冰冻AOE
        if (anyFrost && maxFrostStacks > 0)
        {
            TriggerFrostShatter(enemies, radiusSqr, maxFrostStacks, critChance, critMult);
        }

        // #17 引爆视觉反馈：闪白 + 巨大伤害弹字
        if (enemiesHit > 0 && totalDamage > 0)
        {
            DetonateFlashEffect.Show(0.15f);
            DamagePopup.CreateDetonateTotal(transform.position, totalDamage, enemiesHit);
        }

        // 屏幕抖动（根据命中数调整强度）
        var cam = Camera.main;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null)
            {
                float intensity = Mathf.Clamp(1.5f + enemiesHit * 0.2f, 1.5f, 4f);
                float duration = Mathf.Clamp(0.5f + enemiesHit * 0.05f, 0.5f, 1.2f);
                shake.Shake(intensity, duration);
            }
        }

        // 播放引爆音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayDetonate();

        // 记录引爆伤害到 DamageMeter
        if (DamageMeter.Instance != null)
            DamageMeter.Instance.RecordDetonate(totalDamage, enemiesHit);

        // #26 连锁引爆里程碑：引爆命中 >10 个敌人 → 3秒内 DOT 伤害翻倍
        if (enemiesHit > 10)
        {
            _chainDetonateEndTime = Time.time + 3f;
            _lastDetonateEnemyCount = enemiesHit;
            SyncDotDamageMultiplierToAll();
            DebugHelper.Log($"[MagePassive] ★ MILESTONE: Chain Detonate! DOT damage x2 for 3s!");
        }

        DebugHelper.Log($"[MagePassive] DETONATE! Hit {enemiesHit} enemies for {totalDamage} total damage!" +
            (anyBurn && maxBurnStacks > 10 ? $" [EMBER x{maxBurnStacks}]" : "") +
            (anyFrost ? $" [FROST SHATTER x{maxFrostStacks}]" : "") +
            (enemiesHit > 10 ? " [CHAIN DETONATE x2 DOT for 3s]" : ""));
        return enemiesHit > 0;
    }

    /// <summary>
    /// #22 燃烧余烬：燃烧层数>10时引爆后在敌人位置留下火焰区域
    /// 每层燃烧造成 1 点/0.5秒的地面持续伤害，持续 3 秒
    /// </summary>
    private void SpawnEmberFireZones(IReadOnlyList<GameObject> enemies, float radiusSqr, int burnStacks)
    {
        int emberDmg = Mathf.Max(1, burnStacks); // 每层 1 点伤害
        int zonesCreated = 0;
        const int MAX_ZONES = 5; // 最多创建 5 个余烬区域，避免性能问题

        for (int i = 0; i < enemies.Count && zonesCreated < MAX_ZONES; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > radiusSqr) continue;

            // 在敌人位置创建余烬火焰区域
            FireZone.CreateDefault(enemy.transform.position, emberDmg, 3f, 1.5f, 0.5f);
            zonesCreated++;
        }

        if (zonesCreated > 0)
            DebugHelper.Log($"[MagePassive] EMBER! Created {zonesCreated} fire zones, {emberDmg} dmg each");
    }

    /// <summary>
    /// #22 霜冻碎裂：霜冻引爆时对周围敌人造成冰冻 AOE 伤害 + 减速
    /// 伤害基于霜冻层数：每层 5 点 AOE 伤害
    /// </summary>
    private void TriggerFrostShatter(IReadOnlyList<GameObject> enemies, float radiusSqr, int frostStacks,
        float critChance, float critMult)
    {
        float shatterRadius = 4f; // 碎裂范围
        float shatterRadiusSqr = shatterRadius * shatterRadius;
        int shatterDmg = Mathf.Max(1, frostStacks * 5); // 每层 5 点碎裂伤害
        int targetsHit = 0;

        // 对范围内的所有敌人造成冰冻 AOE
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            // 使用碎裂半径（可能比引爆半径小）
            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > shatterRadiusSqr) continue;

            var d = enemy.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;

            // 碎裂伤害（可暴击）
            int finalDmg = shatterDmg;
            if (Random.value < critChance)
                finalDmg = Mathf.RoundToInt(finalDmg * critMult);

            d.TakeDamage(finalDmg);
            targetsHit++;

            // 对被碎裂命中的敌人施加霜冻减速
            var sem = enemy.GetComponent<StatusEffectManager>();
            if (sem == null) sem = enemy.gameObject.AddComponent<StatusEffectManager>();
            sem.ApplyEffect(StatusEffectType.Frostbite, 1f, 2f);
        }

        // 碎裂视觉效果：冰蓝色爆炸
        if (targetsHit > 0)
        {
            CombatManager.CreateExplosionEffect(transform.position, shatterRadius,
                new Color(0.4f, 0.7f, 1f), 0.5f);
            DebugHelper.Log($"[MagePassive] FROST SHATTER! {targetsHit} targets, {shatterDmg} dmg each");
        }
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
        public int upgradeLevel; // #20 升级等级（每次重复选择+1）
    }

    // ═══ #38 统一升级应用接口 ═══

    private MageUpgradeConfig _upgradeConfig;

    /// <summary>
    /// 设置升级配置（由 GameSceneBootstrap 在初始化时注入）
    /// </summary>
    public void SetUpgradeConfig(MageUpgradeConfig config)
    {
        _upgradeConfig = config;
    }

    /// <summary>
    /// 获取当前升级配置（供 LevelUpUI 等外部访问）
    /// </summary>
    public MageUpgradeConfig GetUpgradeConfig()
    {
        return _upgradeConfig;
    }

    /// <summary>
    /// 统一升级应用接口 — 根据 upgradeId 应用对应的升级效果。
    /// 配置数据从 MageUpgradeConfig 读取，不再硬编码在代码中。
    ///
    /// 返回 true 表示成功应用，false 表示未找到对应升级。
    /// </summary>
    public bool ApplyUpgrade(string upgradeId)
    {
        if (_upgradeConfig == null)
        {
            DebugHelper.LogError("[MagePassive] ApplyUpgrade: MageUpgradeConfig not set!");
            return false;
        }

        // ── 检查是否是 DOT 子弹枪 ──
        var dotGunEntry = _upgradeConfig.GetDotGunEntry(upgradeId);
        if (dotGunEntry.HasValue)
        {
            var dg = dotGunEntry.Value;
            UnlockDotGun(dg.effectType, dg.color, dg.cooldown, dg.impactDmg, dg.dotDps, dg.dotDuration);
            DebugHelper.Log($"[MagePassive] Unlocked DOT gun: {dg.displayName}");
            return true;
        }

        // ── 检查是否是增强升级 ──
        var upgradeEntry = _upgradeConfig.GetUpgradeEntry(upgradeId);
        if (!upgradeEntry.HasValue)
        {
            DebugHelper.LogWarning($"[MagePassive] ApplyUpgrade: unknown upgradeId '{upgradeId}'");
            return false;
        }

        var ue = upgradeEntry.Value;
        switch (ue.category)
        {
            case CharacterUpgradeOption.UpgradeCategory.ArmorReduction:
                _corrosionArmorReduction += ue.value1;
                DebugHelper.Log($"[MagePassive] Corrosion armor reduction +{ue.value1 * 100}%");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DotSpread:
                _curseSpreadTargets += (int)ue.value1;
                DebugHelper.Log($"[MagePassive] Curse spread targets +{(int)ue.value1}");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DotFrequency:
                _dotFrequencyBonus += ue.value1;
                DebugHelper.Log($"[MagePassive] DOT frequency bonus +{ue.value1 * 100}%");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DotCritBurst:
                _dotCritBurstChance += ue.value1;
                DebugHelper.Log($"[MagePassive] DOT crit burst chance +{ue.value1 * 100}%");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier:
                _detonateMultiplier += ue.value1;
                DebugHelper.Log($"[MagePassive] Detonate multiplier +{ue.value1}");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DetonateAbility:
                _detonateCooldown *= (1f - ue.value1);
                DebugHelper.Log($"[MagePassive] Detonate cooldown -{ue.value1 * 100}%");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DotTrigger:
                _erosionTriggerCount = Mathf.Max(2, _erosionTriggerCount - (int)ue.value1);
                _erosionDamagePercent += ue.value2;
                DebugHelper.Log($"[MagePassive] Erosion trigger count -> {_erosionTriggerCount}, dmg +{ue.value2 * 100}%");
                break;

            case CharacterUpgradeOption.UpgradeCategory.AttackSpeed:
                _attackSpeedBonus += ue.value1;
                _bulletSpeedBonus += ue.value2;
                DebugHelper.Log($"[MagePassive] Attack speed +{ue.value1 * 100}%, bullet speed +{ue.value2 * 100}%");
                break;

            case CharacterUpgradeOption.UpgradeCategory.BulletCount:
                _bulletCountBonus += (int)ue.value1;
                DebugHelper.Log($"[MagePassive] Bullet count +{(int)ue.value1}");
                break;

            case CharacterUpgradeOption.UpgradeCategory.Ricochet:
                _ricochetChance += ue.value1;
                if (_ricochetChance > 1f)
                {
                    _ricochetMaxBounces += 1;
                    _ricochetChance -= 1f;
                }
                DebugHelper.Log($"[MagePassive] Ricochet chance +{ue.value1 * 100}%, max bounces: {_ricochetMaxBounces}");
                break;

            case CharacterUpgradeOption.UpgradeCategory.BulletSize:
                _bulletSizeBonus += ue.value1;
                _knockbackBonus += ue.value2;
                DebugHelper.Log($"[MagePassive] Bullet size +{ue.value1 * 100}%, knockback +{ue.value2 * 100}%");
                break;

            default:
                DebugHelper.Log($"[MagePassive] Applied upgrade: {ue.upgradeName} ({ue.category})");
                break;
        }

        return true;
    }
}
