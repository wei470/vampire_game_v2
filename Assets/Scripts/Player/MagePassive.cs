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

    [Header("运行时状态")]
    [SerializeField] private float _lastDetonateTime = -999f;

    private List<DotGunState> _dotGuns = new List<DotGunState>();

    public float DetonateCooldown => _detonateCooldown;
    public float DetonateCooldownRemaining => Mathf.Max(0f, _detonateCooldown - (Time.time - _lastDetonateTime));
    public bool DetonateReady => Time.time - _lastDetonateTime >= _detonateCooldown;
    public float DetonateMultiplier { get => _detonateMultiplier; set => _detonateMultiplier = value; }
    public float DetonateCooldownValue { get => _detonateCooldown; set => _detonateCooldown = value; }
    public List<DotGunState> DotGuns => _dotGuns;

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

        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            if (Time.time - gun.lastFireTime >= gun.cooldown)
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

        switch (gun.effectType)
        {
            case StatusEffectType.Bleed:
                BleedBullet.Create(transform.position, direction, 14f, gun.impactDamage,
                    gun.dotDps, gun.dotDuration * durMult, dmgMultiplier,
                    canCrit, critChance, _dotCritMultiplier);
                break;

            case StatusEffectType.Poison:
                Vector2 target = (Vector2)transform.position + direction * 6f;
                PoisonPotion.Create(transform.position, target, 10f,
                    gun.dotDuration * durMult, 2f, gun.dotDps, dmgMultiplier,
                    canCrit, critChance, _dotCritMultiplier);
                break;

            case StatusEffectType.Burn:
                BurnBullet.Create(transform.position, direction, 8f, gun.impactDamage,
                    gun.dotDps, gun.dotDuration * durMult, dmgMultiplier,
                    canCrit, critChance, _dotCritMultiplier);
                break;

            case StatusEffectType.Frostbite:
                FrostBullet.Create(transform.position, direction, 20f, gun.impactDamage,
                    gun.dotDps, 1f, 0.3f, dmgMultiplier,
                    canCrit, critChance, _dotCritMultiplier);
                break;
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