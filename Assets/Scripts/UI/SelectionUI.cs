using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 选择界面（IMGUI）— 管理角色/武器/技能的三步选择流程。
/// 
/// 从 GameSceneBootstrap 中拆分而来，独立负责选择界面的渲染和交互。
/// 选择完成后通过回调通知 GameSceneBootstrap。
/// </summary>
public class SelectionUI : MonoBehaviour
{
    /// <summary>
    /// 选择阶段
    /// </summary>
    public enum SelectPhase { Character, Weapon, Skill, Done }

    [Header("当前状态")]
    [SerializeField] private SelectPhase _selectPhase = SelectPhase.Character;
    [SerializeField] private int _selectedChar = 0;
    [SerializeField] private int _selectedWeapon = 0;
    [SerializeField] private int _selectedSkill = 0;

    private CharacterData[] _characters;
    private WeaponData[] _weapons;
    private SkillData[] _skills;

    /// <summary>
    /// 选择完成回调（charIndex, weaponIndex, skillIndex）
    /// </summary>
    public System.Action<int, int, int> OnSelectionConfirmed;

    /// <summary>
    /// 当前选择阶段
    /// </summary>
    public SelectPhase CurrentPhase => _selectPhase;

    /// <summary>
    /// 选择是否已完成
    /// </summary>
    public bool IsDone => _selectPhase == SelectPhase.Done;

    /// <summary>
    /// 初始化选择数据
    /// </summary>
    public void Setup(CharacterData[] characters, WeaponData[] weapons, SkillData[] skills)
    {
        _characters = characters ?? new CharacterData[0];
        _weapons = weapons ?? new WeaponData[0];
        _skills = skills ?? new SkillData[0];
    }

    /// <summary>
    /// 设置预选（用于测试模式）
    /// </summary>
    public void SetPreSelection(int charIdx, int weaponIdx, int skillIdx)
    {
        _selectedChar = charIdx;
        _selectedWeapon = weaponIdx;
        _selectedSkill = skillIdx;
    }

    /// <summary>
    /// 确认当前选择，进入下一阶段
    /// </summary>
    public void ConfirmSelection()
    {
        switch (_selectPhase)
        {
            case SelectPhase.Character:
                _selectPhase = SelectPhase.Weapon;
                break;
            case SelectPhase.Weapon:
                _selectPhase = SelectPhase.Skill;
                break;
            case SelectPhase.Skill:
                _selectPhase = SelectPhase.Done;
                OnSelectionConfirmed?.Invoke(_selectedChar, _selectedWeapon, _selectedSkill);
                break;
        }
    }

    /// <summary>
    /// 渲染选择界面（在 OnGUI 中调用）
    /// </summary>
    public void DrawSelectionUI()
    {
        if (IsDone) return;

        // 全屏半透明背景
        GUI.color = Color.white;
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

        float panelW = 700;
        float panelH = Screen.height - 80;
        float panelX = (Screen.width - panelW) / 2f;
        float panelY = 40;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH));

        // ── 标题 ──
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        GUI.color = new Color(0.9f, 0.2f, 0.2f);
        GUILayout.Label("Vampire Survivors", titleStyle);
        GUILayout.Space(10);

        // ── 步骤标题 ──
        var stepStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        switch (_selectPhase)
        {
            case SelectPhase.Character:
                GUI.color = Color.yellow;
                GUILayout.Label("Step 1/3: Choose Character", stepStyle);
                GUILayout.Space(10);
                DrawCharacterSelection();
                break;
            case SelectPhase.Weapon:
                GUI.color = Color.cyan;
                GUILayout.Label("Step 2/3: Choose Weapon", stepStyle);
                GUILayout.Space(10);
                DrawWeaponSelection();
                break;
            case SelectPhase.Skill:
                GUI.color = new Color(0.5f, 1f, 0.5f);
                GUILayout.Label("Step 3/3: Choose Skill", stepStyle);
                GUILayout.Space(10);
                DrawSkillSelection();
                break;
        }

        GUILayout.Space(20);

        // ── 确认按钮 ──
        GUI.color = new Color(0.2f, 0.7f, 0.3f);
        var btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        string btnText = _selectPhase == SelectPhase.Skill ? ">>> Start Game! <<<" : "Confirm  (Enter/Space)";
        if (GUILayout.Button(btnText, btnStyle, GUILayout.Height(50)))
        {
            ConfirmSelection();
        }

        // ── 操作提示 ──
        GUILayout.Space(10);
        GUI.color = Color.gray;
        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label("Click to select  |  Enter/Space to confirm", hintStyle);

        GUILayout.EndArea();
    }

    private void DrawCharacterSelection()
    {
        var nameStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
        var descStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };

        for (int i = 0; i < _characters.Length; i++)
        {
            if (_characters[i] == null) continue;
            var c = _characters[i];

            bool selected = (i == _selectedChar);
            GUI.color = selected ? new Color(0.3f, 0.5f, 0.8f) : new Color(0.15f, 0.15f, 0.2f);

            if (GUILayout.Button($"  {(i + 1)}. {c.characterName}" +
                $"  (HP:{c.maxHP} SPD:{c.moveSpeed:F1} ARM:{c.armor} ATK:{c.attackDamage})",
                nameStyle, GUILayout.Height(35)))
            {
                _selectedChar = i;
            }
        }

        // 显示选中角色详情
        if (_selectedChar < _characters.Length && _characters[_selectedChar] != null)
        {
            var c = _characters[_selectedChar];
            GUILayout.Space(10);
            GUI.color = new Color(0.8f, 0.8f, 0.9f);
            GUILayout.Label($"Passive: {c.passiveDescription}", descStyle);
        }
    }

    private void DrawWeaponSelection()
    {
        var nameStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
        var descStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] == null) continue;
            var w = _weapons[i];

            bool selected = (i == _selectedWeapon);
            GUI.color = selected ? new Color(0.3f, 0.6f, 0.8f) : new Color(0.15f, 0.15f, 0.2f);

            if (GUILayout.Button($"  {(i + 1)}. {w.weaponName}  (DMG:{w.baseDamage} CD:{w.cooldown:F1}s Type:{w.projectileType})",
                nameStyle, GUILayout.Height(35)))
            {
                _selectedWeapon = i;
            }
        }

        if (_selectedWeapon < _weapons.Length && _weapons[_selectedWeapon] != null)
        {
            var w = _weapons[_selectedWeapon];
            GUILayout.Space(10);
            GUI.color = new Color(0.8f, 0.8f, 0.9f);
            GUILayout.Label($"Desc: {w.description}", descStyle);
        }
    }

    private void DrawSkillSelection()
    {
        var nameStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
        var descStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };

        for (int i = 0; i < _skills.Length; i++)
        {
            if (_skills[i] == null) continue;
            var s = _skills[i];

            bool selected = (i == _selectedSkill);
            GUI.color = selected ? new Color(0.3f, 0.7f, 0.4f) : new Color(0.15f, 0.15f, 0.2f);

            if (GUILayout.Button($"  {(i + 1)}. {s.skillName}  (DMG:{s.baseDamage} CD:{s.cooldown:F1}s Type:{s.skillType})",
                nameStyle, GUILayout.Height(35)))
            {
                _selectedSkill = i;
            }
        }

        if (_selectedSkill < _skills.Length && _skills[_selectedSkill] != null)
        {
            var s = _skills[_selectedSkill];
            GUILayout.Space(10);
            GUI.color = new Color(0.8f, 0.8f, 0.9f);
            GUILayout.Label($"Desc: {s.description}", descStyle);
        }
    }
}