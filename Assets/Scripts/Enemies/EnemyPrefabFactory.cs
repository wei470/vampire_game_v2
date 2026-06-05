using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人工厂 — 负责创建所有 14 种敌人预制体（运行时生成）
/// 从 SpawnManager 拆分而来，职责单一：预制体创建和池键映射
/// </summary>
public class EnemyPrefabFactory
{
    // ── 敌人预制体引用（14 种）──
    public GameObject BasicEnemyPrefab { get; set; }
    public GameObject RangedEnemyPrefab { get; set; }
    public GameObject TankEnemyPrefab { get; set; }
    public GameObject FastEnemyPrefab { get; set; }
    public GameObject ThrowerEnemyPrefab { get; set; }
    public GameObject HealerEnemyPrefab { get; set; }
    public GameObject EnhancerEnemyPrefab { get; set; }
    public GameObject SplitterEnemyPrefab { get; set; }
    public GameObject SummonerEnemyPrefab { get; set; }
    public GameObject ChargerEnemyPrefab { get; set; }
    public GameObject ShielderEnemyPrefab { get; set; }
    public GameObject StealthEnemyPrefab { get; set; }
    public GameObject BurstEnemyPrefab { get; set; }
    public GameObject ChainHealerEnemyPrefab { get; set; }

    private readonly int _enemyLayer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="enemyLayer">敌人所在 Layer</param>
    public EnemyPrefabFactory(int enemyLayer)
    {
        _enemyLayer = enemyLayer;
    }

    /// <summary>
    /// 确保所有敌人预制体都已创建（Inspector 未赋值时运行时生成）
    /// </summary>
    public void EnsureEnemyPrefabs()
    {
        if (BasicEnemyPrefab == null)
            BasicEnemyPrefab = CreateEnemyPrefab("BasicEnemy", new Color(0.85f, 0.2f, 0.2f), 20, 5f, 10, 5, SpriteFactory.Square);
        if (RangedEnemyPrefab == null)
            RangedEnemyPrefab = CreateRangedEnemyPrefab("RangedEnemy", new Color(0.8f, 0.4f, 0.4f), sprite: SpriteFactory.Triangle);
        if (TankEnemyPrefab == null)
            TankEnemyPrefab = CreateEnemyPrefab("TankEnemy", new Color(0.5f, 0.5f, 0.5f), 80, 2.5f, 15, 10, SpriteFactory.Hexagon);
        if (FastEnemyPrefab == null)
            FastEnemyPrefab = CreateEnemyPrefab("FastEnemy", new Color(0.6f, 0.2f, 0.8f), 10, 9f, 8, 5, SpriteFactory.Triangle);
        if (ThrowerEnemyPrefab == null)
            ThrowerEnemyPrefab = CreateEnemyPrefab("ThrowerEnemy", new Color(1f, 0.5f, 0f), 18, 4f, 12, 8, SpriteFactory.Diamond);
        if (HealerEnemyPrefab == null)
            HealerEnemyPrefab = CreateEnemyPrefab("HealerEnemy", new Color(0.2f, 0.8f, 0.2f), 25, 4f, 5, 10, SpriteFactory.Cross);
        if (EnhancerEnemyPrefab == null)
            EnhancerEnemyPrefab = CreateEnemyPrefab("EnhancerEnemy", new Color(0.8f, 0.8f, 0.2f), 30, 4f, 8, 10, SpriteFactory.Pentagon);
        if (SplitterEnemyPrefab == null)
            SplitterEnemyPrefab = CreateEnemyPrefab("SplitterEnemy", new Color(0.6f, 0.2f, 0.4f), 35, 4f, 10, 8, SpriteFactory.Diamond);
        if (SummonerEnemyPrefab == null)
            SummonerEnemyPrefab = CreateEnemyPrefab("SummonerEnemy", new Color(0.4f, 0.1f, 0.6f), 30, 4f, 8, 12, SpriteFactory.Pentagon);
        if (ChargerEnemyPrefab == null)
            ChargerEnemyPrefab = CreateEnemyPrefab("ChargerEnemy", new Color(0.8f, 0.3f, 0.1f), 35, 5f, 15, 10, SpriteFactory.Triangle);
        if (ShielderEnemyPrefab == null)
            ShielderEnemyPrefab = CreateEnemyPrefab("ShielderEnemy", new Color(0.3f, 0.5f, 1f), 40, 4f, 5, 10, SpriteFactory.Hexagon);
        if (StealthEnemyPrefab == null)
            StealthEnemyPrefab = CreateEnemyPrefab("StealthEnemy", new Color(0.4f, 0.4f, 0.4f), 20, 7f, 12, 8, SpriteFactory.Diamond);
        if (BurstEnemyPrefab == null)
            BurstEnemyPrefab = CreateEnemyPrefab("BurstEnemy", new Color(1f, 0.6f, 0f), 25, 5f, 10, 8, SpriteFactory.Star);
        if (ChainHealerEnemyPrefab == null)
            ChainHealerEnemyPrefab = CreateEnemyPrefab("ChainHealerEnemy", new Color(0.2f, 0.8f, 0.6f), 25, 4f, 5, 10, SpriteFactory.Cross);

        DebugHelper.Log($"[EnemyPrefabFactory] Enemy prefabs ensured (basic={BasicEnemyPrefab != null})");
    }

