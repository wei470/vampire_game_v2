using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 引爆系统 — 负责所有引爆相关逻辑（蓄力引爆、连锁引爆、余烬、碎裂）
/// 从 MagePassive 拆分而来
/// </summary>
public class DetonateSystem : MonoBehaviour
{
    [Header("引爆参数")]
    [SerializeField] private float _detonateCooldown = 12f;
    [SerializeField] private float _detonateMultiplier = 3f;
    [SerializeField] private float _detonateRadius = 50f;

    [Header("连锁引爆")]
    [SerializeField] private int _maxChainCount = 3;
    [SerializeField] private float _chainRadius = 10f;
    [SerializeField] private float _chainDamageRatio = 0.5f;

    [Header("蓄力引爆")]
    [SerializeField] private float _chargeMoveSpeedPenalty = 0.3f;
    [SerializeField] private float _chargeMaxTime = 3f;

    [Header("运行时状态")]
    [SerializeField] private float _lastDetonateTime = -999f;
    private bool _isCharging = false;
    private float _chargeStartTime = 0f;
    private int _lastDetonateEnemyCount = 0;

    // ── 里程碑状态 ──
    private float _chainDetonateEndTime = 0f;

    // ── 公共属性 ──
    public float DetonateCooldown => _detonateCooldown;
    public float DetonateCooldownRemaining => Mathf.Max(0f, _detonateCooldown - (Time.time - _lastDetonateTime));
    public bool DetonateReady => Time.time - _lastDetonateTime >= _detonateCooldown;
    public float DetonateMultiplier { get => _detonateMultiplier; set => _detonateMultiplier = value; }
    public float DetonateCooldownValue { get => _detonateCooldown; set => _detonateCooldown = value; }
    public bool IsCharging => _isCharging;
    public float ChargeProgress => _isCharging ? Mathf.Clamp01((Time.time - _chargeStartTime) / _chargeMaxTime) : 0f;
    public float ChargeMultiplier => GetChargeMultiplier(GetChargeDuration());
    public float ChargeMaxTime => _chargeMaxTime;
    public int LastDetonateEnemyCount => _lastDetonateEnemyCount;
    public bool IsChainDetonateActive => Time.time < _chainDetonateEndTime;
    public int MaxChainCount { get => _maxChainCount; set => _maxChainCount = value; }
    public float ChainRadius { get => _chainRadius; set => _chainRadius = value; }
    public float ChainDamageRatio { get => _chainDamageRatio; set => _chainDamageRatio = value; }

    // ── 引用 ──
    private MagePassive _magePassive;
    private ScreenShake _cachedScreenShake;

    public void Init(MagePassive magePassive)
    {
        _magePassive = magePassive;
        CacheScreenShake();
    }

    private void CacheScreenShake()
    {
        var cam = GameReferences.MainCamera;
        if (cam != null) cam.TryGetComponent(out _cachedScreenShake);
    }

    private ScreenShake GetScreenShake()
    {
        if (_cachedScreenShake == null) CacheScreenShake();
        return _cachedScreenShake;
    }

    /// <summary>
    /// 获取移动速度倍率（蓄力时减速）
    /// </summary>
    public float GetChargeMoveSpeedMultiplier()
    {
        if (!_isCharging) return 1f;
        return 1f - _chargeMoveSpeedPenalty;
    }

