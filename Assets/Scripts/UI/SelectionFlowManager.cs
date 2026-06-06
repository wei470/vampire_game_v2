using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 3步选择流程管理器 — 角色选择 → 武器选择 → 技能选择 → 开始游戏。
/// 对应 Python: ui/selection_flow.py
/// 
/// 流程：
///   Step 1/3: 角色选择（8 角色，左右面板，中央详情）
///   Step 2/3: 武器选择（8 武器）
///   Step 3/3: 技能选择（8 技能）+ "Start Game!" 按钮
///   每步必须 Confirm 才能进入下一步
/// 
/// 使用方式：挂载到 Canvas 下
/// </summary>
public class SelectionFlowManager : MonoBehaviour
{
    [Header("数据资源")]
    [SerializeField] private CharacterData[] _characters = new CharacterData[8];
    [SerializeField] private WeaponData[] _weapons = new WeaponData[8];
    [SerializeField] private SkillData[] _skills = new SkillData[8];

    [Header("UI 引用")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _stepTitle;
    [SerializeField] private Text _itemName;
    [SerializeField] private Text _itemDescription;
    [SerializeField] private Text _itemStats;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Image _itemPreview;

    [Header("选择按钮面板")]
    [SerializeField] private Button[] _selectionButtons = new Button[8];
    [SerializeField] private Text[] _selectionTexts = new Text[8];

    // 运行时状态
    private int _currentStep = 0; // 0=角色, 1=武器, 2=技能
    private int _currentIndex = 0;
    private CharacterData _selectedCharacter;
    private WeaponData _selectedWeapon;
    private SkillData _selectedSkill;

    // 按钮颜色
    private Color _normalColor = new Color(0.2f, 0.2f, 0.3f);
    private Color _selectedColor = new Color(0.3f, 0.5f, 0.8f);
    private Color _confirmColor = new Color(0.2f, 0.7f, 0.3f);

    private void Awake()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    private void Start()
    {
        // 自动加载数据资源（如果 Inspector 没有手动分配）
        if (_characters == null || _characters.Length == 0 || _characters[0] == null)
        {
            LoadDataAssets();
        }

        // 设置按钮监听
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnConfirm);
        if (_prevButton != null)
            _prevButton.onClick.AddListener(OnPrev);
        if (_nextButton != null)
            _nextButton.onClick.AddListener(OnNext);

        for (int i = 0; i < _selectionButtons.Length; i++)
        {
            if (_selectionButtons[i] != null)
            {
                int capturedIndex = i;
                _selectionButtons[i].onClick.AddListener(() => OnItemSelected(capturedIndex));
            }
        }
    }

    /// <summary>
    /// 从 ScriptableObjects 目录加载数据
    /// </summary>
    private void LoadDataAssets()
    {
        _characters = SelectionDataLoader.LoadCharacters();
        _skills = SelectionDataLoader.LoadSkills();
    }

    /// <summary>
    /// 开始选择流程
    /// </summary>
    public void StartSelection()
    {
        _currentStep = 0;
        _currentIndex = 0;
        _selectedCharacter = null;
        _selectedWeapon = null;
        _selectedSkill = null;

        if (_panel != null)
            _panel.SetActive(true);

        Time.timeScale = 0.0001f;
        ShowStep();
    }

    /// <summary>
    /// 显示当前步骤
    /// </summary>
    private void ShowStep()
    {
        _currentIndex = 0;

        switch (_currentStep)
        {
            case 0: ShowCharacterSelection(); break;
            case 1: ShowWeaponSelection(); break;
            case 2: ShowSkillSelection(); break;
        }

        UpdateDetailPanel();
        UpdateSelectionHighlight();
    }

    /// <summary>
    /// 显示角色选择
    /// </summary>
    private void ShowCharacterSelection()
    {
        if (_stepTitle != null)
            _stepTitle.text = "Step 1/3: Choose Character";

        for (int i = 0; i < _selectionButtons.Length; i++)
        {
            if (_selectionButtons[i] == null) continue;

            if (i < _characters.Length && _characters[i] != null)
            {
                _selectionButtons[i].gameObject.SetActive(true);
                if (_selectionTexts[i] != null)
                    _selectionTexts[i].text = _characters[i].characterName;
            }
            else
            {
                _selectionButtons[i].gameObject.SetActive(false);
            }
        }

        if (_confirmButton != null)
        {
            var text = _confirmButton.GetComponentInChildren<Text>();
            if (text != null) text.text = "Confirm Character";
        }
    }

