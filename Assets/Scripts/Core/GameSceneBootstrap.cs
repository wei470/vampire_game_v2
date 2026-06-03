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

    /// <summary>
    /// 当前选择的角色数据（静态，供 LevelUpUI 等访问）
    /// </summary>
    public static CharacterData CurrentCharacter { get; private set; }

    /// <summary>
    /// 重置角色选择数据（返回菜单时调用，防止残留）
    /// </summary>
    public static void ResetCharacter()
    {
        CurrentCharacter = null;
    }

    // ── 数据 ──
    private CharacterData[] _characters;
    private WeaponData[] _weapons;
    private SkillData[] _skills;

    // ── #38 Mage 升级配置 ──
    private MageUpgradeConfig _mageUpgradeConfig;

    private void Start()
    {
        // #17 配置 Physics2D 碰撞矩阵（Bullet/Enemy/Player/Pickup/Environment Layer 分离）
        PhysicsLayerSetup.SetupCollisionMatrix();

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

        // 确保 SFXManager 存在
        if (SFXManager.Instance == null)
        {
            var sfxObj = new GameObject("SFXManager");
            sfxObj.AddComponent<SFXManager>();
            DebugHelper.Log("[GameSceneBootstrap] Created SFXManager");
        }

        // 创建 DamageMeter 伤害统计
        if (DamageMeter.Instance == null)
        {
            gameObject.AddComponent<DamageMeter>();
            DebugHelper.Log("[GameSceneBootstrap] Created DamageMeter");
        }

        // #21 创建 BossHealthBarUI（Boss 战专属血条 + 阶段指示器）
        if (BossHealthBarUI.Instance == null)
        {
            var bossBarObj = new GameObject("BossHealthBarUI");
            bossBarObj.AddComponent<BossHealthBarUI>();
            DebugHelper.Log("[GameSceneBootstrap] Created BossHealthBarUI");
        }

        // 创建并初始化 GameInputHandler
        _inputHandler = gameObject.AddComponent<GameInputHandler>();
        _inputHandler.Setup(_spawnManager);
        _inputHandler.SetShopUI(_shopUI);

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
            _selectionUI.SetPreSelection(1, 0, 3);
            _selectionUI.ConfirmSelection();
            // 直接跳到最后一步（需要连续确认2次）
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

        // ── #38 加载 MageUpgradeConfig ──
        _mageUpgradeConfig = LoadAsset<MageUpgradeConfig>("Assets/ScriptableObjects/Config/MageUpgradeConfig.asset");
        if (_mageUpgradeConfig == null)
        {
            // 运行时创建默认配置（与编辑器中的 .asset 一致）
            _mageUpgradeConfig = ScriptableObject.CreateInstance<MageUpgradeConfig>();
            DebugHelper.Log("[GameSceneBootstrap] MageUpgradeConfig: runtime default created");
        }

        // ── 为 Mage 角色运行时注入专属升级和描述 ──
        foreach (var c in _characters)
        {
            if (c != null && (c.characterId == "mage" || c.characterName.ToLower().Contains("mage")))
            {
                // 从配置读取描述和颜色
                c.description = _mageUpgradeConfig.description;
                c.passiveDescription = _mageUpgradeConfig.passiveDescription;
                c.characterColor = _mageUpgradeConfig.characterColor;

                // 使用配置生成升级选项（替代硬编码的 CreateMageUpgrades）
                if (c.customUpgrades == null || c.customUpgrades.Length == 0)
                {
                    c.customUpgrades = _mageUpgradeConfig.BuildCustomUpgrades();
                    c.useGenericUpgrades = false; // Mage 只用专属升级
                    DebugHelper.Log($"[GameSceneBootstrap] Injected {c.customUpgrades.Length} Mage custom upgrades from config");
                }
            }
        }

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
        c.moveSpeed = 10f;
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
    /// 创建 Mage 角色的 16 个专属升级选项（运行时注入）
    /// </summary>
    private CharacterUpgradeOption[] CreateMageUpgrades()
    {
        return new CharacterUpgradeOption[]
        {
            // ═══ 4 种 DOT 子弹（解锁新子弹类型）═══
            MakeUpgrade("bleed", "流血 (Bleed)",
                "🔴 红色子弹 | DPS:3/s | 持续4秒\n移动越快受伤越频繁",
                CharacterUpgradeOption.UpgradeCategory.DotType, 0f, 0f, 0f),
            MakeUpgrade("poison", "中毒 (Poison)",
                "🟢 药瓶爆炸生成毒液池 | 持续5秒\n叠加层数越高伤害越高",
                CharacterUpgradeOption.UpgradeCategory.DotType, 0f, 0f, 0f),
            MakeUpgrade("burn", "燃烧 (Burn)",
                "🟠 快速橙色子弹 | DPS:2/s | 持续3秒\n叠加层数加速燃烧频率",
                CharacterUpgradeOption.UpgradeCategory.DotType, 0f, 0f, 0f),
            MakeUpgrade("frostbite", "霜冻 (Frostbite)",
                "🔵 快速冰霜子弹 | 冰冻1秒\n永久减速30% + 每2秒霜伤",
                CharacterUpgradeOption.UpgradeCategory.DotType, 0f, 0f, 0f),

            // ═══ DOT 增强（4 种）═══
            MakeUpgrade("corrosion", "腐蚀 (Corrosion)",
                "破甲：DOT敌人护甲-10%\n可无限叠加，越打越疼",
                CharacterUpgradeOption.UpgradeCategory.ArmorReduction, 0.10f, 0f, 0f),
            MakeUpgrade("curse", "诅咒 (Curse)",
                "传染：DOT敌人死亡时\n扩散所有DOT给附近1个敌人\n每层+1目标",
                CharacterUpgradeOption.UpgradeCategory.DotSpread, 1f, 0f, 0f),
            MakeUpgrade("agony", "痛苦 (Agony)",
                "频率：DOT触发间隔-10%\n可无限叠加，总伤不变但节奏更快",
                CharacterUpgradeOption.UpgradeCategory.DotFrequency, 0.10f, 0f, 0f),
            MakeUpgrade("wither", "凋零 (Wither)",
                "暴击：DOT生效时10%几率双倍伤害\n超过100%后暴击倍率+100%",
                CharacterUpgradeOption.UpgradeCategory.DotCritBurst, 0.10f, 0f, 0f),

            // ═══ 引爆增强（2 种）═══
            MakeUpgrade("radiate", "辐射 (Radiation)",
                "引爆伤害 +30%，可无限叠加",
                CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier, 0.30f, 0f, 0f),
            MakeUpgrade("contaminate", "污染 (Contaminate)",
                "引爆冷却 -30%，可无限叠加",
                CharacterUpgradeOption.UpgradeCategory.DetonateAbility, 0.30f, 0f, 0f),

            // ═══ DOT 时间增强（1 种）═══
            MakeUpgrade("erosion", "侵蚀 (Erosion)",
                "DOT每生效5次额外冲击\n造成单跳总伤50%瞬间伤害\n每层触发次数-1（最低2次）",
                CharacterUpgradeOption.UpgradeCategory.DotTrigger, 1f, 0.50f, 0f),

            // ═══ 子弹增强（3 种，共振已删除）═══
            MakeUpgrade("haste", "急速 (Haste)",
                "攻速+15% 子弹速度+10%\n速度超100%获得穿透+1",
                CharacterUpgradeOption.UpgradeCategory.AttackSpeed, 0.15f, 0.10f, 0f),
            MakeUpgrade("barrage", "弹幕 (Barrage)",
                "子弹数量+1\n超过5发自动转为追踪弹",
                CharacterUpgradeOption.UpgradeCategory.BulletCount, 1f, 0f, 0f),
            MakeUpgrade("ricochet", "反弹 (Ricochet)",
                "子弹30%几率反弹\n超100%增加反弹次数并移除衰减",
                CharacterUpgradeOption.UpgradeCategory.Ricochet, 0.30f, 0f, 0f),
        };
    }

    /// <summary>
    /// 工具方法：创建一个 CharacterUpgradeOption
    /// </summary>
    private CharacterUpgradeOption MakeUpgrade(string id, string name, string desc,
        CharacterUpgradeOption.UpgradeCategory category, float v1, float v2, float v3)
    {
        return new CharacterUpgradeOption
        {
            upgradeId = id,
            upgradeName = name,
            description = desc,
            category = category,
            value1 = v1,
            value2 = v2,
            value3 = v3,
            maxStacks = 0 // 无限叠加
        };
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

        // 记录当前角色数据
        if (selectedChar < _characters.Length && _characters[selectedChar] != null)
            CurrentCharacter = _characters[selectedChar];

        // 应用角色 + 永久加成
        if (selectedChar < _characters.Length && _characters[selectedChar] != null)
        {
            var charData = _characters[selectedChar];
            if (_player != null)
            {
                _player.MoveSpeed = 20f; // 固定高速移动
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
                        // 不再乘以speedMult，避免永久升级拉低速度

                        DebugHelper.Log($"[GameSceneBootstrap] Permanent bonuses: HP+{hpBonus}, ARM+{armorBonus}, ATK+{atkBonus}, SPD×{speedMult:F2}");
                    }

                    dmg.SetMaxHp(baseHp);
                    dmg.Heal(baseHp);
                    dmg.SetArmor(baseArmor);
                }
                var sr = _player.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // 绑定选择界面的角色 Sprite 到游戏内 Player
                    if (charData.icon != null)
                        sr.sprite = charData.icon;
                    sr.color = charData.characterColor;
                }
            }
            DebugHelper.Log($"[GameSceneBootstrap] Character: {charData.characterName}");
        }

        // 初始化玩家永久加成（HP 回复等）
        if (_player != null) _player.InitPermanentBonuses();

        // 预热对象池（敌人 + 掉落物）
        WarmUpObjectPools();

        // 创建地图边界
        MapBoundary.Create(50f);

        // 应用武器（所有人自带默认子弹，Mage不用武器只用DOT枪）
        {
            var wc = _player?.GetComponent<WeaponController>();
            bool isMage = CurrentCharacter != null &&
                (CurrentCharacter.characterId == "mage" || CurrentCharacter.characterName.ToLower().Contains("mage"));
            if (isMage)
            {
                // Mage 不使用武器系统，攻击完全由 MagePassive 的毒子弹DOT枪驱动
                if (wc != null) wc.enabled = false;
                DebugHelper.Log("[GameSceneBootstrap] Mage: no weapon (DOT gun only)");
            }
            else
            {
                if (wc != null)
                {
                    wc.SetWeapon(_weapons[0]); // Bullet
                    wc.SelectionLocked = true;
                }
                DebugHelper.Log("[GameSceneBootstrap] Weapon: Bullet (默认)");
            }
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

        // 根据角色类型添加专属被动组件
        if (_player != null && CurrentCharacter != null)
        {
            // Mage 角色专属：DOT 增强 + 暴击 + 引爆
            if (CurrentCharacter.characterId == "mage" || CurrentCharacter.characterName.ToLower().Contains("mage"))
            {
                var magePassive = _player.GetComponent<MagePassive>();
                if (magePassive == null)
                {
                    magePassive = _player.gameObject.AddComponent<MagePassive>();
                    DebugHelper.Log("[GameSceneBootstrap] MagePassive component added");
                }
                // #38 注入升级配置到 MagePassive
                magePassive.SetUpgradeConfig(_mageUpgradeConfig);
                DebugHelper.Log("[GameSceneBootstrap] MageUpgradeConfig injected into MagePassive");
            }
        }

        // 广播选择完成事件
        CharacterData cd = selectedChar < _characters.Length ? _characters[selectedChar] : null;
        WeaponData wd = selectedWeapon < _weapons.Length ? _weapons[selectedWeapon] : null;
        SkillData sd = selectedSkill < _skills.Length ? _skills[selectedSkill] : null;
        EventManager.TriggerSelectionComplete(cd, wd, sd);

        // #10 根据角色类型设置 UI 主题
        bool isMageChar = CurrentCharacter != null &&
            (CurrentCharacter.characterId == "mage" || CurrentCharacter.characterName.ToLower().Contains("mage"));
        UIColorTheme.SetTheme(isMageChar ? "mage" : "default");
        if (isMageChar)
            DebugHelper.Log("[GameSceneBootstrap] UI Theme: Mage (purple/green DOT theme)");

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

        // ── 创建游戏内 HUD 组件 ──
        // 左上角 - 玩家血量条
        if (gameObject.GetComponent<PlayerHealthBarHUD>() == null)
            gameObject.AddComponent<PlayerHealthBarHUD>();

        // 中上方 - Boss 血量条
        if (gameObject.GetComponent<BossHealthBarHUD>() == null)
            gameObject.AddComponent<BossHealthBarHUD>();

        // 左下角 - 技能冷却与操作提示
        if (gameObject.GetComponent<SkillHUD>() == null)
            gameObject.AddComponent<SkillHUD>();

        // 右下角 - Mage引爆冷却显示（已移除，不显示）
        // if (gameObject.GetComponent<DetonateHUD>() == null)
        //     gameObject.AddComponent<DetonateHUD>();

        // #1 Mage 专属 HUD 统计面板（仅 Mage 角色显示）
        if (CurrentCharacter != null &&
            (CurrentCharacter.characterId == "mage" || CurrentCharacter.characterName.ToLower().Contains("mage")))
        {
            if (gameObject.GetComponent<MageStatsHUD>() == null)
            {
                gameObject.AddComponent<MageStatsHUD>();
                DebugHelper.Log("[GameSceneBootstrap] Created MageStatsHUD for Mage character");
            }
        }

        // #22 敌人生成预警 UI
        if (SpawnWarningUI.Instance == null)
        {
            var warnObj = new GameObject("SpawnWarningUI");
            warnObj.AddComponent<SpawnWarningUI>();
            DebugHelper.Log("[GameSceneBootstrap] Created SpawnWarningUI");
        }

        // #25 波次间歇期统计 UI
        if (WaveIntermissionUI.Instance == null)
        {
            var intermissionObj = new GameObject("WaveIntermissionUI");
            intermissionObj.AddComponent<WaveIntermissionUI>();
            DebugHelper.Log("[GameSceneBootstrap] Created WaveIntermissionUI");
        }

        // #30 快捷键提示 HUD
        if (KeyHintHUD.Instance == null)
        {
            var keyHintObj = new GameObject("KeyHintHUD");
            var keyHint = keyHintObj.AddComponent<KeyHintHUD>();
            // Mage 角色使用专属键位（含 E 引爆）
            if (CurrentCharacter != null &&
                (CurrentCharacter.characterId == "mage" || CurrentCharacter.characterName.ToLower().Contains("mage")))
            {
                keyHint.SetMageKeys();
            }
            DebugHelper.Log("[GameSceneBootstrap] Created KeyHintHUD");
        }

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
            go.layer = PhysicsLayerSetup.LAYER_PICKUP; // #17 Pickup Layer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle;
            sr.color = new Color(0.1f, 0.9f, 0.2f);
            go.transform.localScale = Vector3.one * 0.6f;
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
            go.layer = PhysicsLayerSetup.LAYER_PICKUP; // #17 Pickup Layer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle;
            sr.color = new Color(1f, 0.85f, 0f);
            go.transform.localScale = Vector3.one * 0.5f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;
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
    /// 所有子组件共享 GUIScaleHelper 缩放，以 1920×1080 为参考分辨率。
    /// </summary>
    private void OnGUI()
    {
        GUIScaleHelper.BeginScale();

        // ── 选择界面（全屏）──
        if (!_selectionDone)
        {
            _selectionUI.DrawSelectionUI();
            GUIScaleHelper.EndScale();
            return;
        }

        // ── 暂停菜单 ──
        _pauseMenuUI.DrawPauseMenu();

        // ── 调试面板 ──
        _debugOverlay.DrawDebugGUI();

        GUIScaleHelper.EndScale();
    }
}