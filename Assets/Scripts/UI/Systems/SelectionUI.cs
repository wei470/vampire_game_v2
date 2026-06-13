using UnityEngine;
using System.Collections.Generic;

public class SelectionUI : MonoBehaviour
{
    public enum SelectPhase { Character, Skill, Done }

    [Header("当前状态")]
    [SerializeField] private SelectPhase _selectPhase = SelectPhase.Character;
    [SerializeField] private int _selectedChar = 0;
    [SerializeField] private int _selectedSkill = 0;

    private CharacterData[] _characters;
    private WeaponData[] _weapons;
    private SkillData[] _skills;

    private Texture2D _bgTex, _btnNormalTex, _btnSelectedTex, _btnConfirmTex, _btnHoverTex, _previewBgTex;

    private GUIStyle _titleLargeStyle;
    private GUIStyle _titleStepStyle;
    private GUIStyle _btnItemStyle;
    private GUIStyle _lockLabelStyle;
    private GUIStyle _previewInitialStyle;
    private GUIStyle _detailTitleStyle;
    private GUIStyle _detailDescStyle;
    private GUIStyle _detailStatsStyle;
    private GUIStyle _confirmBtnStyle;
    private bool _stylesInit;

    private void EnsureStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;
        _titleLargeStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        _titleStepStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        _btnItemStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, alignment = TextAnchor.MiddleLeft, hover = { background = _btnHoverTex } };
        _lockLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.6f, 0.5f, 0.3f) } };
        _previewInitialStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        _detailTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan }, wordWrap = true };
        _detailDescStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = UIColorTheme.TextPrimary }, wordWrap = true };
        _detailStatsStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, normal = { textColor = UIColorTheme.TextSecondary }, wordWrap = true, richText = true };
        _confirmBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = _btnConfirmTex }, hover = { textColor = UIColorTheme.AccentCyan, background = _btnHoverTex } };
    }

    public System.Action<int, int, int> OnSelectionConfirmed;
    public SelectPhase CurrentPhase => _selectPhase;
    public bool IsDone => _selectPhase == SelectPhase.Done;
    private const float LEFT_RATIO = 0.35f, CENTER_RATIO = 0.28f;

    public void Setup(CharacterData[] c, WeaponData[] w, SkillData[] s) { _characters = c ?? new CharacterData[0]; _weapons = w ?? new WeaponData[0]; _skills = s ?? new SkillData[0]; InitTextures(); }
    public void SetPreSelection(int a, int b, int c) { _selectedChar = a; _selectedSkill = c; }
    public void ConfirmSelection() { switch (_selectPhase) { case SelectPhase.Character: _selectPhase = SelectPhase.Skill; break; case SelectPhase.Skill: _selectPhase = SelectPhase.Done; OnSelectionConfirmed?.Invoke(_selectedChar, 0, _selectedSkill); break; } }

    private void InitTextures() { _bgTex = UIColorTheme.MakeTexture(UIColorTheme.OverlayDark); _btnNormalTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonNormal); _btnSelectedTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonSelected); _btnConfirmTex = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan); _btnHoverTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonHover); _previewBgTex = UIColorTheme.MakeTexture(UIColorTheme.PanelBackground); }

    public void DrawSelectionUI()
    {
        if (IsDone) return; if (_bgTex == null) InitTextures();
        EnsureStyles();
        float sw = Screen.width, sh = Screen.height;
        GUI.color = UIColorTheme.OverlayDark; GUI.DrawTexture(new Rect(0, 0, sw, sh), _bgTex); GUI.color = Color.white;
        float leftW = sw * LEFT_RATIO, centerW = sw * CENTER_RATIO, rightW = sw - leftW - centerW;
        DrawTitleBar(sw);
        float topY = 110f, bottomAreaH = sh - topY - 120f;
        GUILayout.BeginArea(new Rect(8, topY, leftW - 16, bottomAreaH)); DrawButtonList(); GUILayout.EndArea();
        DrawPreviewArea(new Rect(leftW, topY, centerW, bottomAreaH));
        DrawDetailArea(new Rect(leftW + centerW, topY, rightW - 8, bottomAreaH));
        DrawBottomBar(sw, sh);
    }

    private void DrawTitleBar(float sw)
    {
        var ts = _titleLargeStyle;
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(0, 10, sw, 40), "Vampire Survivors", ts); GUI.color = Color.white;
        string st = _selectPhase switch { SelectPhase.Character => "Step 1/2: Choose Character", SelectPhase.Skill => "Step 2/2: Choose Skill", _ => "" };
        var ss = _titleStepStyle;
        GUI.Label(new Rect(0, 55, sw, 30), st, ss);
        GUI.color = UIColorTheme.PanelBackground; GUI.DrawTexture(new Rect(sw * 0.1f, 90, sw * 0.8f, 2), _previewBgTex); GUI.color = Color.white;
    }

    private void DrawButtonList()
    {
        int count, si;
        switch (_selectPhase) { case SelectPhase.Character: count = _characters.Length; si = _selectedChar; break; case SelectPhase.Skill: count = _skills.Length; si = _selectedSkill; break; default: return; }
        float aw = Screen.width * LEFT_RATIO - 16f, btnH = 60f, sp = btnH + 14f;
        for (int i = 0; i < count; i++)
        {
            bool sel = i == si;
            // #42 检查角色解锁状态
            bool isLocked = false;
            string lockLabel = "";
            if (_selectPhase == SelectPhase.Character && i < _characters.Length && _characters[i] != null)
            {
                isLocked = !CharacterUnlockManager.IsCharacterUnlocked(_characters[i]);
                if (isLocked) lockLabel = CharacterUnlockManager.GetUnlockConditionText(_characters[i]);
            }

            Rect br = new Rect(0, i * sp, aw, btnH);
            // #42 锁定角色使用灰色样式
            Color nameColor = isLocked ? new Color(0.4f, 0.4f, 0.4f) : (sel ? UIColorTheme.AccentCyan : UIColorTheme.TextPrimary);
            _btnItemStyle.normal.textColor = nameColor;
            _btnItemStyle.normal.background = sel ? _btnSelectedTex : _btnNormalTex;
            _btnItemStyle.hover.textColor = isLocked ? nameColor : UIColorTheme.AccentCyan;
            if (sel && !isLocked) UIColorTheme.DrawButtonGlow(br);
            GUI.color = Color.white;

            string btnLabel = $"  {(i + 1)}. {GetItemName(i)}";
            if (isLocked) btnLabel = $"  🔒 {GetItemName(i)}";

            if (GUI.Button(br, btnLabel, _btnItemStyle))
            {
                if (!isLocked) SetSelection(i);
            }

            // #42 锁定角色显示解锁条件
            if (isLocked && !string.IsNullOrEmpty(lockLabel))
            {
                GUI.Label(new Rect(10, i * sp + btnH - 16, aw - 20, 16), lockLabel, _lockLabelStyle);
            }
        }
    }

    private string GetItemName(int i) => _selectPhase switch { SelectPhase.Character => i < _characters.Length && _characters[i] != null ? _characters[i].characterName : "???", SelectPhase.Skill => i < _skills.Length && _skills[i] != null ? _skills[i].skillName : "???", _ => "???" };
    private void SetSelection(int i) { switch (_selectPhase) { case SelectPhase.Character: _selectedChar = i; break; case SelectPhase.Skill: _selectedSkill = i; break; } }

    private void DrawPreviewArea(Rect a)
    {
        GUI.color = UIColorTheme.DarkBackground; GUI.DrawTexture(a, _previewBgTex);
        Sprite icon = GetSelectedIcon();
        if (icon != null)
        {
            float s = Mathf.Min(a.width, a.height) * 0.6f;
            GUI.DrawTexture(new Rect(a.x + (a.width - s) / 2f, a.y + (a.height - s) / 2f - 20f, s, s), icon.texture);
        }
        else
        {
            Color bc = GetSelectedColor();
            float s = Mathf.Min(a.width, a.height) * 0.5f, bx = a.x + (a.width - s) / 2f, by = a.y + (a.height - s) / 2f - 20f;
            GUI.color = bc; GUI.DrawTexture(new Rect(bx, by, s, s), Texture2D.whiteTexture);
            GUI.color = Color.white; GUI.Label(new Rect(bx, by, s, s), GetSelectedInitial(), _previewInitialStyle);
        }
        GUI.color = Color.white;
    }

    private Sprite GetSelectedIcon() => _selectPhase switch { SelectPhase.Character => _selectedChar < _characters.Length && _characters[_selectedChar] != null ? _characters[_selectedChar].icon : null, SelectPhase.Skill => _selectedSkill < _skills.Length && _skills[_selectedSkill] != null ? _skills[_selectedSkill].icon : null, _ => null };
    private string GetSelectedName() => _selectPhase switch { SelectPhase.Character => _selectedChar < _characters.Length && _characters[_selectedChar] != null ? _characters[_selectedChar].characterName : "???", SelectPhase.Skill => _selectedSkill < _skills.Length && _skills[_selectedSkill] != null ? _skills[_selectedSkill].skillName : "???", _ => "???" };
    private Color GetSelectedColor() => _selectPhase switch { SelectPhase.Character => _selectedChar < _characters.Length && _characters[_selectedChar] != null ? _characters[_selectedChar].characterColor : UIColorTheme.AccentCyan, _ => UIColorTheme.AccentCyan };
    private string GetSelectedInitial() { string n = GetSelectedName(); return string.IsNullOrEmpty(n) ? "?" : n[0].ToString().ToUpper(); }

    private void DrawDetailArea(Rect a)
    {
        GUI.color = UIColorTheme.DarkBackground; GUI.DrawTexture(a, _previewBgTex);
        float pad = 16f, y = a.y + pad;
        string t, d, e;
        switch (_selectPhase)
        {
            case SelectPhase.Character:
                if (_selectedChar < _characters.Length && _characters[_selectedChar] != null) { var c = _characters[_selectedChar]; t = c.characterName; d = c.description; e = $"HP: {c.maxHP}  |  SPD: {c.moveSpeed:F1}  |  ARM: {c.armor}\nATK: {c.attackDamage}  |  Crit: {c.critChance:P0}\n\nPassive: {c.passiveDescription}"; }
                else { t = "???"; d = ""; e = ""; } break;
            case SelectPhase.Skill:
                if (_selectedSkill < _skills.Length && _skills[_selectedSkill] != null) { var s = _skills[_selectedSkill]; t = s.skillName; d = s.description; e = $"Type: {s.skillType}\nDMG: {s.baseDamage}  |  CD: {s.cooldown:F1}s\nDuration: {s.duration:F1}s  |  Radius: {s.effectRadius:F1}"; }
                else { t = "???"; d = ""; e = ""; } break;
            default: return;
        }
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(a.x + pad, y, a.width - pad * 2, 36), t, _detailTitleStyle); y += 43;
        GUI.color = UIColorTheme.TextPrimary; float dh = _detailDescStyle.CalcHeight(new GUIContent(d), a.width - pad * 2);
        GUI.Label(new Rect(a.x + pad, y, a.width - pad * 2, dh), d, _detailDescStyle); y += dh + 16;
        GUI.color = UIColorTheme.PanelBackground; GUI.DrawTexture(new Rect(a.x + pad, y, a.width - pad * 2, 1), _previewBgTex); y += 10;
        GUI.color = UIColorTheme.TextSecondary; GUI.Label(new Rect(a.x + pad, y, a.width - pad * 2, a.height - (y - a.y) - pad), e, _detailStatsStyle);
        GUI.color = Color.white;
    }

    private void DrawBottomBar(float sw, float sh)
    {
        float w = 220f, h = 60f, cx = (sw - w) / 2f, cy = sh - h - 40f;
        GUI.color = Color.white;
        if (GUI.Button(new Rect(cx, cy, w, h), _selectPhase == SelectPhase.Skill ? "Start" : "Confirm", _confirmBtnStyle)) ConfirmSelection();
        GUI.color = Color.white;
    }
}