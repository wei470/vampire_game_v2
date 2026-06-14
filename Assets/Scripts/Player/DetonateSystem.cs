using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 引爆系统 — 负责所有引爆相关逻辑（蓄力引爆、连锁引爆、余烬、碎裂）
/// 从 MagePassive 拆分而来
/// V2: 集成 SpatialGrid 空间分区加速范围查询
/// </summary>
public class DetonateSystem : MonoBehaviour
{
    #region Fields & Config
    [Header("引爆参数")]
    private float _detonateCooldown = 12f;
    private float _detonateMultiplier = 3f;
    private float _detonateRadius = 50f;
    private float _detonateWaveDuration = 0.3f;
    private bool _detonateTimeStop = true;
    private float _detonateShakeIntensity = 2.5f;
    private float _detonateShakeDuration = 0.5f;

    [Header("引爆伤害比例")]
    private float _detonateBleedHpPct = 0.2f;
    private float _detonateBurnHpPct = 0.15f;
    private float _detonatePoisonHpPct = 0.15f;

    [Header("连锁引爆")]
    private int _maxChainCount = 3;
    private float _chainRadius = 10f;
    private float _chainDamageRatio = 0.5f;

    [Header("蓄力引爆")]
    private float _chargeMoveSpeedPenalty = 0.5f;
    private float _chargeMaxTime = 3f;

    [Header("余烬")]
    private int _emberThreshold = 10;
    private int _emberMaxZones = 5;
    private float _emberDuration = 3f;
    private float _emberRadius = 1.5f;

    [Header("霜爆")]
    private float _frostShatterThreshold = 0.8f;
    private float _frostShatterRadius = 4f;
    private int _frostShatterDmgPerStack = 5;

    [Header("连锁反应")]
    private int _highHitThreshold = 10;
    private float _highHitWindow = 3f;
    private float _chainReactionInterval = 0.3f;
    private float _chainReactionDecay = 0.5f;

    [Header("运行时状态")]
    [SerializeField] private float _lastDetonateTime = -999f;
    private bool _isCharging = false;
    private float _chargeStartTime = 0f;
    private int _lastDetonateEnemyCount = 0;

    // ── 里程碑状态 ──
    private float _chainDetonateEndTime = 0f;
    
    // ── GC 缓存（类级复用，避免每帧分配） ──
    private readonly List<GameObject> _cachedChainTargets = new List<GameObject>(32);
    private readonly List<Vector3> _cachedChainSources = new List<Vector3>(32);
    private readonly HashSet<GameObject> _cachedAlreadyHit = new HashSet<GameObject>();

    #endregion

    #region Properties
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

    // ── 进化系统：引爆时触发所有DOT组合 ──
    public bool TriggerAllCombosOnDetonate { get; set; }
    public float ChainRadius { get => _chainRadius; set => _chainRadius = value; }
    public float ChainDamageRatio { get => _chainDamageRatio; set => _chainDamageRatio = value; }

    #endregion

    #region Init & Config
    // ── 引用 ──
    private IDotCharacterPassive _character;
    private ScreenShake _cachedScreenShake;

    public void Init(IDotCharacterPassive character)
    {
        _character = character;
        CacheScreenShake();
        RefreshFromConfig();
        DotEffectConfig.OnConfigChanged += RefreshFromConfig;
    }

    private void OnDisable()
    {
        DotEffectConfig.OnConfigChanged -= RefreshFromConfig;
    }

