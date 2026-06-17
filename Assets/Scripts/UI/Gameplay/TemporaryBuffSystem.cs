using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 临时增益系统 — 管理玩家身上所有临时buff的计时和移除
/// 由 SpecialDrop 拾取时调用 AddBuff()，到期自动移除
/// </summary>
public class TemporaryBuffSystem : MonoBehaviour
{
    public enum BuffType
    {
        BerserkPotion,      // 攻速×2
        GhostWalk,          // 穿越敌人+移速+50%
        AutoMagnet,         // 自动拾取所有掉落物
        TimeSlowField,      // 全场敌人减速70%
        ElementStorm,       // 随机DOT效果覆盖全场
        InvincibleShield,   // 免疫所有伤害
        XPTriple,           // 经验×3
        GoldRain,           // 敌人掉落金币×5
        ThornsShield,       // 反弹100%伤害
        RevivalFlame        // 死亡时复活
    }

    public struct ActiveBuff
    {
        public BuffType type;
        public float remainingTime;
        public float totalDuration;
    }

    private List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

    // 原始值缓存
    #pragma warning disable CS0414
    private float _originalMoveSpeed = -1f; // 保留用于未来扩展

    public bool HasBuff(BuffType type)
    {
        for (int i = 0; i < _activeBuffs.Count; i++)
            if (_activeBuffs[i].type == type) return true;
        return false;
    }

    /// <summary>
    /// 获取特定buff剩余时间比例（0~1），供UI使用
    /// </summary>
    public float GetBuffProgress(BuffType type)
    {
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].type == type)
                return _activeBuffs[i].remainingTime / _activeBuffs[i].totalDuration;
        }
        return 0f;
    }

    public List<ActiveBuff> ActiveBuffs => _activeBuffs;

    /// <summary>
    /// 添加临时增益
    /// </summary>
    public void AddBuff(BuffType type, float duration, float value = 0f)
    {
        // 如果已有同类buff，刷新持续时间
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].type == type)
            {
                var buff = _activeBuffs[i];
                buff.remainingTime = duration;
                buff.totalDuration = duration;
                _activeBuffs[i] = buff;
                DebugHelper.Log($"[Buff] Refreshed {type} for {duration}s");
                return;
            }
        }

        _activeBuffs.Add(new ActiveBuff { type = type, remainingTime = duration, totalDuration = duration });
        ApplyBuffEffect(type, true);
        DebugHelper.Log($"[Buff] Added {type} for {duration}s");
    }

    private void Update()
    {
        if (_activeBuffs.Count == 0) return;

        var player = GameReferences.Player;
        if (player == null) return;

        float dt = Time.deltaTime;

        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = _activeBuffs[i];
            buff.remainingTime -= dt;
            _activeBuffs[i] = buff;

            if (buff.remainingTime <= 0)
            {
                ApplyBuffEffect(buff.type, false);
                _activeBuffs.RemoveAt(i);
                DebugHelper.Log($"[Buff] Expired {buff.type}");
            }
        }

        // 磁铁buff：持续吸引掉落物
        if (HasBuff(BuffType.AutoMagnet))
            ContinuousMagnet();

        // 元素风暴：持续对附近敌人施加DOT
        if (HasBuff(BuffType.ElementStorm))
            ContinuousElementStorm();
    }

    private void ApplyBuffEffect(BuffType type, bool apply)
    {
        var player = GameReferences.Player;
        if (player == null) return;

        switch (type)
        {
            case BuffType.GhostWalk:
                // 移速buff — 通过PlayerController处理
                break;

            case BuffType.InvincibleShield:
                // Invincibility handled via TemporaryBuffSystem.IsInvincible() check in Damageable
                break;

            case BuffType.ThornsShield:
                // 通过CombatManager的反射伤害实现
                break;
        }
    }

    private void ContinuousMagnet()
    {
        var player = GameReferences.Player;
        if (player == null) return;
        Vector3 pos = player.transform.position;

        var gems = FindObjectsByType<XPGem>();
        var coins = FindObjectsByType<Coin>();

        foreach (var gem in gems)
        {
            if (gem == null) continue;
            var rb = gem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (pos - gem.transform.position).normalized;
                rb.linearVelocity = dir * 25f;
            }
        }
        foreach (var coin in coins)
        {
            if (coin == null) continue;
            var rb = coin.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (pos - coin.transform.position).normalized;
                rb.linearVelocity = dir * 25f;
            }
        }
    }

    private float _lastStormTime;
    private const float STORM_INTERVAL = 1f;

    private void ContinuousElementStorm()
    {
        if (Time.time - _lastStormTime < STORM_INTERVAL) return;
        _lastStormTime = Time.time;

        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;

        StatusEffectType[] types = { StatusEffectType.Bleed, StatusEffectType.Poison,
            StatusEffectType.Burn, StatusEffectType.Frostbite };
        StatusEffectType chosen = types[Random.Range(0, types.Length)];

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;
            DotBulletHelper.EnsureStatusEffectManager(e);
            var sem = e.GetComponent<StatusEffectManager>();
            if (sem != null)
                sem.ApplyEffect(chosen, 2f, 3f);
        }

        CombatManager.CreateExplosionEffect(GameReferences.Player.transform.position, 15f,
            new Color(0.8f, 0.3f, 1f), 0.3f);
    }

    /// <summary>
    /// 检查并消耗复活火焰（返回true表示已触发复活）
    /// </summary>
    public bool TryConsumeRevival()
    {
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].type == BuffType.RevivalFlame)
            {
                _activeBuffs.RemoveAt(i);
                DebugHelper.Log("[Buff] REVIVAL FLAME consumed!");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检查经验倍率buff
    /// </summary>
    public float GetXPMultiplier()
    {
        return HasBuff(BuffType.XPTriple) ? 3f : 1f;
    }

    /// <summary>
    /// 检查金币倍率buff
    /// </summary>
    public float GetGoldMultiplier()
    {
        return HasBuff(BuffType.GoldRain) ? 5f : 1f;
    }

    /// <summary>
    /// 检查攻速倍率buff
    /// </summary>
    public float GetAttackSpeedMultiplier()
    {
        return HasBuff(BuffType.BerserkPotion) ? 2f : 1f;
    }

    /// <summary>
    /// 检查移速加成buff
    /// </summary>
    public float GetMoveSpeedBonus()
    {
        return HasBuff(BuffType.GhostWalk) ? 0.5f : 0f;
    }

    /// <summary>
    /// 检查免伤buff
    /// </summary>
    public bool IsInvincible()
    {
        return HasBuff(BuffType.InvincibleShield);
    }

    /// <summary>
    /// 检查荆棘反伤buff
    /// </summary>
    public bool HasThornsShield()
    {
        return HasBuff(BuffType.ThornsShield);
    }

    private void OnEnable()
    {
        _activeBuffs.Clear();
    }
}