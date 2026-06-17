#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 游戏主场景运行时引导器 — 协调器角色
/// 
/// 职责拆分：
/// - GameDataLoader: 数据加载（角色/武器/技能/Mage配置）
/// - GameStarter: 游戏启动逻辑（应用配置、预热池、开始游戏）
/// - GameHUDFactory: HUD 创建
/// - GameSceneBootstrap: 组件协调、生命周期管理
/// 
/// 使用方式：挂载到场景中的空 GameObject 上
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    private PlayerController _player;
    private SpawnManager _spawnManager;
    private GameInputHandler _inputHandler;
    private SelectionUI _selectionUI;
    private DebugOverlay _debugOverlay;
    private PauseMenuUI _pauseMenuUI;

    private bool _selectionDone = false;
    private bool _gameStarted = false;

    /// <summary>
    /// 当前选择的角色数据（静态，供 LevelUpUI 等访问）
    /// </summary>
    public static CharacterData CurrentCharacter { get; set; }

    /// <summary>
    /// 重置角色选择数据（返回菜单时调用，防止残留）
    /// </summary>
    public static void ResetCharacter()
    {
        CurrentCharacter = null;
    }

    // ── 拆分后的组件 ──
    private GameDataLoader _dataLoader;
    private GameStarter _gameStarter;
    private GameHUDFactory _hudFactory;

    private void Start()
    {
        Application.targetFrameRate = 120;

        // #17 配置 Physics2D 碰撞矩阵
        PhysicsLayerSetup.SetupCollisionMatrix();

        // ── 加载数据 ──
        _dataLoader = new GameDataLoader();
        _dataLoader.LoadSelectionData();

        // SaveManager 是 Singleton，Instance getter 自动创建
        _ = SaveManager.Instance;

        // 初始状态：暂停游戏，等待选择
        Time.timeScale = 0.0001f;

        // 暂停 SpawnManager（初始化时查找，非 Singleton 无 .Instance）
        _spawnManager = FindAnyObjectByType<SpawnManager>();
        if (_spawnManager != null)
            _spawnManager.enabled = false;

        _player = FindAnyObjectByType<PlayerController>(); // 初始化查找，之后缓存到 GameReferences.Player

        // 注入全局引用缓存
        GameReferences.Player = _player;
        GameReferences.SpawnManager = _spawnManager;
        GameReferences.MainCamera = Camera.main;

        // ── 确保管理器单例存在 ──
        var shopUI = EnsureManagerExists<ShopUI>("ShopUI");
        var bgmManager = EnsureManagerExists<BGMManager>("BGMManager");
        EnsureSFXManager();
        EnsureDamageMeter();

        // ── 创建 HUD 工厂并生成预游戏 HUD ──
        _hudFactory = new GameHUDFactory(gameObject);
        _hudFactory.CreatePreGameHUD();

        // ── 创建 GameInputHandler ──
        _inputHandler = gameObject.AddComponent<GameInputHandler>();
        _inputHandler.Setup(_spawnManager);
        _inputHandler.SetShopUI(shopUI);

        // ── 创建 SelectionUI ──
        _selectionUI = gameObject.AddComponent<SelectionUI>();
        _selectionUI.Setup(_dataLoader.Characters, _dataLoader.Weapons);
        _selectionUI.OnSelectionConfirmed = OnSelectionConfirmed;

        // ── 创建 DebugOverlay ──
        _debugOverlay = gameObject.AddComponent<DebugOverlay>();
        _debugOverlay.Setup(_player, _spawnManager);

        // ── Debug 面板 ──
        if (gameObject.GetComponent<DebugConfigPanel>() == null)
            gameObject.AddComponent<DebugConfigPanel>();
        if (gameObject.GetComponent<DebugPoolMonitor>() == null)
            gameObject.AddComponent<DebugPoolMonitor>();

        // ── 创建 PauseMenuUI ──
        _pauseMenuUI = gameObject.AddComponent<PauseMenuUI>();
        _inputHandler.SetPauseMenuUI(_pauseMenuUI);

        // ── 创建连击系统 ──
        if (ComboSystem.Instance == null)
        {
            gameObject.AddComponent<ComboSystem>();
            DebugHelper.Log("[GameSceneBootstrap] Created ComboSystem");
        }

        // ── 创建 GameStarter ──
        _gameStarter = new GameStarter(_player, _spawnManager, bgmManager, _hudFactory);

        DebugHelper.Log("[GameSceneBootstrap] Showing selection flow...");

        // 测试模式
        if (GameReferences.TestMode)
        {
            _selectionDone = true;
            var testBulletUI = gameObject.AddComponent<TestBulletSelectUI>();
            testBulletUI.Setup(_dataLoader.MageUpgradeConfig, OnTestBulletSelectionConfirmed);
            DebugHelper.Log("[GameSceneBootstrap] TestMode: Showing bullet selection UI");
        }
        else
        {
            _selectionDone = false;
            DebugHelper.Log("[GameSceneBootstrap] Showing character selection");
        }
    }

    /// <summary>

    private void OnSelectionConfirmed(int selectedChar, int selectedWeapon, int selectedSkill)
    {
        _selectionDone = true;
        _gameStarted = true;
        _gameStarter.ApplySelectionAndStartGame(
            selectedChar, selectedWeapon,
            _dataLoader.Characters, _dataLoader.Weapons,
            _dataLoader.MageUpgradeConfig);
    }

    /// <summary>
    /// Test 模式子弹选择完成回调
    /// </summary>
    private void OnTestBulletSelectionConfirmed(List<string> selectedBulletIds, Dictionary<string, int> selectedUpgrades, int startWave)
    {
        var characters = _dataLoader.Characters;

        int mageIndex = 0;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null && characters[i].characterId == "mage")
            {
                mageIndex = i;
                break;
            }
        }

        _gameStarted = true;

        if (GameReferences.DpsTestMode)
        {
            if (_spawnManager != null) _spawnManager.enabled = false;
            Time.timeScale = 1f;

            _gameStarter.ApplySelectionAndStartGame(
                mageIndex, 0,
                characters, _dataLoader.Weapons,
                _dataLoader.MageUpgradeConfig);

            if (_spawnManager != null) _spawnManager.enabled = false;

            ApplyTestBulletsAndUpgrades(selectedBulletIds, selectedUpgrades);
            SpawnDpsTestDummy();
            return;
        }

        _gameStarter.ApplySelectionAndStartGame(
            mageIndex, 0,
            characters, _dataLoader.Weapons,
            _dataLoader.MageUpgradeConfig);

        ApplyTestBulletsAndUpgrades(selectedBulletIds, selectedUpgrades);

        if (_spawnManager != null && !GameReferences.DpsTestMode)
        {
            _spawnManager.enabled = true;
            _spawnManager.StartFromWave(startWave);
        }
        DebugHelper.Log($"[GameSceneBootstrap] TestMode: Game started with Mage + Teleport, {selectedBulletIds.Count} bullets, {(selectedUpgrades != null ? selectedUpgrades.Count : 0)} upgrades");
    }

    private void ApplyTestBulletsAndUpgrades(List<string> selectedBulletIds, Dictionary<string, int> selectedUpgrades)
    {
        var dotPassive = _player?.GetComponent<IDotCharacterPassive>();
        if (dotPassive == null) return;

        dotPassive.ClearAllDotGuns();
        var config = _dataLoader.MageUpgradeConfig;
        if (config != null && selectedBulletIds.Count > 0)
        {
            foreach (string bulletId in selectedBulletIds)
            {
                var entry = config.GetDotGunEntry(bulletId);
                if (entry.HasValue)
                {
                    var dg = entry.Value;
                    dotPassive.UnlockDotGun(dg.effectType, dg.color, dg.cooldown, dg.impactDmg, dg.dotDps, dg.dotDuration);
                    DebugHelper.Log($"[GameSceneBootstrap] TestMode: Added bullet '{dg.displayName}'");
                }
            }
        }
        else if (selectedBulletIds.Count == 0)
        {
            DebugHelper.Log("[GameSceneBootstrap] TestMode: No bullets selected, entering with empty loadout");
        }

        if (selectedUpgrades != null && selectedUpgrades.Count > 0)
        {
            var magePassive = dotPassive as MagePassive;
            foreach (var kvp in selectedUpgrades)
            {
                for (int s = 0; s < kvp.Value; s++)
                {
                    // 走 MagePassive.ApplyUpgrade（而非直接调 MageUpgradeApplier），
                    // 否则不会累加 UpgradeStacks → 升级界面显示 [0/5] 且已满层强化仍会被刷出。
                    if (magePassive != null)
                        magePassive.ApplyUpgrade(kvp.Key);
                    else
                        dotPassive.ApplyUpgrade(kvp.Key);
                }
                DebugHelper.Log($"[GameSceneBootstrap] TestMode: Applied upgrade '{kvp.Key}' x{kvp.Value}");
            }
        }
    }

    private void SpawnDpsTestDummy()
    {
        var gc = Resources.Load<GameConfig>("Configs/GameConfig");

        var dummyObj = new GameObject("DpsBoss");
        dummyObj.transform.position = Vector3.zero;

        // 图形 — 巨大木桩外观
        var sr = dummyObj.AddComponent<SpriteRenderer>();
        sr.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        sr.color = new Color(0.6f, 0.1f, 0.1f);
        sr.sortingOrder = 5;
        dummyObj.transform.localScale = Vector3.one * 8f;

        // 碰撞 — Static 刚体，绝对不可推动
        var col = dummyObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        var rb = dummyObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Static;

        // Damageable — 200万HP
        var dmg = dummyObj.AddComponent<Damageable>();
        int hp = gc != null ? (int)Mathf.Min(gc.dpsDummyHP, int.MaxValue) : 2000000;
        dmg.SetMaxHp(hp);
        dmg.Heal(hp);

        // TrainingDummy 组件
        var dummy = dummyObj.AddComponent<TrainingDummy>();
        dummy.Init(hp,
            gc != null ? gc.dpsDummyRegen : 0f,
            gc != null ? gc.dpsDummyInvincible : false,
            gc != null ? gc.dpsDummyRespawnDelay : 1f);

        // 血条
        var healthBar = dummyObj.AddComponent<EnemyHealthBar>();
        healthBar.Setup(dmg, 8.0f, 0.5f, 3f);

        // DPS 追踪器
        var trackerObj = new GameObject("DpsTracker");
        trackerObj.AddComponent<DpsTracker>();

        // 传送玩家到木桩面前
        var player = GameReferences.Player;
        if (player != null)
            player.transform.position = new Vector3(-8f, 0f, 0f);

        // 名字标签
        var labelObj = new GameObject("BossLabel");
        labelObj.transform.SetParent(dummyObj.transform);
        labelObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var tm = labelObj.AddComponent<TextMesh>();
        tm.text = "DPS TEST";
        tm.characterSize = 0.2f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 50;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.3f, 0.3f);
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (!_selectionDone)
        {
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                _selectionUI.ConfirmSelection();
            }
        }
    }

    /// <summary>
    /// 选择界面 GUI + 调试 GUI
    /// </summary>
    private void OnGUI()
    {
        GUIScaleHelper.BeginScale();

        if (!_selectionDone)
        {
            _selectionUI.DrawSelectionUI();
            GUIScaleHelper.EndScale();
            return;
        }

        _pauseMenuUI.DrawPauseMenu();

        if (_spawnManager != null && _spawnManager.ChallengeSystem != null)
            _spawnManager.ChallengeSystem.DrawChallengeUI();

        _debugOverlay.DrawDebugGUI();

        GUIScaleHelper.EndScale();
    }

    // ── 辅助方法 ──

    private T EnsureManagerExists<T>(string objName) where T : MonoBehaviour
    {
        var existing = FindAnyObjectByType<T>();
        if (existing == null)
        {
            var obj = new GameObject(objName);
            existing = obj.AddComponent<T>();
            DebugHelper.Log($"[GameSceneBootstrap] Created {objName}");
        }
        return existing;
    }

    private void EnsureSFXManager()
    {
        if (SFXManager.Instance == null)
        {
            var sfxObj = new GameObject("SFXManager");
            sfxObj.AddComponent<SFXManager>();
            DebugHelper.Log("[GameSceneBootstrap] Created SFXManager");
        }
    }

    private void EnsureDamageMeter()
    {
        if (DamageMeter.Instance == null)
        {
            gameObject.AddComponent<DamageMeter>();
            DebugHelper.Log("[GameSceneBootstrap] Created DamageMeter");
        }
    }
}