using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test 模式专属选择 UI — 在游戏开始前让开发者选择要携带的 DOT 子弹 + 升级属性强化。
///
/// 功能：
/// 1. 显示所有可用的 DOT 子弹（从 MageUpgradeConfig.dotGunEntries 自动读取）
/// 2. 显示所有可用的属性强化（从 MageUpgradeConfig.upgradeEntries 自动读取）
/// 3. 子弹：勾选/取消 | 强化：点击增加叠加次数，右键减少
/// 4. 确认后进入游戏，同时应用子弹和强化
///
/// 使用方式：由 GameSceneBootstrap 在 TestMode 时动态创建
/// </summary>
public class TestBulletSelectUI : MonoBehaviour
{
    private MageUpgradeConfig _config;
    private System.Action<List<string>, Dictionary<string, int>> _onConfirmed;

    /// <summary>每种子弹的勾选状态（upgradeId → 是否选中）</summary>
    private Dictionary<string, bool> _bulletSelections = new Dictionary<string, bool>();

    /// <summary>每个升级的叠加次数（upgradeId → 叠加次数，0=未选）</summary>
    private Dictionary<string, int> _upgradeSelections = new Dictionary<string, int>();

    /// <summary>滚动位置</summary>
    private Vector2 _bulletScrollPos;
    private Vector2 _upgradeScrollPos;

    /// <summary>当前标签页：0=子弹, 1=强化</summary>
    private int _activeTab = 0;

    private bool _isVisible = true;

    // ── 布局常量（全屏利用 1920×1080）──
    private const float SCREEN_W = 1920f;
    private const float SCREEN_H = 1080f;
    private const float MARGIN = 60f;
    private const float PANEL_W = SCREEN_W - MARGIN * 2; // 1800
    private const float PANEL_X = MARGIN;                // 60
    private const float PANEL_Y = 140f;
    private const float PANEL_H = SCREEN_H - PANEL_Y - 90f; // 850

    /// <summary>
    /// 初始化选择 UI
    /// </summary>
    public void Setup(MageUpgradeConfig config, System.Action<List<string>, Dictionary<string, int>> onConfirmed)
    {
        _config = config;
        _onConfirmed = onConfirmed;

        _bulletSelections.Clear();
        if (_config != null && _config.dotGunEntries != null)
        {
            for (int i = 0; i < _config.dotGunEntries.Length; i++)
                _bulletSelections[_config.dotGunEntries[i].upgradeId] = true;
        }

        _upgradeSelections.Clear();
        if (_config != null && _config.upgradeEntries != null)
        {
            for (int i = 0; i < _config.upgradeEntries.Length; i++)
                _upgradeSelections[_config.upgradeEntries[i].upgradeId] = 0;
        }

        DebugHelper.Log($"[TestBulletSelectUI] Initialized: {_bulletSelections.Count} bullets, {_upgradeSelections.Count} upgrades");
    }

