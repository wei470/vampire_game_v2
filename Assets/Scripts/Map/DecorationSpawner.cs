#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 装饰物生成器，根据当前地图主题生成对应的装饰物。
/// 对应 Python: game/decorations.py
/// 
/// 12 种装饰物类型（每种主题 3-4 种）：
/// - Forest: Tree, Rock, Bush, Mushroom
/// - Dungeon: Torch, Pillar
/// - Desert: Cactus, Dune
/// - Volcano: LavaRock, Obsidian
/// - Ice: IceCrystal, SnowPile
/// 
/// 装饰物仅视觉效果，不参与碰撞。
/// 使用方式：挂载到场景中的空 GameObject 上
/// </summary>
public class DecorationSpawner : MonoBehaviour
{
    [Header("生成配置")]
    [Tooltip("装饰物生成范围（以玩家为中心的半径）")]
    [SerializeField] private float _spawnRadius = 25f;

    [Tooltip("装饰物清理范围（超出此距离的装饰物将被销毁）")]
    [SerializeField] private float _cleanupRadius = 35f;

    [Tooltip("每 10x10 单位面积的装饰物数量")]
    [SerializeField] private float _density = 5f;

    [Tooltip("最小不透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float _minAlpha = 0.6f;

    [Tooltip("最大不透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float _maxAlpha = 1f;

    [Tooltip("装饰物排序层")]
    [SerializeField] private int _sortingOrder = -40;

    [Header("更新配置")]
    [Tooltip("更新间隔（秒）")]
    [SerializeField] private float _updateInterval = 1f;

    [Tooltip("每次更新最多生成的装饰物数")]
    [SerializeField] private int _maxSpawnPerUpdate = 5;

    // 运行时状态
    private List<GameObject> _activeDecorations = new List<GameObject>();
    private Transform _playerTransform;
    private float _lastUpdateTime;
    private MapThemeData _currentTheme;
    private System.Random _rng;

    // 装饰物 Sprite 缓存
    private Dictionary<MapThemeData.DecorationType, Sprite> _spriteCache = new Dictionary<MapThemeData.DecorationType, Sprite>();

    private void Start()
    {
        // #12 使用 MapThemeManager 的种子初始化，确保装饰物随机性与地图主题一致
        int seed = MapThemeManager.Instance != null ? MapThemeManager.Instance.CurrentSeed : 0;
        _rng = seed > 0 ? new System.Random(seed + GetInstanceID()) : new System.Random(GetInstanceID());

        var player = GameReferences.Player;
        if (player != null)
            _playerTransform = player.transform;

        // 订阅主题切换事件
        MapThemeManager.OnMapThemeChanged += OnThemeChanged;

        // 检查初始主题
        if (MapThemeManager.Instance != null && MapThemeManager.Instance.CurrentTheme != null)
        {
            OnThemeChanged(MapThemeManager.Instance.CurrentTheme, MapThemeManager.Instance.CurrentThemeIndex);
        }
    }

    private void OnDestroy()
    {
        MapThemeManager.OnMapThemeChanged -= OnThemeChanged;
    }

    private void Update()
    {
        if (_currentTheme == null) return;
        if (Time.time - _lastUpdateTime < _updateInterval) return;

        _lastUpdateTime = Time.time;

        // 清理超出范围的装饰物
        CleanupDistantDecorations();

        // 生成新装饰物
        SpawnDecorations();
    }

    /// <summary>
    /// 主题切换时清除所有装饰物并重新生成
    /// </summary>
    private void OnThemeChanged(MapThemeData newTheme, int themeIndex)
    {
        if (newTheme == null) return;

        _currentTheme = newTheme;
        _density = newTheme.decorationDensity;
        _spawnRadius = newTheme.decorationRadius;
        _minAlpha = newTheme.decorationMinAlpha;
        _maxAlpha = newTheme.decorationMaxAlpha;

        DebugHelper.Log($"[DecorationSpawner] Theme changed to {newTheme.themeName}, clearing and respawning decorations.");

        // 清除所有现有装饰物
        ClearAllDecorations();

        // 清除 Sprite 缓存
        _spriteCache.Clear();
    }

    /// <summary>
    /// 清理超出范围的装饰物
    /// </summary>
    private void CleanupDistantDecorations()
    {
        if (_playerTransform == null) return;

        Vector3 playerPos = _playerTransform.position;
        float cleanupSqr = _cleanupRadius * _cleanupRadius;

        for (int i = _activeDecorations.Count - 1; i >= 0; i--)
        {
            if (_activeDecorations[i] == null)
            {
                _activeDecorations.RemoveAt(i);
                continue;
            }

            float sqrDist = (_activeDecorations[i].transform.position - playerPos).sqrMagnitude;
            if (sqrDist > cleanupSqr)
            {
                Destroy(_activeDecorations[i]);
                _activeDecorations.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 生成新的装饰物
    /// </summary>
    private void SpawnDecorations()
    {
        if (_playerTransform == null || _currentTheme == null) return;
        if (_currentTheme.decorations == null || _currentTheme.decorations.Length == 0) return;

        // 计算目标数量
        float area = Mathf.PI * _spawnRadius * _spawnRadius;
        int targetCount = Mathf.RoundToInt(area * _density / 100f);

        // 清理空引用
        _activeDecorations.RemoveAll(d => d == null);

        int toSpawn = Mathf.Min(targetCount - _activeDecorations.Count, _maxSpawnPerUpdate);
        if (toSpawn <= 0) return;

        Vector3 playerPos = _playerTransform.position;

        for (int i = 0; i < toSpawn; i++)
        {
            // 在玩家周围随机位置生成
            Vector2 offset = Random.insideUnitCircle * _spawnRadius;
            Vector3 spawnPos = playerPos + new Vector3(offset.x, offset.y, 0);

            // 选择随机装饰物类型
            var decoType = _currentTheme.decorations[_rng.Next(_currentTheme.decorations.Length)];

            // 创建装饰物
            var deco = CreateDecoration(spawnPos, decoType);
            if (deco != null)
            {
                _activeDecorations.Add(deco);
            }
        }
    }

    /// <summary>
    /// 创建单个装饰物
    /// </summary>
    private GameObject CreateDecoration(Vector3 position, MapThemeData.DecorationType type)
    {
        var go = new GameObject($"Decoration_{type}");
        go.transform.position = position;

        // 添加 SpriteRenderer
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDecorationSprite(type);
        sr.color = GetDecorationColor(type);
        sr.sortingOrder = _sortingOrder;

        // 随机缩放（0.5x ~ 1.5x）
        float scale = 0.5f + (float)_rng.NextDouble();
        go.transform.localScale = Vector3.one * scale;

        // 随机旋转
        go.transform.rotation = Quaternion.Euler(0, 0, (float)_rng.NextDouble() * 360f);

        // 随机不透明度
        float alpha = Mathf.Lerp(_minAlpha, _maxAlpha, (float)_rng.NextDouble());
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;

        return go;
    }

    /// <summary>
    /// 获取装饰物 Sprite（带缓存）
    /// </summary>
    private Sprite GetDecorationSprite(MapThemeData.DecorationType type)
    {
        if (_spriteCache.TryGetValue(type, out Sprite cached))
            return cached;

        Sprite sprite = DecorationSpriteHelper.CreateDecorationSprite(type);
        _spriteCache[type] = sprite;
        return sprite;
    }

    /// <summary>
    /// 获取装饰物颜色
    /// </summary>
    private Color GetDecorationColor(MapThemeData.DecorationType type)
    {
        return DecorationSpriteHelper.GetDecorationColor(type);
    }

    // ============================================================
    // 公共方法
    // ============================================================

    /// <summary>
    /// 清除所有装饰物
    /// </summary>
    public void ClearAllDecorations()
    {
        foreach (var deco in _activeDecorations)
        {
            if (deco != null) Destroy(deco);
        }
        _activeDecorations.Clear();
    }

    /// <summary>
    /// 设置生成密度
    /// </summary>
    public void SetDensity(float density)
    {
        _density = Mathf.Clamp(density, 0f, 20f);
    }

    /// <summary>
    /// 设置生成范围
    /// </summary>
    public void SetSpawnRadius(float radius)
    {
        _spawnRadius = Mathf.Max(5f, radius);
    }

    /// <summary>
    /// 当前活跃装饰物数量
    /// </summary>
    public int ActiveDecorationCount => _activeDecorations.Count;
}