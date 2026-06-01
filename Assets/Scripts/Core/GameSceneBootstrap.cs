#pragma warning disable CS0414
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 游戏主场景运行时引导器 — 在游戏场景启动时自动完成以下工作：
/// 
/// 1. 加载角色/武器/技能数据
/// 2. 通过 SelectionUI 组件显示选择界面
/// 3. 选择完成后应用配置并开始游戏
/// 4. 通过 DebugOverlay 组件显示调试信息
/// 
/// 职责已拆分：
/// - SelectionUI: 选择界面渲染和交互
/// - DebugOverlay: 调试面板渲染
/// - GameSceneBootstrap: 数据加载、游戏启动、组件协调
/// 
/// 使用方式：挂载到场景中的空 GameObject 上
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    private PlayerController _player;
    private SpawnManager _spawnManager;
    private ShopUI _shopUI;
    private BGMManager _bgmManager;
    private GameInputHandler _inputHandler;
    private SelectionUI _selectionUI;
    private DebugOverlay _debugOverlay;
    private PauseMenuUI _pauseMenuUI;

    private bool _selectionDone = false;
    private bool _gameStarted = false;

    // ── 数据 ──
    private CharacterData[] _characters;
    private WeaponData[] _weapons;
    private SkillData[] _skills;

    private void Start()
    {
        // 加载数据
        LoadSelectionData();

        // 确保 SaveManager 存在
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

        // 确保 ShopUI 存在
        _shopUI = FindAnyObjectByType<ShopUI>();
        if (_shopUI == null)
        {
            var shopObj = new GameObject("ShopUI");
            _shopUI = shopObj.AddComponent<ShopUI>();
            DebugHelper.Log("[GameSceneBootstrap] Created ShopUI");
        }

        // 确保 BGMManager 存在
        _bgmManager = FindAnyObjectByType<BGMManager>();
        if (_bgmManager == null)
        {
            var bgmObj = new GameObject("BGMManager");
            _bgmManager = bgmObj.AddComponent<BGMManager>();
            DebugHelper.Log("[GameSceneBootstrap] Created BGMManager");
        }

        // 创建并初始化 GameInputHandler
        _inputHandler = gameObject.AddComponent<GameInputHandler>();
        _inputHandler.Setup(_spawnManager);
        _inputHandler.SetShopUI(_shopUI);

        // 注入 WeaponController 到 InputHandler（武器切换输入迁移）
        var wc = _player?.GetComponent<WeaponController>();
        if (wc != null)
            _inputHandler.SetWeaponController(wc);

        // 创建 SelectionUI 组件
        _selectionUI = gameObject.AddComponent<SelectionUI>();
        _selectionUI.Setup(_characters, _weapons, _skills);
        _selectionUI.OnSelectionConfirmed = OnSelectionConfirmed;

        // 创建 DebugOverlay 组件
        _debugOverlay = gameObject.AddComponent<DebugOverlay>();
        _debugOverlay.Setup(_player, _spawnManager);

        // 创建 PauseMenuUI 组件
        _pauseMenuUI = gameObject.AddComponent<PauseMenuUI>();
        _inputHandler.SetPauseMenuUI(_pauseMenuUI);

        DebugHelper.Log("[GameSceneBootstrap] Showing selection flow...");

        // 测试模式：跳过选择，直接用默认配置开始
        if (GameReferences.TestMode)
        {
            _selectionUI.SetPreSelection(0, 1, 3);
            _selectionUI.ConfirmSelection();
            // 直接跳到最后一步（需要连续确认3次）
            _selectionUI.ConfirmSelection();
            _selectionUI.ConfirmSelection();
        }
    }

    /// <summary>
    /// 加载角色/武器/技能数据
    /// </summary>
    private void LoadSelectionData()
    {
        // ── 加载角色（从 ScriptableObjects/Characters/*.asset）──
        var charNames = new string[] {
            "Char_warrior", "Char_mage", "Char_ranger", "Char_vampire",
            "Char_assassin", "Char_paladin", "Char_necromancer", "Char_berserker"
        };
        var charList = new List<CharacterData>();
        foreach (var name in charNames)
        {
            var c = LoadAsset<CharacterData>($"Assets/ScriptableObjects/Characters/{name}.asset");
            if (c != null) charList.Add(c);
        }
        _characters = charList.Count > 0 ? charList.ToArray() : new CharacterData[0];

        // ── 加载武器（运行时创建，因为 Weapons 目录为空）──
        _weapons = CreateDefaultWeapons();

        // ── 加载技能（从 ScriptableObjects/Skills/*.asset）──
        var skillNames = new string[] {
            "Skill_WindWave", "Skill_Berserk", "Skill_TheWorld", "Skill_Teleport",
            "Skill_DeathAura", "Skill_LightningStorm", "Skill_GravityWell", "Skill_FrostNova"
        };
        var skillList = new List<SkillData>();
        foreach (var name in skillNames)
        {
            var s = LoadAsset<SkillData>($"Assets/ScriptableObjects/Skills/{name}.asset");
            if (s != null) skillList.Add(s);
        }
        _skills = skillList.Count > 0 ? skillList.ToArray() : new SkillData[0];

        // ── 确保至少有选项 ──
        if (_characters.Length == 0) _characters = new CharacterData[] { CreateDefaultCharacter() };
        if (_weapons.Length == 0) _weapons = CreateDefaultWeapons();
        if (_skills.Length == 0) _skills = new SkillData[] { CreateDefaultSkill() };

        DebugHelper.Log($"[GameSceneBootstrap] Loaded: {_characters.Length} characters, {_weapons.Length} weapons, {_skills.Length} skills");
    }

    private T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
        return null;
