using UnityEngine;

/// <summary>
/// 对象池辅助工具类 — 统一管理池键常量、预热和回收逻辑。
/// 
/// 功能：
/// - 统一池键常量（避免字符串魔法值散落各处）
/// - 封装预热（WarmUp）和回收（DespawnOrDestroy）便捷方法
/// - 支持运行时注册虚拟预制体（代码创建的对象）
/// 
/// 使用方式：
///   PoolHelper.WarmUpPools(enemyPrefabs, lootPrefab);
///   PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.XP_GEM);
/// </summary>
public static class PoolHelper
{
    // ── 池键常量（14 种敌人 + Boss）──
    public const string BASIC_ENEMY = "Enemy_Basic";
    public const string RANGED_ENEMY = "Enemy_Ranged";
    public const string TANK_ENEMY = "Enemy_Tank";
    public const string FAST_ENEMY = "Enemy_Fast";
    public const string THROWER_ENEMY = "Enemy_Thrower";
    public const string HEALER_ENEMY = "Enemy_Healer";
    public const string ENHANCER_ENEMY = "Enemy_Enhancer";
    public const string SPLITTER_ENEMY = "Enemy_Splitter";
    public const string SUMMONER_ENEMY = "Enemy_Summoner";
    public const string CHARGER_ENEMY = "Enemy_Charger";
    public const string SHIELDER_ENEMY = "Enemy_Shielder";
    public const string STEALTH_ENEMY = "Enemy_Stealth";
    public const string BURST_ENEMY = "Enemy_Burst";
    public const string CHAIN_HEALER_ENEMY = "Enemy_ChainHealer";
    public const string BOSS_ENEMY = "Enemy_Boss";

    // ── 投射物/特效池键常量 ──
    public const string BULLET = "Bullet";
    public const string ENEMY_BULLET = "EnemyBullet";
    public const string EXPLOSION_VFX = "ExplosionVFX";
    public const string LIGHTNING_LINE = "LightningLine";

    // ── 掉落物池键常量 ──
    public const string XP_GEM = "Loot_XPGem";
    public const string COIN = "Loot_Coin";

    /// <summary>
    /// 预热敌人对象池（从预制体，支持全部 14 种）
    /// </summary>
    public static void WarmUpEnemyPools(
        GameObject basicPrefab, GameObject rangedPrefab,
        GameObject tankPrefab, GameObject fastPrefab,
        GameObject throwerPrefab = null, GameObject healerPrefab = null,
        GameObject enhancerPrefab = null, GameObject splitterPrefab = null,
        GameObject summonerPrefab = null, GameObject chargerPrefab = null,
        GameObject shielderPrefab = null, GameObject stealthPrefab = null,
        GameObject burstPrefab = null, GameObject chainHealerPrefab = null)
    {
        var pool = ObjectPool.Instance;
        if (pool == null) return;

        if (basicPrefab != null) pool.WarmUp(BASIC_ENEMY, basicPrefab, 20);
        if (rangedPrefab != null) pool.WarmUp(RANGED_ENEMY, rangedPrefab, 10);
        if (tankPrefab != null) pool.WarmUp(TANK_ENEMY, tankPrefab, 5);
        if (fastPrefab != null) pool.WarmUp(FAST_ENEMY, fastPrefab, 10);
        if (throwerPrefab != null) pool.WarmUp(THROWER_ENEMY, throwerPrefab, 5);
        if (healerPrefab != null) pool.WarmUp(HEALER_ENEMY, healerPrefab, 5);
        if (enhancerPrefab != null) pool.WarmUp(ENHANCER_ENEMY, enhancerPrefab, 5);
        if (splitterPrefab != null) pool.WarmUp(SPLITTER_ENEMY, splitterPrefab, 5);
        if (summonerPrefab != null) pool.WarmUp(SUMMONER_ENEMY, summonerPrefab, 5);
        if (chargerPrefab != null) pool.WarmUp(CHARGER_ENEMY, chargerPrefab, 5);
        if (shielderPrefab != null) pool.WarmUp(SHIELDER_ENEMY, shielderPrefab, 5);
        if (stealthPrefab != null) pool.WarmUp(STEALTH_ENEMY, stealthPrefab, 5);
        if (burstPrefab != null) pool.WarmUp(BURST_ENEMY, burstPrefab, 5);
        if (chainHealerPrefab != null) pool.WarmUp(CHAIN_HEALER_ENEMY, chainHealerPrefab, 5);

        DebugHelper.Log("[PoolHelper] Enemy pools warmed up (14 types)");
    }

