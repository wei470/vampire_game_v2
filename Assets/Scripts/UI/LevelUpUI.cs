using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级选择界面，升级时弹出显示 3 个选项。
/// 对应 Python: game/level_up.py 中的升级选择逻辑
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
    private int _pendingLevelUpCount = 0;

    /// <summary>
    /// 全局磁铁范围倍率（由升级系统修改，XPGem 读取）
    /// </summary>
    public static float MagnetRangeMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 升级选项类型（含武器升级）
    /// </summary>
    private enum UpgradeType
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

    private UpgradeType[] _currentOptions = new UpgradeType[3];

    private void Awake()
    {
        // 初始隐藏面板
        if (_panel != null)
            _panel.SetActive(false);

        // 应用主题颜色到面板
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

    /// <summary>
    /// 对面板和按钮应用 UI 主题颜色
    /// </summary>
    private void ApplyThemeColors()
    {
        // 面板背景
        if (_panel != null)
        {
            var panelImg = _panel.GetComponent<Image>();
            if (panelImg != null) panelImg.color = UIColorTheme.OverlayDark;
        }

        // 标题
        if (_titleText != null)
            _titleText.color = UIColorTheme.AccentCyan;

        // 按钮列表
        var buttons = new[] { _option1Button, _option2Button, _option3Button };
        var texts = new[] { _option1Text, _option2Text, _option3Text };

        for (int i = 0; i < 3; i++)
        {
            if (buttons[i] != null)
            {
                var img = buttons[i].GetComponent<Image>();
                if (img != null) img.color = UIColorTheme.PanelBackground;

                // 按钮颜色过渡配置
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
        // 键盘快捷键：升级界面显示时按 1/2/3 选择选项
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
        _playerController = GameReferences.Player;
        _levelSystem = GameReferences.Player?.GetComponent<PlayerLevelSystem>();
        _weaponController = GameReferences.Player?.GetComponent<WeaponController>();
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
        {
            _playerController = GameReferences.Player;
        }
        if (_levelSystem == null)
        {
            _levelSystem = GameReferences.Player?.GetComponent<PlayerLevelSystem>();
        }
        if (_weaponController == null)
        {
            _weaponController = GameReferences.Player?.GetComponent<WeaponController>();
        }

        // 暂停游戏物理但不冻结 UI
        Time.timeScale = 0.0001f;

        // 生成 3 个随机升级选项
        GenerateOptions();

        // 更新 UI
        if (_titleText != null)
        {
            int level = _levelSystem != null ? _levelSystem.Level : 0;
            _titleText.text = $"Level Up! (Lv.{level})";
        }

        SetupButton(_option1Button, _option1Text, 0);
        SetupButton(_option2Button, _option2Text, 1);
        SetupButton(_option3Button, _option3Text, 2);

        // 显示面板
        if (_panel != null)
            _panel.SetActive(true);

        DebugHelper.Log("[LevelUpUI] Level up UI shown, game paused");
    }

    /// <summary>
    /// 生成 3 个随机升级选项（确保不重复）
    /// </summary>
    private void GenerateOptions()
    {
        var allTypes = new System.Collections.Generic.List<UpgradeType>
        {
            UpgradeType.AttackUp,
            UpgradeType.MaxHpUp,
            UpgradeType.SpeedUp,
            UpgradeType.ArmorUp,
            UpgradeType.MagnetRangeUp
        };

        bool canUpgradeWeapon = _weaponController != null &&
                                _weaponController.CurrentWeapon != null &&
                                _weaponController.CurrentWeapon.UpgradeLevel < WeaponData.MAX_UPGRADE_LEVEL;

        if (canUpgradeWeapon)
        {
            allTypes.Add(UpgradeType.WeaponDamageUp);
            allTypes.Add(UpgradeType.WeaponPierceUp);
            allTypes.Add(UpgradeType.WeaponCooldownDown);
            allTypes.Add(UpgradeType.WeaponRangeUp);
        }

        var arr = allTypes.ToArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }

        for (int i = 0; i < 3; i++)
        {
            _currentOptions[i] = arr[i];
        }
    }

    /// <summary>
    /// 设置按钮文本和点击事件
    /// </summary>
    private void SetupButton(Button button, Text text, int index)
    {
        if (button == null) { DebugHelper.LogWarning($"[LevelUpUI] Button {index} is null!"); return; }

        var option = _currentOptions[index];
        if (text != null)
        {
            text.text = GetOptionDescription(option);
        }
        else
        {
            DebugHelper.LogWarning($"[LevelUpUI] Text for button {index} is null!");
        }

        button.onClick.RemoveAllListeners();
        int capturedIndex = index;
        button.onClick.AddListener(() =>
        {
            DebugHelper.Log($"[LevelUpUI] Button {capturedIndex} clicked!");
            OnOptionSelected(capturedIndex);
        });

        DebugHelper.Log($"[LevelUpUI] Button {index} set up: {GetOptionDescription(option)}");
    }

    /// <summary>
    /// 获取选项描述文字
    /// </summary>
    private string GetOptionDescription(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.AttackUp: return "+ATK\n攻击力 +15%";
            case UpgradeType.MaxHpUp: return "+HP\n最大生命 +20%";
            case UpgradeType.SpeedUp: return "+Speed\n移动速度 +10%";
            case UpgradeType.ArmorUp: return "+Armor\n护甲 +3";
            case UpgradeType.MagnetRangeUp: return "+Magnet\n拾取范围 +30%";
            case UpgradeType.WeaponDamageUp:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.DamageUp);
            case UpgradeType.WeaponPierceUp:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.PierceUp);
            case UpgradeType.WeaponCooldownDown:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.CooldownDown);
            case UpgradeType.WeaponRangeUp:
                return GetWeaponUpgradeDesc(WeaponData.WeaponUpgradeType.RangeUp);
            default: return "???";
        }
    }

    /// <summary>
    /// 获取武器升级描述（含当前武器名和等级）
    /// </summary>
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
            if (index < 0 || index >= _currentOptions.Length)
            {
                DebugHelper.LogError($"[LevelUpUI] Invalid index: {index}");
                return;
            }

            var selectedOption = _currentOptions[index];
            DebugHelper.Log($"[LevelUpUI] Selected option {index}: {selectedOption}");

            ApplyUpgrade(selectedOption);

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
    /// 应用升级效果到玩家
    /// </summary>
    private void ApplyUpgrade(UpgradeType type)
    {
        if (_playerController == null) return;

        var damageable = _playerController.Damageable;
        if (damageable == null) return;

        switch (type)
        {
            case UpgradeType.AttackUp:
                if (_weaponController != null)
                {
                    _weaponController.DamageMultiplier *= 1.15f;
                }
                DebugHelper.Log($"[LevelUpUI] ATK +15% applied, multiplier: {(_weaponController != null ? _weaponController.DamageMultiplier : 0):F2}");
                break;

            case UpgradeType.MaxHpUp:
                int newMaxHp = Mathf.RoundToInt(damageable.MaxHp * 1.2f);
                damageable.SetMaxHp(newMaxHp);
                damageable.Heal(Mathf.RoundToInt(damageable.MaxHp * 0.2f));
                DebugHelper.Log($"[LevelUpUI] Max HP increased to {newMaxHp}");
                break;

            case UpgradeType.SpeedUp:
                _playerController.MoveSpeed *= 1.1f;
                DebugHelper.Log($"[LevelUpUI] Speed increased to {_playerController.MoveSpeed:F1}");
                break;

            case UpgradeType.ArmorUp:
                damageable.SetArmor(damageable.Armor + 3);
                DebugHelper.Log($"[LevelUpUI] Armor increased to {damageable.Armor}");
                break;

            case UpgradeType.MagnetRangeUp:
                MagnetRangeMultiplier *= 1.3f;
                DebugHelper.Log($"[LevelUpUI] Magnet range +30% applied, multiplier: {MagnetRangeMultiplier:F2}");
                break;

            case UpgradeType.WeaponDamageUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.DamageUp);
                break;
            case UpgradeType.WeaponPierceUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.PierceUp);
                break;
            case UpgradeType.WeaponCooldownDown:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.CooldownDown);
                break;
            case UpgradeType.WeaponRangeUp:
                ApplyWeaponUpgrade(WeaponData.WeaponUpgradeType.RangeUp);
                break;
        }
    }

    /// <summary>
    /// 隐藏升级界面（用于强制关闭）
    /// </summary>
    public void Hide()
    {
        if (_panel != null)
            _panel.SetActive(false);
        _pendingLevelUpCount = 0;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 应用武器升级
    /// </summary>
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
    /// 重置磁铁倍率（游戏重新开始时调用）
    /// </summary>
    public static void ResetMagnetMultiplier()
    {
        MagnetRangeMultiplier = 1f;
    }
}