    private void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        var dotChar = _character;
        float reduction = _character != null ? _character.DetonateCooldownReduction : 0f;
        float mult = Mathf.Max(0.1f, 1f - reduction);
        _detonateCooldown = cfg.DetonateCooldown * mult;
        _detonateMultiplier = cfg.DetonateMultiplier;
        _detonateRadius = cfg.DetonateRadius;
        _detonateWaveDuration = cfg.DetonateWaveDuration;
        _detonateTimeStop = cfg.DetonateTimeStop;
        _detonateShakeIntensity = cfg.DetonateShakeIntensity;
        _detonateShakeDuration = cfg.DetonateShakeDuration;
        _detonateBleedHpPct = cfg.DetonateBleedHpPct;
        _detonateBurnHpPct = cfg.DetonateBurnHpPct;
        _detonatePoisonHpPct = cfg.DetonatePoisonHpPct;
        _maxChainCount = cfg.DetonateMaxChainCount;
        _chainRadius = cfg.DetonateChainRadius;
        _chainDamageRatio = cfg.DetonateChainDamageRatio;
        _chargeMoveSpeedPenalty = cfg.DetonateChargeMoveSpeedPenalty;
        _chargeMaxTime = cfg.DetonateChargeMaxTime;
        _emberThreshold = cfg.DetonateEmberThreshold;
        _emberMaxZones = cfg.DetonateEmberMaxZones;
        _emberDuration = cfg.DetonateEmberDuration;
        _emberRadius = cfg.DetonateEmberRadius;
        _frostShatterThreshold = cfg.DetonateFrostShatterThreshold;
        _frostShatterRadius = cfg.DetonateFrostShatterRadius;
        _frostShatterDmgPerStack = cfg.DetonateFrostShatterDmgPerStack;
        _highHitThreshold = cfg.DetonateHighHitThreshold;
        _highHitWindow = cfg.DetonateHighHitWindow;
        _chainReactionInterval = cfg.DetonateChainReactionInterval;
        _chainReactionDecay = cfg.DetonateChainReactionDecay;
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

    #endregion

    #region Charging
    /// <summary>
    /// 获取移动速度倍率（蓄力时减速50%）
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
        float speedBonus = _character != null ? _character.ChargeSpeedBonus : 0f;
        float effectiveTime = chargeTime * (1f + speedBonus);
        float extraDmg = _character != null ? _character.ChargeDamageBonus : 0f;
        if (effectiveTime >= 3f) return 3f + extraDmg;
        if (effectiveTime >= 2f) return 2f + extraDmg * 0.5f;
        if (effectiveTime >= 1f) return 1.5f;
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

    #endregion

    #region Core Detonation
    /// <summary>
    /// 引爆 — 从玩家炸出红色冲击波，接触到的敌人触发一次引爆伤害
    /// </summary>
    public bool Detonate()
    {
        if (!DetonateReady)
        {
            DebugHelper.Log("[DetonateSystem] Detonate on cooldown");
            return false;
        }

        _lastDetonateTime = Time.time;

        float critChance = _character.GetDotCritChance();
        float critMult = _character.GetDotCritMultiplier();

        // 屏幕晃动
        var shake = GetScreenShake();
        if (shake != null) shake.Shake(_detonateShakeIntensity, _detonateShakeDuration);

        // 音效
        if (SFXManager.Instance != null) SFXManager.Instance.PlayDetonate();

        // 时停（冲击波期间游戏暂停）
        if (_detonateTimeStop) Time.timeScale = 0f;

        // 生成红色冲击波
        float waveSpeed = _detonateRadius / Mathf.Max(0.05f, _detonateWaveDuration);
        var waveObj = new GameObject("DetonateWave");
        waveObj.transform.position = transform.position;
        var wave = waveObj.AddComponent<DetonateWaveEffect>();
        wave.Init(_detonateRadius, waveSpeed, _detonateMultiplier,
            critChance, critMult, _character,
            _detonateBleedHpPct, _detonateBurnHpPct, _detonatePoisonHpPct,
            OnWaveComplete);

        return true;
    }

