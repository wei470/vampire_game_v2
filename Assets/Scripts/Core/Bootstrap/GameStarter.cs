using UnityEngine;

/// <summary>
/// 游戏启动器 — 负责应用选择配置、预热对象池、开始游戏
/// 从 GameSceneBootstrap 拆分而来，职责单一：游戏启动逻辑
/// </summary>
public class GameStarter
{
    private readonly PlayerController _player;
    private readonly SpawnManager _spawnManager;
    private readonly BGMManager _bgmManager;
    private readonly GameHUDFactory _hudFactory;

    public GameStarter(PlayerController player, SpawnManager spawnManager,
        BGMManager bgmManager, GameHUDFactory hudFactory)
    {
        _player = player;
        _spawnManager = spawnManager;
        _bgmManager = bgmManager;
        _hudFactory = hudFactory;
    }

    /// <summary>
    /// 应用选择配置并开始游戏
    /// </summary>
    public void ApplySelectionAndStartGame(
        int selectedChar, int selectedWeapon, int selectedSkill,
        CharacterData[] characters, WeaponData[] weapons, SkillData[] skills,
        MageUpgradeConfig mageUpgradeConfig)
    {
        CharacterData charData = selectedChar < characters.Length ? characters[selectedChar] : null;
        CharacterData cd = charData;
        WeaponData wd = selectedWeapon < weapons.Length ? weapons[selectedWeapon] : null;
        SkillData sd = selectedSkill < skills.Length ? skills[selectedSkill] : null;

        LoadCharacterConfig(charData);
        ApplyUpgrades(charData, sd, weapons, mageUpgradeConfig);
        StartGame(cd, wd, sd);
    }

    /// <summary>
    /// 加载角色配置：记录角色数据、应用属性与永久加成、初始化进化系统
    /// </summary>
    private void LoadCharacterConfig(CharacterData charData)
    {
        if (charData == null) return;

        GameSceneBootstrap.CurrentCharacter = charData;

        if (_player != null)
        {
            _player.MoveSpeed = 20f;
            var dmg = _player.Damageable;
            if (dmg != null)
            {
                int baseHp = charData.maxHP;
                int baseArmor = charData.armor;
                int baseAtk = charData.attackDamage;

                if (SaveManager.Instance != null)
                {
                    float hpBonus = SaveManager.Instance.GetPermanentBonus("max_hp");
                    float armorBonus = SaveManager.Instance.GetPermanentBonus("armor");
                    float atkBonus = SaveManager.Instance.GetPermanentBonus("attack_damage");
                    float speedMult = SaveManager.Instance.GetPermanentMultiplier("speed");

                    baseHp += Mathf.RoundToInt(hpBonus);
                    baseArmor += Mathf.RoundToInt(armorBonus);
                    baseAtk += Mathf.RoundToInt(atkBonus);

                    DebugHelper.Log($"[GameStarter] Permanent bonuses: HP+{hpBonus}, ARM+{armorBonus}, ATK+{atkBonus}, SPD×{speedMult:F2}");
                }

                dmg.SetMaxHp(baseHp);
                dmg.Heal(baseHp);
                dmg.SetArmor(baseArmor);
            }
            var sr = _player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (charData.icon != null)
                    sr.sprite = charData.icon;
                sr.color = charData.characterColor;
            }
        }
        DebugHelper.Log($"[GameStarter] Character: {charData.characterName}");

