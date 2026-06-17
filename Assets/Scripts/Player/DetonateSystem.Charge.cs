using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// DetonateSystem 蓄力引爆 — 蓄力输入、蓄力曲线、蓄力减速/辐射区域。
/// </summary>
public partial class DetonateSystem
{
    #region Charging
    public float GetChargeMoveSpeedMultiplier()
    {
        if (!_isCharging) return 1f;
        return 1f - _chargeMoveSpeedPenalty;
    }

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

    private void ApplyChargeSlowdown(float slowPercent, float duration)
    {
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

    #endregion
}