    /// <summary>
    /// 显示武器选择
    /// </summary>
    private void ShowWeaponSelection()
    {
        if (_stepTitle != null)
            _stepTitle.text = "Step 2/3: Choose Weapon";

        // 获取武器数据
        var weaponData = GetWeaponDataArray();

        for (int i = 0; i < _selectionButtons.Length; i++)
        {
            if (_selectionButtons[i] == null) continue;

            if (i < weaponData.Length && weaponData[i] != null)
            {
                _selectionButtons[i].gameObject.SetActive(true);
                if (_selectionTexts[i] != null)
                    _selectionTexts[i].text = weaponData[i].weaponName;
            }
            else
            {
                _selectionButtons[i].gameObject.SetActive(false);
            }
        }

        if (_confirmButton != null)
        {
            var text = _confirmButton.GetComponentInChildren<Text>();
            if (text != null) text.text = "Confirm Weapon";
        }
    }

    /// <summary>
    /// 显示技能选择
    /// </summary>
    private void ShowSkillSelection()
    {
        if (_stepTitle != null)
            _stepTitle.text = "Step 3/3: Choose Skill";

        for (int i = 0; i < _selectionButtons.Length; i++)
        {
            if (_selectionButtons[i] == null) continue;

            if (i < _skills.Length && _skills[i] != null)
            {
                _selectionButtons[i].gameObject.SetActive(true);
                if (_selectionTexts[i] != null)
                    _selectionTexts[i].text = _skills[i].skillName;
            }
            else
            {
                _selectionButtons[i].gameObject.SetActive(false);
            }
        }

        if (_confirmButton != null)
        {
            var text = _confirmButton.GetComponentInChildren<Text>();
            if (text != null) text.text = "Start Game!";
        }
    }

    /// <summary>
    /// 获取武器数据数组
    /// </summary>
    private WeaponData[] GetWeaponDataArray()
    {
        return SelectionDataLoader.GetWeaponDataArray(_weapons);
    }

    /// <summary>
    /// 更新详情面板
    /// </summary>
    private void UpdateDetailPanel()
    {
        switch (_currentStep)
        {
            case 0:
                if (_currentIndex < _characters.Length && _characters[_currentIndex] != null)
                {
                    var c = _characters[_currentIndex];
                    if (_itemName != null) _itemName.text = c.characterName;
                    if (_itemDescription != null) _itemDescription.text = $"{c.description}\n\n<b>Passive:</b> {c.passiveDescription}";
                    if (_itemStats != null)
                    {
                        _itemStats.text = $"HP: {c.maxHP}  |  Speed: {c.moveSpeed:F1}  |  Armor: {c.armor}\n" +
                                         $"ATK: {c.attackDamage}  |  Crit: {c.critChance * 100:F0}%\n" +
                                         $"Pierce: {c.pierce}  |  Lifesteal: {c.lifesteal * 100:F0}%";
                    }
                    if (_itemPreview != null) _itemPreview.color = c.characterColor;
                }
                break;

            case 1:
                var weaponData = GetWeaponDataArray();
                if (_currentIndex < weaponData.Length && weaponData[_currentIndex] != null)
                {
                    var w = weaponData[_currentIndex];
                    if (_itemName != null) _itemName.text = w.weaponName;
                    if (_itemDescription != null) _itemDescription.text = w.description;
                    if (_itemStats != null)
                    {
                        _itemStats.text = $"Damage: {w.baseDamage}  |  Cooldown: {w.cooldown:F1}s\n" +
                                         $"Pierce: {w.pierce}  |  Speed: {w.projectileSpeed:F0}\n" +
                                         $"Type: {w.projectileType}";
                    }
                    if (_itemPreview != null) _itemPreview.color = w.projectileColor;
                }
                break;

            case 2:
                if (_currentIndex < _skills.Length && _skills[_currentIndex] != null)
                {
                    var s = _skills[_currentIndex];
                    if (_itemName != null) _itemName.text = s.skillName;
                    if (_itemDescription != null) _itemDescription.text = s.description;
                    if (_itemStats != null)
                    {
                        _itemStats.text = $"Damage: {s.baseDamage}  |  Cooldown: {s.cooldown:F1}s\n" +
                                         $"Duration: {s.duration:F1}s  |  Radius: {s.effectRadius:F1}\n" +
                                         $"Type: {s.skillType}  |  Target: {s.targetType}";
                    }
                    if (_itemPreview != null) _itemPreview.color = s.skillColor;
                }
                break;
        }
    }

    /// <summary>
    /// 更新选中高亮
    /// </summary>
    private void UpdateSelectionHighlight()
    {
        for (int i = 0; i < _selectionButtons.Length; i++)
        {
            if (_selectionButtons[i] == null) continue;

            var colors = _selectionButtons[i].colors;
            colors.normalColor = (i == _currentIndex) ? _selectedColor : _normalColor;
            _selectionButtons[i].colors = colors;
        }
    }

