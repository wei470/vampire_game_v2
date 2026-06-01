using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店界面，玩家可在游戏结束或波次间歇购买局外永久升级。
/// 对应 Python: ui/shop.py
/// 
/// 功能：
/// - 显示当前金币数
/// - 显示 12 种升级卡片（等级/价格/进度条）
/// - 显示 10 个里程碑（进度/领取）
/// - "返回主菜单" 按钮
/// - 统计数据（最高波次/总击杀/总游戏数）
/// </summary>
public class ShopUI : MonoBehaviour
{
    private GameObject _shopPanel;
    private Text _coinsText;
    private Text _statsText;
    private Text[] _upgradeNameTexts;
    private Text[] _upgradeLevelTexts;
    private Text[] _upgradeCostTexts;
    private Image[] _upgradeBars;
    private Button[] _upgradeButtons;
    private Text[] _milestoneNameTexts;
    private Text[] _milestoneDescTexts;
    private Text[] _milestoneStatusTexts;
    private Button[] _milestoneButtons;

    private void Start()
    {
        CreateShopUI();
        _shopPanel.SetActive(false);

        // 监听事件
        SaveManager.OnCoinsChanged += OnCoinsChanged;
    }

    private void OnDestroy()
    {
        SaveManager.OnCoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int coins)
    {
        RefreshCoins();
    }

    /// <summary>
    /// 打开商店
    /// </summary>
    public void OpenShop()
    {
        _shopPanel.SetActive(true);
        RefreshAll();
    }

    /// <summary>
    /// 关闭商店
    /// </summary>
    public void CloseShop()
    {
        _shopPanel.SetActive(false);
    }

    public bool IsOpen => _shopPanel != null && _shopPanel.activeSelf;

    // ============================================================
    // 刷新 UI
    // ============================================================

    private void RefreshAll()
    {
        if (SaveManager.Instance == null) return;

        RefreshCoins();
        RefreshUpgrades();
        RefreshMilestones();
        RefreshStats();
    }

    private void RefreshCoins()
    {
        if (_coinsText != null && SaveManager.Instance != null)
            _coinsText.text = $"Coins: {SaveManager.Instance.GetCoins()}";
    }

    private void RefreshUpgrades()
    {
        var sm = SaveManager.Instance;
        if (sm == null) return;

        var upgrades = SaveManager.SHOP_UPGRADES;
        for (int i = 0; i < upgrades.Length; i++)
        {
            if (i >= _upgradeNameTexts.Length) break;
            var upgrade = upgrades[i];
            int level = sm.GetUpgradeLevel(upgrade.attr);
            bool canAfford = sm.GetCoins() >= upgrade.cost;
            bool isMaxed = level >= upgrade.maxLevel;

            // 名称
            _upgradeNameTexts[i].text = upgrade.name;

            // 等级
            _upgradeLevelTexts[i].text = isMaxed ? "MAX" : $"Lv.{level}";
            _upgradeLevelTexts[i].color = isMaxed ? Color.green : new Color(1f, 0.85f, 0f);

            // 费用
            if (isMaxed)
            {
                _upgradeCostTexts[i].text = "MAXED";
                _upgradeCostTexts[i].color = Color.green;
            }
            else
            {
                _upgradeCostTexts[i].text = $"{upgrade.cost} coins";
                _upgradeCostTexts[i].color = canAfford ? Color.yellow : Color.red;
            }

            // 进度条
            float fill = Mathf.Clamp01((float)level / upgrade.maxLevel);
            _upgradeBars[i].fillAmount = fill;
            _upgradeBars[i].color = isMaxed ? Color.green : new Color(1f, 0.85f, 0f);

            // 按钮
            _upgradeButtons[i].interactable = sm.CanBuyUpgrade(i);
        }
    }

