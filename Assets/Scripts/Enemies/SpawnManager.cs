using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 波次生成系统，管理敌人波次的生成和进度。
/// 对应 Python: game/spawn_manager.py
/// 
/// 波次配置：baseEnemies + wave * 2
/// 从屏幕边缘随机位置生成
/// 所有敌人击杀后触发 OnWaveComplete → 进入下一波
/// 波次间短暂休息（2 秒）
/// 
/// 使用方式：挂载到场景中的空 GameObject 上
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("波次配置")]
    [SerializeField] private int _baseEnemyCount = 3;
    [SerializeField] private int _enemiesPerWave = 2;      // 每波增加的敌人数量
    [SerializeField] private float _restBetweenWaves = 2f;  // 波次间休息时间（秒）
    [SerializeField] private float _spawnInterval = 0.5f;   // 每个敌人生成间隔
    [SerializeField] private float _spawnRadius = 15f;      // 生成距离（屏幕边缘外）

    [Header("敌人预制体（14 种）")]
    [SerializeField] private GameObject _basicEnemyPrefab;
    [SerializeField] private GameObject _rangedEnemyPrefab;
    [SerializeField] private GameObject _tankEnemyPrefab;
    [SerializeField] private GameObject _fastEnemyPrefab;
    [SerializeField] private GameObject _throwerEnemyPrefab;
    [SerializeField] private GameObject _healerEnemyPrefab;
    [SerializeField] private GameObject _enhancerEnemyPrefab;
    [SerializeField] private GameObject _splitterEnemyPrefab;
    [SerializeField] private GameObject _summonerEnemyPrefab;
    [SerializeField] private GameObject _chargerEnemyPrefab;
    [SerializeField] private GameObject _shielderEnemyPrefab;
    [SerializeField] private GameObject _stealthEnemyPrefab;
    [SerializeField] private GameObject _burstEnemyPrefab;
    [SerializeField] private GameObject _chainHealerEnemyPrefab;

    [Header("运行时状态")]
    [SerializeField] private int _currentWave = 0;
    [SerializeField] private int _enemiesAlive = 0;
    [SerializeField] private bool _isSpawning = false;
    [SerializeField] private bool _waveInProgress = false;
    [SerializeField] private float _cleanupInterval = 0.25f; // 敌人列表清理间隔（秒）
    private float _lastCleanupTime;

    /// <summary>
    /// 当前波次
    /// </summary>
    public int CurrentWave => _currentWave;

    /// <summary>
    /// 存活敌人数量
    /// </summary>
    public int EnemiesAlive => _enemiesAlive;

    /// <summary>
    /// 波次是否正在进行
    /// </summary>
    public bool WaveInProgress => _waveInProgress;

    /// <summary>
    /// 生成的敌人列表（用于跟踪存活数）
    /// </summary>
    private List<GameObject> _activeEnemies = new List<GameObject>();

    /// <summary>
    /// 伤害倍率（S 曲线递增：前 10 波缓慢，中期加速，后期平稳）
    /// </summary>
    public float DamageMultiplier => CalculateSCurveMultiplier(_currentWave, 0.08f);

    /// <summary>
    /// HP 倍率（S 曲线递增：前 10 波缓慢，中期加速，后期平稳）
    /// </summary>
    public float HpMultiplier => CalculateSCurveMultiplier(_currentWave, 0.12f);

    /// <summary>
    /// S 曲线成长公式 — 前 10 波缓慢增长，中期加速，后期给玩家喘息空间
    /// </summary>
    private static float CalculateSCurveMultiplier(int wave, float baseRate)
    {
        if (wave <= 1) return 1f;

        // S 曲线参数
        float w = wave - 1;
        float midpoint = 15f;   // 曲线中点（第 16 波左右）
        float steepness = 0.2f; // 曲线陡峭程度

        // 标准 S 曲线：1 / (1 + e^(-k*(x-midpoint)))
        // 归一化到 [0, 1] 范围后乘以最大倍率
        float sCurve = 1f / (1f + Mathf.Exp(-steepness * (w - midpoint)));
        float sCurveMin = 1f / (1f + Mathf.Exp(steepness * midpoint));     // wave=0 时的 S 值
        float sCurveMax = 1f / (1f + Mathf.Exp(-steepness * (50f - midpoint))); // wave=50 时的 S 值

        // 归一化到 [0, 1]
        float normalized = (sCurve - sCurveMin) / (sCurveMax - sCurveMin);
        normalized = Mathf.Clamp01(normalized);

        // 最大倍率 = 1 + 50波 * baseRate ≈ 5x
        float maxMult = 1f + 50f * baseRate * 1.5f;
        return 1f + (maxMult - 1f) * normalized;
    }

    /// <summary>
    /// 敌人削弱倍率（来自 SaveManager weaken_enemies 升级）
    /// </summary>
    public float WeakenMultiplier
    {
        get
        {
            float mult = SaveManager.Instance?.GetPermanentMultiplier("weaken_enemies") ?? 1f;
            return mult > 0f ? mult : 1f;
        }
    }

    private Transform _playerTransform;

    private void OnEnable()
    {
        EventManager.OnPlayerDeath += OnPlayerDeath;
    }

    private void OnDisable()
    {
        EventManager.OnPlayerDeath -= OnPlayerDeath;
    }

    private void Start()
    {
        // 使用全局引用缓存，避免昂贵的 FindAnyObjectByType 调用
        var player = GameReferences.Player;
        if (player != null)
            _playerTransform = player.transform;

        // 从配置读取生成距离
        if (ConfigLoader.Game != null)
            _spawnRadius = ConfigLoader.Game.spawnRadius;

        // 如果预制体未在 Inspector 中赋值，运行时创建默认敌人预制体
        EnsureEnemyPrefabs();
    }

    /// <summary>
    /// 确保所有敌人预制体都已创建（Inspector 未赋值时运行时生成）
    /// </summary>
    private void EnsureEnemyPrefabs()
    {
        if (_basicEnemyPrefab == null)
            _basicEnemyPrefab = CreateEnemyPrefab("BasicEnemy", Color.red, 30, 5f, 10, 5);
        if (_rangedEnemyPrefab == null)
            _rangedEnemyPrefab = CreateRangedEnemyPrefab();
        if (_tankEnemyPrefab == null)
            _tankEnemyPrefab = CreateEnemyPrefab("TankEnemy", new Color(0.5f, 0.2f, 0.2f), 80, 3f, 15, 10);
        if (_fastEnemyPrefab == null)
            _fastEnemyPrefab = CreateEnemyPrefab("FastEnemy", Color.yellow, 15, 10f, 8, 5);
        if (_throwerEnemyPrefab == null)
            _throwerEnemyPrefab = CreateRangedEnemyPrefab("ThrowerEnemy", new Color(0.8f, 0.4f, 0f), 25, 4f, 12, 8);
        if (_healerEnemyPrefab == null)
            _healerEnemyPrefab = CreateEnemyPrefab("HealerEnemy", Color.green, 25, 5f, 5, 8);
        if (_enhancerEnemyPrefab == null)
            _enhancerEnemyPrefab = CreateEnemyPrefab("EnhancerEnemy", new Color(1f, 0.5f, 1f), 20, 6f, 5, 6);
        if (_splitterEnemyPrefab == null)
            _splitterEnemyPrefab = CreateEnemyPrefab("SplitterEnemy", new Color(0.6f, 0.4f, 0.8f), 25, 5f, 10, 8);
        if (_summonerEnemyPrefab == null)
            _summonerEnemyPrefab = CreateEnemyPrefab("SummonerEnemy", new Color(0.4f, 0.2f, 0.6f), 30, 4f, 8, 12);
        if (_chargerEnemyPrefab == null)
            _chargerEnemyPrefab = CreateEnemyPrefab("ChargerEnemy", new Color(1f, 0.3f, 0.3f), 35, 6f, 15, 8);
        if (_shielderEnemyPrefab == null)
            _shielderEnemyPrefab = CreateEnemyPrefab("ShielderEnemy", new Color(0.3f, 0.5f, 1f), 40, 4f, 5, 10);
        if (_stealthEnemyPrefab == null)
            _stealthEnemyPrefab = CreateEnemyPrefab("StealthEnemy", new Color(0.4f, 0.4f, 0.4f), 20, 7f, 12, 8);
        if (_burstEnemyPrefab == null)
            _burstEnemyPrefab = CreateEnemyPrefab("BurstEnemy", new Color(1f, 0.6f, 0f), 25, 5f, 10, 8);
        if (_chainHealerEnemyPrefab == null)
            _chainHealerEnemyPrefab = CreateEnemyPrefab("ChainHealerEnemy", new Color(0.2f, 0.8f, 0.6f), 25, 4f, 5, 10);

        DebugHelper.Log($"[SpawnManager] Enemy prefabs ensured (basic={_basicEnemyPrefab != null})");
    }

    /// <summary>
    /// 创建默认近战敌人预制体（运行时生成）
    /// </summary>
    private GameObject CreateEnemyPrefab(string name, Color color, int hp, float speed, int damage, int xpReward)
    {
        var go = new GameObject(name);
        go.tag = "Enemy";
        go.layer = gameObject.layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
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

    /// <summary>
    /// 创建默认远程敌人预制体（运行时生成）
    /// </summary>
    private GameObject CreateRangedEnemyPrefab(string name = "RangedEnemy", Color color = default, int hp = 20, float speed = 4f, int damage = 8, int xpReward = 8)
    {
        if (color == default) color = new Color(0.8f, 0.4f, 0.4f);
        // 远程敌人使用 RangedEnemy 组件
        var go = new GameObject(name);
        go.tag = "Enemy";
        go.layer = gameObject.layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
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

    /// <summary>
    /// 由 GameSceneBootstrap 在选择完成后显式调用，开始第 1 波。
    /// 不能在 Start() 中自动调用，因为 GameSceneBootstrap 可能先禁用 SpawnManager。
    /// </summary>
    public void StartFirstWave()
    {
        // 确保玩家引用已缓存
        if (_playerTransform == null)
        {
            var player = GameReferences.Player;
            if (player != null)
                _playerTransform = player.transform;
        }

        StartNextWave();
    }

    private void Update()
    {
        if (!_waveInProgress || _isSpawning) return;

        // 定期清理已销毁的敌人（逆序手动遍历，避免 RemoveAll 委托分配）
        if (Time.time - _lastCleanupTime >= _cleanupInterval)
        {
            _lastCleanupTime = Time.time;
            CleanDeadEnemies();
        }

        // 所有敌人已清除，进入下一波
        if (_enemiesAlive <= 0)
        {
            _waveInProgress = false;
            DebugHelper.Log($"[SpawnManager] Wave {_currentWave} complete! All enemies defeated.");
            EventManager.TriggerWaveComplete(_currentWave);
            StartCoroutine(RestBeforeNextWave());
        }
    }

    /// <summary>
    /// 逆序遍历移除已销毁的敌人引用（无 GC 分配）
    /// </summary>
    private void CleanDeadEnemies()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] == null)
            {
                int last = _activeEnemies.Count - 1;
                if (i != last)
                    _activeEnemies[i] = _activeEnemies[last];
                _activeEnemies.RemoveAt(last);
            }
        }
        _enemiesAlive = _activeEnemies.Count;
    }

    /// <summary>
    /// 波次间休息，然后开始下一波
    /// </summary>
    private IEnumerator RestBeforeNextWave()
    {
        DebugHelper.Log($"[SpawnManager] Resting for {_restBetweenWaves}s before next wave...");
        yield return new WaitForSeconds(_restBetweenWaves);
        StartNextWave();
    }

    /// <summary>
    /// 开始下一波
    /// </summary>
    public void StartNextWave()
    {
        _currentWave++;
        int enemyCount = _baseEnemyCount + (_currentWave - 1) * _enemiesPerWave;

        DebugHelper.Log($"[SpawnManager] Starting wave {_currentWave} with {enemyCount} enemies");
        EventManager.TriggerWaveStart(_currentWave);

        _waveInProgress = true;

        // 每 5 波生成 Boss
        if (_currentWave % 5 == 0)
        {
            // Boss HP 使用 S 曲线：前期温和，中期挑战，后期可控
            int bossHp = Mathf.RoundToInt((300 + _currentWave * 60) * HpMultiplier);
            DebugHelper.Log($"[SpawnManager] ⚔️ BOSS WAVE {_currentWave}! Boss HP={bossHp}");
            SpawnBoss(bossHp);
            // Boss 波同时生成少量小怪
            StartCoroutine(SpawnWave(Mathf.Min(enemyCount, 3)));
        }
        else
        {
            StartCoroutine(SpawnWave(enemyCount));
        }
    }

    /// <summary>
    /// 生成 Boss
    /// </summary>
    private void SpawnBoss(int hp)
    {
        if (_playerTransform == null) return;

        Vector2 spawnPos = GetRandomSpawnPosition();
        BossEnemy.CreateBoss(spawnPos, hp);
        DebugHelper.Log($"[SpawnManager] Boss spawned at {spawnPos} with {hp} HP!");
    }

    /// <summary>
    /// 生成一波敌人
    /// </summary>
    private IEnumerator SpawnWave(int count)
    {
        _isSpawning = true;

        for (int i = 0; i < count; i++)
        {
            SpawnRandomEnemy();
            yield return new WaitForSeconds(_spawnInterval);
        }

        _isSpawning = false;
    }

    /// <summary>
    /// 在屏幕边缘随机位置生成一个敌人（全部通过预制体 + 对象池生成）
    /// </summary>
    private void SpawnRandomEnemy()
    {
        if (_playerTransform == null) return;

        Vector2 spawnPos = GetRandomSpawnPosition();

        // 根据波次决定敌人类型（预制体选择）
        GameObject prefab = ChooseEnemyPrefab();
        if (prefab == null)
        {
            DebugHelper.LogWarning($"[SpawnManager] No prefab available for wave {_currentWave}, skipping spawn");
            return;
        }

        // 通过对象池取出敌人（自动注册池 + 回收复用）
        string poolKey = GetPoolKeyForPrefab(prefab);
        var enemy = PoolHelper.SpawnOrInstantiate(poolKey, prefab, spawnPos, Quaternion.identity);
        if (enemy == null) return;

        var enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            // 根据波次调整敌人属性 + 敌人削弱
            var dmg = enemy.GetComponent<Damageable>();
            if (dmg != null)
            {
                int scaledMaxHp = Mathf.RoundToInt(dmg.MaxHp * HpMultiplier * WeakenMultiplier);
                dmg.SetMaxHp(scaledMaxHp);
            }

            // 削弱敌人速度
            float weaken = WeakenMultiplier;
            enemyBase.MoveSpeed *= weaken;

            enemyBase.SetTarget(_playerTransform);
        }

        _activeEnemies.Add(enemy);
        _enemiesAlive = _activeEnemies.Count;
    }

    /// <summary>
    /// 根据波次选择敌人类型（全部 14 种，渐进解锁）
    /// </summary>
    private GameObject ChooseEnemyPrefab()
    {
        // 第 1-2 波：只有普通敌人
        if (_currentWave <= 2)
            return _basicEnemyPrefab;

        float roll = Random.value;

        // 波 3-4: Basic + Ranged + Fast
        if (_currentWave <= 4)
        {
            if (roll < 0.30f && _rangedEnemyPrefab != null) return _rangedEnemyPrefab;
            if (roll < 0.60f && _fastEnemyPrefab != null) return _fastEnemyPrefab;
            return _basicEnemyPrefab;
        }

        // 波 5-7: +Tank, +Thrower
        if (_currentWave <= 7)
        {
            if (roll < 0.10f && _tankEnemyPrefab != null) return _tankEnemyPrefab;
            if (roll < 0.25f && _rangedEnemyPrefab != null) return _rangedEnemyPrefab;
            if (roll < 0.40f && _fastEnemyPrefab != null) return _fastEnemyPrefab;
            if (roll < 0.55f && _throwerEnemyPrefab != null) return _throwerEnemyPrefab;
            return _basicEnemyPrefab;
        }

        // 波 8+: 全部 14 种敌人
        if (roll < 0.05f && _tankEnemyPrefab != null) return _tankEnemyPrefab;
        if (roll < 0.12f && _healerEnemyPrefab != null) return _healerEnemyPrefab;
        if (roll < 0.18f && _enhancerEnemyPrefab != null) return _enhancerEnemyPrefab;
        if (roll < 0.24f && _splitterEnemyPrefab != null) return _splitterEnemyPrefab;
        if (roll < 0.30f && _summonerEnemyPrefab != null) return _summonerEnemyPrefab;
        if (roll < 0.36f && _chargerEnemyPrefab != null) return _chargerEnemyPrefab;
        if (roll < 0.42f && _shielderEnemyPrefab != null) return _shielderEnemyPrefab;
        if (roll < 0.48f && _stealthEnemyPrefab != null) return _stealthEnemyPrefab;
        if (roll < 0.54f && _burstEnemyPrefab != null) return _burstEnemyPrefab;
        if (roll < 0.60f && _chainHealerEnemyPrefab != null) return _chainHealerEnemyPrefab;
        if (roll < 0.70f && _rangedEnemyPrefab != null) return _rangedEnemyPrefab;
        if (roll < 0.80f && _fastEnemyPrefab != null) return _fastEnemyPrefab;
        if (roll < 0.88f && _throwerEnemyPrefab != null) return _throwerEnemyPrefab;
        return _basicEnemyPrefab;
    }

    /// <summary>
    /// 获取屏幕边缘随机生成位置
    /// </summary>
    private Vector2 GetRandomSpawnPosition()
    {
        if (_playerTransform == null) return Random.insideUnitCircle * _spawnRadius;

        // 在玩家周围的圆形边缘随机生成
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spawnRadius;
        return (Vector2)_playerTransform.position + offset;
    }

    /// <summary>
    /// 根据预制体获取对应的池键（全部 14 种）
    /// </summary>
    private string GetPoolKeyForPrefab(GameObject prefab)
    {
        if (prefab == _basicEnemyPrefab) return PoolHelper.BASIC_ENEMY;
        if (prefab == _rangedEnemyPrefab) return PoolHelper.RANGED_ENEMY;
        if (prefab == _tankEnemyPrefab) return PoolHelper.TANK_ENEMY;
        if (prefab == _fastEnemyPrefab) return PoolHelper.FAST_ENEMY;
        if (prefab == _throwerEnemyPrefab) return PoolHelper.THROWER_ENEMY;
        if (prefab == _healerEnemyPrefab) return PoolHelper.HEALER_ENEMY;
        if (prefab == _enhancerEnemyPrefab) return PoolHelper.ENHANCER_ENEMY;
        if (prefab == _splitterEnemyPrefab) return PoolHelper.SPLITTER_ENEMY;
        if (prefab == _summonerEnemyPrefab) return PoolHelper.SUMMONER_ENEMY;
        if (prefab == _chargerEnemyPrefab) return PoolHelper.CHARGER_ENEMY;
        if (prefab == _shielderEnemyPrefab) return PoolHelper.SHIELDER_ENEMY;
        if (prefab == _stealthEnemyPrefab) return PoolHelper.STEALTH_ENEMY;
        if (prefab == _burstEnemyPrefab) return PoolHelper.BURST_ENEMY;
        if (prefab == _chainHealerEnemyPrefab) return PoolHelper.CHAIN_HEALER_ENEMY;
        return PoolHelper.BASIC_ENEMY;
    }

    // CreateSquareSprite 已迁移到 SpriteFactory.Square

    /// <summary>
    /// 玩家死亡时停止生成
    /// </summary>
    private void OnPlayerDeath()
    {
        StopAllCoroutines();
        _isSpawning = false;
        _waveInProgress = false;
    }

    /// <summary>
    /// 立即将已死亡的敌人从活跃列表中移除（由 EnemyBase 死亡时调用）
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        _activeEnemies.Remove(enemy);
        _enemiesAlive = _activeEnemies.Count;
    }

    /// <summary>
    /// 重置波次系统
    /// </summary>
    public void ResetWaves()
    {
        StopAllCoroutines();
        _currentWave = 0;
        _enemiesAlive = 0;
        _isSpawning = false;
        _waveInProgress = false;

        // 清除所有存活敌人
        foreach (var enemy in _activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        _activeEnemies.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
}