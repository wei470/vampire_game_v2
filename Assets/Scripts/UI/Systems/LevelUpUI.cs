using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 升级选择界面 — 协调 UI 显示和升级应用。
/// 选项生成逻辑委托给 LevelUpOptionGenerator。
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _titleText;
    [SerializeField] private Button _option1Button;
    [SerializeField] private Button _option2Button;
    [SerializeField] private Button _option3Button;
    [SerializeField] private Text _option1Text;
    [SerializeField] private Text _option2Text;
    [SerializeField] private Text _option3Text;

    private PlayerController _playerController;
    private PlayerLevelSystem _levelSystem;
    private WeaponController _weaponController;
    private ICharacterPassive _characterPassive;
    private CharacterData _currentCharacter;
    private int _pendingLevelUpCount = 0;

    private LevelUpOptionGenerator _generator;
    private MageUpgradeConfig _mageUpgradeConfig;
    private LevelUpOptionGenerator.UpgradeSlot[] _currentSlots;
    private Dictionary<string, int> _customUpgradeStacks = new Dictionary<string, int>();

    public bool HasPendingOptions() => _pendingLevelUpCount > 0;

    private void Awake()
    {
        AdjustButtonLayout();
        ApplyThemeColors();
        if (_panel != null) _panel.SetActive(false);
    }

    private void OnEnable() => EventManager.OnLevelUp += OnLevelUp;
    private void OnDisable() => EventManager.OnLevelUp -= OnLevelUp;

    private void Start()
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            _playerController = player;
            _levelSystem = player.GetComponent<PlayerLevelSystem>();
            _weaponController = player.GetComponent<WeaponController>();
            _characterPassive = player.GetComponent<ICharacterPassive>();
        }
        _currentCharacter = GameSceneBootstrap.CurrentCharacter;
        InitGenerator();
    }

    private void InitGenerator()
    {
        if (_characterPassive is MagePassive mage)
            _mageUpgradeConfig = mage.GetUpgradeConfig();
        if (_mageUpgradeConfig == null)
        {
            var config = CharacterConfigLoader.Load(_characterPassive?.CharacterId ?? "mage");
            _mageUpgradeConfig = config as MageUpgradeConfig;
        }
        if (_mageUpgradeConfig == null)
            _mageUpgradeConfig = Resources.Load<MageUpgradeConfig>("Configs/MageUpgradeConfig");
#if UNITY_EDITOR
        if (_mageUpgradeConfig == null)
            _mageUpgradeConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<MageUpgradeConfig>(
                "Assets/ScriptableObjects/Config/MageUpgradeConfig.asset");
#endif
        _generator = new LevelUpOptionGenerator();
        _generator.Init(_characterPassive, _mageUpgradeConfig, _currentCharacter, _customUpgradeStacks);
    }

    public void SetCharacter(CharacterData character)
    {
        _currentCharacter = character;
        SyncStacksFromPassive();
        InitGenerator();
    }

    /// <summary>
    /// 以 MagePassive.UpgradeStacks（权威层数来源，每次 ApplyUpgrade 都会累加）同步本地层数缓存。
    /// 这样 TEST 模式预选的层数、以及已选过的强化层数都会被升级界面正确识别（满层不再刷出，显示正确 [n/max]）。
    /// 非 Mage 角色（UpgradeStacks 不可用）保持本地累计，不做改动。
    /// </summary>
    private void SyncStacksFromPassive()
    {
        var mage = _characterPassive as MagePassive;
        if (mage == null || mage.UpgradeStacks == null) return;

        _customUpgradeStacks.Clear();
        foreach (var kvp in mage.UpgradeStacks)
            _customUpgradeStacks[kvp.Key] = kvp.Value;
    }

    private void Update()
    {
        if (_panel != null && _panel.activeSelf)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                    OnOptionSelected(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                    OnOptionSelected(1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                    OnOptionSelected(2);
            }
        }
    }

    private void OnLevelUp(int newLevel) { _pendingLevelUpCount++; ShowLevelUpUI(); }

    private void ShowLevelUpUI()
    {
        if (_playerController == null) _playerController = GameReferences.Player;
        if (_levelSystem == null) _levelSystem = GameReferences.LevelSystem;
        if (_weaponController == null) _weaponController = GameReferences.WeaponCtrl;
        if (_characterPassive == null) _characterPassive = GameReferences.CharacterPassive;
        if (_mageUpgradeConfig == null && _characterPassive is MagePassive mage2)
            _mageUpgradeConfig = mage2.GetUpgradeConfig();

        if (_currentCharacter == null) _currentCharacter = GameSceneBootstrap.CurrentCharacter;
        if (_generator == null) InitGenerator();
        else _generator.Init(_characterPassive, _mageUpgradeConfig, _currentCharacter, _customUpgradeStacks);

        // 以 MagePassive.UpgradeStacks 为权威层数来源同步（覆盖 TEST 模式预选 / 已有进度），
        // 否则升级界面会无视已满层强化、并显示错误的 [0/max]。
        SyncStacksFromPassive();

        Time.timeScale = 0.0001f;
        _currentSlots = _generator.GenerateOptions(_weaponController);

        if (_titleText != null)
        {
            int level = _levelSystem != null ? _levelSystem.Level : 0;
            _titleText.text = $"LEVEL UP - Lv.{level}";
        }

        SetupSlotButton(_option1Button, _option1Text, 0);
        SetupSlotButton(_option2Button, _option2Text, 1);
        SetupSlotButton(_option3Button, _option3Text, 2);
        if (_panel != null) _panel.SetActive(true);
    }

    private void SetupSlotButton(Button button, Text text, int index)
    {
        if (button == null || _currentSlots == null) return;
        var slot = _currentSlots[index];
        if (text != null) text.text = _generator.GetSlotDescription(slot);

        var img = button.GetComponent<Image>();
        var outline = button.GetComponent<Outline>();
        if (outline == null) outline = button.gameObject.AddComponent<Outline>();

        if (slot.isRecommended)
        {
            if (img != null) img.color = new Color(UIColorTheme.PanelBackground.r + 0.08f,
                UIColorTheme.PanelBackground.g + 0.08f, UIColorTheme.PanelBackground.b + 0.05f, 1f);
            outline.effectColor = new Color(1f, 0.85f, 0.2f);
            outline.effectDistance = new Vector2(3f, 3f);
        }
        else
        {
            if (img != null) img.color = UIColorTheme.PanelBackground;
            outline.effectColor = UIColorTheme.AccentCyan;
            outline.effectDistance = new Vector2(2f, 2f);
        }

        button.onClick.RemoveAllListeners();
        int capturedIndex = index;
        button.onClick.AddListener(() => OnOptionSelected(capturedIndex));
    }

    private void OnOptionSelected(int index)
    {
        try
        {
            if (_currentSlots == null || index < 0 || index >= _currentSlots.Length) return;
            var slot = _currentSlots[index];

            if (slot.isCustom)
            {
                ApplyCustomUpgrade(slot.customOption);
                if (!_customUpgradeStacks.ContainsKey(slot.customOption.upgradeId))
                    _customUpgradeStacks[slot.customOption.upgradeId] = 0;
                _customUpgradeStacks[slot.customOption.upgradeId]++;
            }
            else
            {
                ApplyGenericUpgrade(slot.genericType);
            }

            if (_panel != null) _panel.SetActive(false);
            if (SFXManager.Instance != null) SFXManager.Instance.PlaySelect();

            _pendingLevelUpCount--;
            if (_pendingLevelUpCount <= 0) { _pendingLevelUpCount = 0; Time.timeScale = 1f; }
            else ShowLevelUpUI();
        }
        catch (System.Exception e)
        {
            DebugHelper.LogError($"[LevelUpUI] Error: {e.Message}\n{e.StackTrace}");
            if (_panel != null) _panel.SetActive(false);
            _pendingLevelUpCount = 0;
            Time.timeScale = 1f;
        }
    }

    private void ApplyGenericUpgrade(LevelUpOptionGenerator.GenericUpgradeType type)
    {
        if (_playerController == null) return;
        var damageable = _playerController.Damageable;
        if (damageable == null) return;

        switch (type)
        {
            case LevelUpOptionGenerator.GenericUpgradeType.AttackUp:
                if (_weaponController != null) _weaponController.DamageMultiplier *= 1.15f; break;
            case LevelUpOptionGenerator.GenericUpgradeType.MaxHpUp:
                int newMaxHp = Mathf.RoundToInt(damageable.MaxHp * 1.2f);
                damageable.SetMaxHp(newMaxHp);
                damageable.Heal(Mathf.RoundToInt(damageable.MaxHp * 0.2f)); break;
            case LevelUpOptionGenerator.GenericUpgradeType.SpeedUp:
                _playerController.MoveSpeed *= 1.1f; break;
            case LevelUpOptionGenerator.GenericUpgradeType.MagnetRangeUp:
                MagnetMultiplierSystem.ApplyMagnetRangeUp(); break;
            case LevelUpOptionGenerator.GenericUpgradeType.WeaponDamageUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.DamageUp); break;
            case LevelUpOptionGenerator.GenericUpgradeType.WeaponPierceUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.PierceUp); break;
            case LevelUpOptionGenerator.GenericUpgradeType.WeaponCooldownDown:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.CooldownDown); break;
            case LevelUpOptionGenerator.GenericUpgradeType.WeaponRangeUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.RangeUp); break;
        }
    }

    private void ApplyCustomUpgrade(CharacterUpgradeOption option)
    {
        if (_characterPassive == null) _characterPassive = GameReferences.Player?.GetComponent<ICharacterPassive>();
        if (_characterPassive != null) _characterPassive.ApplyUpgrade(option.upgradeId);
        else DebugHelper.LogError("[LevelUpUI] ICharacterPassive not found");
    }

    private void ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType type)
    {
        if (_weaponController == null || _weaponController.CurrentWeapon == null) return;
        _weaponController.CurrentWeapon.ApplyUpgrade(type);
        _weaponController.RefreshCurrentWeapon();
    }

    public void Hide()
    {
        if (_panel != null) _panel.SetActive(false);
        _pendingLevelUpCount = 0;
        Time.timeScale = 1f;
    }

    // ── UI 布局（从 Awake 调用）──

    private void AdjustButtonLayout()
    {
        var buttons = new[] { _option1Button, _option2Button, _option3Button };
        var texts = new[] { _option1Text, _option2Text, _option3Text };
        float[] offsets = { -450f, 0f, 450f };

        for (int i = 0; i < 3; i++)
        {
            if (buttons[i] == null) continue;
            var rt = buttons[i].GetComponent<RectTransform>();
            if (rt == null) continue;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(offsets[i], 0f);
            var size = rt.sizeDelta; size.x = 400f; size.y += 15f; rt.sizeDelta = size;
        }

        if (_titleText != null)
        {
            _titleText.gameObject.SetActive(true);
            _titleText.raycastTarget = false;
            _titleText.text = "LEVEL UP";
            _titleText.fontSize += 14;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = UIColorTheme.AccentCyan;
            var titleRt = _titleText.GetComponent<RectTransform>();
            if (titleRt != null)
            {
                titleRt.anchorMin = new Vector2(0.5f, 1f);
                titleRt.anchorMax = new Vector2(0.5f, 1f);
                titleRt.pivot = new Vector2(0.5f, 0.5f);
                titleRt.anchoredPosition = new Vector2(0f, -60f);
                titleRt.sizeDelta = new Vector2(600f, 80f);
            }
        }
    }

    private void ApplyThemeColors()
    {
        if (_panel != null)
        {
            var panelImg = _panel.GetComponent<Image>();
            if (panelImg != null) panelImg.color = UIColorTheme.OverlayDark;
        }
        var buttons = new[] { _option1Button, _option2Button, _option3Button };
        for (int i = 0; i < 3; i++)
        {
            if (buttons[i] == null) continue;
            var img = buttons[i].GetComponent<Image>();
            if (img != null) img.color = UIColorTheme.PanelBackground;
        }
    }
}