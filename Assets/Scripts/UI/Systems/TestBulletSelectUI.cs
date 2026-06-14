using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test 模式专属选择 UI — 子弹 / 一般强化 / 专属强化 三个标签页。
///
/// 一般强化：所有角色通用（攻速/暴击/护甲/移速/HP等）
/// 专属强化：当前角色专属（Mage 的 DOT 增强/引爆/元素等）
///
/// 使用方式：由 GameSceneBootstrap 在 TestMode 时动态创建
/// </summary>
public class TestBulletSelectUI : MonoBehaviour
{
    private MageUpgradeConfig _config;
    private System.Action<List<string>, Dictionary<string, int>, int> _onConfirmed;

    private Dictionary<string, bool> _bulletSelections = new Dictionary<string, bool>();
    private Dictionary<string, int> _upgradeSelections = new Dictionary<string, int>();

    private int _startWave = 1;
    private string _waveInput = "1";

    private Vector2 _bulletScrollPos;
    private Vector2 _generalScrollPos;
    private Vector2 _specificScrollPos;

    /// <summary>当前标签页：0=子弹, 1=一般强化, 2=专属强化</summary>
    private int _activeTab = 0;
    private bool _isVisible = true;

    // ── 一般强化类别（枚举值，自动匹配）──
    private static readonly HashSet<CharacterUpgradeOption.UpgradeCategory> _generalCategories = new HashSet<CharacterUpgradeOption.UpgradeCategory>
    {
        CharacterUpgradeOption.UpgradeCategory.MoveSpeed,
        CharacterUpgradeOption.UpgradeCategory.AttackSpeed,
        CharacterUpgradeOption.UpgradeCategory.BulletCount,
        CharacterUpgradeOption.UpgradeCategory.Ricochet,
    };

    // ── 分类后的升级缓存 ──
    private List<UpgradeEntry> _generalUpgrades = new List<UpgradeEntry>();
    private List<UpgradeEntry> _specificUpgrades = new List<UpgradeEntry>();

    private static Dictionary<string, Color> _categoryColorCache = new Dictionary<string, Color>();

    // ── 缓存 GUIStyle ──
    private bool _stylesInit;
    private GUIStyle _titleStyle;
    private GUIStyle _subtitleStyle;
    private GUIStyle _tabStyle;
    private GUIStyle _bulletNameStyle;
    private GUIStyle _bulletDescStyle;
    private GUIStyle _bulletStatStyle;
    private GUIStyle _upgradeNameStyle;
    private GUIStyle _upgradeDescStyle;
    private GUIStyle _upgradeStackStyle;
    private GUIStyle _upgradeInfoStyle;
    private GUIStyle _bottomBtnStyle;
    private GUIStyle _countStyle;
    private GUIStyle _confirmStyle;

