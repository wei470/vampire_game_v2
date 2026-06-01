using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Game Over 界面。玩家死亡后显示 "Game Over" + 统计 + "Restart" / "Shop" / "Menu" 按钮。
/// Increment 7 新增：显示本局金币/波次/击杀，按钮跳转商店。
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private GameObject _gameOverPanel;
    private Text _statsText;
    private Text _coinsEarnedText;

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
        {
            ShowGameOver();
        }
        else
        {
            _gameOverPanel.SetActive(false);
        }
    }

    private void ShowGameOver()
    {
        _gameOverPanel.SetActive(true);

        // 记录本局数据到存档
        int finalWave = 0;
        // killsThisGame removed (unused)
        int coinsThisGame = Coin.TotalCoins;

        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr != null) finalWave = spawnMgr.CurrentWave;

        var killRewarder = FindAnyObjectByType<KillRewarder>();
        // killsThisGame 从 KillRewarder 统计（如果有的话）
        // 否则用 Coin.TotalCoins 估算

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RecordGameEnd(finalWave, coinsThisGame); // 简化：用金币数估算击杀
        }

        // 更新统计文本
        if (_statsText != null)
            _statsText.text = $"Wave Reached: {finalWave}";
        if (_coinsEarnedText != null)
            _coinsEarnedText.text = $"Coins Earned: {coinsThisGame}";
    }

    private void CreateGameOverUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        _gameOverPanel = new GameObject("GameOverPanel");
        _gameOverPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = _gameOverPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        Image panelBg = _gameOverPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f);

        // GAME OVER 标题
        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(_gameOverPanel.transform, false);
        Text gameOverText = textObj.AddComponent<Text>();
        gameOverText.text = "GAME OVER";
        gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameOverText.fontSize = 72;
        gameOverText.color = Color.red;
        gameOverText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.75f);
        textRect.anchorMax = new Vector2(0.5f, 0.75f);
        textRect.sizeDelta = new Vector2(600, 100);
        textRect.anchoredPosition = Vector2.zero;

        // 统计信息
        _statsText = CreateLabel(_gameOverPanel.transform, "StatsText", "Wave Reached: 0",
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), 30, Color.white, new Vector2(400, 40));

        _coinsEarnedText = CreateLabel(_gameOverPanel.transform, "CoinsEarnedText", "Coins Earned: 0",
            new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), 28, new Color(1f, 0.85f, 0f), new Vector2(400, 35));

        // Restart 按钮
        CreateButton(_gameOverPanel.transform, "RestartButton", "Restart",
            new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(250, 55),
            new Color(0.2f, 0.6f, 0.2f),
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        // Shop 按钮
        CreateButton(_gameOverPanel.transform, "ShopButton", "Upgrade Shop",
            new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(250, 55),
            new Color(0.6f, 0.5f, 0.1f),
            () =>
            {
                var shop = FindAnyObjectByType<ShopUI>();
                if (shop != null)
                {
                    _gameOverPanel.SetActive(false);
                    shop.OpenShop();
                }
            });

        // Menu 按钮
        CreateButton(_gameOverPanel.transform, "MenuButton", "Back to Menu",
            new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(250, 55),
            new Color(0.3f, 0.3f, 0.5f),
            () =>
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.Save();
                SceneManager.LoadScene(0);
            });
    }

    private Text CreateLabel(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = Vector2.zero;
        return text;
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
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 28;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
    }
}