using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// DetonateSystem 连锁引爆 — 连锁触发、连锁波扩散、余烬/霜爆工具方法。
/// </summary>
public partial class DetonateSystem
{
    #region Chain Detonate
    private void TryChainDetonate(float critChance, float critMult, int currentChain)
    {
        if (currentChain >= _maxChainCount) return;
        float chainDamage = 0; int chainHits = 0;
        _cachedChainTargets.Clear();

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
