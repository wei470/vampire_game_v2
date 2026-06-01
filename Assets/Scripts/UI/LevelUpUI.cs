using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 升级选择界面，升级时弹出显示 3 个选项。
/// 对应 Python: game/level_up.py 中的升级选择逻辑
///
/// 支持角色专属升级（CharacterData.customUpgrades）和通用升级混合。
///
/// 选择后游戏恢复，选中的效果应用到玩家。
/// 使用方式：挂载到 Canvas 下的 Panel 上
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
    private MagePassive _magePassive;
    private CharacterData _currentCharacter;
    private int _pendingLevelUpCount = 0;

    /// <summary>
    /// 全局磁铁范围倍率（由升级系统修改，XPGem 读取）
    /// </summary>
    public static float MagnetRangeMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 通用升级选项类型
    /// </summary>
    private enum GenericUpgradeType
    {
        AttackUp,
        MaxHpUp,
        SpeedUp,
        ArmorUp,
        MagnetRangeUp,
        WeaponDamageUp,
        WeaponPierceUp,
        WeaponCooldownDown,
        WeaponRangeUp
    }

    /// <summary>
    /// 当前显示的 3 个升级选项（可能是通用或专属）
    /// </summary>
    private struct UpgradeSlot
    {
        public bool isCustom;
        public GenericUpgradeType genericType;
        public CharacterUpgradeOption customOption;
    }

    private UpgradeSlot[] _currentSlots = new UpgradeSlot[3];

    /// <summary>
    /// 每个专属升级的已选次数
    /// </summary>
    private Dictionary<string, int> _customUpgradeStacks = new Dictionary<string, int>();

    private void Awake()
    {
        if (_panel != null)
            _panel.SetActive(false);

        ApplyThemeColors();
    }

    private void OnEnable()
    {
        EventManager.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        EventManager.OnLevelUp -= OnLevelUp;
    }

    private void ApplyThemeColors()
    {
        if (_panel != null)
        {
            var panelImg = _panel.GetComponent<Image>();
            if (panelImg != null) panelImg.color = UIColorTheme.OverlayDark;
        }

        if (_titleText != null)
            _titleText.color = UIColorTheme.AccentCyan;

        var buttons = new[] { _option1Button, _option2Button, _option3Button };
        var texts = new[] { _option1Text, _option2Text, _option3Text };

        for (int i = 0; i < 3; i++)
        {
            if (buttons[i] != null)
            {
                var img = buttons[i].GetComponent<Image>();
                if (img != null) img.color = UIColorTheme.PanelBackground;

                var cb = buttons[i].colors;
                cb.normalColor = UIColorTheme.ButtonNormal;
                cb.highlightedColor = UIColorTheme.ButtonHover;
                cb.pressedColor = new Color(UIColorTheme.AccentCyan.r, UIColorTheme.AccentCyan.g, UIColorTheme.AccentCyan.b, 0.6f);
                cb.selectedColor = UIColorTheme.ButtonSelected;
                buttons[i].colors = cb;
            }

            if (texts[i] != null)
                texts[i].color = UIColorTheme.TextPrimary;
        }
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

    private void Start()
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            _playerController = player;
            _levelSystem = player.GetComponent<PlayerLevelSystem>();
            _weaponController = player.GetComponent<WeaponController>();
            _magePassive = player.GetComponent<MagePassive>();
        }

        // 获取当前角色数据
        _currentCharacter = GameSceneBootstrap.CurrentCharacter;
    }

    /// <summary>
    /// 设置当前角色数据（由 GameSceneBootstrap 在选择完成后调用）
    /// </summary>
    public void SetCharacter(CharacterData character)
    {
        _currentCharacter = character;
        _customUpgradeStacks.Clear();
    }

    /// <summary>
    /// 升级事件处理
    /// </summary>
    private void OnLevelUp(int newLevel)
    {
        _pendingLevelUpCount++;
        ShowLevelUpUI();
    }

    /// <summary>
    /// 显示升级选择界面
    /// </summary>
    private void ShowLevelUpUI()
    {
        if (_playerController == null)
            _playerController = GameReferences.Player;
        if (_levelSystem == null)
        {
            var player = GameReferences.Player;
            if (player != null) _levelSystem = player.GetComponent<PlayerLevelSystem>();
        }
        if (_weaponController == null)
        {
            var player = GameReferences.Player;
            if (player != null) _weaponController = player.GetComponent<WeaponController>();
        }
        if (_magePassive == null)
        {
            var player = GameReferences.Player;
            if (player != null) _magePassive = player.GetComponent<MagePassive>();
        }

        Time.timeScale = 0.0001f;

        GenerateOptions();

        if (_titleText != null)
        {
            int level = _levelSystem != null ? _levelSystem.Level : 0;
            _titleText.text = $"Level Up! (Lv.{level})";
        }

        SetupSlotButton(_option1Button, _option1Text, 0);
        SetupSlotButton(_option2Button, _option2Text, 1);
        SetupSlotButton(_option3Button, _option3Text, 2);

        if (_panel != null)
            _panel.SetActive(true);

        DebugHelper.Log("[LevelUpUI] Level up UI shown, game paused");
    }

    /// <summary>
    /// 生成 3 个随机升级选项（支持角色专属升级）
    /// </summary>
    private void GenerateOptions()
    {
        // 每次动态获取当前角色（因为 Start() 可能在 CurrentCharacter 设置之前执行）
        if (_currentCharacter == null)
            _currentCharacter = GameSceneBootstrap.CurrentCharacter;

        var allSlots = new List<UpgradeSlot>();

        // 1. 添加通用升级（如果角色允许）
        bool useGeneric = _currentCharacter == null || _currentCharacter.useGenericUpgrades;
        if (useGeneric)
        {
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.AttackUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.MaxHpUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.SpeedUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.ArmorUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.MagnetRangeUp });

            bool canUpgradeWeapon = _weaponController != null &&
                                    _weaponController.CurrentWeapon != null &&
                                    _weaponController.CurrentWeapon.UpgradeLevel < WeaponData.MAX_UPGRADE_LEVEL;
            if (canUpgradeWeapon)
            {
                allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.WeaponDamageUp });
                allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.WeaponPierceUp });
                allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.WeaponCooldownDown });
                allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.WeaponRangeUp });
            }
        }

        // 2. 添加角色专属升级
        if (_currentCharacter != null && _currentCharacter.customUpgrades != null)
        {
            foreach (var upgrade in _currentCharacter.customUpgrades)
            {
                // 检查是否已达最大叠加次数
                if (upgrade.maxStacks > 0)
                {
                    int currentStacks = 0;
                    _customUpgradeStacks.TryGetValue(upgrade.upgradeId, out currentStacks);
                    if (currentStacks >= upgrade.maxStacks) continue;
                }

                allSlots.Add(new UpgradeSlot { isCustom = true, customOption = upgrade });
            }
        }

        // 如果没有可用选项，添加通用默认
        if (allSlots.Count == 0)
        {
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.AttackUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.MaxHpUp });
            allSlots.Add(new UpgradeSlot { isCustom = false, genericType = GenericUpgradeType.SpeedUp });
        }

        // Fisher-Yates 洗牌
        var arr = allSlots.ToArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }

        // 选取前 3 个（不足 3 个则重复）
        for (int i = 0; i < 3; i++)
        {
            _currentSlots[i] = arr[i % arr.Length];
        }
    }

    /// <summary>
    /// 设置按钮文本和点击事件
    /// </summary>
    private void SetupSlotButton(Button button, Text text, int index)
    {
        if (button == null) return;

        var slot = _currentSlots[index];
        if (text != null)
        {
            text.text = GetSlotDescription(slot);
        }

        button.onClick.RemoveAllListeners();
        int capturedIndex = index;
        button.onClick.AddListener(() => OnOptionSelected(capturedIndex));
    }

    /// <summary>
    /// 获取选项描述文字
    /// </summary>
    private string GetSlotDescription(UpgradeSlot slot)
    {
        if (slot.isCustom)
        {
            var opt = slot.customOption;
            int stacks = 0;
            _customUpgradeStacks.TryGetValue(opt.upgradeId, out stacks);
            string stackText = opt.maxStacks > 0 ? $" [{stacks}/{opt.maxStacks}]" : "";
            return $"{opt.upgradeName}{stackText}\n{opt.description}";
        }

        return GetGenericDescription(slot.genericType);
    }

    private string GetGenericDescription(GenericUpgradeType type)
    {
        switch (type)
        {
            case GenericUpgradeType.AttackUp: return "+ATK\n攻击力 +15%";
            case GenericUpgradeType.MaxHpUp: return "+HP\n最大生命 +20%";
            case GenericUpgradeType.SpeedUp: return "+Speed\n移动速度 +10%";
            case GenericUpgradeType.ArmorUp: return "+Armor\n护甲 +3";
            case GenericUpgradeType.MagnetRangeUp: return "+Magnet\n拾取范围 +30%";
            case GenericUpgradeType.WeaponDamageUp:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.DamageUp);
            case GenericUpgradeType.WeaponPierceUp:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.PierceUp);
            case GenericUpgradeType.WeaponCooldownDown:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.CooldownDown);
            case GenericUpgradeType.WeaponRangeUp:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.RangeUp);
            default: return "???";
        }
    }

    private string GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType type)
    {
        if (_weaponController == null || _weaponController.CurrentWeapon == null)
            return "???";

        var weapon = _weaponController.CurrentWeapon;
        string upgradeText = weapon.GetUpgradeDescription(type);
        string weaponTag = $"[{weapon.weaponName} Lv.{weapon.UpgradeLevel + 1}]";
        return $"⚔ {weaponTag}\n{upgradeText}";
    }

    /// <summary>
    /// 玩家选择了一个升级选项
    /// </summary>
    private void OnOptionSelected(int index)
    {
        try
        {
            if (index < 0 || index >= _currentSlots.Length) return;

            var slot = _currentSlots[index];

            if (slot.isCustom)
            {
                ApplyCustomUpgrade(slot.customOption);
                // 记录叠加次数
                if (!_customUpgradeStacks.ContainsKey(slot.customOption.upgradeId))
                    _customUpgradeStacks[slot.customOption.upgradeId] = 0;
                _customUpgradeStacks[slot.customOption.upgradeId]++;
            }
            else
            {
                ApplyGenericUpgrade(slot.genericType);
            }

            if (_panel != null)
                _panel.SetActive(false);

            _pendingLevelUpCount--;
            if (_pendingLevelUpCount <= 0)
            {
                _pendingLevelUpCount = 0;
                Time.timeScale = 1f;
            }
            else
            {
                ShowLevelUpUI();
            }

            DebugHelper.Log("[LevelUpUI] Option applied, game resumed");
        }
        catch (System.Exception e)
        {
            DebugHelper.LogError($"[LevelUpUI] Error in OnOptionSelected: {e.Message}\n{e.StackTrace}");
            if (_panel != null) _panel.SetActive(false);
            _pendingLevelUpCount = 0;
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// 应用通用升级效果
    /// </summary>
    private void ApplyGenericUpgrade(GenericUpgradeType type)
    {
        if (_playerController == null) return;

        var damageable = _playerController.Damageable;
        if (damageable == null) return;

        switch (type)
        {
            case GenericUpgradeType.AttackUp:
                if (_weaponController != null)
                    _weaponController.DamageMultiplier *= 1.15f;
                DebugHelper.Log($"[LevelUpUI] ATK +15% applied");
                break;

            case GenericUpgradeType.MaxHpUp:
                int newMaxHp = Mathf.RoundToInt(damageable.MaxHp * 1.2f);
                damageable.SetMaxHp(newMaxHp);
                damageable.Heal(Mathf.RoundToInt(damageable.MaxHp * 0.2f));
                DebugHelper.Log($"[LevelUpUI] Max HP increased to {newMaxHp}");
                break;

            case GenericUpgradeType.SpeedUp:
                _playerController.MoveSpeed *= 1.1f;
                DebugHelper.Log($"[LevelUpUI] Speed increased");
                break;

            case GenericUpgradeType.ArmorUp:
                damageable.SetArmor(damageable.Armor + 3);
                DebugHelper.Log($"[LevelUpUI] Armor increased to {damageable.Armor}");
                break;

            case GenericUpgradeType.MagnetRangeUp:
                MagnetRangeMultiplier *= 1.3f;
                DebugHelper.Log($"[LevelUpUI] Magnet range +30%");
                break;

            case GenericUpgradeType.WeaponDamageUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.DamageUp);
                break;
            case GenericUpgradeType.WeaponPierceUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.PierceUp);
                break;
            case GenericUpgradeType.WeaponCooldownDown:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.CooldownDown);
                break;
            case GenericUpgradeType.WeaponRangeUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.RangeUp);
                break;
        }
    }

    /// <summary>
    /// 应用角色专属升级效果
    /// </summary>
    private void ApplyCustomUpgrade(CharacterUpgradeOption option)
    {
        if (_magePassive == null)
            _magePassive = GameReferences.Player?.GetComponent<MagePassive>();

        var damageable = _playerController?.Damageable;

        // 根据 upgradeId 解锁对应的 DOT 子弹枪
        var dotGun = GetDotGunForUpgrade(option.upgradeId);
        if (dotGun.HasValue && _magePassive != null)
        {
            var dg = dotGun.Value;
            _magePassive.UnlockDotGun(dg.type, dg.color, dg.cooldown, dg.impactDmg, dg.dotDps, dg.dotDuration);
            DebugHelper.Log($"[LevelUpUI] Unlocked DOT gun: {option.upgradeName}");
            return;
        }

        // 非 DOT 子弹类型的升级
        switch (option.category)
        {
            case CharacterUpgradeOption.UpgradeCategory.DotDamage:
                if (_magePassive != null)
                {
                    _magePassive.EnhanceAllDotGuns(option.value1);
                    DebugHelper.Log($"[LevelUpUI] All DOT damage +{option.value1 * 100}%");
                }
                break;

            case CharacterUpgradeOption.UpgradeCategory.DotDuration:
                if (_magePassive != null)
                {
                    _magePassive.AddDotDurationBonus(option.value1);
                    DebugHelper.Log($"[LevelUpUI] DOT Duration +{option.value1 * 100}%");
                }
                break;

            case CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier:
                if (_magePassive != null)
                {
                    _magePassive.DetonateMultiplier += option.value1;
                    DebugHelper.Log($"[LevelUpUI] Detonate multiplier +{option.value1}");
                }
                break;

            case CharacterUpgradeOption.UpgradeCategory.DetonateAbility:
                if (_magePassive != null)
                {
                    _magePassive.DetonateCooldownValue *= (1f - option.value1);
                    DebugHelper.Log($"[LevelUpUI] Detonate cooldown -{option.value1 * 100}%");
                }
                break;

            case CharacterUpgradeOption.UpgradeCategory.StatusEffect:
                // 通用状态效果（诅咒/凋零等）
                DebugHelper.Log($"[LevelUpUI] Applied {option.upgradeName}");
                break;

            default:
                DebugHelper.Log($"[LevelUpUI] Applied {option.upgradeName} ({option.category})");
                break;
        }
    }

    /// <summary>
    /// 根据 upgradeId 获取 DOT 子弹枪配置
    /// </summary>
    private struct DotGunConfig { public StatusEffectType type; public Color color; public float cooldown; public int impactDmg; public float dotDps; public float dotDuration; }
    private DotGunConfig? GetDotGunForUpgrade(string upgradeId)
    {
        switch (upgradeId)
        {
            case "bleed":    return new DotGunConfig { type = StatusEffectType.Bleed,    color = new Color(0.9f, 0.1f, 0.1f), cooldown = 1.0f, impactDmg = 3, dotDps = 2f, dotDuration = 4f };
            case "poison":   return new DotGunConfig { type = StatusEffectType.Poison,   color = new Color(0.1f, 0.9f, 0.2f), cooldown = 2.0f, impactDmg = 0, dotDps = 3f, dotDuration = 5f };
            case "burn":     return new DotGunConfig { type = StatusEffectType.Burn,     color = new Color(1f, 0.4f, 0f),     cooldown = 0.3f, impactDmg = 2, dotDps = 2f, dotDuration = 3f };
            case "frostbite":return new DotGunConfig { type = StatusEffectType.Frostbite,color = new Color(0.3f, 0.6f, 1f),   cooldown = 2.0f, impactDmg = 6, dotDps = 2f, dotDuration = 3f };
            default: return null;
        }
    }

    /// <summary>
    /// 隐藏升级界面
    /// </summary>
    public void Hide()
    {
        if (_panel != null)
            _panel.SetActive(false);
        _pendingLevelUpCount = 0;
        Time.timeScale = 1f;
    }

    private void ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType type)
    {
        if (_weaponController == null || _weaponController.CurrentWeapon == null) return;
        var weapon = _weaponController.CurrentWeapon;
        bool success = weapon.ApplyUpgrade(type);
        if (success)
        {
            DebugHelper.Log($"[LevelUpUI] Weapon '{weapon.weaponName}' upgraded to Lv.{weapon.UpgradeLevel}: {type}");
            _weaponController.RefreshCurrentWeapon();
        }
    }

    /// <summary>
    /// 重置磁铁倍率
    /// </summary>
    public static void ResetMagnetMultiplier()
    {
        MagnetRangeMultiplier = 1f;
    }
}