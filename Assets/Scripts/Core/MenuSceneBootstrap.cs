using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 主菜单运行时引导器 — 处理菜单逻辑。
/// 1. Start 按钮点击后加载 GameScene
/// 2. 按 Enter/Space 也能开始游戏
/// 3. 按钮颜色采用 UI 主题配色，每个按钮不同色
/// </summary>
[ExecuteAlways]
public class MenuSceneBootstrap : MonoBehaviour
{
    private ShopUI _shopUI;

    /// <summary>
    /// Awake 在编辑器加载场景时也会调用（[ExecuteAlways] 模式下）
    /// 负责应用 UI 主题色，让 Scene View 中也能即时看到颜色变化
    /// </summary>
    private void Awake()
    {
        // 应用按钮和标题的主题色（编辑器和运行时都执行）
        ApplyAllButtonThemes();
        ApplyTitleTheme();
    }

    private void Start()
    {
        // ======== 仅运行时执行的初始化 ========
        if (!Application.isPlaying) return;

        // GameManager 默认状态已是 Menu，无需重复设置

        // 确保 SaveManager 存在
        if (FindAnyObjectByType<SaveManager>() == null)
        {
            var saveObj = new GameObject("SaveManager");
            saveObj.AddComponent<SaveManager>();
            DebugHelper.Log("[MenuBootstrap] Created SaveManager");
        }

        // 确保 ShopUI 存在
        _shopUI = FindAnyObjectByType<ShopUI>();
        if (_shopUI == null)
        {
            var shopObj = new GameObject("ShopUI");
            _shopUI = shopObj.AddComponent<ShopUI>();
            DebugHelper.Log("[MenuBootstrap] Created ShopUI");
        }

        // 绑定按钮事件（仅运行时）
        var buttons = FindObjectsByType<Button>();
        foreach (var btn in buttons)
        {
            switch (btn.gameObject.name)
            {
                case "StartButton":
                    btn.onClick.AddListener(StartGame);
                    break;
                case "ShopButton":
                    btn.onClick.AddListener(() =>
                    {
                        if (_shopUI != null) _shopUI.OpenShop();
                    });
                    break;
                case "TestButton":
                    btn.onClick.AddListener(StartTestMode);
                    break;
                case "DailyButton":
                    btn.onClick.AddListener(StartDailyChallenge);
                    break;
            }
        }

        // 如果场景中没有 TestButton，动态创建一个
        bool foundTest = false;
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "TestButton")
            {
                foundTest = true;
                break;
            }
        }
        if (!foundTest)
        {
            CreateTestButton();
        }
    }

    /// <summary>
    /// 对所有菜单按钮应用主题色（编辑器和运行时均执行）
    /// </summary>
    private void ApplyAllButtonThemes()
    {
        var buttons = FindObjectsByType<Button>();
        foreach (var btn in buttons)
        {
            switch (btn.gameObject.name)
            {
                case "StartButton":
                    ApplyButtonTheme(btn, UIColorTheme.AccentCyan);
                    break;
                case "ShopButton":
                    ApplyButtonTheme(btn, UIColorTheme.AccentMagenta);
                    break;
                case "TestButton":
                    ApplyButtonTheme(btn, UIColorTheme.AccentPink);
                    break;
            }
        }
    }

    /// <summary>
    /// 为主题标题文字着色（如果存在 TMP 或 Text 叫 "TitleText"）
    /// </summary>
    private void ApplyTitleTheme()
    {
        var texts = FindObjectsByType<Text>();
        foreach (var t in texts)
        {
            if (t.gameObject.name == "TitleText" || t.gameObject.name == "Title")
            {
                t.color = UIColorTheme.AccentCyan;
            }
        }
    }

    /// <summary>
    /// 对按钮应用统一主题色
    /// </summary>
    private void ApplyButtonTheme(Button btn, Color color)
    {
        if (btn == null) return;

        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }

        // 设置按钮颜色过渡
        var cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = UIColorTheme.ButtonHover;
        cb.pressedColor = new Color(color.r, color.g, color.b, 0.7f);
        cb.selectedColor = color;
        cb.disabledColor = UIColorTheme.TextSecondary;
        btn.colors = cb;

        // 按钮文字颜色
        var label = btn.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = UIColorTheme.TextPrimary;
        }
    }

    /// <summary>
    /// 动态创建 Test Mode 按钮
    /// </summary>
    private void CreateTestButton()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var allCanvases = FindObjectsByType<Canvas>();
            if (allCanvases.Length > 0) canvas = allCanvases[0];
        }
        if (canvas == null)
        {
            DebugHelper.LogWarning("[MenuBootstrap] Cannot find Canvas for TestButton!");
            return;
        }

        var btnObj = new GameObject("TestButton");
        btnObj.transform.SetParent(canvas.transform, false);

        var img = btnObj.AddComponent<Image>();
        img.color = UIColorTheme.AccentPink; // 亮粉

        var btn = btnObj.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = UIColorTheme.AccentPink;
        cb.highlightedColor = UIColorTheme.ButtonHover;
        cb.pressedColor = new Color(UIColorTheme.AccentPink.r, UIColorTheme.AccentPink.g, UIColorTheme.AccentPink.b, 0.7f);
        btn.colors = cb;

        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.18f);
        rect.anchorMax = new Vector2(0.5f, 0.18f);
        rect.sizeDelta = new Vector2(280, 50);

        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(btnObj.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "Test Mode (T)";
        text.font = UIFontProvider.DefaultFont;
        text.fontSize = 24;
        text.color = UIColorTheme.TextPrimary;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(StartTestMode);
        DebugHelper.Log("[MenuBootstrap] Test Mode button created dynamically");

        // Boss测试按钮
        CreateBossTestButton(canvas);
    }

    private void CreateBossTestButton(Canvas canvas)
    {
        var bossBtnObj = new GameObject("BossTestButton");
        bossBtnObj.transform.SetParent(canvas.transform, false);

        var bossImg = bossBtnObj.AddComponent<Image>();
        bossImg.color = new Color(0.8f, 0.2f, 0.2f);

        var bossBtn = bossBtnObj.AddComponent<Button>();
        var rect = bossBtnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.18f);
        rect.anchorMax = new Vector2(0.5f, 0.18f);
        rect.sizeDelta = new Vector2(280, 50);
        rect.anchoredPosition = new Vector2(0f, -60f);

        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(bossBtnObj.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "Boss Test (B)";
        text.font = UIFontProvider.DefaultFont;
        text.fontSize = 24;
        text.color = UIColorTheme.TextPrimary;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        bossBtn.onClick.AddListener(StartBossTestMode);
        DebugHelper.Log("[MenuBootstrap] Boss Test button created dynamically");
    }

    /// <summary>
    /// 测试模式 — 跳过选择，用默认配置直接开始
    /// </summary>
    private void StartTestMode()
    {
        DebugHelper.Log("[MenuBootstrap] Starting TEST mode (Warrior + Lightning + Teleport)...");
        GameReferences.TestMode = true;

        EventManager.ClearAll();
        MagnetMultiplierSystem.Reset();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains("GameScene"))
            {
                SceneManager.LoadScene(path);
                return;
            }
        }

        if (System.IO.File.Exists("Assets/Scenes/GameScene.unity"))
        {
            SceneManager.LoadScene("Assets/Scenes/GameScene.unity");
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            StartGame();
        }

            if (kb.tKey.wasPressedThisFrame)
        {
            StartTestMode();
        }

        // 按B键进入Boss测试模式
        if (kb.bKey.wasPressedThisFrame)
        {
            StartBossTestMode();
        }

        // #43 按 D 键开始每日挑战
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            StartDailyChallenge();
        }
    }

    /// <summary>
    /// #43 每日挑战模式 — 激活今日挑战规则后开始游戏
    /// </summary>
    private void StartDailyChallenge()
    {
        var challenge = DailyChallengeSystem.GetTodayChallenge();
        DebugHelper.Log($"[MenuBootstrap] Starting Daily Challenge (seed={challenge.seed})...");
        DebugHelper.Log($"  Rule 1: {DailyChallengeSystem.GetRuleDescription(challenge.rule1)}");
        DebugHelper.Log($"  Rule 2: {DailyChallengeSystem.GetRuleDescription(challenge.rule2)}");
        DebugHelper.Log($"  Rule 3: {DailyChallengeSystem.GetRuleDescription(challenge.rule3)}");

        // 激活每日挑战模式
        DailyChallengeSystem.ActivateDailyChallenge();

        EventManager.ClearAll();
        MagnetMultiplierSystem.Reset();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains("GameScene"))
            {
                SceneManager.LoadScene(path);
                return;
            }
        }

        if (System.IO.File.Exists("Assets/Scenes/GameScene.unity"))
        {
            SceneManager.LoadScene("Assets/Scenes/GameScene.unity");
        }
    }

    /// <summary>
    /// Boss测试模式 — 只有Boss出现，5波一波Boss
    /// </summary>
    private void StartBossTestMode()
    {
        DebugHelper.Log("[MenuBootstrap] Starting BOSS TEST mode...");
        GameReferences.TestMode = true;
        GameReferences.BossTestMode = true;

        EventManager.ClearAll();
        MagnetMultiplierSystem.Reset();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains("GameScene"))
            {
                SceneManager.LoadScene(path);
                return;
            }
        }

        if (System.IO.File.Exists("Assets/Scenes/GameScene.unity"))
        {
            SceneManager.LoadScene("Assets/Scenes/GameScene.unity");
        }
    }

    private void StartGame()
    {
        DebugHelper.Log("[MenuBootstrap] Starting game...");

        EventManager.ClearAll();
        MagnetMultiplierSystem.Reset();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains("GameScene"))
            {
                SceneManager.LoadScene(path);
                return;
            }
        }

        if (System.IO.File.Exists("Assets/Scenes/GameScene.unity"))
        {
            SceneManager.LoadScene("Assets/Scenes/GameScene.unity");
        }
        else
        {
            DebugHelper.LogError("[MenuBootstrap] GameScene not found! Please run 'Tools → Create Game Scene' first.");
        }
    }
}