    private void OnGUI()
    {
        if (!_isVisible || _config == null) return;

        GUIScaleHelper.BeginScale();

        // ── 全屏半透明遮罩 ──
        GUI.color = new Color(0, 0, 0, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, SCREEN_W, SCREEN_H), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── 标题 ──
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.2f, 0.9f) }
        };
        GUI.Label(new Rect(0, 15, SCREEN_W, 55), "🧪 TEST MODE — 自定义配置", titleStyle);

        // ── 副标题 ──
        var subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        GUI.Label(new Rect(0, 72, SCREEN_W, 35), "Mage + Teleport | 子弹多选 | 强化点击叠加（右键减少）", subtitleStyle);

        // ── 标签页按钮 ──
        DrawTabButtons();

        // ── 面板背景 + 边框 ──
        GUI.color = new Color(0.10f, 0.08f, 0.16f, 0.97f);
        GUI.DrawTexture(new Rect(PANEL_X, PANEL_Y, PANEL_W, PANEL_H), Texture2D.whiteTexture);
        GUI.color = Color.white;
        DrawBorder(new Rect(PANEL_X, PANEL_Y, PANEL_W, PANEL_H), new Color(0.6f, 0.2f, 0.9f, 0.7f), 3);

        // ── 内容 ──
        if (_activeTab == 0)
            DrawBulletPanel();
        else
            DrawUpgradePanel();

        // ── 底部按钮 ──
        DrawBottomButtons();

        GUIScaleHelper.EndScale();
    }

    // ════════════════════════════════════════════════════════════════
    // 标签页
    // ════════════════════════════════════════════════════════════════

    private void DrawTabButtons()
    {
        float tabY = 110f;
        float tabW = 260f;
        float tabH = 30f;
        float centerX = SCREEN_W / 2f;

        var tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };

        GUI.color = _activeTab == 0 ? new Color(0.6f, 0.2f, 0.9f) : new Color(0.25f, 0.2f, 0.35f);
        if (GUI.Button(new Rect(centerX - tabW - 5f, tabY, tabW, tabH), $"🔫 子弹 ({GetSelectedBulletCount()})", tabStyle))
            _activeTab = 0;

        GUI.color = _activeTab == 1 ? new Color(0.2f, 0.6f, 0.9f) : new Color(0.2f, 0.25f, 0.35f);
        if (GUI.Button(new Rect(centerX + 5f, tabY, tabW, tabH), $"⚡ 强化 ({GetSelectedUpgradeCount()})", tabStyle))
            _activeTab = 1;

        GUI.color = Color.white;
    }

    // ════════════════════════════════════════════════════════════════
    // 子弹选择面板
    // ════════════════════════════════════════════════════════════════

    private void DrawBulletPanel()
    {
        float pad = 20f;
        float itemH = 110f;
        float contentW = PANEL_W - pad * 2 - 20f; // 留滚动条空间
        float totalH = _bulletSelections.Count * itemH + pad;

        Rect scrollRect = new Rect(PANEL_X + pad, PANEL_Y + pad, PANEL_W - pad * 2, PANEL_H - pad * 2 - 70f);
        Rect scrollView = new Rect(0, 0, contentW, totalH);

        _bulletScrollPos = GUI.BeginScrollView(scrollRect, _bulletScrollPos, scrollView);

        float yPos = 0f;
        var entries = _config.dotGunEntries;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            string id = entry.upgradeId;
            if (!_bulletSelections.ContainsKey(id)) continue;

            // 交替背景
            Color itemBg = i % 2 == 0
                ? new Color(0.14f, 0.11f, 0.22f, 0.85f)
                : new Color(0.17f, 0.13f, 0.25f, 0.85f);
            GUI.color = itemBg;
            GUI.DrawTexture(new Rect(5f, yPos, contentW - 10f, itemH - 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 彩色条
            GUI.color = entry.color;
            GUI.DrawTexture(new Rect(5f, yPos, 8f, itemH - 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 勾选框
            bool isSelected = _bulletSelections[id];
            float checkY = yPos + 14f;
            _bulletSelections[id] = GUI.Toggle(new Rect(24f, checkY, 36f, 36f), isSelected, "");

            // 名称
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = entry.color }
            };
            GUI.Label(new Rect(72f, checkY - 4f, 500f, 35f), entry.displayName, nameStyle);

            // 描述
            var descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            GUI.Label(new Rect(72f, checkY + 32f, contentW - 100f, 55f), entry.description, descStyle);

            // 属性标签
            var statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = new Color(0.5f, 0.9f, 0.5f) }
            };
            string stats = $"冷却: {entry.cooldown:F1}s | 冲击: {entry.impactDmg} | DOT: {entry.dotDps:F1}/s | 持续: {entry.dotDuration:F1}s";
            GUI.Label(new Rect(contentW - 440f, checkY + 5f, 420f, 28f), stats, statStyle);

            yPos += itemH;
        }

        GUI.EndScrollView();
    }

    // ════════════════════════════════════════════════════════════════
    // 升级选择面板
    // ════════════════════════════════════════════════════════════════

    private void DrawUpgradePanel()
    {
        float pad = 20f;
        float itemH = 80f;
        float contentW = PANEL_W - pad * 2 - 20f;
        float totalH = _upgradeSelections.Count * itemH + pad;

        Rect scrollRect = new Rect(PANEL_X + pad, PANEL_Y + pad, PANEL_W - pad * 2, PANEL_H - pad * 2 - 70f);
        Rect scrollView = new Rect(0, 0, contentW, totalH);

        _upgradeScrollPos = GUI.BeginScrollView(scrollRect, _upgradeScrollPos, scrollView);

        // 类别颜色
        var catColors = new Dictionary<string, Color>
        {
            { "DotType", new Color(0.9f, 0.4f, 0.4f) },
            { "ArmorReduction", new Color(0.8f, 0.6f, 0.2f) },
            { "DotSpread", new Color(0.4f, 0.8f, 0.4f) },
            { "DotFrequency", new Color(0.6f, 0.4f, 0.9f) },
            { "DotCritBurst", new Color(0.9f, 0.2f, 0.4f) },
            { "DetonateMultiplier", new Color(1f, 0.5f, 0f) },
            { "DetonateAbility", new Color(0.9f, 0.3f, 0f) },
            { "DotTrigger", new Color(0.5f, 0.7f, 0.9f) },
            { "DotSaturation", new Color(0.3f, 0.9f, 0.9f) },
            { "DetonateExtra", new Color(1f, 0.6f, 0.1f) },
            { "DotLifesteal", new Color(0.9f, 0.1f, 0.3f) },
            { "DotOverflow", new Color(0.4f, 0.6f, 1f) },
            { "AttackSpeed", new Color(0.8f, 0.8f, 0.2f) },
            { "BulletCount", new Color(0.7f, 0.7f, 0.3f) },
            { "Ricochet", new Color(0.6f, 0.8f, 0.5f) },
            { "DotPandemic", new Color(0.3f, 0.7f, 0.3f) },
            { "ChainReaction", new Color(1f, 0.3f, 0.1f) },
            { "DualWield", new Color(0.5f, 0.5f, 0.9f) },
            { "ChargeMastery", new Color(0.7f, 0.5f, 1f) },
            { "DotResonance", new Color(0.4f, 0.4f, 1f) },
            { "Toxicology", new Color(0.2f, 0.8f, 0.3f) },
            { "CorruptTouch", new Color(0.5f, 0.2f, 0.6f) },
            { "ElementalStorm", new Color(0.9f, 0.6f, 0.9f) },
            { "ShadowLink", new Color(0.3f, 0.1f, 0.5f) },
            { "LightJudgment", new Color(1f, 1f, 0.7f) },
            { "StaticField", new Color(0.4f, 0.8f, 1f) },
            { "FrostExplosion", new Color(0.5f, 0.8f, 1f) },
            { "AmmoMastery", new Color(0.7f, 0.6f, 0.4f) },
            { "ElementalAffinity", new Color(0.6f, 0.3f, 0.9f) },
            { "Penetrate", new Color(0.8f, 0.7f, 0.3f) },
            { "ElementalShield", new Color(0.3f, 0.6f, 0.8f) },
            { "PhaseShift", new Color(0.6f, 0.4f, 0.8f) },
            { "SoulSiphon", new Color(0.7f, 0.2f, 0.5f) },
            { "EmberBoost", new Color(1f, 0.4f, 0.1f) },
            { "ShatterBoost", new Color(0.4f, 0.7f, 1f) },
            { "ElementalMaster", new Color(0.9f, 0.7f, 0.1f) },
            { "Doomsday", new Color(0.8f, 0.1f, 0.1f) },
            { "EternalAgony", new Color(0.5f, 0.1f, 0.3f) },
            { "AnnihilationZone", new Color(0.2f, 0.1f, 0.6f) }
        };

        float yPos = 0f;
        var entries = _config.upgradeEntries;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            string id = entry.upgradeId;
            if (!_upgradeSelections.ContainsKey(id)) continue;

            int stacks = _upgradeSelections[id];
            bool active = stacks > 0;

            // 背景
            Color bg = active
                ? new Color(0.12f, 0.18f, 0.28f, 0.95f)
                : (i % 2 == 0 ? new Color(0.11f, 0.09f, 0.16f, 0.8f) : new Color(0.14f, 0.11f, 0.19f, 0.8f));
            GUI.color = bg;
            GUI.DrawTexture(new Rect(5f, yPos, contentW - 10f, itemH - 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 彩色条
            string catName = entry.category.ToString();
            Color catCol = catColors.ContainsKey(catName) ? catColors[catName] : new Color(0.5f, 0.5f, 0.5f);
            Color barCol = active ? catCol : new Color(catCol.r, catCol.g, catCol.b, 0.35f);
            GUI.color = barCol;
            GUI.DrawTexture(new Rect(5f, yPos, 8f, itemH - 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 名称
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = active ? Color.white : new Color(0.5f, 0.5f, 0.5f) }
            };
            GUI.Label(new Rect(24f, yPos + 6f, 420f, 28f), entry.upgradeName, nameStyle);

            // 描述
            var descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = active ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.45f, 0.45f, 0.45f) }
            };
            GUI.Label(new Rect(24f, yPos + 34f, 650f, 44f), entry.description, descStyle);

            // ── 叠加控制 ──
            float ctrlX = contentW - 460f;
            float ctrlY = yPos + 12f;
            float btnH = 34f;
            float smallBtnH = 30f;

            // 减少
            GUI.color = stacks > 0 ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.25f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(ctrlX, ctrlY, 42f, btnH), "−"))
                if (stacks > 0) _upgradeSelections[id] = stacks - 1;
            GUI.color = Color.white;

            // 数字显示
            var stackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = active ? new Color(0.3f, 1f, 0.3f) : new Color(0.45f, 0.45f, 0.45f) }
            };
            string stackText = entry.maxStacks > 0 ? $"{stacks}/{entry.maxStacks}" : $"{stacks}";
            GUI.Label(new Rect(ctrlX + 48f, ctrlY, 80f, btnH), stackText, stackStyle);

            // 增加
            bool canInc = entry.maxStacks == 0 || stacks < entry.maxStacks;
            GUI.color = canInc ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.25f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(ctrlX + 134f, ctrlY, 42f, btnH), "+"))
                if (canInc) _upgradeSelections[id] = stacks + 1;
            GUI.color = Color.white;

            // 快捷按钮
            var quickStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };

            GUI.color = canInc ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(ctrlX + 190f, ctrlY + 2f, 55f, smallBtnH), "×5", quickStyle))
            {
                int t = stacks + 5;
                if (entry.maxStacks > 0) t = Mathf.Min(t, entry.maxStacks);
                _upgradeSelections[id] = t;
            }

            GUI.color = canInc ? new Color(0.1f, 0.5f, 0.9f) : new Color(0.25f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(ctrlX + 250f, ctrlY + 2f, 60f, smallBtnH), "×10", quickStyle))
            {
                int t = stacks + 10;
                if (entry.maxStacks > 0) t = Mathf.Min(t, entry.maxStacks);
                _upgradeSelections[id] = t;
            }

            GUI.color = canInc ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.25f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(ctrlX + 315f, ctrlY + 2f, 65f, smallBtnH), "MAX", quickStyle))
            {
                _upgradeSelections[id] = entry.maxStacks > 0 ? entry.maxStacks : 99;
            }
            GUI.color = Color.white;

            // 类别 + 上限标签
            var infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(catCol.r, catCol.g, catCol.b, active ? 0.9f : 0.4f) }
            };
            string infoText = entry.maxStacks > 0 ? $"{catName}  (上限 {entry.maxStacks})" : catName;
            GUI.Label(new Rect(ctrlX + 190f, ctrlY + smallBtnH + 4f, 200f, 20f), infoText, infoStyle);

            yPos += itemH;
        }

        GUI.EndScrollView();
    }

    // ════════════════════════════════════════════════════════════════
    // 底部按钮
    // ════════════════════════════════════════════════════════════════

    private void DrawBottomButtons()
    {
        float btnY = PANEL_Y + PANEL_H - 65f;
        float btnH = 46f;

        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };

        float leftX = PANEL_X + 25f;

        if (_activeTab == 0)
        {
            GUI.color = new Color(0.4f, 0.35f, 0.7f);
            if (GUI.Button(new Rect(leftX, btnY, 130f, btnH), "全选子弹", btnStyle))
                foreach (var key in new List<string>(_bulletSelections.Keys)) _bulletSelections[key] = true;

            GUI.color = new Color(0.6f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(leftX + 140f, btnY, 130f, btnH), "清空子弹", btnStyle))
                foreach (var key in new List<string>(_bulletSelections.Keys)) _bulletSelections[key] = false;
        }
        else
        {
            GUI.color = new Color(0.6f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(leftX, btnY, 140f, btnH), "清空强化", btnStyle))
                foreach (var key in new List<string>(_upgradeSelections.Keys)) _upgradeSelections[key] = 0;

            GUI.color = new Color(0.2f, 0.5f, 0.8f);
            if (GUI.Button(new Rect(leftX + 150f, btnY, 150f, btnH), "全设 5 层", btnStyle))
                foreach (var key in new List<string>(_upgradeSelections.Keys)) _upgradeSelections[key] = 5;
        }
        GUI.color = Color.white;

        // 统计
        int bc = GetSelectedBulletCount();
        int uc = GetSelectedUpgradeCount();
        var countStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.85f, 0.35f) }
        };
        GUI.Label(new Rect(PANEL_X + PANEL_W / 2 - 200f, btnY + 8f, 400f, 30f),
            $"子弹: {bc}/{_bulletSelections.Count}  |  强化: {uc}项已叠加", countStyle);

        // 确认
        var confirmStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUI.color = new Color(0.2f, 0.75f, 0.2f, 0.95f);
        if (GUI.Button(new Rect(PANEL_X + PANEL_W - 230f, btnY, 210f, btnH + 4f), "✓ 确认进入", confirmStyle))
            ConfirmSelection();
        GUI.color = Color.white;
    }

    // ════════════════════════════════════════════════════════════════
    // 确认
    // ════════════════════════════════════════════════════════════════

    private void ConfirmSelection()
    {
        var bullets = new List<string>();
        foreach (var kvp in _bulletSelections)
            if (kvp.Value) bullets.Add(kvp.Key);

        var upgrades = new Dictionary<string, int>();
        foreach (var kvp in _upgradeSelections)
            if (kvp.Value > 0) upgrades[kvp.Key] = kvp.Value;

        DebugHelper.Log($"[TestBulletSelectUI] Confirmed: {bullets.Count} bullets, {upgrades.Count} upgrades");
        _isVisible = false;
        _onConfirmed?.Invoke(bullets, upgrades);
    }

    // ════════════════════════════════════════════════════════════════
    // 辅助
    // ════════════════════════════════════════════════════════════════

    private int GetSelectedBulletCount()
    {
        int c = 0; foreach (var kvp in _bulletSelections) if (kvp.Value) c++; return c;
    }
    private int GetSelectedUpgradeCount()
    {
        int c = 0; foreach (var kvp in _upgradeSelections) if (kvp.Value > 0) c++; return c;
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