    private void RefreshMilestones()
    {
        var sm = SaveManager.Instance;
        if (sm == null) return;

        var milestones = SaveManager.MILESTONES;
        for (int i = 0; i < milestones.Length; i++)
        {
            if (i >= _milestoneNameTexts.Length) break;
            var ms = milestones[i];
            bool claimed = sm.IsMilestoneClaimed(ms.id);
            int statVal = sm.GetStatForMilestone(ms.stat);
            bool unlocked = statVal >= ms.threshold;

            _milestoneNameTexts[i].text = ms.name;
            _milestoneDescTexts[i].text = ms.desc;

            if (claimed)
            {
                _milestoneStatusTexts[i].text = ms.rewardDesc + " [CLAIMED]";
                _milestoneStatusTexts[i].color = Color.green;
                _milestoneButtons[i].interactable = false;
            }
            else if (unlocked)
            {
                _milestoneStatusTexts[i].text = "Click to claim!";
                _milestoneStatusTexts[i].color = Color.yellow;
                _milestoneButtons[i].interactable = true;
            }
            else
            {
                _milestoneStatusTexts[i].text = $"{statVal}/{ms.threshold}";
                _milestoneStatusTexts[i].color = new Color(0.5f, 0.5f, 0.55f);
                _milestoneButtons[i].interactable = false;
            }
        }
    }

    private void RefreshStats()
    {
        var sm = SaveManager.Instance;
        if (sm == null || _statsText == null) return;

        var d = sm.Data;
        _statsText.text = $"Highest Wave: {d.highestWave}  |  Total Kills: {d.totalKills}  |  Games Played: {d.totalGames}";
    }

    // ============================================================
    // UI 创建
    // ============================================================

    private void CreateShopUI()
    {
        // 找到 Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 主面板（半透明黑色背景）
        _shopPanel = CreatePanel(canvas.transform, "ShopPanel", new Color(0, 0, 0, 0.85f));
        RectTransform panelRect = _shopPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // 标题
        CreateText(_shopPanel.transform, "Title", "UPGRADE SHOP",
            new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), 40,
            new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter, new Vector2(400, 50));

        // 金币显示
        _coinsText = CreateText(_shopPanel.transform, "CoinsText", "Coins: 0",
            new Vector2(0.5f, 0.89f), new Vector2(0.5f, 0.89f), 28,
            new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter, new Vector2(300, 35));

