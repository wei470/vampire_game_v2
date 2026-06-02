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
    private MageUpgradeConfig _mageUpgradeConfig; // #38 升级配置
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
    /// #33 技能升级选项
    /// </summary>
    private struct SkillUpgradeSlot
    {
        public BaseSkill skill;
    }

    /// <summary>
    /// 当前显示的 3 个升级选项（可能是通用或专属或技能升级）
    /// </summary>
    private struct UpgradeSlot
    {
        public bool isCustom;
        public bool isSkillUpgrade;
        public GenericUpgradeType genericType;
        public CharacterUpgradeOption customOption;
        public SkillUpgradeSlot skillUpgrade;
    }

    private UpgradeSlot[] _currentSlots = new UpgradeSlot[3];

    /// <summary>
    /// 每个专属升级的已选次数
    /// </summary>
    private Dictionary<string, int> _customUpgradeStacks = new Dictionary<string, int>();

    private bool _buttonsLayoutAdjusted = false;

    private void Awake()
    {
        // 先调整布局（此时 panel 还在激活态），再隐藏
        ApplyThemeColors();
        AdjustButtonLayout();

        if (_panel != null)
            _panel.SetActive(false);
    }

    /// <summary>
    /// 调整三个按钮和标题的布局：按钮屏幕居中横向排列，宽度+20 高度+15，实色背景荧光边框
    /// </summary>
    private void AdjustButtonLayout()
    {
        if (_buttonsLayoutAdjusted) return;
        _buttonsLayoutAdjusted = true;

        var buttons = new[] { _option1Button, _option2Button, _option3Button };
        var texts = new[] { _option1Text, _option2Text, _option3Text };

        // 三个按钮横向并排分布在屏幕中央，间距 50px（按钮宽 400，中心距 450）
        float[] buttonXOffsets = { -450f, 0f, 450f };

        for (int i = 0; i < 3; i++)
        {
            if (buttons[i] == null) continue;

            var rt = buttons[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            // 锚点屏幕正中央，横向排列
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(buttonXOffsets[i], 0f);

            // 宽度设为 400px
            var size = rt.sizeDelta;
            size.x = 400f;
            size.y += 15f;
            rt.sizeDelta = size;

            // ── 按钮内文字 10px margin ──
            if (texts[i] != null)
            {
                var textRt = texts[i].GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = new Vector2(10f, 10f);
                    textRt.offsetMax = new Vector2(-10f, -10f);
                }
            }

            // ── 实色背景（不要透明）──
            var img = buttons[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = UIColorTheme.PanelBackground; // 深蓝青实色
                img.raycastTarget = true;
            }

            // ── 荧光边框（Outline 组件）──
            var outline = buttons[i].GetComponent<Outline>();
            if (outline == null)
                outline = buttons[i].gameObject.AddComponent<Outline>();
            outline.effectColor = UIColorTheme.AccentCyan;
            outline.effectDistance = new Vector2(2f, 2f);
        }

        // ── 标题 "Level Up"：屏幕顶部居中，大尺寸确保可见 ──
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
                // 锚定到顶部居中
                titleRt.anchorMin = new Vector2(0.5f, 1f);
                titleRt.anchorMax = new Vector2(0.5f, 1f);
                titleRt.pivot = new Vector2(0.5f, 0.5f);
                titleRt.anchoredPosition = new Vector2(0f, -60f);
                // 确保宽度足够显示
                titleRt.sizeDelta = new Vector2(600f, 80f);
            }
        }
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

        // #38 加载 MageUpgradeConfig
        _mageUpgradeConfig = _magePassive?.GetUpgradeConfig();
        if (_mageUpgradeConfig == null)
        {
            // 尝试从 ScriptableObjects 加载
#if UNITY_EDITOR
            _mageUpgradeConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<MageUpgradeConfig>(
                "Assets/ScriptableObjects/Config/MageUpgradeConfig.asset");
#endif
        }
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
            _titleText.text = $"LEVEL UP - Lv.{level}";
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

            // 武器升级已移除（武器系统已简化）
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

                // ═══ 关键修复：跳过已拥有的 DOT 子弹枪 ═══
                // 4 种 DOT 子弹解锁后应从牌库移除，防止重复拾取
                if (_magePassive == null)
                    _magePassive = GameReferences.Player?.GetComponent<MagePassive>();
                if (IsDotGunUpgrade(upgrade.upgradeId) && _magePassive != null)
                {
                    // 检查 MagePassive 是否已拥有该子弹
                    bool alreadyOwned = false;
                    var dotGuns = _magePassive.DotGuns;
                    var dotGunConfig = GetDotGunForUpgrade(upgrade.upgradeId);
                    if (dotGunConfig.HasValue)
                    {
                        foreach (var gun in dotGuns)
                        {
                            if (gun.effectType == dotGunConfig.Value.type)
                            {
                                alreadyOwned = true;
                                break;
                            }
                        }
                    }
                    if (alreadyOwned) continue;
                }

                allSlots.Add(new UpgradeSlot { isCustom = true, customOption = upgrade });
            }
        }

        // #33 添加技能升级选项（已拥有且未满级的主动技能可升级）
        var skillMgr = GameReferences.Player?.GetComponent<PlayerSkillManager>();
        if (skillMgr != null)
        {
            foreach (var skill in skillMgr.ActiveSkills)
            {
                if (skill == null || skill.Data == null) continue;
                if (skill.CurrentLevel >= skill.Data.maxLevel) continue; // 已满级跳过
                allSlots.Add(new UpgradeSlot { isSkillUpgrade = true, skillUpgrade = new SkillUpgradeSlot { skill = skill } });
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

        // #33 技能升级描述
        if (slot.isSkillUpgrade)
        {
            var skill = slot.skillUpgrade.skill;
            var data = skill.Data;
            int nextLevel = skill.CurrentLevel + 1;
            int newDmg = data.GetDamageAtLevel(nextLevel);
            float newCd = data.GetCooldownAtLevel(nextLevel);
            float newEff = data.GetEffectAtLevel(nextLevel);
            string desc = $"⬆ Lv.{nextLevel}\n";
            if (newDmg > 0) desc += $"DMG: {data.GetDamageAtLevel(skill.CurrentLevel)} → {newDmg}\n";
            desc += $"CD: {data.GetCooldownAtLevel(skill.CurrentLevel):F1}s → {newCd:F1}s";
            return $"🔮 {data.skillName} {desc}";
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
            else if (slot.isSkillUpgrade)
            {
                // #33 应用技能升级
                var skill = slot.skillUpgrade.skill;
                if (skill != null)
                {
                    skill.Upgrade();
                    DebugHelper.Log($"[LevelUpUI] Skill upgraded: {skill.Data.skillName} → Lv.{skill.CurrentLevel}");
                }
            }
            else
            {
                ApplyGenericUpgrade(slot.genericType);
            }

            if (_panel != null)
                _panel.SetActive(false);

            // 播放升级选择音效
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySelect();

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
    /// 应用角色专属升级效果 — 委托给 MagePassive.ApplyUpgrade() 统一处理
    /// </summary>
    private void ApplyCustomUpgrade(CharacterUpgradeOption option)
    {
        if (_magePassive == null)
            _magePassive = GameReferences.Player?.GetComponent<MagePassive>();

        if (_magePassive != null)
        {
            bool success = _magePassive.ApplyUpgrade(option.upgradeId);
            if (!success)
                DebugHelper.LogWarning($"[LevelUpUI] Failed to apply upgrade: {option.upgradeId}");
        }
        else
        {
            DebugHelper.LogError("[LevelUpUI] MagePassive not found, cannot apply upgrade");
        }
    }

    /// <summary>
    /// 判断 upgradeId 是否属于 DOT 子弹枪类型 — 通过 MageUpgradeConfig 查询
    /// </summary>
    private bool IsDotGunUpgrade(string upgradeId)
    {
        // 优先使用配置查询
        if (_mageUpgradeConfig != null)
            return _mageUpgradeConfig.IsDotGunUpgrade(upgradeId);

        // Fallback：硬编码检查（兼容无配置情况）
        return upgradeId == "bleed" || upgradeId == "poison" ||
               upgradeId == "burn" || upgradeId == "frostbite";
    }

    /// <summary>
    /// 根据 upgradeId 获取 DOT 子弹枪配置 — 通过 MageUpgradeConfig 查询
    /// </summary>
    private struct DotGunConfig { public StatusEffectType type; public Color color; public float cooldown; public int impactDmg; public float dotDps; public float dotDuration; }
    private DotGunConfig? GetDotGunForUpgrade(string upgradeId)
    {
        // 优先使用配置查询
        if (_mageUpgradeConfig != null)
        {
            var entry = _mageUpgradeConfig.GetDotGunEntry(upgradeId);
            if (entry.HasValue)
            {
                var e = entry.Value;
                return new DotGunConfig
                {
                    type = e.effectType, color = e.color, cooldown = e.cooldown,
                    impactDmg = e.impactDmg, dotDps = e.dotDps, dotDuration = e.dotDuration
                };
            }
            return null;
        }

        // Fallback：硬编码（兼容无配置情况）
        switch (upgradeId)
        {
            case "bleed":    return new DotGunConfig { type = StatusEffectType.Bleed,    color = new Color(0.9f, 0.1f, 0.1f), cooldown = 1.0f, impactDmg = 3, dotDps = 2f, dotDuration = 4f };
            case "poison":   return new DotGunConfig { type = StatusEffectType.Poison,   color = new Color(0.1f, 0.9f, 0.2f), cooldown = 2.0f, impactDmg = 0, dotDps = 3f, dotDuration = 5f };
            case "burn":     return new DotGunConfig { type = StatusEffectType.Burn,     color = new Color(1f, 0.4f, 0f),     cooldown = 0.2f, impactDmg = 2, dotDps = 2f, dotDuration = 3f };
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