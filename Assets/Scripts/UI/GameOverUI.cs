using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Game Over 界面。
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
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(600, 100));
        gameOverText.fontSize = 72;
        gameOverText.color = UIColorTheme.AccentMagenta;

        _statsText = CreateTextObj(_gameOverPanel.transform, "StatsText", "Wave Reached: 0",
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(400, 40));
        _statsText.fontSize = 30;
        _statsText.color = UIColorTheme.TextPrimary;

        _coinsEarnedText = CreateTextObj(_gameOverPanel.transform, "CoinsEarnedText", "Coins Earned: 0",
            new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), new Vector2(400, 35));
        _coinsEarnedText.fontSize = 28;
        _coinsEarnedText.color = UIColorTheme.GoldText;

        CreateButton(_gameOverPanel.transform, "RestartButton", "Restart",
            new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(250, 55),
            UIColorTheme.AccentCyan,
            () =>
            {
                ResetGameState();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

        CreateButton(_gameOverPanel.transform, "ShopButton", "Upgrade Shop",
            new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(250, 55),
            UIColorTheme.AccentMagenta,
            () =>
            {
                var shop = FindAnyObjectByType<ShopUI>();
                if (shop != null) { _gameOverPanel.SetActive(false); shop.OpenShop(); }
            });

        CreateButton(_gameOverPanel.transform, "MenuButton", "Back to Menu",
            new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(250, 55),
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
        btnText.fontSize = 28;
        btnText.color = UIColorTheme.TextPrimary;
        btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
    }
}