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
        // 记录当前角色数据
        if (selectedChar < characters.Length && characters[selectedChar] != null)
            GameSceneBootstrap.CurrentCharacter = characters[selectedChar];

        // 应用角色 + 永久加成
        if (selectedChar < characters.Length && characters[selectedChar] != null)
        {
            var charData = characters[selectedChar];
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

                        DebugHelper.Log($"[GameStarter] Permanent bonuses: HP+{hpBonus}, ARM+{armorBonus}, ATK+{atkBonus}, SPD×{speedMult:F2}");
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
            DebugHelper.Log($"[GameStarter] Character: {charData.characterName}");

            // 初始化角色专属被动进化系统
            InitEvolutionSystem(charData);
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
            bool isMage = GameSceneBootstrap.CurrentCharacter != null &&
                (GameSceneBootstrap.CurrentCharacter.characterId == "mage" ||
                 GameSceneBootstrap.CurrentCharacter.characterName.ToLower().Contains("mage"));
            if (isMage)
            {
                // Mage 不使用武器系统，攻击完全由 MagePassive 的毒子弹DOT枪驱动
                if (wc != null) wc.enabled = false;
                DebugHelper.Log("[GameStarter] Mage: no weapon (DOT gun only)");
            }
            else
            {
                if (wc != null)
                {
                    wc.SetWeapon(weapons[0]); // Bullet
                    wc.SelectionLocked = true;
                }
                DebugHelper.Log("[GameStarter] Weapon: Bullet (默认)");
            }
        }

        // 应用技能
        if (selectedSkill < skills.Length && skills[selectedSkill] != null)
        {
            var skillMgr = _player?.GetComponent<PlayerSkillManager>();
            if (skillMgr != null)
            {
                skillMgr.ClearAllSkills();
                skillMgr.AddSkillByData(skills[selectedSkill]);
            }
            DebugHelper.Log($"[GameStarter] Skill: {skills[selectedSkill].skillName}");
        }

        // 根据角色类型添加专属被动组件
        if (_player != null && GameSceneBootstrap.CurrentCharacter != null)
        {
            // Mage 角色专属：DOT 增强 + 暴击 + 引爆
            if (GameSceneBootstrap.CurrentCharacter.characterId == "mage" ||
                GameSceneBootstrap.CurrentCharacter.characterName.ToLower().Contains("mage"))
            {
                var magePassive = _player.GetComponent<MagePassive>();
                if (magePassive == null)
                {
                    magePassive = _player.gameObject.AddComponent<MagePassive>();
                    DebugHelper.Log("[GameStarter] MagePassive component added");
                }
                // #38 注入升级配置到 MagePassive
                magePassive.SetUpgradeConfig(mageUpgradeConfig);
                DebugHelper.Log("[GameStarter] MageUpgradeConfig injected into MagePassive");
            }
        }

        // 广播选择完成事件
        CharacterData cd = selectedChar < characters.Length ? characters[selectedChar] : null;
        WeaponData wd = selectedWeapon < weapons.Length ? weapons[selectedWeapon] : null;
        SkillData sd = selectedSkill < skills.Length ? skills[selectedSkill] : null;
        EventManager.TriggerSelectionComplete(cd, wd, sd);

        // #10 根据角色类型设置 UI 主题
        bool isMageChar = GameSceneBootstrap.CurrentCharacter != null &&
            (GameSceneBootstrap.CurrentCharacter.characterId == "mage" ||
             GameSceneBootstrap.CurrentCharacter.characterName.ToLower().Contains("mage"));
        UIColorTheme.SetTheme(isMageChar ? "mage" : "default");
        if (isMageChar)
            DebugHelper.Log("[GameStarter] UI Theme: Mage (purple/green DOT theme)");

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