    /// <summary>
    /// 冲击波结束后的后处理（连锁/余烬/霜爆/末日审判等）
    /// </summary>
    private void OnWaveComplete(float totalDamage, int enemiesHit)
    {
        // 恢复时间
        if (_detonateTimeStop) Time.timeScale = 1f;

        _lastDetonateEnemyCount = enemiesHit;

        if (DamageMeter.Instance != null)
            DamageMeter.Instance.RecordDetonate(totalDamage, enemiesHit);

        // ── 余烬/霜爆/末日审判（遍历范围内敌人） ──
        var spawnMgr = GameReferences.SpawnManager;
        var allEnemies = spawnMgr?.ActiveEnemies;
        if (allEnemies != null)
        {
            SpatialGrid.Rebuild(allEnemies);
            var nearby = SpatialGrid.QueryRadius((Vector2)transform.position, _detonateRadius);

            for (int i = 0; i < nearby.Count; i++)
            {
                var enemy = nearby[i];
                if (enemy == null || !enemy.activeInHierarchy) continue;
                if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;

                // 霜爆
                if (_character != null && _character.FrostExplosionPct > 0
                    && enemy.TryGetComponent<FrostEffect>(out var frost) && frost.slowPercent >= _frostShatterThreshold)
                {
                    float frostDmg = Mathf.Min(800f, Mathf.Max(0.01f, 50f * _detonateMultiplier));
                    d.TakeDamage(frostDmg, new Color(0.4f, 0.7f, 1f));
                    totalDamage += frostDmg;
                    CombatManager.CreateExplosionEffect(enemy.transform.position, 2f, new Color(0.4f, 0.7f, 1f), 0.4f);
                    DamagePopup.Create(enemy.transform.position, frostDmg, new Color(0.4f, 0.8f, 1f), false);
                }

                // 末日审判
                if (_character != null && _character.DoomsdayThreshold > 0
                    && enemy.TryGetComponent<StatusEffectManager>(out var sem))
                {
                    int dotTypes = 0;
                    foreach (var eff in sem.ActiveEffects)
                        if (eff.type != StatusEffectType.Radiate && eff.type != StatusEffectType.Wither) dotTypes++;
                    if (dotTypes >= 3 && d.HpPercent <= _character.DoomsdayThreshold)
                    {
                        int killDmg = d.CurrentHp;
                        d.TakeDamage(killDmg, new Color(1f, 0.1f, 0.1f));
                        totalDamage += killDmg;
                        DamagePopup.Create(enemy.transform.position, killDmg, new Color(1f, 0.2f, 0f), false, "DOOMSDAY!");
                        CombatManager.CreateExplosionEffect(enemy.transform.position, 4f, new Color(1f, 0.1f, 0f), 0.8f);
                    }
                }
            }

            // 余烬（燃烧叠层 > 10 时生成火焰区域）
            int maxBurnStacks = 0;
            for (int i = 0; i < nearby.Count; i++)
            {
                if (nearby[i] != null && nearby[i].TryGetComponent<BurnStackEffect>(out var b))
                    maxBurnStacks = Mathf.Max(maxBurnStacks, b.StackCount);
            }
            if (maxBurnStacks > _emberThreshold) SpawnEmberFireZones(maxBurnStacks);
        }

        // ── 高命中连锁引爆 ──
        if (enemiesHit > _highHitThreshold)
        {
            _chainDetonateEndTime = Time.time + _highHitWindow;
            if (_character != null) _character.SyncDotDamageMultiplierToAll();
        }

        if (enemiesHit > 0)
            TryChainDetonate(_character.GetDotCritChance(), _character.GetDotCritMultiplier(), 0);

        DebugHelper.Log($"[DetonateSystem] DETONATE WAVE! Hit {enemiesHit} enemies for {totalDamage} total damage!");
    }

    #endregion

