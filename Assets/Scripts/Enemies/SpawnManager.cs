using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 波次生成系统 — 协调器角色（~220行）
/// 
/// 职责拆分：
/// - EnemyPrefabFactory: 14种敌人预制体创建 + 池键映射 + 敌人选择
/// - WaveConfigHelper: 波次配置加载 + 难度倍率计算
/// - SpawnManager: 波次管理 + 生成逻辑 + 状态管理
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("波次配置")]
    [SerializeField] private int _baseEnemyCount = 3;
    [SerializeField] private int _enemiesPerWave = 2;
    [SerializeField] private float _restBetweenWaves = 3.5f;
    [SerializeField] private float _spawnInterval = 0.5f;
    [SerializeField] private float _spawnRadius = 15f;

    [Header("运行时状态")]
    [SerializeField] private int _currentWave = 0;
    [SerializeField] private int _enemiesAlive = 0;
    [SerializeField] private bool _isSpawning = false;
    [SerializeField] private bool _waveInProgress = false;
    [SerializeField] private float _cleanupInterval = 0.25f;
    private float _lastCleanupTime;

    // ── 公共属性 ──
    public int CurrentWave => _currentWave;
    public int EnemiesAlive => _enemiesAlive;
    public bool WaveInProgress => _waveInProgress;
    public IReadOnlyList<GameObject> ActiveEnemies => _activeEnemies;
    public float DamageMultiplier => _configHelper != null ? _configHelper.GetDamageMultiplier(_currentWave) : 1f;
    public float HpMultiplier => _configHelper != null ? _configHelper.GetHpMultiplier(_currentWave) : 1f;
    public float WeakenMultiplier
    {
        get { float m = SaveManager.Instance?.GetPermanentMultiplier("weaken_enemies") ?? 1f; return m > 0f ? m : 1f; }
    }
    public WaveChallengeSystem ChallengeSystem => _challengeSystem;

    // ── 内部状态 ──
    private List<GameObject> _activeEnemies = new List<GameObject>();
    private EnemyWaveConfig.SpecialWaveType _currentSpecialWave = EnemyWaveConfig.SpecialWaveType.None;
    private WaveChallengeSystem _challengeSystem;
    private Transform _playerTransform;

    // ── 拆分后的组件 ──
    private EnemyPrefabFactory _prefabFactory;
    private WaveConfigHelper _configHelper;

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
        _challengeSystem = gameObject.AddComponent<WaveChallengeSystem>();

        var player = GameReferences.Player;
        if (player != null) _playerTransform = player.transform;

        if (ConfigLoader.Game != null)
            _spawnRadius = ConfigLoader.Game.spawnRadius;

        // 初始化配置助手
        _configHelper = new WaveConfigHelper();
        _configHelper.LoadWaveConfig();
        _configHelper.GetBaseConfig(ref _baseEnemyCount, ref _enemiesPerWave, ref _spawnInterval, ref _spawnRadius);

        // 初始化预制体工厂
        _prefabFactory = new EnemyPrefabFactory(gameObject.layer);
        _prefabFactory.EnsureEnemyPrefabs();
    }

    /// <summary>
    /// 由 GameSceneBootstrap 显式调用，开始第 1 波
    /// </summary>
    public void StartFirstWave()
    {
        // 懒初始化（Start() 可能还没执行）
        if (_configHelper == null)
        {
            _configHelper = new WaveConfigHelper();
            _configHelper.LoadWaveConfig();
            _configHelper.GetBaseConfig(ref _baseEnemyCount, ref _enemiesPerWave, ref _spawnInterval, ref _spawnRadius);
        }
        if (_challengeSystem == null)
            _challengeSystem = gameObject.GetComponent<WaveChallengeSystem>() ?? gameObject.AddComponent<WaveChallengeSystem>();
        if (_prefabFactory == null)
            _prefabFactory = new EnemyPrefabFactory(gameObject.layer);

        StopAllCoroutines();
        _currentWave = 0;
        _enemiesAlive = 0;
        _isSpawning = false;
        _waveInProgress = false;

        ForceDestroyAllEnemies();
        _prefabFactory.EnsureEnemyPrefabs();
        EnsureEnemyPoolsWarmedUp();

        _playerTransform = null;
        var player = GameReferences.Player;
        if (player != null) _playerTransform = player.transform;
        else _playerTransform = FindAnyObjectByType<PlayerController>()?.transform;

        DebugHelper.Log("[SpawnManager] StartFirstWave: All enemies destroyed, state fully reset, starting wave 1");
        StartNextWave();
    }

    /// <summary>
    /// 开始下一波
    /// </summary>
    public void StartNextWave()
    {
        _currentWave++;
        int enemyCount = _configHelper.GetEnemyCountForWave(_currentWave, _baseEnemyCount, _enemiesPerWave);

        // 每波开始时强制清除场上残留的 DOT 子弹和毒液池
        CombatManager.DespawnAllDotBullets();

        EventManager.TriggerWaveStart(_currentWave);
        _waveInProgress = true;

        _currentSpecialWave = _configHelper.GetSpecialWaveType(_currentWave);
        bool isBossWave = _configHelper.IsBossWave(_currentWave);

        // Boss测试模式：每波都是Boss，5波一波Boss
        if (GameReferences.BossTestMode)
        {
            int bossHp = _configHelper.GetBossHP(_currentWave, HpMultiplier);

            if (SpawnWarningUI.Instance != null)
                SpawnWarningUI.Instance.ShowBossWarning($"Boss Wave {_currentWave}");

            DebugHelper.Log($"[SpawnManager] ⚔️ BOSS TEST MODE Wave {_currentWave}! Boss HP={bossHp}");
            SpawnBoss(bossHp);
        }
        else if (isBossWave)
        {
            int bossHp = _configHelper.GetBossHP(_currentWave, HpMultiplier);
            int bossMinions = _configHelper.GetBossMinions(_currentWave, enemyCount);

            if (SpawnWarningUI.Instance != null)
                SpawnWarningUI.Instance.ShowBossWarning($"Wave {_currentWave} Boss");

            DebugHelper.Log($"[SpawnManager] ⚔️ BOSS WAVE {_currentWave}! Boss HP={bossHp}");
            SpawnBoss(bossHp);
            StartCoroutine(SpawnWave(bossMinions));
        }
        else if (_currentSpecialWave != EnemyWaveConfig.SpecialWaveType.None)
        {
            int specialCount = _configHelper.GetSpecialWaveEnemyCount(enemyCount, _currentSpecialWave);

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
    /// 立即将已死亡的敌人从活跃列表中移除
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        _activeEnemies.Remove(enemy);
        _enemiesAlive = _activeEnemies.Count;
    }

    /// <summary>
    /// 预热所有敌人对象池（供 GameStarter 等外部调用）
    /// </summary>
    public void WarmUpAllEnemyPools()
    {
        if (_prefabFactory == null)
        {
            _prefabFactory = new EnemyPrefabFactory(gameObject.layer);
            _prefabFactory.EnsureEnemyPrefabs();
        }
        _prefabFactory.WarmUpEnemyPools();
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
        foreach (var enemy in _activeEnemies)
            if (enemy != null) Destroy(enemy);
        _activeEnemies.Clear();
    }

    private void Update()
    {
        if (!_waveInProgress || _isSpawning) return;

        if (Time.time - _lastCleanupTime >= _cleanupInterval)
        {
            _lastCleanupTime = Time.time;
            CleanDeadEnemies();
        }

        if (_enemiesAlive <= 0)
        {
            _waveInProgress = false;
            DebugHelper.Log($"[SpawnManager] Wave {_currentWave} complete! All enemies defeated.");
            EventManager.TriggerWaveComplete(_currentWave);
            StartCoroutine(RestBeforeNextWave());
        }
    }

    // ── 生成协程 ──

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

    private IEnumerator SpawnSpecialWave(int count, EnemyWaveConfig.SpecialWaveType type)
    {
        _isSpawning = true;
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = _prefabFactory.ChooseSpecialWaveEnemy(type);
            if (prefab != null) SpawnSpecificEnemy(prefab);
            else SpawnRandomEnemy();
            yield return new WaitForSeconds(_spawnInterval);
        }
        _isSpawning = false;
    }

    private IEnumerator RestBeforeNextWave()
    {
        DebugHelper.Log($"[SpawnManager] Resting for {_restBetweenWaves}s before next wave...");

        if (WaveIntermissionUI.Instance != null)
        {
            WaveIntermissionUI.Instance.SetRestDuration(_restBetweenWaves);
            int nextWave = _currentWave + 1;
            bool isNextBoss = _configHelper.IsBossWave(nextWave);
            var st = _configHelper.GetSpecialWaveType(nextWave);
            string specialType = st != EnemyWaveConfig.SpecialWaveType.None ? st.ToString() : null;
            WaveIntermissionUI.Instance.SetNextWavePreview(nextWave, isNextBoss, specialType);
        }

        if (_challengeSystem != null && _challengeSystem.OfferChallenge(_currentWave + 1))
            yield return _challengeSystem.WaitForChallengeResolution();

        yield return new WaitForSeconds(_restBetweenWaves);
        StartNextWave();
    }

    // ── 敌人生成 ──

    private void SpawnRandomEnemy()
    {
        if (!EnsurePlayerTransform()) return;

        Vector2 spawnPos = GetRandomSpawnPosition();
        GameObject prefab = _prefabFactory.ChooseEnemyPrefab(_currentWave);
        if (prefab == null) return;

        string poolKey = _prefabFactory.GetPoolKeyForPrefab(prefab);
        var enemy = PoolHelper.SpawnOrInstantiate(poolKey, prefab, spawnPos, Quaternion.identity);
        if (enemy == null) return;

        float challengeHp = _challengeSystem != null ? _challengeSystem.ChallengeHpMultiplier : 1f;
        float challengeSpd = _challengeSystem != null ? _challengeSystem.ChallengeSpeedMultiplier : 1f;
        int eliteArmor = _challengeSystem != null ? _challengeSystem.ChallengeEliteArmor : 0;
        EnemyScalingHelper.ApplyScaling(enemy, _playerTransform, HpMultiplier, WeakenMultiplier, challengeHp, challengeSpd, eliteArmor);

        // ── 精英词缀系统：第5波起概率生成精英 ──
        if (EliteModifierSystem.ShouldSpawnElite(_currentWave))
        {
            var eliteMod = enemy.GetComponent<EliteModifierSystem>();
            if (eliteMod == null) eliteMod = enemy.AddComponent<EliteModifierSystem>();
            eliteMod.ApplyElite(_currentWave);
            DebugHelper.Log($"[SpawnManager] Elite enemy spawned at wave {_currentWave}");
        }

        _activeEnemies.Add(enemy);
        _enemiesAlive = _activeEnemies.Count;
    }

    /// <summary>
    /// 在指定位置生成单个敌人（供召唤词缀等外部调用）
    /// </summary>
    public void SpawnSingleEnemy(Vector3 position)
    {
        GameObject prefab = _prefabFactory.ChooseEnemyPrefab(_currentWave);
        if (prefab == null) return;

        string poolKey = _prefabFactory.GetPoolKeyForPrefab(prefab);
        var enemy = PoolHelper.SpawnOrInstantiate(poolKey, prefab, position, Quaternion.identity);
        if (enemy == null) return;

        float challengeHp = _challengeSystem != null ? _challengeSystem.ChallengeHpMultiplier : 1f;
        float challengeSpd = _challengeSystem != null ? _challengeSystem.ChallengeSpeedMultiplier : 1f;
        int eliteArmor = _challengeSystem != null ? _challengeSystem.ChallengeEliteArmor : 0;
        EnemyScalingHelper.ApplyScaling(enemy, _playerTransform, HpMultiplier, WeakenMultiplier, challengeHp, challengeSpd, eliteArmor);

        _activeEnemies.Add(enemy);
        _enemiesAlive = _activeEnemies.Count;
    }

    private void SpawnSpecificEnemy(GameObject prefab)
    {
        if (_playerTransform == null || prefab == null) return;

        Vector2 spawnPos = GetRandomSpawnPosition();
        string poolKey = _prefabFactory.GetPoolKeyForPrefab(prefab);
        var enemy = PoolHelper.SpawnOrInstantiate(poolKey, prefab, spawnPos, Quaternion.identity);
        if (enemy == null) return;

        bool isSpeedSurge = _currentSpecialWave == EnemyWaveConfig.SpecialWaveType.SpeedSurge;
        EnemyScalingHelper.ApplySpecialScaling(enemy, _playerTransform, HpMultiplier, WeakenMultiplier, isSpeedSurge);

        _activeEnemies.Add(enemy);
        _enemiesAlive = _activeEnemies.Count;
    }

    private void SpawnBoss(int hp)
    {
        if (_playerTransform == null) return;
        Vector2 spawnPos = GetRandomSpawnPosition();
        BossEnemy boss = BossEnemy.CreateBoss(spawnPos, hp);
        if (boss != null)
        {
            // 将 Boss 加入活跃敌人列表，确保波次完成检查会等待 Boss 死亡
            _activeEnemies.Add(boss.gameObject);
            _enemiesAlive = _activeEnemies.Count;
        }
        DebugHelper.Log($"[SpawnManager] Boss spawned at {spawnPos} with {hp} HP!");
    }

    // ── 辅助方法 ──

    private bool EnsurePlayerTransform()
    {
        if (_playerTransform == null)
        {
            var player = GameReferences.Player;
            if (player != null) _playerTransform = player.transform;
            else _playerTransform = FindAnyObjectByType<PlayerController>()?.transform;
        }
        if (_playerTransform == null)
        {
            DebugHelper.LogWarning("[SpawnManager] Player transform is null, skipping spawn");
            return false;
        }
        return true;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        if (_playerTransform == null) return Random.insideUnitCircle * _spawnRadius;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spawnRadius;
        return (Vector2)_playerTransform.position + offset;
    }

    private void CleanDeadEnemies()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] == null || !_activeEnemies[i].activeInHierarchy)
            {
                int last = _activeEnemies.Count - 1;
                if (i != last) _activeEnemies[i] = _activeEnemies[last];
                _activeEnemies.RemoveAt(last);
            }
        }
        _enemiesAlive = _activeEnemies.Count;
    }

    private void ForceDestroyAllEnemies()
    {
        _activeEnemies.Clear();
        _enemiesAlive = 0;
        int destroyed = 0;
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (enemy != null) { Object.DestroyImmediate(enemy); destroyed++; }
        }
        foreach (var boss in FindObjectsByType<BossEnemy>())
        {
            if (boss != null) { Object.DestroyImmediate(boss.gameObject); destroyed++; }
        }
        DebugHelper.Log($"[SpawnManager] ForceDestroyAllEnemies: Destroyed {destroyed} enemies");
    }

    private void EnsureEnemyPoolsWarmedUp()
    {
        if (ObjectPool.Instance == null) return;
        if (_prefabFactory == null)
        {
            _prefabFactory = new EnemyPrefabFactory(gameObject.layer);
            _prefabFactory.EnsureEnemyPrefabs();
        }
        _prefabFactory.WarmUpEnemyPools();
        DebugHelper.Log("[SpawnManager] EnsureEnemyPoolsWarmedUp: Enemy pools re-warmed after restart");
    }

    private void OnPlayerDeath()
    {
        StopAllCoroutines();
        _isSpawning = false;
        _waveInProgress = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
}