#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 屏幕外实体自动销毁器 — 性能优化组件。
/// 
/// 功能：
/// - 每隔固定间隔检测所有受管实体是否超出屏幕范围
/// - 超出屏幕边界 800px 的投射物、经验球、金币自动回收/销毁
/// - 对敌人不自动销毁（避免波次计数错误），但可标记为休眠
/// - 支持与 ObjectPool 集成（优先回收而非销毁）
/// 
/// 使用方式：Singleton，场景中自动创建
/// </summary>
public class OffScreenCuller : Singleton<OffScreenCuller>
{
    [Header("裁剪设置")]
    [SerializeField] private float _cullMargin = 800f;          // 屏幕外多少像素裁剪
    [SerializeField] private float _checkInterval = 0.5f;       // 检查间隔（秒）
    [SerializeField] private bool _debugLog = false;

    [Header("统计")]
    [SerializeField] private int _totalCulledProjectiles = 0;
    [SerializeField] private int _totalCulledLoot = 0;

    private float _lastCheckTime;
    private Camera _mainCamera;

    // 受管对象字典（分类型管理，O(1) 查找/移除）
    private Dictionary<int, TrackedEntity> _trackedProjectiles = new Dictionary<int, TrackedEntity>();
    private Dictionary<int, TrackedEntity> _trackedLoot = new Dictionary<int, TrackedEntity>();

    // 缓存待移除 key 列表和待回收实体，避免 GC 分配
    private List<int> _removalKeys = new List<int>(64);
    private List<TrackedEntity> _toDespawn = new List<TrackedEntity>(64);

