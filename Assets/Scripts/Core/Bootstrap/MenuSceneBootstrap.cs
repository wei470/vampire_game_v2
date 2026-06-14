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
        if (!Application.isPlaying) return;

        _ = SaveManager.Instance;

        // 确保 BGMManager 存在并播放主菜单 BGM
        if (BGMManager.Instance == null)
        {
            var bgmObj = new GameObject("BGMManager");
            bgmObj.AddComponent<BGMManager>();
        }
        BGMManager.Instance.PlayClip("music1");

        _shopUI = FindAnyObjectByType<ShopUI>();
        if (_shopUI == null)
        {
            var shopObj = new GameObject("ShopUI");
            _shopUI = shopObj.AddComponent<ShopUI>();
        }

        bool isAdmin = AdminConfig.Instance.IsAdmin;

        // 绑定按钮事件
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
                    if (!isAdmin) btn.gameObject.SetActive(false);
                    break;
                case "TestButton":
                    btn.onClick.AddListener(StartTestMode);
                    if (!isAdmin) btn.gameObject.SetActive(false);
                    break;
                case "DailyButton":
                    btn.onClick.AddListener(StartDailyChallenge);
                    if (!isAdmin) btn.gameObject.SetActive(false);
                    break;
            }
        }

        if (!isAdmin) return;

        // 管理员模式：动态创建测试按钮
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var allCanvases = FindObjectsByType<Canvas>();
            if (allCanvases.Length > 0) canvas = allCanvases[0];
        }

        bool foundTest = false;
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "TestButton") { foundTest = true; break; }
        }

        if (!foundTest && canvas != null)
        {
            var testBtn = MenuButtonFactory.CreateTestButton(canvas);
            testBtn.onClick.AddListener(StartTestMode);
        }

        if (canvas != null)
        {
            var bossBtn = MenuButtonFactory.CreateBossTestButton(canvas);
            bossBtn.onClick.AddListener(StartBossTestMode);
            var dpsBtn = MenuButtonFactory.CreateDpsTestButton(canvas);
            dpsBtn.onClick.AddListener(StartDpsTestMode);
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

        if (!AdminConfig.Instance.IsAdmin) return;

        if (kb.tKey.wasPressedThisFrame)
        {
            StartTestMode();
        }

        if (kb.bKey.wasPressedThisFrame)
        {
            StartBossTestMode();
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            StartDailyChallenge();
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            StartDpsTestMode();
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

    /// <summary>
    /// DPS 测试模式 — 选择子弹和强化后进入木桩房间
    /// </summary>
    private void StartDpsTestMode()
    {
        DebugHelper.Log("[MenuBootstrap] Starting DPS TEST mode...");
        GameReferences.TestMode = true;
        GameReferences.DpsTestMode = true;

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
            SceneManager.LoadScene("Assets/Scenes/GameScene.unity");
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