        InitEvolutionSystem(charData);
    }

    /// <summary>
    /// 应用起始升级：永久加成、对象池预热、武器、技能、角色专属被动组件
    /// </summary>
    private void ApplyUpgrades(CharacterData charData, SkillData skill,
        WeaponData[] weapons, MageUpgradeConfig mageUpgradeConfig)
    {
        if (_player != null) _player.InitPermanentBonuses();

        WarmUpObjectPools();

        MapBoundary.Create(50f);

        {
            var wc = _player?.GetComponent<WeaponController>();
            bool isMage = GameSceneBootstrap.CurrentCharacter != null &&
                (GameSceneBootstrap.CurrentCharacter.characterId == "mage" ||
                 GameSceneBootstrap.CurrentCharacter.characterName.ToLower().Contains("mage"));
            if (isMage)
            {
                if (wc != null) wc.enabled = false;
                DebugHelper.Log("[GameStarter] Mage: no weapon (DOT gun only)");
            }
            else
            {
                if (wc != null)
                {
                    wc.SetWeapon(weapons[0]);
                    wc.SelectionLocked = true;
                }
                DebugHelper.Log("[GameStarter] Weapon: Bullet (默认)");
            }
        }

        if (skill != null)
        {
            var skillMgr = _player?.GetComponent<PlayerSkillManager>();
            if (skillMgr != null)
            {
                skillMgr.ClearAllSkills();
                skillMgr.AddSkillByData(skill);
            }
            DebugHelper.Log($"[GameStarter] Skill: {skill.skillName}");
        }

        if (_player != null && GameSceneBootstrap.CurrentCharacter != null)
        {
            string characterId = GameSceneBootstrap.CurrentCharacter.characterId;

            ICharacterPassive characterPassive = _player.GetComponent<ICharacterPassive>();
            if (characterPassive == null)
            {
                characterPassive = CharacterFactory.Create(characterId, _player.gameObject);
                DebugHelper.Log($"[GameStarter] {characterId} passive component added via CharacterFactory");
            }

            if (characterPassive is MagePassive magePassive)
            {
                magePassive.SetUpgradeConfig(mageUpgradeConfig);
                DebugHelper.Log("[GameStarter] MageUpgradeConfig injected into MagePassive");
            }
        }
    }

    /// <summary>
    /// 开始游戏：广播事件、设置UI主题、切换状态、启动生成、播放BGM、创建HUD
    /// </summary>
    private void StartGame(CharacterData cd, WeaponData wd, SkillData sd)
    {
        EventManager.TriggerSelectionComplete(cd, wd, sd);

        bool isMageChar = GameSceneBootstrap.CurrentCharacter != null &&
            (GameSceneBootstrap.CurrentCharacter.characterId == "mage" ||
             GameSceneBootstrap.CurrentCharacter.characterName.ToLower().Contains("mage"));
        UIColorTheme.SetTheme(isMageChar ? "mage" : "default");
        if (isMageChar)
            DebugHelper.Log("[GameStarter] UI Theme: Mage (purple/green DOT theme)");

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);

        if (_spawnManager != null)
        {
            _spawnManager.enabled = true;
            _spawnManager.StartFirstWave();
        }

        Time.timeScale = 1f;

        if (_bgmManager != null)
            _bgmManager.Play();

        _hudFactory.CreateInGameHUD(GameSceneBootstrap.CurrentCharacter);

        DebugHelper.Log("[GameStarter] Game started! WASD=Move, Mouse=Aim/Shoot, E=Skill, R=Restart");
    }

    /// <summary>
    /// 初始化角色专属被动进化系统
    /// </summary>
    private void InitEvolutionSystem(CharacterData charData)
    {
        if (_player == null || charData == null) return;
        if (charData.evolutionTree == null || charData.evolutionTree.Length == 0) return;

        var levelSystem = _player.GetComponent<PlayerLevelSystem>();
        if (levelSystem == null) return;

        var evoSystem = _player.GetComponent<EvolutionSystem>();
        if (evoSystem == null)
            evoSystem = _player.gameObject.AddComponent<EvolutionSystem>();

        evoSystem.Init(charData, levelSystem);
        DebugHelper.Log($"[GameStarter] EvolutionSystem initialized: {charData.evolutionTree.Length} milestones for {charData.characterName}");
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
            go.layer = PhysicsLayerSetup.LAYER_PICKUP;
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
            go.layer = PhysicsLayerSetup.LAYER_PICKUP;
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

        // 如果 SpawnManager 存在，预热敌人池（全部 14 种）
        if (_spawnManager != null)
        {
            _spawnManager.WarmUpAllEnemyPools();
        }

        DebugHelper.Log("[GameStarter] Object pools warmed up");
    }
}