    /// <summary>
    /// 根据预制体获取对应的池键（全部 14 种）
    /// </summary>
    public string GetPoolKeyForPrefab(GameObject prefab)
    {
        if (prefab == BasicEnemyPrefab) return PoolHelper.BASIC_ENEMY;
        if (prefab == RangedEnemyPrefab) return PoolHelper.RANGED_ENEMY;
        if (prefab == TankEnemyPrefab) return PoolHelper.TANK_ENEMY;
        if (prefab == FastEnemyPrefab) return PoolHelper.FAST_ENEMY;
        if (prefab == ThrowerEnemyPrefab) return PoolHelper.THROWER_ENEMY;
        if (prefab == HealerEnemyPrefab) return PoolHelper.HEALER_ENEMY;
        if (prefab == EnhancerEnemyPrefab) return PoolHelper.ENHANCER_ENEMY;
        if (prefab == SplitterEnemyPrefab) return PoolHelper.SPLITTER_ENEMY;
        if (prefab == SummonerEnemyPrefab) return PoolHelper.SUMMONER_ENEMY;
        if (prefab == ChargerEnemyPrefab) return PoolHelper.CHARGER_ENEMY;
        if (prefab == ShielderEnemyPrefab) return PoolHelper.SHIELDER_ENEMY;
        if (prefab == StealthEnemyPrefab) return PoolHelper.STEALTH_ENEMY;
        if (prefab == BurstEnemyPrefab) return PoolHelper.BURST_ENEMY;
        if (prefab == ChainHealerEnemyPrefab) return PoolHelper.CHAIN_HEALER_ENEMY;
        return PoolHelper.BASIC_ENEMY;
    }

    /// <summary>
    /// 根据波次选择敌人类型（全部 14 种，渐进解锁）
    /// </summary>
    public GameObject ChooseEnemyPrefab(int currentWave)
    {
        // 第 1-2 波：只有普通敌人
        if (currentWave <= 2)
            return BasicEnemyPrefab;

        float roll = Random.value;

        // 波 3-4: Basic + Ranged + Fast
        if (currentWave <= 4)
        {
            if (roll < 0.30f && RangedEnemyPrefab != null) return RangedEnemyPrefab;
            if (roll < 0.60f && FastEnemyPrefab != null) return FastEnemyPrefab;
            return BasicEnemyPrefab;
        }

        // 波 5-7: +Tank, +Thrower
        if (currentWave <= 7)
        {
            if (roll < 0.10f && TankEnemyPrefab != null) return TankEnemyPrefab;
            if (roll < 0.25f && RangedEnemyPrefab != null) return RangedEnemyPrefab;
            if (roll < 0.40f && FastEnemyPrefab != null) return FastEnemyPrefab;
            if (roll < 0.55f && ThrowerEnemyPrefab != null) return ThrowerEnemyPrefab;
            return BasicEnemyPrefab;
        }

        // 波 8+: 全部 14 种敌人
        if (roll < 0.05f && TankEnemyPrefab != null) return TankEnemyPrefab;
        if (roll < 0.12f && HealerEnemyPrefab != null) return HealerEnemyPrefab;
        if (roll < 0.18f && EnhancerEnemyPrefab != null) return EnhancerEnemyPrefab;
        if (roll < 0.24f && SplitterEnemyPrefab != null) return SplitterEnemyPrefab;
        if (roll < 0.30f && SummonerEnemyPrefab != null) return SummonerEnemyPrefab;
        if (roll < 0.36f && ChargerEnemyPrefab != null) return ChargerEnemyPrefab;
        if (roll < 0.42f && ShielderEnemyPrefab != null) return ShielderEnemyPrefab;
        if (roll < 0.48f && StealthEnemyPrefab != null) return StealthEnemyPrefab;
        if (roll < 0.54f && BurstEnemyPrefab != null) return BurstEnemyPrefab;
        if (roll < 0.60f && ChainHealerEnemyPrefab != null) return ChainHealerEnemyPrefab;
        if (roll < 0.70f && RangedEnemyPrefab != null) return RangedEnemyPrefab;
        if (roll < 0.80f && FastEnemyPrefab != null) return FastEnemyPrefab;
        if (roll < 0.88f && ThrowerEnemyPrefab != null) return ThrowerEnemyPrefab;
        return BasicEnemyPrefab;
    }

