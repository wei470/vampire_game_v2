using UnityEngine;

/// <summary>
/// Mage 引爆技能冷却 HUD — 在右下角显示引爆冷却状态。
/// 避免与左下角 HUD 重叠。
/// </summary>
public class DetonateHUD : MonoBehaviour
{
    private GUIStyle _readyStyle;
    private GUIStyle _cooldownStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _glowBgStyle;
    private GUIStyle _pulsingReadyStyle;
    private bool _stylesInitialized;

    // 脉冲动画
    private bool _wasReady;
    private float _pulseTime;

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _readyStyle = new GUIStyle(GUI.skin.label);
        _readyStyle.fontSize = 28;
        _readyStyle.fontStyle = FontStyle.Bold;
        _readyStyle.normal.textColor = new Color(0.8f, 0.2f, 1f);
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

        _glowBgStyle = new GUIStyle();
        _glowBgStyle.normal.background = Texture2D.whiteTexture;

        _pulsingReadyStyle = new GUIStyle(_readyStyle);
    }

    private void OnGUI()
    {
        InitStyles();

        var dotPassive = GameReferences.DotCharacterPassive;
        if (dotPassive == null) return;

        var det = dotPassive.GetDetonateSystem();
        if (det == null) return;

        float rightMargin = 20f;
        float bottomMargin = 80f;
        float width = 200f;
        float height = 60f;

        float x = Screen.width - rightMargin - width;
        float y = Screen.height - bottomMargin - height;

        GUI.Label(new Rect(x, y, width, 30f), "[E] 引爆", _labelStyle);

        if (det.DetonateReady)
        {
            if (!_wasReady)
            {
                _wasReady = true;
                _pulseTime = Time.unscaledTime;
            }

            float pulse = Time.unscaledTime - _pulseTime;
            float pulseAlpha = 0.5f + 0.5f * Mathf.Sin(pulse * 6f);

            Color glowColor = new Color(0.8f, 0.2f, 1f, 0.15f + 0.1f * pulseAlpha);
            GUI.color = glowColor;
            GUI.Box(new Rect(x - 5f, y + 23f, width + 10f, 40f), GUIContent.none, _glowBgStyle);
            GUI.color = Color.white;

            float fontSize = 28f + 4f * pulseAlpha;
            _pulsingReadyStyle.fontSize = Mathf.RoundToInt(fontSize);
            _pulsingReadyStyle.normal.textColor = Color.Lerp(new Color(0.8f, 0.2f, 1f), new Color(1f, 0.5f, 1f), pulseAlpha);

            GUI.Label(new Rect(x, y + 28f, width, 35f), "READY!", _pulsingReadyStyle);
        }
        else
        {
            _wasReady = false;

            float remaining = det.DetonateCooldownRemaining;
            float totalCd = det.DetonateCooldown;
            string cdText = $"{remaining:F1}s";
            GUI.Label(new Rect(x, y + 28f, width, 35f), cdText, _cooldownStyle);

            float progress = 1f - (remaining / totalCd);
            DrawCooldownArc(new Vector2(x + width - 10f, y + 45f), 18f, progress);
        }
    }

    private void DrawCooldownArc(Vector2 center, float radius, float progress)
    {
        int segments = 24;
        int activeSegments = Mathf.CeilToInt(segments * progress);

        GUI.color = new Color(0.3f, 0.1f, 0.4f, 0.3f);
        GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), Texture2D.whiteTexture);

        for (int i = 0; i < activeSegments; i++)
        {
            float angle = (float)i / segments * 360f - 90f;
            float rad = angle * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(rad) * radius * 0.7f;
            float y = center.y + Mathf.Sin(rad) * radius * 0.7f;
            float dotSize = 5f;

            GUI.color = Color.Lerp(new Color(0.5f, 0.1f, 0.7f), new Color(0.9f, 0.3f, 1f), (float)i / segments);
            GUI.DrawTexture(new Rect(x - dotSize / 2, y - dotSize / 2, dotSize, dotSize), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}