    /// <summary>
    /// 预热掉落物对象池（从预制体）
    /// </summary>
    public static void WarmUpLootPools(GameObject xpGemPrefab, GameObject coinPrefab)
    {
        var pool = ObjectPool.Instance;
        if (pool == null) return;

        if (xpGemPrefab != null) pool.WarmUp(XP_GEM, xpGemPrefab, 30);
        if (coinPrefab != null) pool.WarmUp(COIN, coinPrefab, 30);

        DebugHelper.Log("[PoolHelper] Loot pools warmed up");
    }

    /// <summary>
    /// 创建虚拟预制体并注册到对象池（用于代码创建的对象）
    /// 先创建一个模板对象作为预制体源，注册到池中。
    /// </summary>
    public static void RegisterVirtualPrefab(string poolKey, System.Func<GameObject> factory, int warmupCount = 10)
    {
        var pool = ObjectPool.Instance;
        if (pool == null) return;

        if (pool.HasPool(poolKey)) return; // 已注册

        // 创建一个模板对象作为虚拟预制体
        var template = factory();
        template.SetActive(false);
        template.name = poolKey + "_Template";

        // 注册到池并预热
        pool.WarmUp(poolKey, template, warmupCount);

        // 销毁模板（池已复制了足够数量）
        Object.Destroy(template);

        DebugHelper.Log($"[PoolHelper] Registered virtual prefab '{poolKey}' with {warmupCount} instances");
    }

    /// <summary>
    /// 从对象池取出对象（按池键 + 预制体）
    /// 如果池中没有且有预制体，自动注册
    /// </summary>
    public static GameObject SpawnOrInstantiate(string poolKey, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = ObjectPool.Instance;

        // 尝试从池中取出
        if (pool != null && pool.HasPool(poolKey))
        {
            var obj = pool.Spawn(poolKey, position, rotation);
            if (obj != null) return obj;
        }

        // 池中没有，使用 Instantiate 创建（并注册到池供后续回收）
        if (prefab != null)
        {
            var obj = Object.Instantiate(prefab, position, rotation);

            // 给对象设置池标记
            if (pool != null)
            {
                if (!pool.HasPool(poolKey))
                {
                    pool.RegisterPrefab(poolKey, prefab);
                }
                // 确保有 PoolMarker
                var marker = obj.GetComponent<PoolMarker>();
                if (marker == null) marker = obj.AddComponent<PoolMarker>();
                marker.PoolKey = poolKey;
            }

            return obj;
        }

        return null;
    }

    /// <summary>
    /// 根据组件类型推断池键
    /// </summary>
    public static string GetPoolKey(Component component)
    {
        if (component is Bullet) return BULLET;
        if (component is HomingProjectile) return "HomingProjectile";
        if (component is ShockwaveProjectile) return "ShockwaveProjectile";
        if (component is MineTrap) return "MineTrap";
        if (component is FrostOrb) return "FrostOrb";
        if (component is VenomDart) return "VenomDart";
        if (component is FireZone) return "FireZone";
        if (component is LightningBolt) return "LightningBolt";
        if (component is EnemyBullet) return ENEMY_BULLET;
        if (component is XPGem) return XP_GEM;
        if (component is Coin) return COIN;
        return null;
    }

    /// <summary>
    /// 回收到对象池，如果池不存在则直接销毁（自动推断池键）
    /// </summary>
    public static void DespawnOrDestroy(GameObject obj, string poolKey = null)
    {
        if (obj == null) return;

        if (string.IsNullOrEmpty(poolKey))
        {
            poolKey = InferPoolKey(obj);
        }

        if (!string.IsNullOrEmpty(poolKey) &&
            ObjectPool.Instance != null &&
            ObjectPool.Instance.HasPool(poolKey))
        {
            ObjectPool.Instance.Despawn(poolKey, obj);
        }
        else
        {
            Object.Destroy(obj);
        }
    }

    /// <summary>
    /// 从 PoolMarker 推断池键
    /// </summary>
    private static string InferPoolKey(GameObject obj)
    {
        var marker = obj.GetComponent<PoolMarker>();
        if (marker != null) return marker.PoolKey;

        // 从组件推断
        var proj = obj.GetComponent<Projectile>();
        if (proj != null) return GetPoolKey(proj);

        var gem = obj.GetComponent<XPGem>();
        if (gem != null) return XP_GEM;

        var coin = obj.GetComponent<Coin>();
        if (coin != null) return COIN;

        return null;
    }

    /// <summary>
    /// 延迟回收到对象池
    /// </summary>
    public static void DespawnOrDestroy(GameObject obj, string poolKey, float delay)
    {
        if (obj == null) return;

        if (delay <= 0f)
        {
            DespawnOrDestroy(obj, poolKey);
            return;
        }

        var pool = ObjectPool.Instance;
        if (pool != null && pool.HasPool(poolKey))
        {
            pool.Despawn(obj, delay);
        }
        else
        {
            Object.Destroy(obj, delay);
        }
    }
}