using UnityEngine;

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
        GameReferences.Player = player;
    }

    public void ApplySelectionAndStartGame(
        int selectedChar, int selectedWeapon,
        CharacterData[] characters, WeaponData[] weapons,
        MageUpgradeConfig mageUpgradeConfig)
    {
        CharacterData charData = selectedChar < characters.Length ? characters[selectedChar] : null;
        WeaponData wd = selectedWeapon < weapons.Length ? weapons[selectedWeapon] : null;

        LoadCharacterConfig(charData);
        ApplyUpgrades(charData, weapons, mageUpgradeConfig);
        StartGame(charData, wd);
    }

    private void LoadCharacterConfig(CharacterData charData)
    {
        if (charData == null) return;

        GameSceneBootstrap.CurrentCharacter = charData;

        if (_player != null)
        {
            var playerConfig = Resources.Load<PlayerConfig>("Configs/PlayerConfig");
            if (playerConfig == null)
            {
                playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
                playerConfig.defaultMoveSpeed = 10f;
                playerConfig.characters = new PlayerConfig.CharacterStats[]
                {
                    new PlayerConfig.CharacterStats { characterId = "mage", moveSpeed = 10f }
                };
            }
            string charId = charData != null ? charData.characterId : "";
            _player.MoveSpeed = playerConfig.GetMoveSpeed(charId);
            var dmg = _player.Damageable;
            if (dmg != null)
            {
                int baseHp = charData.maxHP;
                int baseArmor = charData.armor;

                if (SaveManager.Instance != null)
                {
                    float hpBonus = SaveManager.Instance.GetPermanentBonus("max_hp");
                    float armorBonus = SaveManager.Instance.GetPermanentBonus("armor");
                    baseHp += Mathf.RoundToInt(hpBonus);
                    baseArmor += Mathf.RoundToInt(armorBonus);
                }

                dmg.SetMaxHp(baseHp);
                dmg.Heal(baseHp);
                dmg.SetArmor(baseArmor);
            }
            var sr = _player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (charData.icon != null) sr.sprite = charData.icon;
                sr.color = charData.characterColor;
            }
        }

        InitEvolutionSystem(charData);
    }

    private void ApplyUpgrades(CharacterData charData,
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
            }
            else
            {
                if (wc != null)
                {
                    wc.SetWeapon(weapons[0]);
                    wc.SelectionLocked = true;
                }
            }
        }

        if (_player != null && GameSceneBootstrap.CurrentCharacter != null)
        {
            string characterId = GameSceneBootstrap.CurrentCharacter.characterId;
            ICharacterPassive characterPassive = _player.GetComponent<ICharacterPassive>();
            if (characterPassive == null)
                characterPassive = CharacterFactory.Create(characterId, _player.gameObject);

            if (characterPassive is MagePassive magePassive)
                magePassive.SetUpgradeConfig(mageUpgradeConfig);
        }
    }

    private void StartGame(CharacterData cd, WeaponData wd)
    {
        EventManager.TriggerSelectionComplete(cd, wd, null);

        bool isMageChar = GameSceneBootstrap.CurrentCharacter != null &&
            (GameSceneBootstrap.CurrentCharacter.characterId == "mage" ||
             GameSceneBootstrap.CurrentCharacter.characterName.ToLower().Contains("mage"));
        UIColorTheme.SetTheme(isMageChar ? "mage" : "default");

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);

        if (_spawnManager != null && !GameReferences.DpsTestMode && !GameReferences.TestMode)
        {
            _spawnManager.enabled = true;
            _spawnManager.StartFirstWave();
        }

        Time.timeScale = 1f;
        if (BGMManager.Instance != null) BGMManager.Instance.PlayClip("music2");
        _hudFactory.CreateInGameHUD(GameSceneBootstrap.CurrentCharacter);
    }

    private void InitEvolutionSystem(CharacterData charData)
    {
        if (_player == null || charData == null) return;
        if (charData.evolutionTree == null || charData.evolutionTree.Length == 0) return;
        var levelSystem = _player.GetComponent<PlayerLevelSystem>();
        if (levelSystem == null) return;
        var evoSystem = _player.GetComponent<EvolutionSystem>();
        if (evoSystem == null) evoSystem = _player.gameObject.AddComponent<EvolutionSystem>();
        evoSystem.Init(charData, levelSystem);
    }

    private void WarmUpObjectPools()
    {
        if (ObjectPool.Instance == null)
        {
            var poolObj = new GameObject("ObjectPool");
            poolObj.AddComponent<ObjectPool>();
        }

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

        if (_spawnManager != null) _spawnManager.WarmUpAllEnemyPools();
    }
}
