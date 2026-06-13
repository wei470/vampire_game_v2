using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 通用对象池系统 — 减少 Instantiate/Destroy 的 GC 开销。
/// 
/// 功能：
/// - 泛型池化管理（按 prefab 或类型标识）
/// - 自动扩容（池空时自动创建新实例）
/// - 回收时自动禁用 GameObject
/// - 支持预热（WarmUp）预分配对象
/// - 场景切换时自动清理
/// 
/// 使用方式：
///   ObjectPool.Instance.WarmUp("Bullet", bulletPrefab, 20);
///   var obj = ObjectPool.Instance.Spawn("Bullet", position, rotation);
///   ObjectPool.Instance.Despawn("Bullet", obj);
/// </summary>
public class ObjectPool : Singleton<ObjectPool>
{
    [Header("调试")]
    [SerializeField] private bool _debugLog = false;

    /// <summary>
    /// 每个池的配置和运行时数据
    /// </summary>
    private class PoolData
    {
        public GameObject Prefab;
        public Transform Parent;
        public Queue<GameObject> InactiveQueue = new Queue<GameObject>();
        public int TotalCreated;
        public int TotalSpawned;
        public int TotalDespawned;
    }

    private Dictionary<string, PoolData> _pools = new Dictionary<string, PoolData>();

    /// <summary>
    /// 预热对象池 — 预先创建指定数量的实例
    /// </summary>
    public void WarmUp(string poolKey, GameObject prefab, int count)
    {
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new PoolData
            {
                Prefab = prefab,
                Parent = CreatePoolParent(poolKey)
            };
        }

        var pool = _pools[poolKey];
        for (int i = 0; i < count; i++)
        {
            var obj = CreateNewInstance(pool, poolKey);
            obj.SetActive(false);
            pool.InactiveQueue.Enqueue(obj);
        }