    private void EnsureStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.2f, 0.9f) }
        };

        _subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        _tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };

        _bulletNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        _bulletDescStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
        _bulletStatStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperRight, normal = { textColor = new Color(0.5f, 0.9f, 0.5f) } };

        _upgradeNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        _upgradeDescStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
        _upgradeStackStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        _upgradeInfoStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleRight };

        _bottomBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };
        _countStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.85f, 0.35f) }
        };
        _confirmStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
    }

    // ── 布局常量 ──
    private const float SCREEN_W = 1920f;
    private const float SCREEN_H = 1080f;
    private const float MARGIN = 60f;
    private const float PANEL_W = SCREEN_W - MARGIN * 2;
    private const float PANEL_X = MARGIN;
    private const float PANEL_Y = 140f;
    private const float PANEL_H = SCREEN_H - PANEL_Y - 90f;

    public void Setup(MageUpgradeConfig config, System.Action<List<string>, Dictionary<string, int>, int> onConfirmed)
    {
        _config = config;
        _onConfirmed = onConfirmed;

        _bulletSelections.Clear();
        if (_config?.dotGunEntries != null)
        {
            for (int i = 0; i < _config.dotGunEntries.Length; i++)
                _bulletSelections[_config.dotGunEntries[i].upgradeId] = true;
        }

        _upgradeSelections.Clear();
        _generalUpgrades.Clear();
        _specificUpgrades.Clear();

        if (_config?.upgradeEntries != null)
        {
            for (int i = 0; i < _config.upgradeEntries.Length; i++)
            {
                var entry = _config.upgradeEntries[i];
                _upgradeSelections[entry.upgradeId] = 0;

                string cat = entry.category.ToString();
                if (_generalCategories.Contains(entry.category))
                    _generalUpgrades.Add(entry);
                else
                    _specificUpgrades.Add(entry);
            }
        }

        RebuildCategoryColorCache();
        DebugHelper.Log($"[TestBulletSelectUI] {_bulletSelections.Count} bullets, {_generalUpgrades.Count} general, {_specificUpgrades.Count} specific");
    }

    public void Refresh()
    {
        if (_config != null) Setup(_config, _onConfirmed);
    }

    private void RebuildCategoryColorCache()
    {
        _categoryColorCache.Clear();
        if (_config?.upgradeEntries == null) return;
        for (int i = 0; i < _config.upgradeEntries.Length; i++)
        {
            string cat = _config.upgradeEntries[i].category.ToString();
            if (!_categoryColorCache.ContainsKey(cat))
            {
                int h = cat.GetHashCode();
                float hue = (h & 0xFF) / 255f;
                float sat = 0.5f + ((h >> 8) & 0x7F) / 256f * 0.4f;
                float val = 0.7f + ((h >> 16) & 0x7F) / 256f * 0.3f;
                _categoryColorCache[cat] = Color.HSVToRGB(hue, sat, val);
            }
        }
    }

    // ═══ OnGUI ═══

    private void OnGUI()
    {
        if (!_isVisible || _config == null) return;
        EnsureStyles();
        GUIScaleHelper.BeginScale();

        // 全屏遮罩
        GUI.color = new Color(0, 0, 0, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, SCREEN_W, SCREEN_H), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, 15, SCREEN_W, 55), "🧪 TEST MODE", _titleStyle);
        GUI.Label(new Rect(0, 72, SCREEN_W, 30), "子弹多选 | 强化左键+ 右键- | 一般强化=全角色通用 | 专属强化=当前角色独有", _subtitleStyle);

        // 标签页
        DrawTabButtons();

        // 面板
        GUI.color = new Color(0.10f, 0.08f, 0.16f, 0.97f);
        GUI.DrawTexture(new Rect(PANEL_X, PANEL_Y, PANEL_W, PANEL_H), Texture2D.whiteTexture);
        GUI.color = Color.white;
        DrawBorder(new Rect(PANEL_X, PANEL_Y, PANEL_W, PANEL_H), new Color(0.6f, 0.2f, 0.9f, 0.7f), 3);

        // 内容
        switch (_activeTab)
        {
            case 0: DrawBulletPanel(); break;
            case 1: DrawUpgradePanel(_generalUpgrades, ref _generalScrollPos); break;
            case 2: DrawUpgradePanel(_specificUpgrades, ref _specificScrollPos); break;
        }

        DrawBottomButtons();
        GUIScaleHelper.EndScale();
    }

    // ═══ 标签页 ═══

    private void DrawTabButtons()
    {
        float tabY = 105f;
        float tabW = 220f;
        float tabH = 30f;
        float gap = 8f;
        float totalW = tabW * 3 + gap * 2;
        float startX = (SCREEN_W - totalW) / 2f;

        GUI.color = _activeTab == 0 ? new Color(0.6f, 0.2f, 0.9f) : new Color(0.25f, 0.2f, 0.35f);
        if (GUI.Button(new Rect(startX, tabY, tabW, tabH), $"🔫 子弹 ({GetSelectedBulletCount()})", _tabStyle))
            _activeTab = 0;

        GUI.color = _activeTab == 1 ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.2f, 0.3f, 0.2f);
        if (GUI.Button(new Rect(startX + tabW + gap, tabY, tabW, tabH), $"🛡 一般强化 ({GetCount(_generalUpgrades)})", _tabStyle))
            _activeTab = 1;

        GUI.color = _activeTab == 2 ? new Color(0.9f, 0.4f, 0.1f) : new Color(0.35f, 0.2f, 0.1f);
        if (GUI.Button(new Rect(startX + (tabW + gap) * 2, tabY, tabW, tabH), $"⚡ 专属强化 ({GetCount(_specificUpgrades)})", _tabStyle))
            _activeTab = 2;

        GUI.color = Color.white;
    }

    // ═══ 子弹面板 ═══

    private void DrawBulletPanel()
    {
        float pad = 20f;
        float itemH = 110f;
        float contentW = PANEL_W - pad * 2 - 20f;
        float totalH = _bulletSelections.Count * itemH + pad;

        Rect scrollRect = new Rect(PANEL_X + pad, PANEL_Y + pad, PANEL_W - pad * 2, PANEL_H - pad * 2 - 70f);
        _bulletScrollPos = GUI.BeginScrollView(scrollRect, _bulletScrollPos, new Rect(0, 0, contentW, totalH));

        float yPos = 0f;
        var entries = _config.dotGunEntries;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            string id = entry.upgradeId;
            if (!_bulletSelections.ContainsKey(id)) continue;

            Color itemBg = i % 2 == 0 ? new Color(0.14f, 0.11f, 0.22f, 0.85f) : new Color(0.17f, 0.13f, 0.25f, 0.85f);
            GUI.color = itemBg;
            GUI.DrawTexture(new Rect(5f, yPos, contentW - 10f, itemH - 6f), Texture2D.whiteTexture);

            GUI.color = entry.color;
            GUI.DrawTexture(new Rect(5f, yPos, 8f, itemH - 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            bool isSelected = _bulletSelections[id];
            if (GUI.Button(new Rect(5f, yPos, contentW - 10f, itemH - 6f), "", GUIStyle.none))
                _bulletSelections[id] = !isSelected;
            isSelected = _bulletSelections[id];

            GUI.color = isSelected ? new Color(0.3f, 1f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
            GUI.DrawTexture(new Rect(24f, yPos + 14f, 36f, 36f), Texture2D.whiteTexture);
            if (isSelected) { GUI.color = new Color(0.1f, 0.3f, 0.1f); GUI.DrawTexture(new Rect(28f, yPos + 18f, 28f, 28f), Texture2D.whiteTexture); }
            GUI.color = Color.white;

            _bulletNameStyle.normal.textColor = entry.color;
            GUI.Label(new Rect(72f, yPos + 10f, 500f, 35f), entry.displayName, _bulletNameStyle);

            GUI.Label(new Rect(72f, yPos + 46f, contentW - 100f, 55f), entry.description, _bulletDescStyle);

            GUI.Label(new Rect(contentW - 440f, yPos + 19f, 420f, 28f), $"冷却:{entry.cooldown:F1}s 冲击:{entry.impactDmg} DOT:{entry.dotDps:F1}/s 持续:{entry.dotDuration:F1}s", _bulletStatStyle);

            yPos += itemH;
        }
        GUI.EndScrollView();
    }

    // ═══ 强化面板（通用，一般/专属共用） ═══

    private void DrawUpgradePanel(List<UpgradeEntry> entries, ref Vector2 scrollPos)
    {
        float pad = 20f;
        float itemH = 110f;
        float contentW = PANEL_W - pad * 2 - 20f;
        float totalH = entries.Count * itemH + pad;

        Rect scrollRect = new Rect(PANEL_X + pad, PANEL_Y + pad, PANEL_W - pad * 2, PANEL_H - pad * 2 - 70f);
        scrollPos = GUI.BeginScrollView(scrollRect, scrollPos, new Rect(0, 0, contentW, totalH));

        float yPos = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            string id = entry.upgradeId;
            if (!_upgradeSelections.ContainsKey(id)) continue;

            int stacks = _upgradeSelections[id];
            bool active = stacks > 0;

            Color bg = active ? new Color(0.12f, 0.18f, 0.28f, 0.95f)
                : (i % 2 == 0 ? new Color(0.11f, 0.09f, 0.16f, 0.8f) : new Color(0.14f, 0.11f, 0.19f, 0.8f));
            GUI.color = bg;
            GUI.DrawTexture(new Rect(5f, yPos, contentW - 10f, itemH - 5f), Texture2D.whiteTexture);

            string catName = entry.category.ToString();
            Color catCol = _categoryColorCache.ContainsKey(catName) ? _categoryColorCache[catName] : new Color(0.5f, 0.5f, 0.5f);
            GUI.color = active ? catCol : new Color(catCol.r, catCol.g, catCol.b, 0.35f);
            GUI.DrawTexture(new Rect(5f, yPos, 8f, itemH - 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            Rect rowRect = new Rect(5f, yPos, contentW - 10f, itemH - 5f);
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    bool canInc = entry.maxStacks == 0 || stacks < entry.maxStacks;
                    if (canInc) _upgradeSelections[id] = stacks + 1;
                    Event.current.Use();
                }
                else if (Event.current.button == 1)
                {
                    if (stacks > 0) _upgradeSelections[id] = stacks - 1;
                    Event.current.Use();
                }
            }
            GUI.Button(rowRect, "", GUIStyle.none);
            stacks = _upgradeSelections[id];
            active = stacks > 0;

            _upgradeNameStyle.normal.textColor = active ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(new Rect(24f, yPos + 6f, contentW - 80f, 28f), entry.upgradeName, _upgradeNameStyle);

            _upgradeDescStyle.normal.textColor = active ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.45f, 0.45f, 0.45f);
            GUI.Label(new Rect(24f, yPos + 34f, contentW - 80f, 44f), entry.description, _upgradeDescStyle);

            _upgradeStackStyle.normal.textColor = active ? new Color(0.3f, 1f, 0.3f) : new Color(0.45f, 0.45f, 0.45f);
            string stackText = entry.maxStacks > 0 ? $"{stacks}/{entry.maxStacks}" : $"{stacks}";
            GUI.Label(new Rect(contentW - 220f, yPos + 20f, 200f, 40f), stackText, _upgradeStackStyle);

            _upgradeInfoStyle.normal.textColor = new Color(catCol.r, catCol.g, catCol.b, active ? 0.9f : 0.4f);
            string info = entry.maxStacks > 0 ? $"{catName} (上限{entry.maxStacks}) 左键+ 右键-" : $"{catName} 左键+ 右键-";
            GUI.Label(new Rect(contentW - 320f, yPos + 75f, 300f, 20f), info, _upgradeInfoStyle);

            yPos += itemH;
        }
        GUI.EndScrollView();
    }

    // ═══ 底部按钮 ═══

    private void DrawBottomButtons()
    {
        float btnY = PANEL_Y + PANEL_H - 65f;
        float btnH = 46f;
        float leftX = PANEL_X + 25f;

        if (_activeTab == 0)
        {
            GUI.color = new Color(0.4f, 0.35f, 0.7f);
            if (GUI.Button(new Rect(leftX, btnY, 130f, btnH), "全选子弹", _bottomBtnStyle))
                foreach (var key in new List<string>(_bulletSelections.Keys)) _bulletSelections[key] = true;
            GUI.color = new Color(0.6f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(leftX + 140f, btnY, 130f, btnH), "清空子弹", _bottomBtnStyle))
                foreach (var key in new List<string>(_bulletSelections.Keys)) _bulletSelections[key] = false;
        }
        else if (_activeTab == 1)
        {
            GUI.color = new Color(0.6f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(leftX, btnY, 140f, btnH), "清空一般", _bottomBtnStyle))
                foreach (var e in _generalUpgrades) _upgradeSelections[e.upgradeId] = 0;
            GUI.color = new Color(0.2f, 0.5f, 0.8f);
            if (GUI.Button(new Rect(leftX + 150f, btnY, 150f, btnH), "全设 5 层", _bottomBtnStyle))
                foreach (var e in _generalUpgrades) _upgradeSelections[e.upgradeId] = 5;
        }
        else
        {
            GUI.color = new Color(0.6f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(leftX, btnY, 140f, btnH), "清空专属", _bottomBtnStyle))
                foreach (var e in _specificUpgrades) _upgradeSelections[e.upgradeId] = 0;
            GUI.color = new Color(0.2f, 0.5f, 0.8f);
            if (GUI.Button(new Rect(leftX + 150f, btnY, 150f, btnH), "全设 5 层", _bottomBtnStyle))
                foreach (var e in _specificUpgrades) _upgradeSelections[e.upgradeId] = 5;
        }
        GUI.color = Color.white;

        // 统计 + 波次选择
        int bc = GetSelectedBulletCount();
        int gc = GetCount(_generalUpgrades);
        int sc = GetCount(_specificUpgrades);
        GUI.Label(new Rect(PANEL_X + PANEL_W / 2 - 350f, btnY + 2f, 300f, 24f),
            $"子弹:{bc}/{_bulletSelections.Count}  一般:{gc}  专属:{sc}", _countStyle);

        // 波次输入
        GUI.Label(new Rect(PANEL_X + PANEL_W / 2 + 10f, btnY + 2f, 60f, 24f), "起始波:", _countStyle);
        _waveInput = GUI.TextField(new Rect(PANEL_X + PANEL_W / 2 + 70f, btnY + 2f, 50f, 24f), _waveInput, _countStyle);
        if (int.TryParse(_waveInput, out int parsed) && parsed >= 1)
            _startWave = parsed;

        // 确认
        GUI.color = new Color(0.2f, 0.75f, 0.2f, 0.95f);
        if (GUI.Button(new Rect(PANEL_X + PANEL_W - 230f, btnY, 210f, btnH + 4f), "✓ 确认进入", _confirmStyle))
            ConfirmSelection();
        GUI.color = Color.white;
    }

    // ═══ 确认 ═══

    private void ConfirmSelection()
    {
        var bullets = new List<string>();
        foreach (var kvp in _bulletSelections)
            if (kvp.Value) bullets.Add(kvp.Key);

        var upgrades = new Dictionary<string, int>();
        foreach (var kvp in _upgradeSelections)
            if (kvp.Value > 0) upgrades[kvp.Key] = kvp.Value;

        DebugHelper.Log($"[TestBulletSelectUI] Confirmed: {bullets.Count} bullets, {upgrades.Count} upgrades, wave {_startWave}");
        _isVisible = false;
        _onConfirmed?.Invoke(bullets, upgrades, _startWave);
    }

    // ═══ 辅助 ═══

    private int GetSelectedBulletCount()
    {
        int c = 0; foreach (var kvp in _bulletSelections) if (kvp.Value) c++; return c;
    }
    private int GetCount(List<UpgradeEntry> entries)
    {
        int c = 0;
        foreach (var e in entries) if (_upgradeSelections.ContainsKey(e.upgradeId) && _upgradeSelections[e.upgradeId] > 0) c++;
        return c;
    }
    private void DrawBorder(Rect rect, Color color, int t)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - t, rect.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, t, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - t, rect.y, t, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