    private struct TrackedEntity
    {
        public GameObject GameObject;
        public string PoolKey; // 如果来自对象池，记录池键
    }

    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Time.time - _lastCheckTime < _checkInterval) return;
        _lastCheckTime = Time.time;

        _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        Rect screenBounds = GetScreenBounds();

        // 检查投射物
        CullDict(_trackedProjectiles, screenBounds, ref _totalCulledProjectiles);

        // 检查掉落物
        CullDict(_trackedLoot, screenBounds, ref _totalCulledLoot);
    }

    /// <summary>
    /// 获取屏幕世界坐标边界（含边距）
    /// </summary>
    private Rect GetScreenBounds()
    {
        float camHeight = _mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * _mainCamera.aspect;
        Vector3 camPos = _mainCamera.transform.position;

        float marginWorld = _cullMargin / 100f; // 像素转世界单位（假设 100 pixels per unit）

        return new Rect(
            camPos.x - camWidth / 2f - marginWorld,
            camPos.y - camHeight / 2f - marginWorld,
            camWidth + marginWorld * 2f,
            camHeight + marginWorld * 2f
        );
    }

    /// <summary>
    /// 裁剪字典中的超界对象（O(n) 遍历 + O(1) 移除）
    /// 注意：回收操作延迟到遍历结束后执行，避免 Despawn→OnDisable→Untrack 修改字典
    /// </summary>
    private void CullDict(Dictionary<int, TrackedEntity> dict, Rect bounds, ref int culledCount)
    {
        _removalKeys.Clear();
        _toDespawn.Clear();

        foreach (var kvp in dict)
        {
            var entity = kvp.Value;

            // 清理已销毁的引用
            if (entity.GameObject == null)
            {
                _removalKeys.Add(kvp.Key);
                continue;
            }

            Vector2 pos = entity.GameObject.transform.position;

            if (!bounds.Contains(pos))
            {
                _removalKeys.Add(kvp.Key);
                _toDespawn.Add(entity);
                culledCount++;

                if (_debugLog)
                {
                    DebugHelper.Log($"[OffScreenCuller] Will cull {entity.GameObject.name} at {pos}");
                }
            }
        }

        // 先从字典移除引用（遍历结束后）
        for (int i = 0; i < _removalKeys.Count; i++)
        {
            dict.Remove(_removalKeys[i]);
        }

        // 再执行回收/销毁（此时 Untrack 不会再修改 dict 中的 key）
        for (int i = 0; i < _toDespawn.Count; i++)
        {
            var entity = _toDespawn[i];
            if (entity.GameObject == null) continue;

            if (!string.IsNullOrEmpty(entity.PoolKey) &&
                ObjectPool.Instance != null &&
                ObjectPool.Instance.HasPool(entity.PoolKey))
            {
                ObjectPool.Instance.Despawn(entity.PoolKey, entity.GameObject);
            }
            else
            {
                Destroy(entity.GameObject);
            }
        }
    }

    // ── 注册/注销 API ──

    /// <summary>
    /// 注册投射物到裁剪管理（O(1)）
    /// </summary>
    public static void TrackProjectile(GameObject obj, string poolKey = null)
    {
        if (Instance == null || obj == null) return;
        int id = obj.GetInstanceID();
        Instance._trackedProjectiles[id] = new TrackedEntity
        {
            GameObject = obj,
            PoolKey = poolKey
        };
    }

    /// <summary>
    /// 注册掉落物（经验球、金币）到裁剪管理（O(1)）
    /// </summary>
    public static void TrackLoot(GameObject obj, string poolKey = null)
    {
        if (Instance == null || obj == null) return;
        int id = obj.GetInstanceID();
        Instance._trackedLoot[id] = new TrackedEntity
        {
            GameObject = obj,
            PoolKey = poolKey
        };
    }

    /// <summary>
    /// 注销对象（被拾取或手动回收时调用）— O(1) 查找和移除
    /// </summary>
    public static void Untrack(GameObject obj)
    {
        if (obj == null) return;
        // 应用退出时单例已销毁，直接跳过避免警告
        if (!Application.isPlaying) return;
        var inst = Instance;
        if (inst == null) return;
        int id = obj.GetInstanceID();

        // 先尝试从投射物字典移除，再尝试掉落物字典
        if (!inst._trackedProjectiles.Remove(id))
        {
            inst._trackedLoot.Remove(id);
        }
    }

    /// <summary>
    /// 清理所有跟踪数据（场景重载时调用）
    /// </summary>
    public void ClearAll()
    {
        _trackedProjectiles.Clear();
        _trackedLoot.Clear();
        _totalCulledProjectiles = 0;
        _totalCulledLoot = 0;
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public string GetStats()
    {
        return $"[OffScreenCuller] Tracked: {_trackedProjectiles.Count} projectiles, " +
               $"{_trackedLoot.Count} loot | Culled: {_totalCulledProjectiles} proj, " +
               $"{_totalCulledLoot} loot | Lookup: O(1)";
    }

    /// <summary>
    /// #31 判断世界坐标是否在屏幕外（含边距）
    /// 用于 StatusEffectManager 降低屏幕外敌人 DOT tick 频率
    /// </summary>
    public static bool IsOffScreen(Vector2 worldPos)
    {
        var inst = Instance;
        if (inst == null) return false;
        var cam = Camera.main;
        if (cam == null) return false;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;
        float marginWorld = inst._cullMargin / 100f;

        float dx = Mathf.Abs(worldPos.x - camPos.x);
        float dy = Mathf.Abs(worldPos.y - camPos.y);
        return dx > camWidth / 2f + marginWorld || dy > camHeight / 2f + marginWorld;
    }

    // ── 便捷注册组件 ──

    /// <summary>
    /// 自动注册投射物的组件（挂载到投射物上即可自动注册）
    /// </summary>
    public class AutoTrackProjectile : MonoBehaviour
    {
        [SerializeField] private string _poolKey;

        private void OnEnable()
        {
            TrackProjectile(gameObject, _poolKey);
        }

        private void OnDisable()
        {
            Untrack(gameObject);
        }
    }

    /// <summary>
    /// 自动注册掉落物的组件
    /// </summary>
    public class AutoTrackLoot : MonoBehaviour
    {
        [SerializeField] private string _poolKey;

        private void OnEnable()
        {
            TrackLoot(gameObject, _poolKey);
        }

        private void OnDisable()
        {
            Untrack(gameObject);
        }
    }
}