    /// <summary>
    /// #32 根据特殊波次类型选择敌人预制体
    /// </summary>
    public GameObject ChooseSpecialWaveEnemy(EnemyWaveConfig.SpecialWaveType type)
    {
        switch (type)
        {
            case EnemyWaveConfig.SpecialWaveType.TankRush:
                return TankEnemyPrefab ?? BasicEnemyPrefab;
            case EnemyWaveConfig.SpecialWaveType.SpeedSurge:
                return FastEnemyPrefab ?? BasicEnemyPrefab;
            case EnemyWaveConfig.SpecialWaveType.SwarmWave:
                return BasicEnemyPrefab;
            case EnemyWaveConfig.SpecialWaveType.EliteWave:
                return ChooseEliteEnemy() ?? BasicEnemyPrefab;
            case EnemyWaveConfig.SpecialWaveType.HealerArmy:
                return Random.value < 0.5f
                    ? (HealerEnemyPrefab ?? BasicEnemyPrefab)
                    : (ChainHealerEnemyPrefab ?? BasicEnemyPrefab);
            case EnemyWaveConfig.SpecialWaveType.BossRush:
                return ChooseEliteEnemy() ?? TankEnemyPrefab ?? BasicEnemyPrefab;
            default:
                return BasicEnemyPrefab;
        }
    }

    /// <summary>
    /// 选择精英敌人（排除 Basic）
    /// </summary>
    public GameObject ChooseEliteEnemy()
    {
        GameObject[] elites = {
            TankEnemyPrefab, ChargerEnemyPrefab, BurstEnemyPrefab,
            ShielderEnemyPrefab, StealthEnemyPrefab, SplitterEnemyPrefab
        };
        List<GameObject> valid = new List<GameObject>();
        foreach (var e in elites)
            if (e != null) valid.Add(e);
        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : null;
    }

    /// <summary>
    /// 预热所有敌人对象池
    /// </summary>
    public void WarmUpEnemyPools()
    {
        PoolHelper.WarmUpEnemyPools(
            BasicEnemyPrefab, RangedEnemyPrefab,
            TankEnemyPrefab, FastEnemyPrefab,
            ThrowerEnemyPrefab, HealerEnemyPrefab,
            EnhancerEnemyPrefab, SplitterEnemyPrefab,
            SummonerEnemyPrefab, ChargerEnemyPrefab,
            ShielderEnemyPrefab, StealthEnemyPrefab,
            BurstEnemyPrefab, ChainHealerEnemyPrefab);
    }

    // ── 私有辅助 ──

    private GameObject CreateEnemyPrefab(string name, Color color, int hp, float speed, int damage, int xpReward, Sprite sprite = null)
    {
        var go = new GameObject(name);
        go.tag = "Enemy";
        go.layer = _enemyLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : SpriteFactory.Square;
        sr.color = color;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(hp);
        dmg.Heal(hp);

        var entity = go.AddComponent<BaseEntity>();

        var enemyBase = go.AddComponent<EnemyBase>();
        enemyBase.Setup(speed, damage, xpReward, (int)(xpReward * 0.5f));

        var killReward = go.AddComponent<KillRewarder>();

        go.SetActive(false);
        return go;
    }

    private GameObject CreateRangedEnemyPrefab(string name = "RangedEnemy", Color color = default, int hp = 20, float speed = 4f, int damage = 8, int xpReward = 8, Sprite sprite = null)
    {
        if (color == default) color = new Color(0.8f, 0.4f, 0.4f);
        var go = new GameObject(name);
        go.tag = "Enemy";
        go.layer = _enemyLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : SpriteFactory.Square;
        sr.color = color;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(hp);
        dmg.Heal(hp);

        var entity = go.AddComponent<BaseEntity>();

        var enemyBase = go.AddComponent<RangedEnemy>();
        enemyBase.Setup(speed, damage, xpReward, (int)(xpReward * 0.5f));

        var killReward = go.AddComponent<KillRewarder>();

        go.SetActive(false);
        return go;
    }
}