        // 统计
        _statsText = CreateText(_shopPanel.transform, "StatsText", "Highest Wave: 0  |  Total Kills: 0  |  Games: 0",
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), 20,
            new Color(0.7f, 0.7f, 0.75f), TextAnchor.MiddleCenter, new Vector2(600, 25));

        // 升级网格 (6 列 x 2 行 = 12 个)
        int upgradeCount = SaveManager.SHOP_UPGRADES.Length;
        _upgradeNameTexts = new Text[upgradeCount];
        _upgradeLevelTexts = new Text[upgradeCount];
        _upgradeCostTexts = new Text[upgradeCount];
        _upgradeBars = new Image[upgradeCount];
        _upgradeButtons = new Button[upgradeCount];

        int cols = 6;
        float cardW = 140f;
        float cardH = 130f;
        float gapX = 10f;
        float gapY = 10f;
        float startX = -(cols * cardW + (cols - 1) * gapX) / 2f;
        float startY = 180f;

        for (int i = 0; i < upgradeCount; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = startX + col * (cardW + gapX) + cardW / 2f;
            float y = startY - row * (cardH + gapY);

            GameObject card = CreateUpgradeCard(_shopPanel.transform, i, x, y, cardW, cardH);
        }

        // 里程碑区域
        int milestoneCount = SaveManager.MILESTONES.Length;
        _milestoneNameTexts = new Text[milestoneCount];
        _milestoneDescTexts = new Text[milestoneCount];
        _milestoneStatusTexts = new Text[milestoneCount];
        _milestoneButtons = new Button[milestoneCount];

        float msStartY = -80f;
        CreateText(_shopPanel.transform, "MilestoneTitle", "--- MILESTONES ---",
            new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), 24,
            new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter, new Vector2(300, 30));

        int msCols = 5;
        float msCardW = 150f;
        float msCardH = 55f;
        float msGapX = 8f;
        float msGapY = 5f;
        float msStartX = -(msCols * msCardW + (msCols - 1) * msGapX) / 2f;

        for (int i = 0; i < milestoneCount; i++)
        {
            int col = i % msCols;
            int row = i / msCols;
            float x = msStartX + col * (msCardW + msGapX) + msCardW / 2f;
            float y = msStartY - row * (msCardH + msGapY);

            CreateMilestoneCard(_shopPanel.transform, i, x, y, msCardW, msCardH);
        }

        // 返回按钮
        CreateBackButton(_shopPanel.transform);
    }

    private GameObject CreateUpgradeCard(Transform parent, int index, float x, float y, float w, float h)
    {
        var sm = SaveManager.Instance;
        var upgrade = SaveManager.SHOP_UPGRADES[index];

        GameObject card = new GameObject($"UpgradeCard_{index}");
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.2f, 0.9f);

        // 按钮
        Button btn = card.AddComponent<Button>();
        int capturedIndex = index;
        btn.onClick.AddListener(() => OnUpgradeClicked(capturedIndex));
        _upgradeButtons[index] = btn;

        // 名称
        _upgradeNameTexts[index] = CreateText(card.transform, "Name", upgrade.name,
            new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), 12,
            Color.white, TextAnchor.MiddleCenter, new Vector2(w - 10, 18));

        // 等级
        _upgradeLevelTexts[index] = CreateText(card.transform, "Level", "Lv.0",
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), 18,
            new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter, new Vector2(w - 10, 25));

        // 进度条背景
        GameObject barBg = new GameObject("BarBg");
        barBg.transform.SetParent(card.transform, false);
        RectTransform barBgRect = barBg.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.1f, 0.38f);
        barBgRect.anchorMax = new Vector2(0.9f, 0.52f);
        barBgRect.sizeDelta = Vector2.zero;
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.1f, 0.08f, 0.15f, 0.8f);

        // 进度条填充
        GameObject barFill = new GameObject("BarFill");
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform barFillRect = barFill.AddComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.sizeDelta = new Vector2(0, 0);
        barFillRect.pivot = new Vector2(0, 0.5f);
        Image barFillImg = barFill.AddComponent<Image>();
        barFillImg.color = new Color(1f, 0.85f, 0f);
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        _upgradeBars[index] = barFillImg;

        // 费用
        _upgradeCostTexts[index] = CreateText(card.transform, "Cost", "5 coins",
            new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), 14,
            Color.yellow, TextAnchor.MiddleCenter, new Vector2(w - 10, 20));

        // 描述（小字，底部）
        CreateText(card.transform, "Desc", upgrade.desc,
            new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f), 9,
            new Color(0.6f, 0.6f, 0.65f), TextAnchor.MiddleCenter, new Vector2(w - 10, 15));

        return card;
    }

    private void CreateMilestoneCard(Transform parent, int index, float x, float y, float w, float h)
    {
        var ms = SaveManager.MILESTONES[index];

        GameObject card = new GameObject($"Milestone_{index}");
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.1f, 0.18f, 0.85f);

        Button btn = card.AddComponent<Button>();
        int capturedIndex = index;
        btn.onClick.AddListener(() => OnMilestoneClicked(capturedIndex));
        _milestoneButtons[index] = btn;

        _milestoneNameTexts[index] = CreateText(card.transform, "Name", ms.name,
            new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), 12,
            Color.white, TextAnchor.MiddleCenter, new Vector2(w - 8, 16));

        _milestoneDescTexts[index] = CreateText(card.transform, "Desc", ms.desc,
            new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), 10,
            new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleCenter, new Vector2(w - 8, 14));

        _milestoneStatusTexts[index] = CreateText(card.transform, "Status", "0/5",
            new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), 11,
            new Color(0.5f, 0.5f, 0.55f), TextAnchor.MiddleCenter, new Vector2(w - 8, 14));
    }

    private void CreateBackButton(Transform parent)
    {
        GameObject btnObj = new GameObject("BackButton");
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.02f);
        rect.anchorMax = new Vector2(0.5f, 0.02f);
        rect.sizeDelta = new Vector2(200, 40);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.15f, 0.3f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => CloseShop());

        CreateText(btnObj.transform, "Text", "Back to Menu",
            Vector2.zero, Vector2.one, 22, Color.white, TextAnchor.MiddleCenter, Vector2.zero);
        Text btnText = btnObj.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            RectTransform textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }
    }

    // ============================================================
    // 点击处理
    // ============================================================

    private void OnUpgradeClicked(int index)
    {
        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.BuyUpgrade(index))
        {
            RefreshAll();
        }
    }

    private void OnMilestoneClicked(int index)
    {
        if (SaveManager.Instance == null) return;
        var ms = SaveManager.MILESTONES[index];
        if (SaveManager.Instance.ClaimMilestone(ms.id))
        {
            RefreshAll();
        }
    }

    // ============================================================
    // UI 工具方法
    // ============================================================

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    private Text CreateText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAnchor alignment, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = Vector2.zero;
        return text;
    }
}