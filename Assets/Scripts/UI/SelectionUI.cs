using UnityEngine;
using System.Collections.Generic;

public class SelectionUI : MonoBehaviour
{
    public enum SelectPhase { Character, Weapon, Skill, Done }

    [Header("当前状态")]
    [SerializeField] private SelectPhase _selectPhase = SelectPhase.Character;
    [SerializeField] private int _selectedChar = 0;
    [SerializeField] private int _selectedWeapon = 0;
    [SerializeField] private int _selectedSkill = 0;

    private CharacterData[] _characters;
    private WeaponData[] _weapons;
    private SkillData[] _skills;

    private Texture2D _bgTex, _btnNormalTex, _btnSelectedTex, _btnConfirmTex, _btnHoverTex, _previewBgTex;
    public System.Action<int, int, int> OnSelectionConfirmed;
    public SelectPhase CurrentPhase => _selectPhase;
    public bool IsDone => _selectPhase == SelectPhase.Done;
    private const float LEFT_RATIO = 0.35f, CENTER_RATIO = 0.28f;

    public void Setup(CharacterData[] c, WeaponData[] w, SkillData[] s) { _characters = c ?? new CharacterData[0]; _weapons = w ?? new WeaponData[0]; _skills = s ?? new SkillData[0]; InitTextures(); }
    public void SetPreSelection(int a, int b, int c) { _selectedChar = a; _selectedWeapon = b; _selectedSkill = c; }
    public void ConfirmSelection() { switch (_selectPhase) { case SelectPhase.Character: _selectPhase = SelectPhase.Weapon; break; case SelectPhase.Weapon: _selectPhase = SelectPhase.Skill; break; case SelectPhase.Skill: _selectPhase = SelectPhase.Done; OnSelectionConfirmed?.Invoke(_selectedChar, _selectedWeapon, _selectedSkill); break; } }