    /// <summary>
    /// 选择项目
    /// </summary>
    private void OnItemSelected(int index)
    {
        _currentIndex = index;
        UpdateDetailPanel();
        UpdateSelectionHighlight();
    }

    /// <summary>
    /// 上一个
    /// </summary>
    private void OnPrev()
    {
        int maxItems = GetMaxItems();
        if (maxItems == 0) return;
        _currentIndex = (_currentIndex - 1 + maxItems) % maxItems;
        UpdateDetailPanel();
        UpdateSelectionHighlight();
    }

    /// <summary>
    /// 下一个
    /// </summary>
    private void OnNext()
    {
        int maxItems = GetMaxItems();
        if (maxItems == 0) return;
        _currentIndex = (_currentIndex + 1) % maxItems;
        UpdateDetailPanel();
        UpdateSelectionHighlight();
    }

    /// <summary>
    /// 获取当前步骤的最大选项数
    /// </summary>
    private int GetMaxItems()
    {
        switch (_currentStep)
        {
            case 0: return _characters.Length;
            case 1: return GetWeaponDataArray().Length;
            case 2: return _skills.Length;
            default: return 0;
        }
    }

    /// <summary>
    /// 确认选择
    /// </summary>
    private void OnConfirm()
    {
        switch (_currentStep)
        {
            case 0:
                if (_currentIndex < _characters.Length && _characters[_currentIndex] != null)
                {
                    _selectedCharacter = _characters[_currentIndex];
                    EventManager.TriggerCharacterSelected(_selectedCharacter);
                    DebugHelper.Log($"[SelectionFlow] Character selected: {_selectedCharacter.characterName}");
                }
                _currentStep = 1;
                ShowStep();
                break;

            case 1:
                var weaponData = GetWeaponDataArray();
                if (_currentIndex < weaponData.Length && weaponData[_currentIndex] != null)
                {
                    _selectedWeapon = weaponData[_currentIndex];
                    DebugHelper.Log($"[SelectionFlow] Weapon selected: {_selectedWeapon.weaponName}");
                }
                _currentStep = 2;
                ShowStep();
                break;

            case 2:
                if (_currentIndex < _skills.Length && _skills[_currentIndex] != null)
                {
                    _selectedSkill = _skills[_currentIndex];
                    DebugHelper.Log($"[SelectionFlow] Skill selected: {_selectedSkill.skillName}");
                }
                CompleteSelection();
                break;
        }
    }

    /// <summary>
    /// 完成选择流程
    /// </summary>
    private void CompleteSelection()
    {
        DebugHelper.Log($"[SelectionFlow] Selection complete! Character={_selectedCharacter?.characterName}, " +
                  $"Weapon={_selectedWeapon?.weaponName}, Skill={_selectedSkill?.skillName}");

        // 隐藏面板
        if (_panel != null)
            _panel.SetActive(false);

        Time.timeScale = 1f;

        // 广播选择完成事件
        EventManager.TriggerSelectionComplete(_selectedCharacter, _selectedWeapon, _selectedSkill);
    }

    /// <summary>
    /// 键盘输入处理
    /// </summary>
    private void Update()
    {
        if (_panel == null || !_panel.activeSelf) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // 左右箭头切换选项
        if (kb.leftArrowKey.wasPressedThisFrame)
            OnPrev();
        else if (kb.rightArrowKey.wasPressedThisFrame)
            OnNext();

        // 数字键 1-8 直接选择
        if (kb.digit1Key.wasPressedThisFrame && 1 <= GetMaxItems()) OnItemSelected(0);
        else if (kb.digit2Key.wasPressedThisFrame && 2 <= GetMaxItems()) OnItemSelected(1);
        else if (kb.digit3Key.wasPressedThisFrame && 3 <= GetMaxItems()) OnItemSelected(2);
        else if (kb.digit4Key.wasPressedThisFrame && 4 <= GetMaxItems()) OnItemSelected(3);
        else if (kb.digit5Key.wasPressedThisFrame && 5 <= GetMaxItems()) OnItemSelected(4);
        else if (kb.digit6Key.wasPressedThisFrame && 6 <= GetMaxItems()) OnItemSelected(5);
        else if (kb.digit7Key.wasPressedThisFrame && 7 <= GetMaxItems()) OnItemSelected(6);
        else if (kb.digit8Key.wasPressedThisFrame && 8 <= GetMaxItems()) OnItemSelected(7);

        // Enter 确认
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            OnConfirm();
    }


    /// <summary>
    /// 获取选中的角色/武器/技能
    /// </summary>
    public CharacterData SelectedCharacter => _selectedCharacter;
    public WeaponData SelectedWeapon => _selectedWeapon;
    public SkillData SelectedSkill => _selectedSkill;
}