    #region Chain Detonate
    private void TryChainDetonate(float critChance, float critMult, int currentChain)
    {
        if (currentChain >= _maxChainCount) return;
        float chainDamage = 0; int chainHits = 0;
        _cachedChainTargets.Clear();

        // ── 使用空间分区查询范围内敌人 ──
        var nearbyEnemies = SpatialGrid.QueryRadius((Vector2)transform.position, _detonateRadius);

        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            var enemy = nearbyEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;

            if (enemy.TryGetComponent<StatusEffectManager>(out var sem) && sem.HasAnyDot)
            {
                DetonateResult detResult;
                float dmg = sem.Detonate(_detonateMultiplier * _chainDamageRatio, critChance, critMult, out detResult);
                if (dmg > 0) { chainDamage += dmg; chainHits++; _cachedChainTargets.Add(enemy);
                    CombatManager.CreateExplosionEffect(enemy.transform.position, 2f, new Color(0.6f, 0.1f, 0.9f), 0.4f);
                    DamagePopup.Create(enemy.transform.position, dmg, new Color(0.6f, 0.1f, 0.9f), false); }
            }

            bool hasAnyDotEffect = enemy.TryGetComponent<BleedEffect>(out _)
                                 || enemy.TryGetComponent<BurnStackEffect>(out _)
                                 || enemy.TryGetComponent<PoisonStackEffect>(out _);
            if (hasAnyDotEffect && d.CurrentHp > 0)
            {
                float extra = 10f * _detonateMultiplier * _chainDamageRatio;
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
                _cachedChainSources.Clear();
                for (int i = 0; i < _cachedChainTargets.Count; i++)
                    if (_cachedChainTargets[i] != null) _cachedChainSources.Add(_cachedChainTargets[i].transform.position);
                if (_cachedChainSources.Count > 0)
                    StartCoroutine(ChainDetonateWave(_cachedChainSources, critChance, critMult, currentChain + 1));
            }
        }
    }

    private IEnumerator ChainDetonateWave(List<Vector3> sources, float critChance, float critMult, int chainLevel)
    {
        yield return new WaitForSecondsRealtime(0.1f * chainLevel);

        float chainDamage = 0; int chainHits = 0;
        _cachedChainTargets.Clear();
        _cachedAlreadyHit.Clear();

        for (int s = 0; s < sources.Count; s++)
        {
            // ── 使用空间分区查询连锁范围内敌人 ──
            var nearbyEnemies = SpatialGrid.QueryRadius((Vector2)sources[s], _chainRadius);
            for (int i = 0; i < nearbyEnemies.Count; i++)
            {
                var enemy = nearbyEnemies[i];
                if (enemy == null || !enemy.activeInHierarchy || _cachedAlreadyHit.Contains(enemy)) continue;
                if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;
                _cachedAlreadyHit.Add(enemy);

                if (enemy.TryGetComponent<StatusEffectManager>(out var sem) && sem.HasAnyDot)
                {
                    DetonateResult detResult;
                    float chainMult = _detonateMultiplier * _chainDamageRatio * Mathf.Pow(0.7f, chainLevel);
                    float dmg = sem.Detonate(chainMult, critChance, critMult, out detResult);
                    if (dmg > 0) { chainDamage += dmg; chainHits++; _cachedChainTargets.Add(enemy); }
                }

                CombatManager.CreateExplosionEffect(enemy.transform.position, 1.5f + chainLevel * 0.3f, new Color(0.6f, 0.1f, 0.9f, 0.6f - chainLevel * 0.15f), 0.3f);
            }
        }

        if (chainHits > 0)
        {
            var shake3 = GetScreenShake();
            if (shake3 != null) shake3.Shake(0.8f + chainLevel * 0.3f, 0.2f + chainLevel * 0.1f);
            if (chainLevel + 1 < _maxChainCount && _cachedChainTargets.Count > 0)
            {
                _cachedChainSources.Clear();
                for (int i = 0; i < _cachedChainTargets.Count; i++)
                    if (_cachedChainTargets[i] != null) _cachedChainSources.Add(_cachedChainTargets[i].transform.position);
                if (_cachedChainSources.Count > 0)
                    StartCoroutine(ChainDetonateWave(_cachedChainSources, critChance, critMult, chainLevel + 1));
            }
        }
    }

    #endregion

    #region Utility
    private void ApplyChargeSlowdown(float slowPercent, float duration)
    {
        // ── 使用空间分区查询范围内敌人 ──
        var nearbyEnemies = SpatialGrid.QueryRadius((Vector2)transform.position, _detonateRadius);
        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            var enemy = nearbyEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            if (!enemy.TryGetComponent<StatusEffectManager>(out var sem))
                sem = enemy.gameObject.AddComponent<StatusEffectManager>();
            sem.ApplyEffect(StatusEffectType.Frostbite, duration, 2f);
        }
    }

    private void SpawnChargeRadiationZone()
    {
        FireZone.CreateDefault(transform.position, 8, 5f, 4f, 0.5f);
    }

    private void SpawnEmberFireZones(int burnStacks)
    {
        DetonateSubEffects.SpawnEmberFireZones(
            (Vector2)transform.position, _detonateRadius,
            _emberMaxZones, _emberDuration, _emberRadius, burnStacks);
    }

    private void TriggerFrostShatter(int frostStacks, float critChance, float critMult)
    {
        DetonateSubEffects.TriggerFrostShatter(
            (Vector2)transform.position, _frostShatterRadius,
            frostStacks, _frostShatterDmgPerStack, critChance, critMult);
    }

    #endregion
}