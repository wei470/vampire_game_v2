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

    [Header("#12 连锁引爆")]
    [SerializeField] private int _maxChainCount = 3;           // 最大连锁次数
    [SerializeField] private float _chainRadius = 10f;         // 二次引爆范围
    [SerializeField] private float _chainDamageRatio = 0.5f;   // 二次引爆伤害比例
    #pragma warning disable CS0414
    [SerializeField] private float _chainWaveSpeed = 50f;      // 波浪扩散速度（格/秒）
    #pragma warning restore CS0414

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

    [Header("#5 蓄力引爆")]
    [SerializeField] private float _chargeMoveSpeedPenalty = 0.3f;    // 蓄力时移速减少
    [SerializeField] private float _chargeMaxTime = 3f;                // 最大蓄力时间

    [Header("运行时状态")]
    [SerializeField] private float _lastDetonateTime = -999f;
    // #5 蓄力状态
    private bool _isCharging = false;
    private float _chargeStartTime = 0f;

    private List<DotGunState> _dotGuns = new List<DotGunState>();
    private WeaponController _weaponController; // 缓存引用，避免每帧 GetComponent

    // ── #26 里程碑系统 ──
    private bool _elementMasterTriggered = false;  // 集齐4种DOT子弹
    private float _chainDetonateEndTime = 0f;       // 连锁引爆结束时间
    private int _lastDetonateEnemyCount = 0;        // 上次引爆命中敌人数量

    // ── #4 协同系统 ──
    private HashSet<string> _activeSynergies = new HashSet<string>();

    // ── #9 DOT 进化系统 ──
    private HashSet<StatusEffectType> _evolvedTypes = new HashSet<StatusEffectType>();

    /// <summary>
    /// #9 查询某种 DOT 子弹是否已进化
    /// </summary>
    public bool IsEvolved(StatusEffectType type) => _evolvedTypes.Contains(type);

    /// <summary>
    /// #9 查询所有已进化类型（供 MageStatsHUD 显示）
    /// </summary>
    public HashSet<StatusEffectType> EvolvedTypes => _evolvedTypes;

    // ── 公共属性 ──
    public float DetonateCooldown => _detonateCooldown;
    public float DetonateCooldownRemaining => Mathf.Max(0f, _detonateCooldown - (Time.time - _lastDetonateTime));
    public bool DetonateReady => Time.time - _lastDetonateTime >= _detonateCooldown;
    public float DetonateMultiplier { get => _detonateMultiplier; set => _detonateMultiplier = value; }
    public float DetonateCooldownValue { get => _detonateCooldown; set => _detonateCooldown = value; }
    public List<DotGunState> DotGuns => _dotGuns;
    public HashSet<string> ActiveSynergies => _activeSynergies; // #4 已激活的协同

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
    /// DOT 持续时间倍率（属性形式，供 UI 显示）
    /// </summary>
    public float DotDurationMultiplier => GetDotDurationMultiplier();
    /// <summary>
    /// DOT 暴击倍率
    /// </summary>
    public float CritMultiplier => _dotCritMultiplier;
    /// <summary>
    /// DOT 暴击率（默认 5%）
    /// </summary>
    public float CritChance => 0.05f;

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
                // #9 检查进化：Lv5 时触发进化
                if (gun.upgradeLevel >= 5 && !_evolvedTypes.Contains(type))
                    TriggerEvolution(type);
                return;
            }
        }

        _dotGuns.Add(new DotGunState
        {
            effectType = type, color = color, cooldown = cooldown,
            impactDamage = impactDmg, dotDps = dotDps, dotDuration = dotDuration,
            lastFireTime = Time.time, // 使用当前时间，避免立即射击
            upgradeLevel = 1 // #20 初始等级 1
        });
        DebugHelper.Log($"[MagePassive] Unlocked {type} DOT gun! (color={color})");

        // #26 解锁新子弹后检查里程碑
        CheckMilestones();
    }

    /// <summary>
    /// #9 触发 DOT 子弹进化 — 当某种 DOT 子弹达到 Lv5 时自动进化
    /// 进化后该种子弹获得独特强化效果
    /// </summary>
    private void TriggerEvolution(StatusEffectType type)
    {
        _evolvedTypes.Add(type);

        // 获取进化信息
        var evo = GetEvolutionInfo(type);
        DebugHelper.Log($"[MagePassive] ✦ EVOLVED: {evo.name}! ({evo.description})");

        // 显示金色进化弹字
        var player = GameReferences.Player;
        if (player != null)
            DamagePopup.Create(player.transform.position + Vector3.up * 3f,
                0, new Color(1f, 0.85f, 0f), false, $"✦ EVOLVED: {evo.name}!");

        // 播放升级音效（增强版）
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        // 应用进化数值效果
        ApplyEvolutionBonus(type);
    }

    /// <summary>
    /// #9 获取进化信息
    /// </summary>
    private (string name, string description) GetEvolutionInfo(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:
                return ("血之狂潮 (Blood Tide)", "流血 DPS x2，流血敌人死亡时爆炸造成范围伤害");
            case StatusEffectType.Poison:
                return ("瘟疫之源 (Plague Source)", "中毒层数无上限，毒液池持续时间 x2");
            case StatusEffectType.Burn:
                return ("地狱之火 (Hellfire)", "燃烧蔓延到周围敌人");
            case StatusEffectType.Frostbite:
                return ("绝对零度 (Absolute Zero)", "霜冻敌人被引爆时碎裂为冰刺");
            default:
                return ("Unknown Evolution", "");
        }
    }

    /// <summary>
    /// #9 应用进化数值加成
    /// </summary>
    private void ApplyEvolutionBonus(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:
                // 血之狂潮：流血 DPS x2
                for (int i = 0; i < _dotGuns.Count; i++)
                {
                    var gun = _dotGuns[i];
                    if (gun.effectType == StatusEffectType.Bleed)
                    {
                        gun.dotDps *= 2f;
                        _dotGuns[i] = gun;
                        break;
                    }
                }
                break;

            case StatusEffectType.Poison:
                // 瘟疫之源：中毒子弹 DPS +50%
                for (int i = 0; i < _dotGuns.Count; i++)
                {
                    var gun = _dotGuns[i];
                    if (gun.effectType == StatusEffectType.Poison)
                    {
                        gun.dotDps *= 1.5f;
                        _dotGuns[i] = gun;
                        break;
                    }
                }
                break;

            case StatusEffectType.Burn:
                // 地狱之火：燃烧 DPS +80%
                for (int i = 0; i < _dotGuns.Count; i++)
                {
                    var gun = _dotGuns[i];
                    if (gun.effectType == StatusEffectType.Burn)
                    {
                        gun.dotDps *= 1.8f;
                        _dotGuns[i] = gun;
                        break;
                    }
                }
                break;

            case StatusEffectType.Frostbite:
                // 绝对零度：霜冻 DPS +100%
                for (int i = 0; i < _dotGuns.Count; i++)
                {
                    var gun = _dotGuns[i];
                    if (gun.effectType == StatusEffectType.Frostbite)
                    {
                        gun.dotDps *= 2f;
                        _dotGuns[i] = gun;
                        break;
                    }
                }
                break;
        }
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

    /// <summary>
    /// 清空所有 DOT 子弹（Test 模式专用）
    /// 用于在 test 模式下重新配置初始子弹
    /// </summary>
    public void ClearAllDotGuns()
    {
        _dotGuns.Clear();
        _elementMasterTriggered = false;
        DebugHelper.Log("[MagePassive] All DOT guns cleared (Test mode)");
    }

    private void Awake()
    {
        // 缓存 WeaponController 引用，避免 Update 中每帧 GetComponent
        _weaponController = GetComponent<WeaponController>();

        // Mage 默认自带毒子弹（可叠加中毒，2 DPS）
        UnlockDotGun(StatusEffectType.Poison, new Color(0.1f, 0.8f, 0.2f), 1.5f, 0, 2f, 5f);
    }

    /// <summary>
    /// #5 蓄力引爆公共查询属性（供 SkillHUD 显示）
    /// </summary>
    public bool IsCharging => _isCharging;
    public float ChargeProgress => _isCharging ? Mathf.Clamp01((Time.time - _chargeStartTime) / _chargeMaxTime) : 0f;
    public float ChargeMultiplier => GetChargeMultiplier(GetChargeDuration());

    /// <summary>
    /// #5 获取当前蓄力时长
    /// </summary>
    private float GetChargeDuration()
    {
        return _isCharging ? (Time.time - _chargeStartTime) : 0f;
    }

    /// <summary>
    /// #5 根据蓄力时长计算引爆倍率倍增
    /// 0-1秒: 1.0x（基础）
    /// 1-2秒: 1.5x + 范围+20%
    /// 2-3秒: 2.0x + 范围+50% + 敌人减速50%
    /// 3秒+:  3.0x + 引爆后留辐射区域
    /// </summary>
    private float GetChargeMultiplier(float chargeTime)
    {
        if (chargeTime >= 3f) return 3f;
        if (chargeTime >= 2f) return 2f;
        if (chargeTime >= 1f) return 1.5f;
        return 1f;
    }

    /// <summary>
    /// #5 获取蓄力范围加成
    /// </summary>
    private float GetChargeRadiusBonus(float chargeTime)
    {
        if (chargeTime >= 3f) return 1.5f;
        if (chargeTime >= 2f) return 1.5f;
        if (chargeTime >= 1f) return 1.2f;
        return 1f;
    }

    /// <summary>
    /// #5 获取蓄力时移动速度倍率
    /// </summary>
    public float GetChargeMoveSpeedMultiplier()
    {
        if (!_isCharging) return 1f;
        return 1f - _chargeMoveSpeedPenalty;
    }

    private void Update()
    {
        // #5 蓄力引爆：长按 E 蓄力，松开引爆
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            // E 键按下开始蓄力
            if (kb.eKey.wasPressedThisFrame && DetonateReady && !_isCharging)
            {
                _isCharging = true;
                _chargeStartTime = Time.time;
            }

            // E 键松开 或 蓄力已满 → 执行引爆
            if (_isCharging && (kb.eKey.wasReleasedThisFrame || GetChargeDuration() >= _chargeMaxTime))
            {
                DetonateWithCharge();
            }

            // 蓄力中被打断（受伤时可选，当前只检查按键释放）
        }

        // 自动发射所有就绪的 DOT 子弹 — 只有 Playing 状态才允许射击
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
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
                _dotGuns[i] = gun; // 关键修复：struct 是值类型，必须写回 List
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
    /// #5 蓄力引爆执行：应用蓄力倍率后调用 Detonate
    /// </summary>
    private bool DetonateWithCharge()
    {
        if (!_isCharging)
        {
            // 非蓄力状态直接引爆（兼容旧路径）
            return Detonate();
        }

        float chargeTime = GetChargeDuration();
        float chargeMult = GetChargeMultiplier(chargeTime);
        float chargeRadiusBonus = GetChargeRadiusBonus(chargeTime);

        // 临时修改引爆参数
        float origMult = _detonateMultiplier;
        float origRadius = _detonateRadius;

        _detonateMultiplier *= chargeMult;
        _detonateRadius *= chargeRadiusBonus;

        // 重置蓄力状态
        _isCharging = false;

        // 执行引爆
        bool result = Detonate();

        // 2-3秒蓄力：引爆后 2 秒内敌人减速 50%
        if (chargeTime >= 2f && result)
        {
            ApplyChargeSlowdown(0.5f, 2f);
        }

        // 3秒+蓄力：引爆后留下辐射区域
        if (chargeTime >= 3f && result)
        {
            SpawnChargeRadiationZone();
        }

        // 恢复原始参数
        _detonateMultiplier = origMult;
        _detonateRadius = origRadius;

        DebugHelper.Log($"[MagePassive] CHARGE DETONATE! Charge: {chargeTime:F1}s, Mult: {chargeMult}x, Radius: {chargeRadiusBonus}x");
        return result;
    }

    /// <summary>
    /// #5 蓄力引爆附加减速效果
    /// </summary>
    private void ApplyChargeSlowdown(float slowPercent, float duration)
    {
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;

        float radiusSqr = _detonateRadius * _detonateRadius;
        int slowed = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > radiusSqr) continue;

            var sem = enemy.GetComponent<StatusEffectManager>();
            if (sem == null) sem = enemy.gameObject.AddComponent<StatusEffectManager>();
            // 通过霜冻效果施加减速
            sem.ApplyEffect(StatusEffectType.Frostbite, duration, 2f);
            slowed++;
        }
        if (slowed > 0)
            DebugHelper.Log($"[MagePassive] Charge slowdown applied to {slowed} enemies");
    }

    /// <summary>
    /// #5 满蓄力辐射区域
    /// </summary>
    private void SpawnChargeRadiationZone()
    {
        // 在玩家位置留下辐射区域，持续 5 秒，造成 DOT 伤害
        FireZone.CreateDefault(transform.position, 8, 5f, 4f, 0.5f);
        DebugHelper.Log("[MagePassive] Charge RADIATION zone spawned!");
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
        var cam = GameReferences.MainCamera;
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

        // #4 末日审判协同：引爆后 3 秒内 DOT 频率翻倍
        if (enemiesHit > 0 && _activeSynergies.Contains("judgment_day"))
        {
            ActivateJudgmentDay();
        }

        DebugHelper.Log($"[MagePassive] DETONATE! Hit {enemiesHit} enemies for {totalDamage} total damage!" +
            (anyBurn && maxBurnStacks > 10 ? $" [EMBER x{maxBurnStacks}]" : "") +
            (anyFrost ? $" [FROST SHATTER x{maxFrostStacks}]" : "") +
            (enemiesHit > 10 ? " [CHAIN DETONATE x2 DOT for 3s]" : ""));

        // #12 连锁引爆：被引爆的敌人如果带有诅咒(Contaminate)，死亡后触发二次引爆
        if (enemiesHit > 0)
        {
            TryChainDetonate(enemies, radiusSqr, critChance, critMult, 0);
        }

        return enemiesHit > 0;
    }

    /// <summary>
    /// #12 连锁引爆：从被引爆的敌人位置向外扩散波浪式引爆
    /// 带有诅咒(Contaminate)的敌人死亡时触发二次引爆（范围 _chainRadius）
    /// 二次引爆伤害 = 原始引爆伤害 × _chainDamageRatio
    /// 最大连锁次数 = _maxChainCount（防止无限连锁）
    /// </summary>
    private void TryChainDetonate(IReadOnlyList<GameObject> enemies, float originalRadiusSqr,
        float critChance, float critMult, int currentChain)
    {
        if (currentChain >= _maxChainCount) return;

        float chainRadiusSqr = _chainRadius * _chainRadius;
        int chainDamage = 0;
        int chainHits = 0;
        List<GameObject> chainTargets = new List<GameObject>();

        // 遍历所有在原始引爆范围内的敌人，查找带有 DOT 的目标进行二次引爆
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > originalRadiusSqr) continue;

            var d = enemy.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;

            // 检查是否还有 DOT 效果（二次引爆）
            var sem = enemy.GetComponent<StatusEffectManager>();
            if (sem != null && sem.HasAnyDot)
            {
                StatusEffectManager.DetonateResult detResult;
                int dmg = sem.Detonate(_detonateMultiplier * _chainDamageRatio, critChance, critMult, out detResult);
                if (dmg > 0)
                {
                    chainDamage += dmg;
                    chainHits++;
                    chainTargets.Add(enemy);

                    // 紫色冲击波视觉效果
                    CombatManager.CreateExplosionEffect(enemy.transform.position, 2f,
                        new Color(0.6f, 0.1f, 0.9f), 0.4f);
                    DamagePopup.Create(enemy.transform.position, dmg,
                        new Color(0.6f, 0.1f, 0.9f), false);
                }
            }

            // 也检查独立 DOT 组件
            var bleed = enemy.GetComponent<BleedEffect>();
            var burn = enemy.GetComponent<BurnStackEffect>();
            var poison = enemy.GetComponent<PoisonStackEffect>();
            if ((bleed != null || burn != null || poison != null) && d != null && d.CurrentHp > 0)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.1f * _detonateMultiplier * _chainDamageRatio);
                if (extra > 0)
                {
                    d.TakeDamage(extra);
                    chainDamage += extra;
                    chainHits++;
                }
            }
        }

        if (chainHits > 0)
        {
            DebugHelper.Log($"[MagePassive] ⛓️ CHAIN DETONATE #{currentChain + 1}! " +
                $"Hit {chainHits} enemies for {chainDamage} damage!");

            // 屏幕抖动（连锁引爆有独特的轻微抖动）
            var cam = GameReferences.MainCamera;
            if (cam != null)
            {
                var shake = cam.GetComponent<ScreenShake>();
                if (shake != null)
                    shake.Shake(1f + currentChain * 0.5f, 0.3f + currentChain * 0.1f);
            }

            // 递归连锁：从被引爆的敌人位置向外扩散
            // 使用延迟模拟波浪扩散效果（0.1秒间隔）
            if (currentChain + 1 < _maxChainCount)
            {
                int nextChain = currentChain + 1;
                // 收集下一轮连锁的源位置
                List<Vector3> chainSources = new List<Vector3>();
                for (int i = 0; i < chainTargets.Count; i++)
                {
                    if (chainTargets[i] != null)
                        chainSources.Add(chainTargets[i].transform.position);
                }

                // 启动协程延迟执行下一轮连锁
                StartCoroutine(ChainDetonateWave(chainSources, critChance, critMult, nextChain));
            }
        }
    }

    /// <summary>
    /// #12 连锁引爆波浪扩散协程
    /// 从多个源位置向外扩散，每 0.1 秒扩展一轮
    /// </summary>
    private System.Collections.IEnumerator ChainDetonateWave(List<Vector3> sources,
        float critChance, float critMult, int chainLevel)
    {
        // 波浪扩散延迟（基于连锁等级，越远延迟越大）
        float delay = 0.1f * chainLevel;
        yield return new WaitForSeconds(delay);

        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies == null || enemies.Count == 0) yield break;

        float chainRadiusSqr = _chainRadius * _chainRadius;
        int chainDamage = 0;
        int chainHits = 0;
        List<GameObject> nextChainTargets = new List<GameObject>();

        HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

        for (int s = 0; s < sources.Count; s++)
        {
            Vector3 sourcePos = sources[s];

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.activeInHierarchy) continue;
                if (alreadyHit.Contains(enemy)) continue;

                Vector2 delta = (Vector2)(enemy.transform.position - sourcePos);
                if (delta.sqrMagnitude > chainRadiusSqr) continue;

                var d = enemy.GetComponent<Damageable>();
                if (d == null || d.CurrentHp <= 0) continue;

                alreadyHit.Add(enemy);

                // 连锁引爆 DOT
                var sem = enemy.GetComponent<StatusEffectManager>();
                if (sem != null && sem.HasAnyDot)
                {
                    StatusEffectManager.DetonateResult detResult;
                    float chainMult = _detonateMultiplier * _chainDamageRatio * Mathf.Pow(0.7f, chainLevel);
                    int dmg = sem.Detonate(chainMult, critChance, critMult, out detResult);
                    if (dmg > 0)
                    {
                        chainDamage += dmg;
                        chainHits++;
                        nextChainTargets.Add(enemy);
                    }
                }

                // 独立 DOT 组件引爆
                var bleed = enemy.GetComponent<BleedEffect>();
                var burnC = enemy.GetComponent<BurnStackEffect>();
                var poison = enemy.GetComponent<PoisonStackEffect>();
                if ((bleed != null || burnC != null || poison != null) && d != null && d.CurrentHp > 0)
                {
                    float chainMult = _chainDamageRatio * Mathf.Pow(0.7f, chainLevel);
                    int extra = Mathf.RoundToInt(d.MaxHp * 0.08f * _detonateMultiplier * chainMult);
                    if (extra > 0)
                    {
                        d.TakeDamage(extra);
                        chainDamage += extra;
                        chainHits++;
                    }
                }

                // 连锁视觉效果：紫色冲击波
                CombatManager.CreateExplosionEffect(enemy.transform.position, 1.5f + chainLevel * 0.3f,
                    new Color(0.6f, 0.1f, 0.9f, 0.6f - chainLevel * 0.15f), 0.3f);
            }
        }

        if (chainHits > 0)
        {
            DebugHelper.Log($"[MagePassive] ⛓️ CHAIN WAVE #{chainLevel}! Hit {chainHits} for {chainDamage}");

            // 屏幕抖动
            var cam = GameReferences.MainCamera;
            if (cam != null)
            {
                var shake = cam.GetComponent<ScreenShake>();
                if (shake != null)
                    shake.Shake(0.8f + chainLevel * 0.3f, 0.2f + chainLevel * 0.1f);
            }

            // 继续下一轮连锁
            if (chainLevel + 1 < _maxChainCount && nextChainTargets.Count > 0)
            {
                List<Vector3> nextSources = new List<Vector3>();
                for (int i = 0; i < nextChainTargets.Count; i++)
                {
                    if (nextChainTargets[i] != null)
                        nextSources.Add(nextChainTargets[i].transform.position);
                }
                if (nextSources.Count > 0)
                    StartCoroutine(ChainDetonateWave(nextSources, critChance, critMult, chainLevel + 1));
            }
        }
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

    // ═══ #4 协同升级系统 ═══

    /// <summary>
    /// 协同效果类型
    /// </summary>
    private enum SynergyType
    {
        Plague,        // 瘟疫：中毒+燃烧+腐蚀
        FrozenBlade,   // 冰封血刃：流血+霜冻+诅咒
        BulletStorm,   // 弹雨风暴：弹幕+急速+反弹
        JudgmentDay    // 末日审判：辐射+污染+侵蚀
    }

    /// <summary>
    /// #4 检查升级组合，激活协同效果
    /// </summary>
    private void CheckSynergies()
    {
        // ── 瘟疫：中毒 + 燃烧 + 腐蚀 ──
        bool hasPoison = false, hasBurn = false, hasCorrosion = false;
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            if (_dotGuns[i].effectType == StatusEffectType.Poison) hasPoison = true;
            if (_dotGuns[i].effectType == StatusEffectType.Burn) hasBurn = true;
        }
        // 通过 _customUpgradeStacks 在 LevelUpUI 中追踪，这里用属性检测
        // 腐蚀 = ArmorReduction > 0.1f（初始0.1，选了就是 > 0.1）
        hasCorrosion = _corrosionArmorReduction > 0.101f;

        TryActivateSynergy("plague", SynergyType.Plague, hasPoison && hasBurn && hasCorrosion);

        // ── 冰封血刃：流血 + 霜冻 + 诅咒 ──
        bool hasBleed = false, hasFrost = false, hasCurse = false;
        for (int i = 0; i < _dotGuns.Count; i++)
        {
            if (_dotGuns[i].effectType == StatusEffectType.Bleed) hasBleed = true;
            if (_dotGuns[i].effectType == StatusEffectType.Frostbite) hasFrost = true;
        }
        hasCurse = _curseSpreadTargets > 1; // 诅咒传播目标 > 1 说明选过诅咒

        TryActivateSynergy("frozen_blade", SynergyType.FrozenBlade, hasBleed && hasFrost && hasCurse);

        // ── 弹雨风暴：弹幕 + 急速 + 反弹 ──
        bool hasBarrage = _bulletCountBonus > 0;
        bool hasHaste = _attackSpeedBonus > 0.001f;
        bool hasRicochet = _ricochetChance > 0.001f;

        TryActivateSynergy("bullet_storm", SynergyType.BulletStorm, hasBarrage && hasHaste && hasRicochet);

        // ── 末日审判：辐射 + 污染 + 侵蚀 ──
        // 辐射 = _detonateMultiplier > 3f（初始3.0，选了就是 > 3.0）
        // 污染 = _detonateCooldown < 12f（初始12，选了就是 < 12）
        bool hasRadiate = _detonateMultiplier > 3.01f;
        bool hasContaminate = _detonateCooldown < 11.99f;
        bool hasErosion = _erosionDamagePercent > 0.001f;

        TryActivateSynergy("judgment_day", SynergyType.JudgmentDay, hasRadiate && hasContaminate && hasErosion);
    }

    /// <summary>
    /// #4 尝试激活一个协同效果，如果尚未激活且条件满足
    /// </summary>
    private void TryActivateSynergy(string synergyId, SynergyType type, bool conditionMet)
    {
        if (!conditionMet || _activeSynergies.Contains(synergyId)) return;

        _activeSynergies.Add(synergyId);
        ApplySynergyEffect(type);

        // 显示协同激活弹字
        var player = GameReferences.Player;
        if (player != null)
        {
            string synergyName = GetSynergyName(type);
            Color synergyColor = GetSynergyColor(type);
            DamagePopup.Create(player.transform.position + Vector3.up * 2.5f,
                0, synergyColor, false, $"✦ SYNERGY: {synergyName}!");
        }

        // 播放独特音效（复用升级音效，增强版）
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        DebugHelper.Log($"[MagePassive] ✦ SYNERGY ACTIVATED: {GetSynergyName(type)}! ({synergyId})");
    }

    /// <summary>
    /// #4 应用协同效果的实际数值
    /// </summary>
    private void ApplySynergyEffect(SynergyType type)
    {
        switch (type)
        {
            case SynergyType.Plague:
                // 瘟疫效果在 StatusEffectManager 的 PoisonStackEffect 中实现
                // 通过标记 _hasPlagueSynergy 让中毒敌人每秒对周围敌人造成 1 点传染伤害
                // 这里只记录状态，实际效果在 DOT 运行时检查
                DebugHelper.Log("[MagePassive] Plague synergy: Poisoned enemies spread 1 DPS to nearby enemies");
                break;

            case SynergyType.FrozenBlade:
                // 冰封血刃效果在 StatusEffectManager 的 BleedEffect 中实现
                // 流血 DOT 有 15% 概率冰冻敌人 0.5 秒
                DebugHelper.Log("[MagePassive] Frozen Blade synergy: Bleed has 15% chance to freeze 0.5s");
                break;

            case SynergyType.BulletStorm:
                // 弹雨风暴：子弹命中后 20% 概率分裂为 2 发小弹
                // 效果在子弹碰撞逻辑中检查
                DebugHelper.Log("[MagePassive] Bullet Storm synergy: 20% chance to split into 2 sub-bullets on hit");
                break;

            case SynergyType.JudgmentDay:
                // 末日审判：引爆后 3 秒内所有 DOT 频率翻倍
                // 效果在 Detonate() 方法中实现
                DebugHelper.Log("[MagePassive] Judgment Day synergy: Detonate doubles DOT frequency for 3s");
                break;
        }
    }

    private string GetSynergyName(SynergyType type)
    {
        switch (type)
        {
            case SynergyType.Plague:      return "瘟疫 (Plague)";
            case SynergyType.FrozenBlade: return "冰封血刃 (Frozen Blade)";
            case SynergyType.BulletStorm: return "弹雨风暴 (Bullet Storm)";
            case SynergyType.JudgmentDay: return "末日审判 (Judgment Day)";
            default: return "Unknown";
        }
    }

    private Color GetSynergyColor(SynergyType type)
    {
        switch (type)
        {
            case SynergyType.Plague:      return new Color(0.1f, 0.9f, 0.3f);  // 绿色
            case SynergyType.FrozenBlade: return new Color(0.4f, 0.7f, 1f);    // 冰蓝
            case SynergyType.BulletStorm: return new Color(1f, 0.9f, 0.2f);    // 金色
            case SynergyType.JudgmentDay: return new Color(1f, 0.3f, 0.1f);    // 火红
            default: return Color.white;
        }
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
            // #4 解锁新子弹后也检查协同
            CheckSynergies();
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

        // #4 每次升级后检查协同
        CheckSynergies();

        return true;
    }

    /// <summary>
    /// #4 获取当前 DOT 频率倍率（含协同加成）
    /// 末日审判：引爆后 3 秒内频率翻倍
    /// </summary>
    public float GetDotFrequencyMultiplier()
    {
        float mult = 1f - _dotFrequencyBonus; // 痛苦缩短间隔
        if (_activeSynergies.Contains("judgment_day") && Time.time < _judgmentDayEndTime)
            mult *= 0.5f; // 频率翻倍 = 间隔减半
        return Mathf.Max(0.1f, mult);
    }

    // ── 末日审判协同运行时状态 ──
    private float _judgmentDayEndTime = 0f;

    /// <summary>
    /// #4 末日审判：引爆后激活 DOT 频率翻倍 3 秒
    /// </summary>
    private void ActivateJudgmentDay()
    {
        _judgmentDayEndTime = Time.time + 3f;
        DebugHelper.Log("[MagePassive] ★ JUDGMENT DAY: DOT frequency doubled for 3s!");
    }

    /// <summary>
    /// #4 协同查询：瘟疫效果是否激活（中毒敌人对周围造成传染伤害）
    /// </summary>
    public bool HasPlagueSynergy => _activeSynergies.Contains("plague");

    /// <summary>
    /// #4 协同查询：冰封血刃是否激活（流血有概率冰冻）
    /// </summary>
    public bool HasFrozenBladeSynergy => _activeSynergies.Contains("frozen_blade");

    /// <summary>
    /// #4 协同查询：弹雨风暴是否激活（子弹命中分裂）
    /// </summary>
    public bool HasBulletStormSynergy => _activeSynergies.Contains("bullet_storm");

    /// <summary>
    /// #4 协同查询：末日审判是否激活（引爆后频率翻倍）
    /// </summary>
    public bool HasJudgmentDaySynergy => _activeSynergies.Contains("judgment_day");
}