#endif
    }

    /// <summary>
    /// 创建默认武器数据（运行时）
    /// </summary>
    private WeaponData[] CreateDefaultWeapons()
    {
        var weapons = new WeaponData[8];

        weapons[0] = CreateWeapon("Bullet", "Rapid fire bullets", WeaponData.ProjectileType.Bullet,
            damage: 15, cooldown: 0.4f, speed: 14f, pierce: 1, color: new Color(0.3f, 0.8f, 1f));

        weapons[1] = CreateWeapon("Lightning", "Chain lightning that jumps between enemies", WeaponData.ProjectileType.ChainLightning,
            damage: 25, cooldown: 1.2f, speed: 0f, pierce: 1, color: new Color(0.5f, 0.5f, 1f));

        weapons[2] = CreateWeapon("Shockwave", "Expanding ring of damage", WeaponData.ProjectileType.Shockwave,
            damage: 20, cooldown: 1.5f, speed: 8f, pierce: 99, color: new Color(1f, 0.7f, 0.3f));

        weapons[3] = CreateWeapon("Homing Missile", "Tracking missiles", WeaponData.ProjectileType.HomingMissile,
            damage: 30, cooldown: 0.8f, speed: 10f, pierce: 1, color: new Color(1f, 0.4f, 0.4f));

        weapons[4] = CreateWeapon("Mine Trap", "Explosive mines on the ground", WeaponData.ProjectileType.MineTrap,
            damage: 40, cooldown: 2f, speed: 0f, pierce: 99, color: new Color(0.8f, 0.2f, 0.2f));

        weapons[5] = CreateWeapon("Flamethrower", "Continuous fire stream", WeaponData.ProjectileType.Flamethrower,
            damage: 8, cooldown: 0.1f, speed: 10f, pierce: 3, color: new Color(1f, 0.5f, 0f));

        weapons[6] = CreateWeapon("Frost Orb", "Slow and damage enemies in area", WeaponData.ProjectileType.FrostOrb,
            damage: 15, cooldown: 1.8f, speed: 6f, pierce: 99, color: new Color(0.5f, 0.8f, 1f));

        weapons[7] = CreateWeapon("Venom Dart", "Poison darts with DOT", WeaponData.ProjectileType.VenomDart,
            damage: 12, cooldown: 0.6f, speed: 12f, pierce: 2, color: new Color(0.3f, 0.8f, 0.3f));

        return weapons;
    }

    private WeaponData CreateWeapon(string name, string desc, WeaponData.ProjectileType type,
        int damage, float cooldown, float speed, int pierce, Color color)
    {
        var w = ScriptableObject.CreateInstance<WeaponData>();
        w.weaponName = name;
        w.description = desc;
        w.projectileType = type;
        w.baseDamage = damage;
        w.cooldown = cooldown;
        w.projectileSpeed = speed;
        w.pierce = pierce;
        w.projectileColor = color;
        return w;
    }

    private CharacterData CreateDefaultCharacter()
    {
        var c = ScriptableObject.CreateInstance<CharacterData>();
        c.characterName = "Default";
        c.description = "Default character";
        c.maxHP = 100;
        c.moveSpeed = 5f;
        c.armor = 0;
        c.attackDamage = 10;
        c.characterColor = Color.blue;
        return c;
    }

    private SkillData CreateDefaultSkill()
    {
        var s = ScriptableObject.CreateInstance<SkillData>();
        s.skillName = "Default Skill";
        s.description = "Default skill";
        s.baseDamage = 10;
        s.cooldown = 10f;
        return s;
    }

    /// <summary>
    /// 选择完成回调（由 SelectionUI 触发）
    /// </summary>
    private void OnSelectionConfirmed(int selectedChar, int selectedWeapon, int selectedSkill)
    {
        _selectionDone = true;
        ApplySelectionAndStartGame(selectedChar, selectedWeapon, selectedSkill);
    }

    /// <summary>
    /// 完成选择，应用选择并开始游戏
    /// </summary>
    private void ApplySelectionAndStartGame(int selectedChar, int selectedWeapon, int selectedSkill)
    {
        _gameStarted = true;

        // 应用角色 + 永久加成
        if (selectedChar < _characters.Length && _characters[selectedChar] != null)
        {
            var charData = _characters[selectedChar];
            if (_player != null)
            {
                _player.MoveSpeed = charData.moveSpeed;
                var dmg = _player.Damageable;
                if (dmg != null)
                {
                    int baseHp = charData.maxHP;
                    int baseArmor = charData.armor;
                    int baseAtk = charData.attackDamage;

                    // 应用永久升级加成
                    if (SaveManager.Instance != null)
                    {
                        float hpBonus = SaveManager.Instance.GetPermanentBonus("max_hp");
                        float armorBonus = SaveManager.Instance.GetPermanentBonus("armor");
                        float atkBonus = SaveManager.Instance.GetPermanentBonus("attack_damage");
                        float speedMult = SaveManager.Instance.GetPermanentMultiplier("speed");

                        baseHp += Mathf.RoundToInt(hpBonus);
                        baseArmor += Mathf.RoundToInt(armorBonus);
                        baseAtk += Mathf.RoundToInt(atkBonus);
                        _player.MoveSpeed *= speedMult;

                        DebugHelper.Log($"[GameSceneBootstrap] Permanent bonuses: HP+{hpBonus}, ARM+{armorBonus}, ATK+{atkBonus}, SPD×{speedMult:F2}");
                    }

                    dmg.SetMaxHp(baseHp);
                    dmg.Heal(baseHp);
                    dmg.SetArmor(baseArmor);
                }
                var sr = _player.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = charData.characterColor;
            }
            DebugHelper.Log($"[GameSceneBootstrap] Character: {charData.characterName}");
        }

        // 初始化玩家永久加成（HP 回复等）
        if (_player != null) _player.InitPermanentBonuses();

        // 预热对象池（敌人 + 掉落物）
        WarmUpObjectPools();

        // 创建地图边界
        MapBoundary.Create(50f);

        // 应用武器
        if (selectedWeapon < _weapons.Length && _weapons[selectedWeapon] != null)
        {
            var wc = _player?.GetComponent<WeaponController>();
            if (wc != null)
            {
                wc.SetWeapon(_weapons[selectedWeapon]);
                wc.SelectionLocked = true;
            }
            DebugHelper.Log($"[GameSceneBootstrap] Weapon: {_weapons[selectedWeapon].weaponName}");
        }

        // 应用技能
        if (selectedSkill < _skills.Length && _skills[selectedSkill] != null)
        {
            var skillMgr = _player?.GetComponent<PlayerSkillManager>();
            if (skillMgr != null)
            {
                skillMgr.ClearAllSkills();
                skillMgr.AddSkillByData(_skills[selectedSkill]);
            }
            DebugHelper.Log($"[GameSceneBootstrap] Skill: {_skills[selectedSkill].skillName}");
        }

        // 广播选择完成事件
        CharacterData cd = selectedChar < _characters.Length ? _characters[selectedChar] : null;
        WeaponData wd = selectedWeapon < _weapons.Length ? _weapons[selectedWeapon] : null;
        SkillData sd = selectedSkill < _skills.Length ? _skills[selectedSkill] : null;
        EventManager.TriggerSelectionComplete(cd, wd, sd);

        // 设置游戏状态
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);

        // 启用 SpawnManager 并开始第 1 波
        if (_spawnManager != null)
        {
            _spawnManager.enabled = true;
            _spawnManager.StartFirstWave();
        }

        // 恢复游戏
        Time.timeScale = 1f;

        // 开始播放 BGM
        if (_bgmManager != null)
            _bgmManager.Play();

        DebugHelper.Log("[GameSceneBootstrap] Game started! WASD=Move, Mouse=Aim/Shoot, E=Skill, R=Restart");
    }

    /// <summary>
    /// 预热对象池 — 减少运行时 Instantiate/Destroy 的 GC 开销
    /// </summary>
    private void WarmUpObjectPools()
    {
        // 确保 ObjectPool 单例存在
        if (ObjectPool.Instance == null)
        {
            var poolObj = new GameObject("ObjectPool");
            poolObj.AddComponent<ObjectPool>();
        }

        // 预热掉落物池（代码创建的虚拟对象）
        PoolHelper.RegisterVirtualPrefab(PoolHelper.XP_GEM, () =>
        {
            var go = new GameObject("XPGem_Template");
            go.tag = "Untagged";
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square;
            sr.color = new Color(0.5f, 1f, 0.5f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.4f, 0.4f);
            go.AddComponent<XPGem>();
            return go;
        }, 30);

        PoolHelper.RegisterVirtualPrefab(PoolHelper.COIN, () =>
        {
            var go = new GameObject("Coin_Template");
            go.tag = "Untagged";
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square;
            sr.color = new Color(1f, 0.85f, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.3f, 0.3f);
            go.AddComponent<Coin>();
            return go;
        }, 30);

        // 如果 SpawnManager 有预制体，预热敌人池（全部 14 种）
        if (_spawnManager != null)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var t = _spawnManager.GetType();
            GameObject GetPrefab(string name) => t.GetField(name, flags)?.GetValue(_spawnManager) as GameObject;

            var basicPrefab = GetPrefab("_basicEnemyPrefab");
            var rangedPrefab = GetPrefab("_rangedEnemyPrefab");
            var tankPrefab = GetPrefab("_tankEnemyPrefab");
            var fastPrefab = GetPrefab("_fastEnemyPrefab");
            var throwerPrefab = GetPrefab("_throwerEnemyPrefab");
            var healerPrefab = GetPrefab("_healerEnemyPrefab");
            var enhancerPrefab = GetPrefab("_enhancerEnemyPrefab");
            var splitterPrefab = GetPrefab("_splitterEnemyPrefab");
            var summonerPrefab = GetPrefab("_summonerEnemyPrefab");
            var chargerPrefab = GetPrefab("_chargerEnemyPrefab");
            var shielderPrefab = GetPrefab("_shielderEnemyPrefab");
            var stealthPrefab = GetPrefab("_stealthEnemyPrefab");
            var burstPrefab = GetPrefab("_burstEnemyPrefab");
            var chainHealerPrefab = GetPrefab("_chainHealerEnemyPrefab");

            if (basicPrefab != null || rangedPrefab != null || tankPrefab != null || fastPrefab != null ||
                throwerPrefab != null || healerPrefab != null || enhancerPrefab != null || splitterPrefab != null ||
                summonerPrefab != null || chargerPrefab != null || shielderPrefab != null || stealthPrefab != null ||
                burstPrefab != null || chainHealerPrefab != null)
            {
                PoolHelper.WarmUpEnemyPools(
                    basicPrefab, rangedPrefab, tankPrefab, fastPrefab,
                    throwerPrefab, healerPrefab, enhancerPrefab, splitterPrefab,
                    summonerPrefab, chargerPrefab, shielderPrefab, stealthPrefab,
                    burstPrefab, chainHealerPrefab);
            }
        }

        DebugHelper.Log("[GameSceneBootstrap] Object pools warmed up");
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
        // 其他输入已迁移到 GameInputHandler
    }

    /// <summary>
    /// 选择界面 GUI + 调试 GUI（委托给拆分的组件）
    /// </summary>
    private void OnGUI()
    {
        // ── 选择界面（全屏）──
        if (!_selectionDone)
        {
            _selectionUI.DrawSelectionUI();
            return;
        }

        // ── 暂停菜单 ──
        _pauseMenuUI.DrawPauseMenu();

        // ── 调试面板 ──
        _debugOverlay.DrawDebugGUI();
    }
}