        if (_debugLog)
        {
            DebugHelper.Log($"[ObjectPool] Warmed up '{poolKey}' with {count} instances");
        }
    }

    /// <summary>
    /// 从池中取出一个对象
    /// </summary>
    public GameObject Spawn(string poolKey, Vector3 position, Quaternion rotation)
    {
        PoolData pool;
        if (!_pools.TryGetValue(poolKey, out pool))
        {
            DebugHelper.LogWarning($"[ObjectPool] Pool '{poolKey}' not found. Use WarmUp first or register with RegisterPrefab.");
            return null;
        }

        GameObject obj = null;

        // 从队列中取非激活对象
        while (pool.InactiveQueue.Count > 0)
        {
            obj = pool.InactiveQueue.Dequeue();
            if (obj != null) break;
            obj = null;
        }

        // 队列空时自动创建
        if (obj == null)
        {
            obj = CreateNewInstance(pool, poolKey);
        }

        // 激活并定位
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // 通知 IPooledObject 接口
        var pooled = obj.GetComponents<IPooledObject>();
        foreach (var p in pooled)
        {
            p.OnSpawnFromPool();
        }

        pool.TotalSpawned++;

        if (_debugLog)
        {
            DebugHelper.Log($"[ObjectPool] Spawned '{poolKey}', active in pool: estimate");
        }

        return obj;
    }

    /// <summary>
    /// 回收对象到池中
    /// </summary>
    public void Despawn(string poolKey, GameObject obj)
    {
        if (obj == null) return;

        PoolData pool;
        if (!_pools.TryGetValue(poolKey, out pool))
        {
            // 没有对应池，直接销毁
            Destroy(obj);
            return;
        }

        // 通知 IPooledObject 接口
        var pooled = obj.GetComponents<IPooledObject>();
        foreach (var p in pooled)
        {
            p.OnDespawnToPool();
        }

        obj.SetActive(false);

        // 重置 Rigidbody 速度
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 重置 TrailRenderer
        var trail = obj.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }

        // 归还到父级
        obj.transform.SetParent(pool.Parent);
        pool.InactiveQueue.Enqueue(obj);
        pool.TotalDespawned++;
    }

    /// <summary>
    /// 回收对象到池中（通过池键自动推断）
    /// </summary>
    public void Despawn(GameObject obj, float delay = 0f)
    {
        if (obj == null) return;

        if (delay > 0f)
        {
            // 延迟回收 — 使用 Coroutine
            Instance.StartCoroutine(DespawnDelayed(obj, delay));
            return;
        }

        // 尝试从对象名推断池
        string poolKey = InferPoolKey(obj);
        if (poolKey != null)
        {
            Despawn(poolKey, obj);
        }
        else
        {
            // 无法推断，直接销毁
            Destroy(obj);
        }
    }

    private System.Collections.IEnumerator DespawnDelayed(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            string poolKey = InferPoolKey(obj);
            if (poolKey != null)
            {
                Despawn(poolKey, obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }

    /// <summary>
    /// 注册预制体到池（不预热，按需创建）
    /// </summary>
    public void RegisterPrefab(string poolKey, GameObject prefab)
    {
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new PoolData
            {
                Prefab = prefab,
                Parent = CreatePoolParent(poolKey)
            };
        }
    }

    /// <summary>
    /// 检查池是否存在
    /// </summary>
    public bool HasPool(string poolKey)
    {
        return _pools.ContainsKey(poolKey);
    }

    /// <summary>
    /// #29 扩展池容量 — 如果池中空闲对象不足，额外创建指定数量
    /// 用于波次间歇预加载，避免运行时 Instantiate 卡顿
    /// </summary>
    public void ExpandPool(string poolKey, int extraCount)
    {
        if (extraCount <= 0) return;
        PoolData pool;
        if (!_pools.TryGetValue(poolKey, out pool)) return;

        // 只在空闲对象不足时扩展
        if (pool.InactiveQueue.Count >= extraCount) return;

        int toCreate = extraCount - pool.InactiveQueue.Count;
        for (int i = 0; i < toCreate; i++)
        {
            var obj = CreateNewInstance(pool, poolKey);
            obj.SetActive(false);
            pool.InactiveQueue.Enqueue(obj);
        }

        if (_debugLog && toCreate > 0)
        {
            DebugHelper.Log($"[ObjectPool] Expanded '{poolKey}' by {toCreate} instances");
        }
    }

    /// <summary>
    /// 获取池统计信息
    /// </summary>
    public string GetPoolStats(string poolKey)
    {
        PoolData pool;
        if (!_pools.TryGetValue(poolKey, out pool))
        {
            return $"'{poolKey}': Not registered";
        }
        return $"'{poolKey}': Created={pool.TotalCreated}, Spawned={pool.TotalSpawned}, " +
               $"Despawned={pool.TotalDespawned}, Inactive={pool.InactiveQueue.Count}";
    }

    /// <summary>
    /// 获取所有池的总统计
    /// </summary>
    public string GetAllStats()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Object Pool Stats ===");
        foreach (var kvp in _pools)
        {
            sb.AppendLine(GetPoolStats(kvp.Key));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 清理所有池（场景切换时调用）
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in _pools)
        {
            var pool = kvp.Value;
            // 销毁所有池化对象
            foreach (var obj in pool.InactiveQueue)
            {
                if (obj != null) Destroy(obj);
            }
            pool.InactiveQueue.Clear();

            // 销毁父容器
            if (pool.Parent != null)
            {
                Destroy(pool.Parent.gameObject);
            }
        }
        _pools.Clear();

        if (_debugLog)
        {
            DebugHelper.Log("[ObjectPool] All pools cleared");
        }
    }

    /// <summary>
    /// 清理指定池
    /// </summary>
    public void ClearPool(string poolKey)
    {
        PoolData pool;
        if (_pools.TryGetValue(poolKey, out pool))
        {
            foreach (var obj in pool.InactiveQueue)
            {
                if (obj != null) Destroy(obj);
            }
            pool.InactiveQueue.Clear();

            if (pool.Parent != null)
            {
                Destroy(pool.Parent.gameObject);
            }
            _pools.Remove(poolKey);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearAll();
    }

    // ── 内部方法 ──

    private GameObject CreateNewInstance(PoolData pool, string poolKey)
    {
        GameObject obj;
        if (pool.Prefab != null)
        {
            obj = Object.Instantiate(pool.Prefab, pool.Parent);
        }
        else
        {
            // 无预制体时创建空对象
            obj = new GameObject(poolKey + "_Pooled");
            obj.transform.SetParent(pool.Parent);
        }

        // 标记池键（用于回收时推断）
        obj.name = poolKey + "_" + pool.TotalCreated;
        var marker = obj.AddComponent<PoolMarker>();
        marker.PoolKey = poolKey;

        pool.TotalCreated++;
        return obj;
    }

    private Transform CreatePoolParent(string poolKey)
    {
        var parentObj = new GameObject("[Pool] " + poolKey);
        parentObj.transform.SetParent(transform);
        // 注意：不能禁用父级，否则子对象SetActive(true)时仍不活跃
        return parentObj.transform;
    }

    /// <summary>
    /// 从对象名或 PoolMarker 推断池键
    /// </summary>
    private string InferPoolKey(GameObject obj)
    {
        var marker = obj.GetComponent<PoolMarker>();
        if (marker != null)
        {
            return marker.PoolKey;
        }

        // 从对象名推断（格式：PoolKey_Index）
        string name = obj.name;
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore > 0)
        {
            string candidate = name.Substring(0, lastUnderscore);
            if (_pools.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}

/// <summary>
/// 标记组件 — 记录对象所属的池键
/// </summary>
public class PoolMarker : MonoBehaviour
{
    public string PoolKey;
}

/// <summary>
/// 池化对象回调接口
/// </summary>
public interface IPooledObject
{
    void OnSpawnFromPool();
    void OnDespawnToPool();
}