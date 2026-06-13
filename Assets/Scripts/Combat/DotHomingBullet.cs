using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 追踪弹桥接组件 — 挂在 HomingProjectile 上，命中敌人时附加 DOT 效果
/// 使用策略字典替代 switch-case，消除分支
/// </summary>
public class DotHomingBullet : MonoBehaviour
{
    private DotGunState _gun;
    private float _durMult;
    private float _dmgMult;
    private bool _canCrit;
    private float _critChance;
    private float _critMult;
    private bool _initialized;

    private static readonly Dictionary<StatusEffectType, System.Action<GameObject, DotGunState, float, float, bool, float, float>> _hitHandlers
        = new Dictionary<StatusEffectType, System.Action<GameObject, DotGunState, float, float, bool, float, float>>
    {
        { StatusEffectType.Poison, ApplyPoison },
        { StatusEffectType.Burn, ApplyBurn },
        { StatusEffectType.Frostbite, ApplyFrost },
    };

    public void Init(DotGunState gun, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        _gun = gun;
        _durMult = durMult;
        _dmgMult = dmgMult;
        _canCrit = canCrit;
        _critChance = critChance;
        _critMult = critMult;
        _initialized = true;
    }

    public void OnHitEnemy(GameObject enemy)
    {
        if (!_initialized || enemy == null) return;
        DotBulletHelper.EnsureStatusEffectManager(enemy);

        if (_hitHandlers.TryGetValue(_gun.effectType, out var handler))
        {
            handler(enemy, _gun, _durMult, _dmgMult, _canCrit, _critChance, _critMult);
        }
    }

    private static void ApplyPoison(GameObject enemy, DotGunState gun, float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var poison = enemy.GetComponent<PoisonStackEffect>();
        if (poison == null) poison = enemy.AddComponent<PoisonStackEffect>();
        poison.AddStack(gun.dotDps * dmgMult, gun.dotDuration * durMult, canCrit, critChance, critMult);
    }

    private static void ApplyBurn(GameObject enemy, DotGunState gun, float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var burn = enemy.GetComponent<BurnStackEffect>();
        if (burn == null) burn = enemy.AddComponent<BurnStackEffect>();
        burn.AddStack(gun.dotDps * dmgMult, gun.dotDuration * durMult, canCrit, critChance, critMult);
    }

    private static void ApplyFrost(GameObject enemy, DotGunState gun, float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var frost = enemy.GetComponent<FrostEffect>();
        if (frost == null) frost = enemy.AddComponent<FrostEffect>();
        frost.ApplyFreeze(gun.dotDuration * durMult, gun.dotDps * dmgMult, 0f, canCrit, critChance, critMult);
    }
}
