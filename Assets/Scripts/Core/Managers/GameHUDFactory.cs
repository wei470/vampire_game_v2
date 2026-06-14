using UnityEngine;

/// <summary>
/// 游戏 HUD 工厂 — 负责创建所有游戏内 HUD 组件
/// 从 GameSceneBootstrap 拆分而来，职责单一：只负责 HUD 创建
/// </summary>
public class GameHUDFactory
{
    private readonly GameObject _hostObject;

    public GameHUDFactory(GameObject hostObject)
    {
        _hostObject = hostObject;
    }

    /// <summary>
    /// 创建所有游戏启动前的 HUD（Boss 血量条、Debug 面板等）
    /// </summary>
    public void CreatePreGameHUD()
    {
        // #21 创建 BossHealthBarUI（Boss 战专属血条 + 阶段指示器）
        if (BossHealthBarUI.Instance == null)
        {
            var bossBarObj = new GameObject("BossHealthBarUI");
            bossBarObj.AddComponent<BossHealthBarUI>();
            DebugHelper.Log("[GameHUDFactory] Created BossHealthBarUI");
        }
    }

    /// <summary>
    /// 创建游戏内 HUD 组件（血量条、技能冷却、波次预警等）
    /// </summary>
    public void CreateInGameHUD(CharacterData currentCharacter)
    {
        // 左上角 - 玩家血量条
        if (_hostObject.GetComponent<PlayerHealthBarHUD>() == null)
            _hostObject.AddComponent<PlayerHealthBarHUD>();

        // 中上方 - Boss 血量条
        if (_hostObject.GetComponent<BossHealthBarHUD>() == null)
            _hostObject.AddComponent<BossHealthBarHUD>();

        // 右下角 - Mage引爆冷却显示
        if (_hostObject.GetComponent<DetonateHUD>() == null)
            _hostObject.AddComponent<DetonateHUD>();

        if (currentCharacter != null && currentCharacter.characterId == "mage")
        {
            if (_hostObject.GetComponent<MageStatsHUD>() == null)
            {
                _hostObject.AddComponent<MageStatsHUD>();
                DebugHelper.Log("[GameHUDFactory] Created MageStatsHUD for Mage character");
            }
        }

        // #22 敌人生成预警 UI
        if (SpawnWarningUI.Instance == null)
        {
            var warnObj = new GameObject("SpawnWarningUI");
            warnObj.AddComponent<SpawnWarningUI>();
            DebugHelper.Log("[GameHUDFactory] Created SpawnWarningUI");
        }

        // #25 波次间歇期统计 UI
        if (WaveIntermissionUI.Instance == null)
        {
            var intermissionObj = new GameObject("WaveIntermissionUI");
            intermissionObj.AddComponent<WaveIntermissionUI>();
            DebugHelper.Log("[GameHUDFactory] Created WaveIntermissionUI");
        }

        // 连击数 HUD
        if (_hostObject.GetComponent<ComboHUD>() == null)
            _hostObject.AddComponent<ComboHUD>();

        // #30 快捷键提示 HUD
        if (KeyHintHUD.Instance == null)
        {
            var keyHintObj = new GameObject("KeyHintHUD");
            var keyHint = keyHintObj.AddComponent<KeyHintHUD>();
            // Mage 角色使用专属键位（含 E 引爆）
            if (currentCharacter != null && currentCharacter.characterId == "mage")
            {
                keyHint.SetMageKeys();
            }
            DebugHelper.Log("[GameHUDFactory] Created KeyHintHUD");
        }
    }
}