    private void InitTextures() { _bgTex = UIColorTheme.MakeTexture(UIColorTheme.OverlayDark); _btnNormalTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonNormal); _btnSelectedTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonSelected); _btnConfirmTex = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan); _btnHoverTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonHover); _previewBgTex = UIColorTheme.MakeTexture(UIColorTheme.PanelBackground); }

    public void DrawSelectionUI()
    {
        if (IsDone) return; if (_bgTex == null) InitTextures();
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
        var ts = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(0, 10, sw, 40), "Vampire Survivors", ts); GUI.color = Color.white;
        string st = _selectPhase switch { SelectPhase.Character => "Step 1/3: Choose Character", SelectPhase.Weapon => "Step 2/3: Choose Weapon", SelectPhase.Skill => "Step 3/3: Choose Skill", _ => "" };
        var ss = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        GUI.Label(new Rect(0, 55, sw, 30), st, ss);
        GUI.color = UIColorTheme.PanelBackground; GUI.DrawTexture(new Rect(sw * 0.1f, 90, sw * 0.8f, 2), _previewBgTex); GUI.color = Color.white;
    }

    private void DrawButtonList()
    {
        int count, si;
        switch (_selectPhase) { case SelectPhase.Character: count = _characters.Length; si = _selectedChar; break; case SelectPhase.Weapon: count = _weapons.Length; si = _selectedWeapon; break; case SelectPhase.Skill: count = _skills.Length; si = _selectedSkill; break; default: return; }
        float aw = Screen.width * LEFT_RATIO - 16f, btnH = 60f, sp = btnH + 14f;
        for (int i = 0; i < count; i++)
        {
            bool sel = i == si;
            Rect br = new Rect(0, i * sp, aw, btnH);
            var bs = new GUIStyle(GUI.skin.button) { fontSize = 24, alignment = TextAnchor.MiddleLeft, normal = { textColor = sel ? UIColorTheme.AccentCyan : UIColorTheme.TextPrimary, background = sel ? _btnSelectedTex : _btnNormalTex }, hover = { textColor = UIColorTheme.AccentCyan, background = _btnHoverTex } };
            if (sel) UIColorTheme.DrawButtonGlow(br);
            GUI.color = Color.white;
            if (GUI.Button(br, $"  {(i + 1)}. {GetItemName(i)}", bs)) SetSelection(i);
        }
    }

    private string GetItemName(int i) => _selectPhase switch { SelectPhase.Character => i < _characters.Length && _characters[i] != null ? _characters[i].characterName : "???", SelectPhase.Weapon => i < _weapons.Length && _weapons[i] != null ? _weapons[i].weaponName : "???", SelectPhase.Skill => i < _skills.Length && _skills[i] != null ? _skills[i].skillName : "???", _ => "???" };
    private void SetSelection(int i) { switch (_selectPhase) { case SelectPhase.Character: _selectedChar = i; break; case SelectPhase.Weapon: _selectedWeapon = i; break; case SelectPhase.Skill: _selectedSkill = i; break; } }

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
            var ps = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            GUI.color = Color.white; GUI.Label(new Rect(bx, by, s, s), GetSelectedInitial(), ps);
        }
        GUI.color = Color.white;
    }

    private Sprite GetSelectedIcon() => _selectPhase switch { SelectPhase.Character => _selectedChar < _characters.Length && _characters[_selectedChar] != null ? _characters[_selectedChar].icon : null, SelectPhase.Weapon => _selectedWeapon < _weapons.Length && _weapons[_selectedWeapon] != null ? _weapons[_selectedWeapon].icon : null, SelectPhase.Skill => _selectedSkill < _skills.Length && _skills[_selectedSkill] != null ? _skills[_selectedSkill].icon : null, _ => null };
    private string GetSelectedName() => _selectPhase switch { SelectPhase.Character => _selectedChar < _characters.Length && _characters[_selectedChar] != null ? _characters[_selectedChar].characterName : "???", SelectPhase.Weapon => _selectedWeapon < _weapons.Length && _weapons[_selectedWeapon] != null ? _weapons[_selectedWeapon].weaponName : "???", SelectPhase.Skill => _selectedSkill < _skills.Length && _skills[_selectedSkill] != null ? _skills[_selectedSkill].skillName : "???", _ => "???" };
    private Color GetSelectedColor() => _selectPhase switch { SelectPhase.Character => _selectedChar < _characters.Length && _characters[_selectedChar] != null ? _characters[_selectedChar].characterColor : UIColorTheme.AccentCyan, SelectPhase.Weapon => _selectedWeapon < _weapons.Length && _weapons[_selectedWeapon] != null ? _weapons[_selectedWeapon].projectileColor : UIColorTheme.AccentCyan, _ => UIColorTheme.AccentCyan };
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
            case SelectPhase.Weapon:
                if (_selectedWeapon < _weapons.Length && _weapons[_selectedWeapon] != null) { var w = _weapons[_selectedWeapon]; t = w.weaponName; d = w.description; e = $"Type: {w.projectileType}\nDMG: {w.baseDamage}  |  CD: {w.cooldown:F1}s\nSpeed: {w.projectileSpeed}  |  Pierce: {w.pierce}"; }
                else { t = "???"; d = ""; e = ""; } break;
            case SelectPhase.Skill:
                if (_selectedSkill < _skills.Length && _skills[_selectedSkill] != null) { var s = _skills[_selectedSkill]; t = s.skillName; d = s.description; e = $"Type: {s.skillType}\nDMG: {s.baseDamage}  |  CD: {s.cooldown:F1}s\nDuration: {s.duration:F1}s  |  Radius: {s.effectRadius:F1}"; }
                else { t = "???"; d = ""; e = ""; } break;
            default: return;
        }
        var ts = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan }, wordWrap = true };
        GUI.color = UIColorTheme.AccentCyan; GUI.Label(new Rect(a.x + pad, y, a.width - pad * 2, 36), t, ts); y += 43;
        var ds = new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = UIColorTheme.TextPrimary }, wordWrap = true };
        GUI.color = UIColorTheme.TextPrimary; float dh = ds.CalcHeight(new GUIContent(d), a.width - pad * 2);
        GUI.Label(new Rect(a.x + pad, y, a.width - pad * 2, dh), d, ds); y += dh + 16;
        GUI.color = UIColorTheme.PanelBackground; GUI.DrawTexture(new Rect(a.x + pad, y, a.width - pad * 2, 1), _previewBgTex); y += 10;
        var es = new GUIStyle(GUI.skin.label) { fontSize = 19, normal = { textColor = UIColorTheme.TextSecondary }, wordWrap = true, richText = true };
        GUI.color = UIColorTheme.TextSecondary; GUI.Label(new Rect(a.x + pad, y, a.width - pad * 2, a.height - (y - a.y) - pad), e, es);
        GUI.color = Color.white;
    }

    private void DrawBottomBar(float sw, float sh)
    {
        float w = 220f, h = 60f, cx = (sw - w) / 2f, cy = sh - h - 40f;
        GUI.color = Color.white;
        var bs = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = _btnConfirmTex }, hover = { textColor = UIColorTheme.AccentCyan, background = _btnHoverTex } };
        if (GUI.Button(new Rect(cx, cy, w, h), _selectPhase == SelectPhase.Skill ? "Start" : "Confirm", bs)) ConfirmSelection();
        GUI.color = Color.white;
    }
}