    /// <summary>
    /// 更新蓄力输入（由 MagePassive.Update 调用）
    /// </summary>
    public void UpdateChargeInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.eKey.wasPressedThisFrame && DetonateReady && !_isCharging)
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
        }

        if (_isCharging && (kb.eKey.wasReleasedThisFrame || GetChargeDuration() >= _chargeMaxTime))
        {
            DetonateWithCharge();
        }
    }

    private float GetChargeDuration()
    {
        return _isCharging ? (Time.time - _chargeStartTime) : 0f;
    }

    private float GetChargeMultiplier(float chargeTime)
    {
        if (chargeTime >= 3f) return 3f;
        if (chargeTime >= 2f) return 2f;
        if (chargeTime >= 1f) return 1.5f;
        return 1f;
    }

    private float GetChargeRadiusBonus(float chargeTime)
    {
        if (chargeTime >= 3f) return 1.5f;
        if (chargeTime >= 2f) return 1.5f;
        if (chargeTime >= 1f) return 1.2f;
        return 1f;
    }

    private bool DetonateWithCharge()
    {
        if (!_isCharging) return Detonate();

        float chargeTime = GetChargeDuration();
        float chargeMult = GetChargeMultiplier(chargeTime);
        float chargeRadiusBonus = GetChargeRadiusBonus(chargeTime);

        float origMult = _detonateMultiplier;
        float origRadius = _detonateRadius;
        _detonateMultiplier *= chargeMult;
        _detonateRadius *= chargeRadiusBonus;

        _isCharging = false;

        bool result = Detonate();

        if (chargeTime >= 2f && result)
            ApplyChargeSlowdown(0.5f, 2f);

        if (chargeTime >= 3f && result)
            SpawnChargeRadiationZone();

        _detonateMultiplier = origMult;
        _detonateRadius = origRadius;

        DebugHelper.Log($"[DetonateSystem] CHARGE DETONATE! Charge: {chargeTime:F1}s, Mult: {chargeMult}x");
        return result;
    }

    /// <summary>
    /// 引爆所有敌人 DOT
    /// </summary>
    public bool Detonate()
    {
        if (!DetonateReady)
        {
            DebugHelper.Log("[DetonateSystem] Detonate on cooldown");
            return false;
        }

        _lastDetonateTime = Time.time;
        float critChance = _magePassive.GetDotCritChance();
        float critMult = _magePassive.GetDotCritMultiplier();
        int totalDamage = 0;
        int enemiesHit = 0;
        float radiusSqr = _detonateRadius * _detonateRadius;

        bool anyBurn = false, anyFrost = false;
        int maxBurnStacks = 0, maxFrostStacks = 0;

        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies == null || enemies.Count == 0) return false;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > radiusSqr) continue;

            if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;

            bool hadEffect = false;
            int enemyDmg = 0;

            if (enemy.TryGetComponent<StatusEffectManager>(out var sem) && sem.HasAnyDot)
            {
                DetonateResult detResult;
                int dmg = sem.Detonate(_detonateMultiplier, critChance, critMult, out detResult);
                if (dmg > 0) { totalDamage += dmg; enemyDmg += dmg; hadEffect = true; }
                if (detResult.hadBurn) { anyBurn = true; maxBurnStacks = Mathf.Max(maxBurnStacks, detResult.burnStacks); }
                if (detResult.hadFrost) { anyFrost = true; maxFrostStacks = Mathf.Max(maxFrostStacks, detResult.frostStacks); }
            }

            if (enemy.TryGetComponent<BleedEffect>(out var bleed))
            { int extra = Mathf.RoundToInt(d.MaxHp * 0.2f * _detonateMultiplier); d.TakeDamage(extra); totalDamage += extra; enemyDmg += extra; hadEffect = true; }

            if (enemy.TryGetComponent<BurnStackEffect>(out var burn))
            { int extra = Mathf.RoundToInt(d.MaxHp * 0.15f * _detonateMultiplier); d.TakeDamage(extra); totalDamage += extra; enemyDmg += extra; hadEffect = true; anyBurn = true; maxBurnStacks = Mathf.Max(maxBurnStacks, burn.StackCount); }

            if (enemy.TryGetComponent<PoisonStackEffect>(out var poison))
            { int extra = Mathf.RoundToInt(d.MaxHp * 0.15f * _detonateMultiplier); d.TakeDamage(extra); totalDamage += extra; enemyDmg += extra; hadEffect = true; }

            if (hadEffect)
            {
                enemiesHit++;
                CombatManager.CreateExplosionEffect(enemy.transform.position, 3f, new Color(1f, 0.3f, 0.8f), 0.6f);
                DamagePopup.Create(enemy.transform.position, enemyDmg, new Color(1f, 0.3f, 0.8f), false);
            }
        }

        if (anyBurn && maxBurnStacks > 10)
            SpawnEmberFireZones(enemies, radiusSqr, maxBurnStacks);

        if (anyFrost && maxFrostStacks > 0)
            TriggerFrostShatter(enemies, radiusSqr, maxFrostStacks, critChance, critMult);

        if (enemiesHit > 0 && totalDamage > 0)
        {
            DetonateFlashEffect.Show(0.15f);
            DamagePopup.CreateDetonateTotal(transform.position, totalDamage, enemiesHit);
        }

        var shake = GetScreenShake();
        if (shake != null)
        {
            float intensity = Mathf.Clamp(1.5f + enemiesHit * 0.2f, 1.5f, 4f);
            float duration = Mathf.Clamp(0.5f + enemiesHit * 0.05f, 0.5f, 1.2f);
            shake.Shake(intensity, duration);
        }

        if (SFXManager.Instance != null) SFXManager.Instance.PlayDetonate();
        if (DamageMeter.Instance != null) DamageMeter.Instance.RecordDetonate(totalDamage, enemiesHit);

        if (enemiesHit > 10)
        {
            _chainDetonateEndTime = Time.time + 3f;
            _lastDetonateEnemyCount = enemiesHit;
            _magePassive.SyncDotDamageMultiplierToAll();
        }

        if (enemiesHit > 0)
            TryChainDetonate(enemies, radiusSqr, critChance, critMult, 0);

        DebugHelper.Log($"[DetonateSystem] DETONATE! Hit {enemiesHit} enemies for {totalDamage} total damage!");
        return enemiesHit > 0;
    }

    private void TryChainDetonate(IReadOnlyList<GameObject> enemies, float originalRadiusSqr, float critChance, float critMult, int currentChain)
    {
        if (currentChain >= _maxChainCount) return;
        float chainRadiusSqr = _chainRadius * _chainRadius;
        int chainDamage = 0, chainHits = 0;
        List<GameObject> chainTargets = new List<GameObject>();

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > originalRadiusSqr) continue;
            if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;

            if (enemy.TryGetComponent<StatusEffectManager>(out var sem) && sem.HasAnyDot)
            {
                DetonateResult detResult;
                int dmg = sem.Detonate(_detonateMultiplier * _chainDamageRatio, critChance, critMult, out detResult);
                if (dmg > 0) { chainDamage += dmg; chainHits++; chainTargets.Add(enemy);
                    CombatManager.CreateExplosionEffect(enemy.transform.position, 2f, new Color(0.6f, 0.1f, 0.9f), 0.4f);
                    DamagePopup.Create(enemy.transform.position, dmg, new Color(0.6f, 0.1f, 0.9f), false); }
            }

            bool hasAnyDotEffect = enemy.TryGetComponent<BleedEffect>(out _)
                                 || enemy.TryGetComponent<BurnStackEffect>(out _)
                                 || enemy.TryGetComponent<PoisonStackEffect>(out _);
            if (hasAnyDotEffect && d.CurrentHp > 0)
            {
                int extra = Mathf.RoundToInt(d.MaxHp * 0.1f * _detonateMultiplier * _chainDamageRatio);
                if (extra > 0) { d.TakeDamage(extra); chainDamage += extra; chainHits++; }
            }
        }

        if (chainHits > 0)
        {
            DebugHelper.Log($"[DetonateSystem] ⛓️ CHAIN DETONATE #{currentChain + 1}! Hit {chainHits} enemies for {chainDamage}");
            var shake2 = GetScreenShake();
            if (shake2 != null) shake2.Shake(1f + currentChain * 0.5f, 0.3f + currentChain * 0.1f);
            if (currentChain + 1 < _maxChainCount)
            {
                List<Vector3> chainSources = new List<Vector3>();
                for (int i = 0; i < chainTargets.Count; i++)
                    if (chainTargets[i] != null) chainSources.Add(chainTargets[i].transform.position);
                if (chainSources.Count > 0)
                    StartCoroutine(ChainDetonateWave(chainSources, critChance, critMult, currentChain + 1));
            }
        }
    }

    private IEnumerator ChainDetonateWave(List<Vector3> sources, float critChance, float critMult, int chainLevel)
    {
        yield return new WaitForSeconds(0.1f * chainLevel);
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies == null || enemies.Count == 0) yield break;

        float chainRadiusSqr = _chainRadius * _chainRadius;
        int chainDamage = 0, chainHits = 0;
        List<GameObject> nextChainTargets = new List<GameObject>();
        HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

        for (int s = 0; s < sources.Count; s++)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.activeInHierarchy || alreadyHit.Contains(enemy)) continue;
                Vector2 delta = (Vector2)(enemy.transform.position - sources[s]);
                if (delta.sqrMagnitude > chainRadiusSqr) continue;
                if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;
                alreadyHit.Add(enemy);

                if (enemy.TryGetComponent<StatusEffectManager>(out var sem) && sem.HasAnyDot)
                {
                    DetonateResult detResult;
                    float chainMult = _detonateMultiplier * _chainDamageRatio * Mathf.Pow(0.7f, chainLevel);
                    int dmg = sem.Detonate(chainMult, critChance, critMult, out detResult);
                    if (dmg > 0) { chainDamage += dmg; chainHits++; nextChainTargets.Add(enemy); }
                }

                CombatManager.CreateExplosionEffect(enemy.transform.position, 1.5f + chainLevel * 0.3f, new Color(0.6f, 0.1f, 0.9f, 0.6f - chainLevel * 0.15f), 0.3f);
            }
        }

        if (chainHits > 0)
        {
            var shake3 = GetScreenShake();
            if (shake3 != null) shake3.Shake(0.8f + chainLevel * 0.3f, 0.2f + chainLevel * 0.1f);
            if (chainLevel + 1 < _maxChainCount && nextChainTargets.Count > 0)
            {
                List<Vector3> nextSources = new List<Vector3>();
                for (int i = 0; i < nextChainTargets.Count; i++)
                    if (nextChainTargets[i] != null) nextSources.Add(nextChainTargets[i].transform.position);
                if (nextSources.Count > 0)
                    StartCoroutine(ChainDetonateWave(nextSources, critChance, critMult, chainLevel + 1));
            }
        }
    }

    private void ApplyChargeSlowdown(float slowPercent, float duration)
    {
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;
        float radiusSqr = _detonateRadius * _detonateRadius;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > radiusSqr) continue;
            if (!enemy.TryGetComponent<StatusEffectManager>(out var sem))
                sem = enemy.gameObject.AddComponent<StatusEffectManager>();
            sem.ApplyEffect(StatusEffectType.Frostbite, duration, 2f);
        }
    }

    private void SpawnChargeRadiationZone()
    {
        FireZone.CreateDefault(transform.position, 8, 5f, 4f, 0.5f);
    }

    private void SpawnEmberFireZones(IReadOnlyList<GameObject> enemies, float radiusSqr, int burnStacks)
    {
        int emberDmg = Mathf.Max(1, burnStacks);
        int zonesCreated = 0;
        const int MAX_ZONES = 5;
        for (int i = 0; i < enemies.Count && zonesCreated < MAX_ZONES; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > radiusSqr) continue;
            FireZone.CreateDefault(enemy.transform.position, emberDmg, 3f, 1.5f, 0.5f);
            zonesCreated++;
        }
        if (zonesCreated > 0)
            DebugHelper.Log($"[DetonateSystem] EMBER! Created {zonesCreated} fire zones");
    }

    private void TriggerFrostShatter(IReadOnlyList<GameObject> enemies, float radiusSqr, int frostStacks, float critChance, float critMult)
    {
        float shatterRadius = 4f;
        float shatterRadiusSqr = shatterRadius * shatterRadius;
        int shatterDmg = Mathf.Max(1, frostStacks * 5);
        int targetsHit = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
            if (delta.sqrMagnitude > shatterRadiusSqr) continue;
            if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;
            int finalDmg = shatterDmg;
            if (Random.value < critChance) finalDmg = Mathf.RoundToInt(finalDmg * critMult);
            d.TakeDamage(finalDmg);
            targetsHit++;
            if (!enemy.TryGetComponent<StatusEffectManager>(out var sem))
                sem = enemy.gameObject.AddComponent<StatusEffectManager>();
            sem.ApplyEffect(StatusEffectType.Frostbite, 1f, 2f);
        }
        if (targetsHit > 0)
        {
            CombatManager.CreateExplosionEffect(transform.position, shatterRadius, new Color(0.4f, 0.7f, 1f), 0.5f);
            DebugHelper.Log($"[DetonateSystem] FROST SHATTER! {targetsHit} targets");
        }
    }
}