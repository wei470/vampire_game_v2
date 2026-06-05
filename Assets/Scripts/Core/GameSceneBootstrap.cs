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
        // #17 配置 Physics2D 碰撞矩阵
        PhysicsLayerSetup.SetupCollisionMatrix();

        // ── 加载数据 ──
        _dataLoader = new GameDataLoader();
        _dataLoader.LoadSelectionData();

        // ── 确保 SaveManager 存在 ──
        if (FindAnyObjectByType<SaveManager>() == null)
        {
            var saveObj = new GameObject("SaveManager");
            saveObj.AddComponent<SaveManager>();
            DebugHelper.Log("[GameSceneBootstrap] Created SaveManager");
        }

        // 初始状态：暂停游戏，等待选择
        Time.timeScale = 0.0001f;

        // 暂停 SpawnManager
        _spawnManager = FindAnyObjectByType<SpawnManager>();
        if (_spawnManager != null)
            _spawnManager.enabled = false;

        _player = FindAnyObjectByType<PlayerController>();

        // 注入全局引用缓存
        GameReferences.Player = _player;
        GameReferences.SpawnManager = _spawnManager;
        GameReferences.MainCamera = Camera.main;

        // ── 确保管理器单例存在 ──
        EnsureManagerExists<ShopUI>("ShopUI");
        EnsureManagerExists<BGMManager>("BGMManager");
        EnsureSFXManager();
        EnsureDamageMeter();

        // ── 创建 HUD 工厂并生成预游戏 HUD ──
        _hudFactory = new GameHUDFactory(gameObject);
        _hudFactory.CreatePreGameHUD();

        // ── 创建 GameInputHandler ──
        var shopUI = FindAnyObjectByType<ShopUI>();
        var bgmManager = FindAnyObjectByType<BGMManager>();
        _inputHandler = gameObject.AddComponent<GameInputHandler>();
        _inputHandler.Setup(_spawnManager);
        _inputHandler.SetShopUI(shopUI);

        // ── 创建 SelectionUI ──
        _selectionUI = gameObject.AddComponent<SelectionUI>();
        _selectionUI.Setup(_dataLoader.Characters, _dataLoader.Weapons, _dataLoader.Skills);
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
    }

    /// <summary>
    /// 选择完成回调（由 SelectionUI 触发）
    /// </summary>
    private void OnSelectionConfirmed(int selectedChar, int selectedWeapon, int selectedSkill)
    {
        _selectionDone = true;
        _gameStarted = true;
        _gameStarter.ApplySelectionAndStartGame(
            selectedChar, selectedWeapon, selectedSkill,
            _dataLoader.Characters, _dataLoader.Weapons, _dataLoader.Skills,
            _dataLoader.MageUpgradeConfig);
    }

    /// <summary>
    /// Test 模式子弹选择完成回调
    /// </summary>
    private void OnTestBulletSelectionConfirmed(List<string> selectedBulletIds)
    {
        var characters = _dataLoader.Characters;
        var skills = _dataLoader.Skills;

        // 找到 Mage 角色索引
        int mageIndex = 0;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null &&
                (characters[i].characterId == "mage" || characters[i].characterName.ToLower().Contains("mage")))
            {
                mageIndex = i;
                break;
            }
        }

        // 找到 Teleport 技能索引
        int teleportIndex = 0;
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] != null && skills[i].skillName.ToLower().Contains("teleport"))
            {
                teleportIndex = i;
                break;
            }
        }

        // 使用 Mage + 无武器(0) + Teleport 开始游戏
        _gameStarted = true;
        _gameStarter.ApplySelectionAndStartGame(
            mageIndex, 0, teleportIndex,
            characters, _dataLoader.Weapons, skills,
            _dataLoader.MageUpgradeConfig);

        // 清空 MagePassive 默认添加的毒子弹，用用户选择替代
        var magePassive = _player?.GetComponent<MagePassive>();
        if (magePassive != null)
        {
            magePassive.ClearAllDotGuns();
            var config = _dataLoader.MageUpgradeConfig;
            if (config != null && selectedBulletIds.Count > 0)
            {
                foreach (string bulletId in selectedBulletIds)
                {
                    var entry = config.GetDotGunEntry(bulletId);
                    if (entry.HasValue)
                    {
                        var dg = entry.Value;
                        magePassive.UnlockDotGun(dg.effectType, dg.color, dg.cooldown, dg.impactDmg, dg.dotDps, dg.dotDuration);
                        DebugHelper.Log($"[GameSceneBootstrap] TestMode: Added bullet '{dg.displayName}'");
                    }
                }
            }
            else if (selectedBulletIds.Count == 0)
            {
                DebugHelper.Log("[GameSceneBootstrap] TestMode: No bullets selected, entering with empty loadout");
            }
        }

        DebugHelper.Log($"[GameSceneBootstrap] TestMode: Game started with Mage + Teleport, {selectedBulletIds.Count} bullets");
    }

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // 选择阶段：Enter/Space 确认
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

    private void EnsureManagerExists<T>(string objName) where T : MonoBehaviour
    {
        if (FindAnyObjectByType<T>() == null)
        {
            var obj = new GameObject(objName);
            obj.AddComponent<T>();
            DebugHelper.Log($"[GameSceneBootstrap] Created {objName}");
        }
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