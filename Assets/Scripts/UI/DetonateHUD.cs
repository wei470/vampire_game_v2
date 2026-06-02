using UnityEngine;

/// <summary>
/// Mage 引爆技能冷却 HUD — 在右下角显示引爆冷却状态。
/// 避免与左下角的 SkillHUD 重叠。
/// </summary>
public class DetonateHUD : MonoBehaviour
{
    private GUIStyle _readyStyle;
    private GUIStyle _cooldownStyle;
    private GUIStyle _labelStyle;
    private bool _stylesInitialized;

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
            GUI.Label(new Rect(x, y + 28f, width, 35f), "READY!", _readyStyle);
        }
        else
        {
            float remaining = mage.DetonateCooldownRemaining;
            string cdText = $"{remaining:F1}s";
            GUI.Label(new Rect(x, y + 28f, width, 35f), cdText, _cooldownStyle);
        }
    }
}