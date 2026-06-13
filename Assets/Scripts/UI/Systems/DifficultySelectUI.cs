using UnityEngine;

/// <summary>
/// 难度选择UI — 在波次选择前显示难度滑块。
/// 已解锁的难度可选，未解锁的显示锁定状态。
/// </summary>
public class DifficultySelectUI : MonoBehaviour
{
    private int _selectedDifficulty = 1;
    private bool _visible = false;
    private GUIStyle _titleStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _lockStyle;
    private GUIStyle _descStyle;
    private GUIStyle _selectedStyle;
    private GUIStyle _confirmStyle;
    private bool _stylesInit = false;

    public static DifficultySelectUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        _selectedDifficulty = DifficultyManager.MaxUnlockedDifficulty;
    }

    public void Show() { _visible = true; _selectedDifficulty = DifficultyManager.MaxUnlockedDifficulty; }
    public void Hide() { _visible = false; }
    public bool IsVisible => _visible;

    public int SelectedDifficulty => _selectedDifficulty;

    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            fixedHeight = 50
        };

        _lockStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fixedHeight = 50,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };

        _descStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        _selectedStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            fixedHeight = 55
        };

        _confirmStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, fixedHeight = 50 };
    }

    private void OnGUI()
    {
        if (!_visible) return;
        InitStyles();

        DifficultyManager.EnsureInitialized();

        float panelW = 700;
        float panelH = 600;
        float x = (Screen.width - panelW) / 2f;
        float y = (Screen.height - panelH) / 2f;

        // 背景
        GUI.color = new Color(0, 0, 0, 0.9f);
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 标题
        GUI.Label(new Rect(x, y + 10, panelW, 40), "选择难度", _titleStyle);

        int maxUnlocked = DifficultyManager.MaxUnlockedDifficulty;
        float btnY = y + 60;

        for (int i = 1; i <= DifficultyManager.MaxDifficulty; i++)
        {
            var cfg = GetConfigForLevel(i);
            bool unlocked = i <= maxUnlocked;
            bool selected = i == _selectedDifficulty;

            string label = cfg != null ? $"[{i}] {cfg.difficultyName}" : $"[{i}] 难度{i}";
            string desc = cfg?.difficultyDescription ?? "";

            if (unlocked)
            {
                GUI.color = selected ? cfg.difficultyColor : Color.white;
                var style = selected ? _selectedStyle : _buttonStyle;
                if (GUI.Button(new Rect(x + 20, btnY, panelW - 40, style.fixedHeight), label, style))
                {
                    _selectedDifficulty = i;
                    DifficultyManager.CurrentDifficulty = i;
                }
                GUI.color = Color.white;

                if (selected && !string.IsNullOrEmpty(desc))
                {
                    GUI.Label(new Rect(x + 20, btnY + style.fixedHeight, panelW - 40, 25), desc, _descStyle);
                    btnY += 25;
                }
            }
            else
            {
                GUI.color = new Color(0.4f, 0.4f, 0.4f);
                GUI.Button(new Rect(x + 20, btnY, panelW - 40, _lockStyle.fixedHeight), $"🔒 难度{i} — 通关难度{i - 1}第20波解锁", _lockStyle);
                GUI.color = Color.white;
            }

            btnY += 55;
        }

        // 当前难度描述
        var currentCfg = GetConfigForLevel(_selectedDifficulty);
        if (currentCfg != null)
        {
            GUI.Label(new Rect(x + 20, btnY + 10, panelW - 40, 50),
                $"难度{_selectedDifficulty}: {currentCfg.difficultyDescription}", _descStyle);
        }

        // 确认按钮
        GUI.color = new Color(0.2f, 0.75f, 0.2f, 0.95f);
        if (GUI.Button(new Rect(x + panelW / 2 - 120, y + panelH - 65, 240, 50), "✓ 确认 (Enter)", _confirmStyle))
        {
            DifficultyManager.CurrentDifficulty = _selectedDifficulty;
            _visible = false;
        }
        GUI.color = Color.white;
    }

    private DifficultyConfig GetConfigForLevel(int level)
    {
        return DifficultyManager.GetConfigForLevel(level);
    }
}
