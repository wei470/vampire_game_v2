using UnityEngine;

/// <summary>
/// 新手引导系统 — 第一局游戏时显示教学提示。
/// 根据波次进度逐步教玩家操作。
/// </summary>
public class TutorialSystem : MonoBehaviour
{
    private int _currentStep = 0;
    private bool _active = false;
    private float _messageEndTime;
    private string _currentMessage = "";
    private GUIStyle _style;

    public static TutorialSystem Instance { get; private set; }

    private struct TutorialStep
    {
        public int wave;
        public string message;
        public float duration;
    }

    private readonly TutorialStep[] _steps = new TutorialStep[]
    {
        new TutorialStep { wave = 1, message = "WASD 移动，鼠标瞄准射击", duration = 5f },
        new TutorialStep { wave = 3, message = "拾取经验球升级！", duration = 4f },
        new TutorialStep { wave = 5, message = "选择升级选项强化自己", duration = 4f },
        new TutorialStep { wave = 10, message = "按 E 引爆所有DOT！", duration = 5f },
        new TutorialStep { wave = 15, message = "不同元素组合触发反应！", duration = 5f },
        new TutorialStep { wave = 20, message = "通关难度20波解锁新难度！", duration = 5f },
    };

    private void Awake()
    {
        Instance = this;
        // Only activate tutorial for first game
        if (SaveManager.Instance != null && SaveManager.Instance.Data.totalGames <= 1)
            _active = true;
    }

    public void OnWaveStart(int wave)
    {
        if (!_active) return;
        if (_currentStep >= _steps.Length) return;

        if (wave >= _steps[_currentStep].wave)
        {
            _currentMessage = _steps[_currentStep].message;
            _messageEndTime = Time.time + _steps[_currentStep].duration;
            _currentStep++;
        }
    }

    private void OnGUI()
    {
        if (!_active || string.IsNullOrEmpty(_currentMessage)) return;
        if (Time.time > _messageEndTime) { _currentMessage = ""; return; }

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 0.5f) }
            };
        }

        float w = 500;
        float h = 40;
        float x = (Screen.width - w) / 2;
        float y = Screen.height - 100;

        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(x - 5, y - 5, w + 10, h + 10), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, w, h), _currentMessage, _style);
    }
}
