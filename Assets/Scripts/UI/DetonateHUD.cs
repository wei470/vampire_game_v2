using UnityEngine;

/// <summary>
/// Mage 引爆技能冷却 HUD — 在右下角显示引爆冷却状态。
/// 避免与左下角的 SkillHUD 重叠。
/// #17 增强：冷却完成时脉冲发光 + 环形进度条
/// </summary>
public class DetonateHUD : MonoBehaviour
{
    private GUIStyle _readyStyle;
    private GUIStyle _cooldownStyle;
    private GUIStyle _labelStyle;
    private bool _stylesInitialized;

    // #17 脉冲动画
    private bool _wasReady;
    private float _pulseTime;

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _readyStyle = new GUIStyle(GUI.skin.label);
        _readyStyle.fontSize = 28;
        _readyStyle.fontStyle = FontStyle.Bold;
        _readyStyle.normal.textColor = new Color(0.8f, 0.2f, 1f); // 紫色
        _readyStyle.alignment = TextAnchor.MiddleRight;

        _cooldownStyle = new GUIStyle(GUI.skin.label);
        _cooldownStyle.fontSize = 28;
        _cooldownStyle.fontStyle = FontStyle.Bold;
        _cooldownStyle.normal.textColor = Color.gray;
        _cooldownStyle.alignment = TextAnchor.MiddleRight;

        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.fontSize = 20;
        _labelStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        _labelStyle.alignment = TextAnchor.MiddleRight;
    }

    private void OnGUI()
    {
        InitStyles();

        // 检查是否是 Mage 角色
        if (GameSceneBootstrap.CurrentCharacter == null) return;
        var cc = GameSceneBootstrap.CurrentCharacter;
        bool isMage = cc.characterId == "mage" || cc.characterName.ToLower().Contains("mage");
        if (!isMage) return;

        // 获取 MagePassive
        var player = GameReferences.Player;
        if (player == null) return;
        var mage = player.GetComponent<MagePassive>();
        if (mage == null) return;

        // 右下角位置（屏幕坐标）
        float rightMargin = 20f;
        float bottomMargin = 80f; // 避开其他UI
        float width = 200f;
        float height = 60f;

        float x = Screen.width - rightMargin - width;
        float y = Screen.height - bottomMargin - height;

        // 标签
        GUI.Label(new Rect(x, y, width, 30f), "[E] 引爆", _labelStyle);

        // 冷却状态
        if (mage.DetonateReady)
        {
            // #17 脉冲发光效果
            if (!_wasReady)
            {
                _wasReady = true;
                _pulseTime = Time.unscaledTime;
            }

            float pulse = Time.unscaledTime - _pulseTime;
            float pulseAlpha = 0.5f + 0.5f * Mathf.Sin(pulse * 6f); // 快速脉冲
            float pulseScale = 1f + 0.05f * Mathf.Sin(pulse * 4f); // 轻微缩放

            // 脉冲发光背景
            Color glowColor = new Color(0.8f, 0.2f, 1f, 0.15f + 0.1f * pulseAlpha);
            GUIStyle glowBg = new GUIStyle();
            glowBg.normal.background = Texture2D.whiteTexture;
            GUI.color = glowColor;
            GUI.Box(new Rect(x - 5f, y + 23f, width + 10f, 40f), GUIContent.none, glowBg);
            GUI.color = Color.white;

            // READY 文字带脉冲
            GUIStyle pulsingReady = new GUIStyle(_readyStyle);
            float fontSize = 28f + 4f * pulseAlpha;
            pulsingReady.fontSize = Mathf.RoundToInt(fontSize);
            pulsingReady.normal.textColor = Color.Lerp(new Color(0.8f, 0.2f, 1f), new Color(1f, 0.5f, 1f), pulseAlpha);

            GUI.Label(new Rect(x, y + 28f, width, 35f), "READY!", pulsingReady);
        }
        else
        {
            _wasReady = false;

            float remaining = mage.DetonateCooldownRemaining;
            float totalCd = mage.DetonateCooldown;
            string cdText = $"{remaining:F1}s";
            GUI.Label(new Rect(x, y + 28f, width, 35f), cdText, _cooldownStyle);

            // #17 环形进度条（用扇形表示冷却进度）
            float progress = 1f - (remaining / totalCd); // 0→1
            DrawCooldownArc(new Vector2(x + width - 10f, y + 45f), 18f, progress);
        }
    }

    /// <summary>
    /// #17 绘制冷却弧形进度条
    /// </summary>
    private void DrawCooldownArc(Vector2 center, float radius, float progress)
    {
        // 使用 OnGUI 的 GL 绘制扇形
        // 由于 OnGUI 中不方便用 GL，改用多个小矩形近似弧形
        int segments = 24;
        int activeSegments = Mathf.CeilToInt(segments * progress);

        // 背景圆
        GUI.color = new Color(0.3f, 0.1f, 0.4f, 0.3f);
        GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), Texture2D.whiteTexture);

        // 进度扇形
        for (int i = 0; i < activeSegments; i++)
        {
            float angle = (float)i / segments * 360f - 90f; // 从顶部开始
            float rad = angle * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(rad) * radius * 0.7f;
            float y = center.y + Mathf.Sin(rad) * radius * 0.7f;
            float dotSize = 5f;

            // 紫色进度点
            GUI.color = Color.Lerp(new Color(0.5f, 0.1f, 0.7f), new Color(0.9f, 0.3f, 1f), (float)i / segments);
            GUI.DrawTexture(new Rect(x - dotSize / 2, y - dotSize / 2, dotSize, dotSize), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}