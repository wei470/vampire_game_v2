using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// #41 Game Over 界面 — 增强版：显示详细战斗统计
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private GameObject _gameOverPanel;
    private Text _statsText;
    private Text _coinsEarnedText;
    private Text _detailStatsText; // #41 详细统计文本

    private static readonly List<KeyValuePair<DamageMeter.DamageSource, object>> _tempSorted
        = new List<KeyValuePair<DamageMeter.DamageSource, object>>(16);

    private void Start()
    {
        CreateGameOverUI();
        _gameOverPanel.SetActive(false);
        GameManager.OnStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.GameOver)
            ShowGameOver();
        else
            _gameOverPanel.SetActive(false);
    }

    private void ShowGameOver()
    {
        _gameOverPanel.SetActive(true);

        int finalWave = 0;
        int coinsThisGame = Coin.TotalCoins;
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr != null) finalWave = spawnMgr.CurrentWave;

        if (SaveManager.Instance != null)
            SaveManager.Instance.RecordGameEnd(finalWave, coinsThisGame);

        // #42 更新角色解锁统计
        var dm = DamageMeter.Instance;
        if (dm != null)
        {
            CharacterUnlockManager.UpdateStatsOnGameEnd(
                dm.CombatTime,
                dm.TotalKills,
                dm.BossKills,
                0, // 引爆次数暂用0（可通过 MagePassive 获取）
                0  // 承受伤害暂用0（后续可扩展）
            );
        }

        if (_statsText != null)
            _statsText.text = $"波次: {finalWave}  |  存活: {UIFormatUtils.FormatTime(DamageMeter.Instance != null ? DamageMeter.Instance.CombatTime : 0f)}";
        if (_coinsEarnedText != null)
            _coinsEarnedText.text = $"金币: {coinsThisGame}";

        // #41 填充详细统计
        if (_detailStatsText != null)
            _detailStatsText.text = BuildDetailStatsText();
    }

    /// <summary>
    /// #41 构建详细统计文本
    /// </summary>
    private string BuildDetailStatsText()
    {
        var dm = DamageMeter.Instance;
        if (dm == null) return "无统计数据";

        var sb = new StringBuilder();
        long totalDmg = dm.TotalDamage;
        float elapsed = dm.CombatTime;
        float avgDps = elapsed > 0 ? totalDmg / elapsed : 0f;

        sb.AppendLine($"总伤害: {UIFormatUtils.FormatDamage(totalDmg)}  |  平均DPS: {UIFormatUtils.FormatDamage((long)avgDps)}/s");
        sb.AppendLine($"击杀: {dm.TotalKills}  |  Boss: {dm.BossKills}");
        sb.AppendLine($"最高引爆: {UIFormatUtils.FormatDamage(dm.MaxSingleDetonateDamage)}  |  最高DPS: {UIFormatUtils.FormatDamage((long)dm.MaxDpsPeak)}/s");
        sb.AppendLine($"DOT触发: {dm.TotalDotTicks}次");
        sb.AppendLine();
        sb.AppendLine("── 伤害分布 ──");

        // 按伤害排序
        _tempSorted.Clear();
        foreach (var kvp in dm.StatsSnapshot)
            _tempSorted.Add(new KeyValuePair<DamageMeter.DamageSource, object>(kvp.Key, kvp.Value));
        _tempSorted.Sort((a, b) =>
        {
            var sa = (DamageMeter.SourceStats)a.Value;
            var sb2 = (DamageMeter.SourceStats)b.Value;
            return sb2.totalDamage.CompareTo(sa.totalDamage);
        });

        foreach (var kvp in _tempSorted)
        {
            var stats = (DamageMeter.SourceStats)kvp.Value;
            float pct = totalDmg > 0 ? (float)stats.totalDamage / totalDmg * 100f : 0f;
            string bar = BuildAsciiBar(pct, 10);
            sb.AppendLine($"  {GetSourceLabel(kvp.Key)} {UIFormatUtils.FormatDamage(stats.totalDamage)} {bar} {pct:F1}%");
        }

        return sb.ToString();
    }

    private string GetSourceLabel(DamageMeter.DamageSource src)
    {
        return src switch
        {
            DamageMeter.DamageSource.Bleed => "🔴流血",
            DamageMeter.DamageSource.Poison => "🟢中毒",
            DamageMeter.DamageSource.Burn => "🟠燃烧",
            DamageMeter.DamageSource.Frostbite => "🔵霜冻",
            DamageMeter.DamageSource.Detonate => "💥引爆",
            DamageMeter.DamageSource.Bullet => "⚡子弹",
            DamageMeter.DamageSource.Skill => "✦ 技能",
            _ => "？其他"
        };
    }

    private string BuildAsciiBar(float percent, int width)
    {
        int filled = Mathf.RoundToInt(percent / 100f * width);
        filled = Mathf.Clamp(filled, 0, width);
        return "[" + new string('█', filled) + new string('░', width - filled) + "]";
    }

    private void CreateGameOverUI()
    {
        // Canvas 是场景 UI 对象，无全局缓存
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        UIFontProvider.EnsureCanvasScaler(canvas);

        _gameOverPanel = new GameObject("GameOverPanel");
        _gameOverPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = _gameOverPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        Image panelBg = _gameOverPanel.AddComponent<Image>();
        panelBg.color = UIColorTheme.OverlayDark;

        // GAME OVER 标题
        Text gameOverText = CreateTextObj(_gameOverPanel.transform, "GameOverText", "GAME OVER",
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(600, 80));
        gameOverText.fontSize = 60;
        gameOverText.color = UIColorTheme.AccentMagenta;

        // 基础统计行（波次 + 存活时间 + 金币）
        _statsText = CreateTextObj(_gameOverPanel.transform, "StatsText", "波次: 0  |  存活: 00:00",
            new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(500, 30));
        _statsText.fontSize = 24;
        _statsText.color = UIColorTheme.TextPrimary;

        _coinsEarnedText = CreateTextObj(_gameOverPanel.transform, "CoinsEarnedText", "金币: 0",
            new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(400, 25));
        _coinsEarnedText.fontSize = 22;
        _coinsEarnedText.color = UIColorTheme.GoldText;

        // #41 详细统计面板
        GameObject detailPanel = new GameObject("DetailStatsPanel");
        detailPanel.transform.SetParent(_gameOverPanel.transform, false);
        RectTransform detailRect = detailPanel.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.5f, 0.32f);
        detailRect.anchorMax = new Vector2(0.5f, 0.75f);
        detailRect.sizeDelta = new Vector2(600, 0);
        Image detailBg = detailPanel.AddComponent<Image>();
        detailBg.color = new Color(0f, 0f, 0f, 0.4f);

        // 详细统计标题
        Text detailTitle = CreateTextObj(detailPanel.transform, "DetailTitle", "⚔ 战斗统计 ⚔",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(600, 30));
        detailTitle.fontSize = 22;
        detailTitle.color = UIColorTheme.AccentCyan;

        // 详细统计内容（等宽风格）
        _detailStatsText = CreateTextObj(detailPanel.transform, "DetailStatsText", "",
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero);
        _detailStatsText.fontSize = 16;
        _detailStatsText.color = UIColorTheme.TextSecondary;
        _detailStatsText.alignment = TextAnchor.UpperLeft;
        _detailStatsText.lineSpacing = 1.2f;
        // 内边距
        var detailContentRect = _detailStatsText.GetComponent<RectTransform>();
        detailContentRect.offsetMin = new Vector2(15, 5);
        detailContentRect.offsetMax = new Vector2(-15, -30);

        // 按钮区域（下方）
        float btnY = 0.22f;
        float btnGap = 0.07f;

        CreateButton(_gameOverPanel.transform, "ShareButton", "📋 复制统计",
            new Vector2(0.5f, btnY - btnGap * 0), new Vector2(0.5f, btnY - btnGap * 0), new Vector2(200, 45),
            new Color(0.2f, 0.6f, 0.3f),
            () =>
            {
                var dm = DamageMeter.Instance;
                if (dm != null)
                {
                    string summary = dm.GenerateStatsSummary();
                    GUIUtility.systemCopyBuffer = summary;
                    DebugHelper.Log("[GameOverUI] Stats copied to clipboard");
                }
            });

        CreateButton(_gameOverPanel.transform, "RestartButton", "重新开始",
            new Vector2(0.5f, btnY - btnGap * 1), new Vector2(0.5f, btnY - btnGap * 1), new Vector2(200, 45),
            UIColorTheme.AccentCyan,
            () =>
            {
                ResetGameState();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

        CreateButton(_gameOverPanel.transform, "ShopButton", "升级商店",
            new Vector2(0.5f, btnY - btnGap * 2), new Vector2(0.5f, btnY - btnGap * 2), new Vector2(200, 45),
            UIColorTheme.AccentMagenta,
            () =>
            {
                var shop = FindAnyObjectByType<ShopUI>(); // ShopUI 无 Instance
                if (shop != null) { _gameOverPanel.SetActive(false); shop.OpenShop(); }
            });

        CreateButton(_gameOverPanel.transform, "MenuButton", "返回菜单",
            new Vector2(0.5f, btnY - btnGap * 3), new Vector2(0.5f, btnY - btnGap * 3), new Vector2(200, 45),
            UIColorTheme.PanelBackground,
            () =>
            {
                ResetGameState();
                SceneManager.LoadScene("MenuScene");
            });
    }

    private Text CreateTextObj(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = UIFontProvider.DefaultFont;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = Vector2.zero;
        return text;
    }

    /// <summary>
    /// #37 使用统一的 GameStateResetter 重置所有游戏状态
    /// </summary>
    private void ResetGameState()
    {
        GameStateResetter.FullReset();
    }

    private void CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = anchorMin;
        btnRect.anchorMax = anchorMax;
        btnRect.sizeDelta = sizeDelta;

        GameObject btnTextObj = new GameObject("ButtonText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.text = label;
        btnText.font = UIFontProvider.DefaultFont;
        btnText.fontSize = 24;
        btnText.color = UIColorTheme.TextPrimary;
        btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
    }
}