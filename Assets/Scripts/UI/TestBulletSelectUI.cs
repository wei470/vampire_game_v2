using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test 模式专属子弹选择 UI — 在游戏开始前让开发者选择要携带的 DOT 子弹。
///
/// 功能：
/// 1. 显示所有可用的 DOT 子弹（从 MageUpgradeConfig.dotGunEntries 自动读取）
/// 2. 每种子弹可勾选/取消（包括默认毒子弹也可以不带）
/// 3. 支持多选，确认后进入游戏
/// 4. 新增子弹到 MageUpgradeConfig 后自动出现在此列表中
///
/// 使用方式：由 GameSceneBootstrap 在 TestMode 时动态创建
/// </summary>
public class TestBulletSelectUI : MonoBehaviour
{
    private MageUpgradeConfig _config;
    private System.Action<List<string>> _onConfirmed;

    /// <summary>每种子弹的勾选状态（upgradeId → 是否选中）</summary>
    private Dictionary<string, bool> _selections = new Dictionary<string, bool>();

    /// <summary>滚动位置</summary>
    private Vector2 _scrollPos;

    private bool _isVisible = true;

    /// <summary>
    /// 初始化子弹选择 UI
    /// </summary>
    /// <param name="config">Mage 升级配置（读取 dotGunEntries）</param>
    /// <param name="onConfirmed">确认回调，参数为选中的 upgradeId 列表</param>
    public void Setup(MageUpgradeConfig config, System.Action<List<string>> onConfirmed)
    {
        _config = config;
        _onConfirmed = onConfirmed;

        // 从配置中读取所有 DOT 子弹类型，初始化选择状态
        // 默认全部勾选（包括毒子弹）
        _selections.Clear();
        if (_config != null && _config.dotGunEntries != null)
        {
            for (int i = 0; i < _config.dotGunEntries.Length; i++)
            {
                var entry = _config.dotGunEntries[i];
                _selections[entry.upgradeId] = true; // 默认全选
            }
        }

        DebugHelper.Log($"[TestBulletSelectUI] Initialized with {_selections.Count} bullet options");
    }

    private void OnGUI()
    {
        if (!_isVisible || _config == null) return;

        GUIScaleHelper.BeginScale();

        // ── 半透明遮罩背景 ──
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── 标题 ──
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.2f, 0.9f) }
        };
        GUI.Label(new Rect(0, 30, 1920, 50), "🧪 TEST MODE — 选择初始子弹", titleStyle);

        // ── 副标题说明 ──
        var subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        GUI.Label(new Rect(0, 80, 1920, 30), "Mage + Teleport | 勾选要携带的子弹类型（可多选/不选）", subtitleStyle);

        // ── 子弹选择面板 ──
        float panelX = 460f;
        float panelY = 130f;
        float panelW = 1000f;
        float panelH = 700f;

        // 面板背景
        GUI.color = new Color(0.12f, 0.1f, 0.18f, 0.95f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 面板边框
        DrawBorder(new Rect(panelX, panelY, panelW, panelH), new Color(0.6f, 0.2f, 0.9f, 0.6f), 2);

        // ── 子弹选项列表（滚动区域）──
        float contentY = panelY + 20f;
        float itemHeight = 110f;
        float totalContentHeight = _selections.Count * itemHeight + 20f;

        // 滚动区域
        Rect scrollRect = new Rect(panelX + 20f, contentY, panelW - 40f, panelH - 100f);
        Rect scrollView = new Rect(0, 0, panelW - 60f, totalContentHeight);

        _scrollPos = GUI.BeginScrollView(scrollRect, _scrollPos, scrollView);

        float yPos = 0f;
        var entries = _config.dotGunEntries;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            string id = entry.upgradeId;
            if (!_selections.ContainsKey(id)) continue;

            // 子弹项背景（交替颜色）
            Color itemBg = i % 2 == 0
                ? new Color(0.15f, 0.12f, 0.22f, 0.8f)
                : new Color(0.18f, 0.14f, 0.25f, 0.8f);
            GUI.color = itemBg;
            GUI.DrawTexture(new Rect(5f, yPos, scrollView.width - 10f, itemHeight - 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 左侧彩色条（子弹颜色标识）
            GUI.color = entry.color;
            GUI.DrawTexture(new Rect(5f, yPos, 6f, itemHeight - 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 勾选框
            bool isSelected = _selections[id];
            float checkboxY = yPos + 15f;
            _selections[id] = GUI.Toggle(new Rect(20f, checkboxY, 30f, 30f), isSelected, "");

            // 子弹名称
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = entry.color }
            };
            GUI.Label(new Rect(60f, checkboxY - 5f, 400f, 35f), entry.displayName, nameStyle);

            // 子弹描述（多行）
            var descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            GUI.Label(new Rect(60f, checkboxY + 28f, scrollView.width - 80f, 60f), entry.description, descStyle);

            // 右侧属性标签
            var statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = new Color(0.6f, 0.9f, 0.6f) }
            };
            string stats = $"冷却: {entry.cooldown:F1}s | 冲击: {entry.impactDmg} | DOT: {entry.dotDps:F1}/s | 持续: {entry.dotDuration:F1}s";
            GUI.Label(new Rect(scrollView.width - 420f, checkboxY + 5f, 400f, 25f), stats, statStyle);

            yPos += itemHeight;
        }

        GUI.EndScrollView();

        // ── 底部按钮区域 ──
        float btnY = panelY + panelH - 60f;

        // 全选 / 全不选按钮
        var smallBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        if (GUI.Button(new Rect(panelX + 30f, btnY, 120f, 40f), "全选", smallBtnStyle))
        {
            foreach (var key in new List<string>(_selections.Keys))
                _selections[key] = true;
        }

        if (GUI.Button(new Rect(panelX + 165f, btnY, 120f, 40f), "全不选", smallBtnStyle))
        {
            foreach (var key in new List<string>(_selections.Keys))
                _selections[key] = false;
        }

        // 已选数量提示
        int selectedCount = 0;
        foreach (var kvp in _selections)
            if (kvp.Value) selectedCount++;

        var countStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = selectedCount > 0 ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.5f, 0.3f) }
        };
        GUI.Label(new Rect(panelX + panelW / 2 - 100f, btnY + 5f, 200f, 30f),
            $"已选 {selectedCount}/{_selections.Count} 种子弹", countStyle);

        // 确认按钮
        var confirmStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUI.color = new Color(0.3f, 0.8f, 0.3f, 0.9f);
        if (GUI.Button(new Rect(panelX + panelW - 200f, btnY, 170f, 45f), "✓ 确认进入", confirmStyle))
        {
            ConfirmSelection();
        }
        GUI.color = Color.white;

        GUIScaleHelper.EndScale();
    }

    /// <summary>
    /// 确认选择，收集选中的子弹 ID 列表并回调
    /// </summary>
    private void ConfirmSelection()
    {
        var selected = new List<string>();
        foreach (var kvp in _selections)
        {
            if (kvp.Value) selected.Add(kvp.Key);
        }

        DebugHelper.Log($"[TestBulletSelectUI] Confirmed: {selected.Count} bullets selected");
        foreach (var id in selected)
            DebugHelper.Log($"  - {id}");

        _isVisible = false;
        _onConfirmed?.Invoke(selected);
    }

    /// <summary>
    /// 绘制矩形边框
    /// </summary>
    private void DrawBorder(Rect rect, Color color, int thickness)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture); // 上
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture); // 下
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture); // 左
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture); // 右
        GUI.color = Color.white;
    }
}