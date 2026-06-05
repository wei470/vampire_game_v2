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
    [SerializeField] private float _restBetweenWaves = 3.5f; // #25 波次间休息时间（秒，增加到3.5秒）
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
    /// 获取当前活跃敌人列表（只读，供引爆等全局效果遍历）
    /// </summary>
    public IReadOnlyList<GameObject> ActiveEnemies => _activeEnemies;

    /// <summary>
    /// 当前波次配置（从 ConfigLoader 或 EnemyWaveConfig 加载）
    /// </summary>
    private EnemyWaveConfig _waveConfig;

    /// <summary>
    /// 当前特殊波次类型（None 表示普通波次）
    /// </summary>
    private EnemyWaveConfig.SpecialWaveType _currentSpecialWave = EnemyWaveConfig.SpecialWaveType.None;

    /// <summary>
    /// 伤害倍率 — 优先使用 EnemyWaveConfig 的可配置 S 曲线
    /// </summary>
    public float DamageMultiplier
    {
        get
        {
            if (_waveConfig != null)
                return _waveConfig.GetDamageMultiplier(_currentWave);
            return CalculateSCurveFallback(_currentWave, 0.08f);
        }
    }

    /// <summary>
    /// HP 倍率 — 优先使用 EnemyWaveConfig 的可配置 S 曲线
    /// </summary>
    public float HpMultiplier
    {
        get
        {
            if (_waveConfig != null)
                return _waveConfig.GetHpMultiplier(_currentWave);
            return CalculateSCurveFallback(_currentWave, 0.12f);
        }
    }

    /// <summary>
    /// 回退 S 曲线（无 EnemyWaveConfig 时使用，保持向后兼容）
    /// </summary>
    private static float CalculateSCurveFallback(int wave, float baseRate)
    {
        if (wave <= 1) return 1f;
        float w = wave - 1;
        float midpoint = 15f;
        float steepness = 0.2f;
        float sCurve = 1f / (1f + Mathf.Exp(-steepness * (w - midpoint)));
        float sCurveMin = 1f / (1f + Mathf.Exp(steepness * midpoint));
        float sCurveMax = 1f / (1f + Mathf.Exp(-steepness * (50f - midpoint)));
        float normalized = Mathf.Clamp01((sCurve - sCurveMin) / (sCurveMax - sCurveMin));
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

    // #39 波次挑战系统
    private WaveChallengeSystem _challengeSystem;

    /// <summary>
    /// 获取挑战系统的引用（供外部访问）
    /// </summary>
    public WaveChallengeSystem ChallengeSystem => _challengeSystem;

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
        // #39 初始化波次挑战系统
        _challengeSystem = gameObject.AddComponent<WaveChallengeSystem>();

        // 使用全局引用缓存，避免昂贵的 FindAnyObjectByType 调用
        var player = GameReferences.Player;
        if (player != null)
            _playerTransform = player.transform;

        // 从配置读取生成距离
        if (ConfigLoader.Game != null)
            _spawnRadius = ConfigLoader.Game.spawnRadius;

        // #32 加载波次配置（如果存在）
        if (ConfigLoader.Wave != null)
        {
            _waveConfig = ConfigLoader.Wave;
            _baseEnemyCount = _waveConfig.baseEnemyCount;
            _enemiesPerWave = _waveConfig.enemiesPerWave;
            _spawnInterval = _waveConfig.spawnInterval;
            _spawnRadius = _waveConfig.spawnRadius;
            DebugHelper.Log("[SpawnManager] Wave config loaded from EnemyWaveConfig");
        }

        // 如果预制体未在 Inspector 中赋值，运行时创建默认敌人预制体
        EnsureEnemyPrefabs();
    }

    /// <summary>
    /// 确保所有敌人预制体都已创建（Inspector 未赋值时运行时生成）
    /// </summary>
    private void EnsureEnemyPrefabs()
    {
        if (_basicEnemyPrefab == null)
            _basicEnemyPrefab = CreateEnemyPrefab("BasicEnemy", new Color(0.85f, 0.2f, 0.2f), 20, 5f, 10, 5, SpriteFactory.Square);
        if (_rangedEnemyPrefab == null)
            _rangedEnemyPrefab = CreateRangedEnemyPrefab("RangedEnemy", new Color(0.8f, 0.4f, 0.4f), sprite: SpriteFactory.Triangle);
        if (_tankEnemyPrefab == null)
            _tankEnemyPrefab = CreateEnemyPrefab("TankEnemy", new Color(0.5f, 0.5f, 0.5f), 80, 2.5f, 15, 10, SpriteFactory.Hexagon);
        if (_fastEnemyPrefab == null)
            _fastEnemyPrefab = CreateEnemyPrefab("FastEnemy", new Color(0.6f, 0.2f, 0.8f), 10, 9f, 8, 5, SpriteFactory.Triangle);
        if (_throwerEnemyPrefab == null)
            _throwerEnemyPrefab = CreateEnemyPrefab("ThrowerEnemy", new Color(1f, 0.5f, 0f), 18, 4f, 12, 8, SpriteFactory.Diamond);
        if (_healerEnemyPrefab == null)
            _healerEnemyPrefab = CreateEnemyPrefab("HealerEnemy", new Color(0.2f, 0.8f, 0.2f), 25, 4f, 5, 10, SpriteFactory.Cross);
        if (_enhancerEnemyPrefab == null)
            _enhancerEnemyPrefab = CreateEnemyPrefab("EnhancerEnemy", new Color(0.8f, 0.8f, 0.2f), 30, 4f, 8, 10, SpriteFactory.Pentagon);
        if (_splitterEnemyPrefab == null)
            _splitterEnemyPrefab = CreateEnemyPrefab("SplitterEnemy", new Color(0.6f, 0.2f, 0.4f), 35, 4f, 10, 8, SpriteFactory.Diamond);
        if (_summonerEnemyPrefab == null)
            _summonerEnemyPrefab = CreateEnemyPrefab("SummonerEnemy", new Color(0.4f, 0.1f, 0.6f), 30, 4f, 8, 12, SpriteFactory.Pentagon);
        if (_chargerEnemyPrefab == null)
            _chargerEnemyPrefab = CreateEnemyPrefab("ChargerEnemy", new Color(0.8f, 0.3f, 0.1f), 35, 5f, 15, 10, SpriteFactory.Triangle);
        if (_shielderEnemyPrefab == null)
            _shielderEnemyPrefab = CreateEnemyPrefab("ShielderEnemy", new Color(0.3f, 0.5f, 1f), 40, 4f, 5, 10, SpriteFactory.Hexagon);
        if (_stealthEnemyPrefab == null)
            _stealthEnemyPrefab = CreateEnemyPrefab("StealthEnemy", new Color(0.4f, 0.4f, 0.4f), 20, 7f, 12, 8, SpriteFactory.Diamond);
        if (_burstEnemyPrefab == null)
            _burstEnemyPrefab = CreateEnemyPrefab("BurstEnemy", new Color(1f, 0.6f, 0f), 25, 5f, 10, 8, SpriteFactory.Star);
        if (_chainHealerEnemyPrefab == null)
            _chainHealerEnemyPrefab = CreateEnemyPrefab("ChainHealerEnemy", new Color(0.2f, 0.8f, 0.6f), 25, 4f, 5, 10, SpriteFactory.Cross);

        DebugHelper.Log($"[SpawnManager] Enemy prefabs ensured (basic={_basicEnemyPrefab != null})");
    }

    /// <summary>
    /// 创建默认近战敌人预制体（运行时生成）
    /// </summary>
    private GameObject CreateEnemyPrefab(string name, Color color, int hp, float speed, int damage, int xpReward, Sprite sprite = null)
    {
        var go = new GameObject(name);
        go.tag = "Enemy";
        go.layer = gameObject.layer;

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

    /// <summary>
    /// 创建默认远程敌人预制体（运行时生成）
    /// </summary>
    private GameObject CreateRangedEnemyPrefab(string name = "RangedEnemy", Color color = default, int hp = 20, float speed = 4f, int damage = 8, int xpReward = 8, Sprite sprite = null)
    {
        if (color == default) color = new Color(0.8f, 0.4f, 0.4f);
        // 远程敌人使用 RangedEnemy 组件
        var go = new GameObject(name);
        go.tag = "Enemy";
        go.layer = gameObject.layer;

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

    /// <summary>
    /// 由 GameSceneBootstrap 在选择完成后显式调用，开始第 1 波。
    /// 不能在 Start() 中自动调用，因为 GameSceneBootstrap 可能先禁用 SpawnManager。
    /// </summary>
    public void StartFirstWave()
    {
        // 彻底重置：停止所有协程、清空状态、销毁场景中所有残留敌人
        StopAllCoroutines();

        _currentWave = 0;
        _enemiesAlive = 0;
        _isSpawning = false;
        _waveInProgress = false;

        // 强制清除场景中所有 Enemy 标签的物体（不依赖 _activeEnemies 列表）
        ForceDestroyAllEnemies();

        // 确保敌人预制体已创建（Start() 可能因组件被禁用而未执行）
        EnsureEnemyPrefabs();

        // 确保对象池已预热（GameSceneBootstrap.WarmUpObjectPools 可能在
        // SpawnManager.Start() 之前运行，导致池中没有敌人实例）
        EnsureEnemyPoolsWarmedUp();

        // 强制重新获取玩家引用（防止上一局的 stale 引用）
        _playerTransform = null;
        var player = GameReferences.Player;
        if (player != null)
            _playerTransform = player.transform;
        else
            _playerTransform = FindAnyObjectByType<PlayerController>()?.transform;

        DebugHelper.Log("[SpawnManager] StartFirstWave: All enemies destroyed, state fully reset, starting wave 1");
        StartNextWave();
    }

    /// <summary>
    /// 彻底销毁场景中所有残留敌人（包括 _activeEnemies 列表中的和未被追踪的）
    /// </summary>
    private void ForceDestroyAllEnemies()
    {
        // 1. 清空内部列表
        _activeEnemies.Clear();
        _enemiesAlive = 0;

        // 2. 查找并销毁场景中所有带 "Enemy" 标签的活跃物体
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        int destroyed = 0;
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(false); // 回到对象池
                destroyed++;
            }
        }

        // 3. 查找并销毁所有 Boss（可能标签不是 Enemy）
        var bosses = FindObjectsByType<BossEnemy>(FindObjectsSortMode.None);
        foreach (var boss in bosses)
        {
            if (boss != null)
            {
                boss.gameObject.SetActive(false);
                destroyed++;
            }
        }

        DebugHelper.Log($"[SpawnManager] ForceDestroyAllEnemies: Destroyed {destroyed} enemies");
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
            if (_activeEnemies[i] == null || !_activeEnemies[i].activeInHierarchy)
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
    /// #25 集成 WaveIntermissionUI 显示波次统计和下一波预告
    /// </summary>
    private IEnumerator RestBeforeNextWave()
    {
        DebugHelper.Log($"[SpawnManager] Resting for {_restBetweenWaves}s before next wave...");

        // #25 通知 WaveIntermissionUI 显示间歇期统计
        if (WaveIntermissionUI.Instance != null)
        {
            WaveIntermissionUI.Instance.SetRestDuration(_restBetweenWaves);

            // 计算下一波预告信息
            int nextWave = _currentWave + 1;
            bool isNextBoss = (_waveConfig != null)
                ? _waveConfig.IsBossWave(nextWave)
                : (nextWave % 5 == 0);
            string specialType = null;
            if (_waveConfig != null)
            {
                var st = _waveConfig.GetSpecialWaveType(nextWave);
                if (st != EnemyWaveConfig.SpecialWaveType.None)
                    specialType = st.ToString();
            }
            WaveIntermissionUI.Instance.SetNextWavePreview(nextWave, isNextBoss, specialType);
        }

        // #39 波次挑战：每 5 波非 Boss 波前提供挑战
        if (_challengeSystem != null && _challengeSystem.OfferChallenge(_currentWave + 1))
        {
            // 暂停倒计时，等待玩家选择
            yield return _challengeSystem.WaitForChallengeResolution();
        }

        yield return new WaitForSeconds(_restBetweenWaves);
        StartNextWave();
    }

    /// <summary>
    /// 开始下一波
    /// #32 支持可配置难度曲线 + 特殊波次事件
    /// </summary>
    public void StartNextWave()
    {
        _currentWave++;
        int enemyCount = _baseEnemyCount + (_currentWave - 1) * _enemiesPerWave;

        // #32 使用 EnemyWaveConfig 的敌人数量计算（如果有配置）
        if (_waveConfig != null)
            enemyCount = _waveConfig.GetEnemyCountForWave(_currentWave);

        EventManager.TriggerWaveStart(_currentWave);
        _waveInProgress = true;

        // #32 检测特殊波次
        _currentSpecialWave = _waveConfig != null
            ? _waveConfig.GetSpecialWaveType(_currentWave)
            : EnemyWaveConfig.SpecialWaveType.None;

        // Boss 波检测（优先于特殊波次）
        bool isBossWave = (_waveConfig != null)
            ? _waveConfig.IsBossWave(_currentWave)
            : (_currentWave % 5 == 0);

        if (isBossWave)
        {
            int bossHp = _waveConfig != null
                ? _waveConfig.GetBossHP(_currentWave)
                : Mathf.RoundToInt((300 + _currentWave * 60) * HpMultiplier);
            int bossMinions = _waveConfig != null
                ? Mathf.Min(enemyCount, _waveConfig.bossMinionsPerWave)
                : Mathf.Min(enemyCount, 3);

            // #22 Boss 预警
            if (SpawnWarningUI.Instance != null)
                SpawnWarningUI.Instance.ShowBossWarning($"Wave {_currentWave} Boss");

            DebugHelper.Log($"[SpawnManager] ⚔️ BOSS WAVE {_currentWave}! Boss HP={bossHp}");
            SpawnBoss(bossHp);
            StartCoroutine(SpawnWave(bossMinions));
        }
        else if (_currentSpecialWave != EnemyWaveConfig.SpecialWaveType.None)
        {
            // #32 特殊波次事件
            int specialCount = _waveConfig.GetSpecialWaveEnemyCount(enemyCount, _currentSpecialWave);

            // #22 特殊波次提示
            if (SpawnWarningUI.Instance != null)
                SpawnWarningUI.Instance.ShowWaveHint($"★ {_currentSpecialWave} Wave {_currentWave}");

            DebugHelper.Log($"[SpawnManager] ★ SPECIAL WAVE {_currentWave}: {_currentSpecialWave} ({specialCount} enemies)");
            StartCoroutine(SpawnSpecialWave(specialCount, _currentSpecialWave));
        }
        else
        {
            DebugHelper.Log($"[SpawnManager] Starting wave {_currentWave} with {enemyCount} enemies");
            StartCoroutine(SpawnWave(enemyCount));
        }
    }

    /// <summary>
    /// #32 生成特殊波次 — 根据特殊波次类型选择敌人和属性修正
    /// </summary>
    private IEnumerator SpawnSpecialWave(int count, EnemyWaveConfig.SpecialWaveType type)
    {
        _isSpawning = true;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = ChooseSpecialWaveEnemy(type);
            if (prefab != null)
                SpawnSpecificEnemy(prefab);
            else
                SpawnRandomEnemy();
            yield return new WaitForSeconds(_spawnInterval);
        }

        _isSpawning = false;
    }

    /// <summary>
    /// #32 根据特殊波次类型选择敌人预制体
    /// </summary>
    private GameObject ChooseSpecialWaveEnemy(EnemyWaveConfig.SpecialWaveType type)
    {
        switch (type)
        {
            case EnemyWaveConfig.SpecialWaveType.TankRush:
                return _tankEnemyPrefab ?? _basicEnemyPrefab;
            case EnemyWaveConfig.SpecialWaveType.SpeedSurge:
                return _fastEnemyPrefab ?? _basicEnemyPrefab;
            case EnemyWaveConfig.SpecialWaveType.SwarmWave:
                return _basicEnemyPrefab; // 大量弱敌
            case EnemyWaveConfig.SpecialWaveType.EliteWave:
                // 随机选择非 Basic 的精英敌人
                return ChooseEliteEnemy() ?? _basicEnemyPrefab;
            case EnemyWaveConfig.SpecialWaveType.HealerArmy:
                // 混合治疗敌人
                return Random.value < 0.5f
                    ? (_healerEnemyPrefab ?? _basicEnemyPrefab)
                    : (_chainHealerEnemyPrefab ?? _basicEnemyPrefab);
            case EnemyWaveConfig.SpecialWaveType.BossRush:
                return ChooseEliteEnemy() ?? _tankEnemyPrefab ?? _basicEnemyPrefab;
            default:
                return _basicEnemyPrefab;
        }
    }

    /// <summary>
    /// 选择精英敌人（排除 Basic）
    /// </summary>
    private GameObject ChooseEliteEnemy()
    {
        GameObject[] elites = {
            _tankEnemyPrefab, _chargerEnemyPrefab, _burstEnemyPrefab,
            _shielderEnemyPrefab, _stealthEnemyPrefab, _splitterEnemyPrefab
        };
        // 过滤 null 并随机选择
        List<GameObject> valid = new List<GameObject>();
        foreach (var e in elites)
            if (e != null) valid.Add(e);
        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : null;
    }

    /// <summary>
    /// 生成指定预制体的敌人（用于特殊波次）
    /// </summary>
    private void SpawnSpecificEnemy(GameObject prefab)
    {
        if (_playerTransform == null || prefab == null) return;

        Vector2 spawnPos = GetRandomSpawnPosition();
        string poolKey = GetPoolKeyForPrefab(prefab);
        var enemy = PoolHelper.SpawnOrInstantiate(poolKey, prefab, spawnPos, Quaternion.identity);
        if (enemy == null) return;

        var enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            var dmg = enemy.GetComponent<Damageable>();
            if (dmg != null)
            {
                int scaledMaxHp = Mathf.RoundToInt(dmg.MaxHp * HpMultiplier * WeakenMultiplier);
                dmg.SetMaxHp(scaledMaxHp);
            }

            // 特殊波次属性修正
            float weaken = WeakenMultiplier;
            float speedMult = weaken * 0.5f;

            // 速度提升波：敌人速度 ×2
            if (_currentSpecialWave == EnemyWaveConfig.SpecialWaveType.SpeedSurge)
                speedMult *= 2f;

            enemyBase.MoveSpeed *= speedMult;
            enemyBase.SetTarget(_playerTransform);
        }

        _activeEnemies.Add(enemy);
        _enemiesAlive = _activeEnemies.Count;
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
        // 安全检查：如果玩家引用丢失，尝试重新获取
        if (_playerTransform == null)
        {
            var player = GameReferences.Player;
            if (player != null)
                _playerTransform = player.transform;
            else
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) _playerTransform = pc.transform;
            }
        }
        if (_playerTransform == null)
        {
            DebugHelper.LogWarning("[SpawnManager] SpawnRandomEnemy: Player transform is null, skipping spawn");
            return;
        }

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
                float challengeHp = _challengeSystem != null ? _challengeSystem.ChallengeHpMultiplier : 1f;
                int scaledMaxHp = Mathf.RoundToInt(dmg.MaxHp * HpMultiplier * WeakenMultiplier * challengeHp);
                dmg.SetMaxHp(scaledMaxHp);
            }

            // 削弱敌人速度 + 全局减速50% + #39 挑战速度倍率
            float weaken = WeakenMultiplier;
            float challengeSpd = _challengeSystem != null ? _challengeSystem.ChallengeSpeedMultiplier : 1f;
            enemyBase.MoveSpeed *= weaken * 0.5f * challengeSpd;

            // #39 精英试炼：给敌人增加护甲
            int eliteArmor = _challengeSystem != null ? _challengeSystem.ChallengeEliteArmor : 0;
            if (eliteArmor > 0 && dmg != null)
                dmg.SetArmor(dmg.Armor + eliteArmor);

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

    /// <summary>
    /// 确保敌人对象池已预热（重启后 ObjectPool 可能是新建的空池）
    /// </summary>
    private void EnsureEnemyPoolsWarmedUp()
    {
        if (ObjectPool.Instance == null) return;

        PoolHelper.WarmUpEnemyPools(
            _basicEnemyPrefab, _rangedEnemyPrefab,
            _tankEnemyPrefab, _fastEnemyPrefab,
            _throwerEnemyPrefab, _healerEnemyPrefab,
            _enhancerEnemyPrefab, _splitterEnemyPrefab,
            _summonerEnemyPrefab, _chargerEnemyPrefab,
            _shielderEnemyPrefab, _stealthEnemyPrefab,
            _burstEnemyPrefab, _chainHealerEnemyPrefab);

        DebugHelper.Log("[SpawnManager] EnsureEnemyPoolsWarmedUp: Enemy pools re-warmed after restart");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
}
