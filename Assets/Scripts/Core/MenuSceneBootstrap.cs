using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 主菜单运行时引导器 — 处理菜单逻辑。
/// 1. Start 按钮点击后加载 GameScene
/// 2. 按 Enter/Space 也能开始游戏
/// </summary>
public class MenuSceneBootstrap : MonoBehaviour
{
    private ShopUI _shopUI;

    private void Start()
    {
        // 设置 GameManager 为 Menu 状态
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Menu);
        }

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

        // 查找 Start 按钮并绑定事件
        var buttons = FindObjectsByType<Button>();
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "StartButton")
            {
                btn.onClick.AddListener(StartGame);
                break;
            }
        }

        // 查找 Shop 按钮并绑定事件
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "ShopButton")
            {
                btn.onClick.AddListener(() =>
                {
                    if (_shopUI != null) _shopUI.OpenShop();
                });
                break;
            }
        }

        // 查找 Test 按钮，找不到则动态创建
        bool foundTest = false;
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "TestButton")
            {
                btn.onClick.AddListener(StartTestMode);
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
    /// 动态创建 Test Mode 按钮（运行时，当场景中没有 TestButton 时）
    /// </summary>
    private void CreateTestButton()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            // 备用：查找任何带 Canvas 的物体
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
        img.color = new Color(0.7f, 0.2f, 0.2f, 1f);

        var btn = btnObj.AddComponent<Button>();
        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.18f);
        rect.anchorMax = new Vector2(0.5f, 0.18f);
        rect.sizeDelta = new Vector2(280, 50);

        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(btnObj.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "Test Mode (T)";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(StartTestMode);
        DebugHelper.Log("[MenuBootstrap] Test Mode button created dynamically");
    }

    /// <summary>
    /// 测试模式 — 跳过选择，用默认配置直接开始
    /// </summary>
    private void StartTestMode()
    {
        DebugHelper.Log("[MenuBootstrap] Starting TEST mode (Warrior + Lightning + Teleport)...");
        GameReferences.TestMode = true;

        EventManager.ClearAll();
        LevelUpUI.ResetMagnetMultiplier();

        // 加载游戏场景
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

        // Enter 或 Space 开始游戏
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            StartGame();
        }

        // T 键快速测试模式
        if (kb.tKey.wasPressedThisFrame)
        {
            StartTestMode();
        }
    }

    private void StartGame()
    {
        DebugHelper.Log("[MenuBootstrap] Starting game...");

        // 清除旧事件订阅
        EventManager.ClearAll();
        LevelUpUI.ResetMagnetMultiplier();

        // 加载游戏场景
        // 优先用 Build Settings 中的 GameScene
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains("GameScene"))
            {
                SceneManager.LoadScene(path);
                return;
            }
        }

        // 备用方案：直接加载路径
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