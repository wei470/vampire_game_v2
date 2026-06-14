using UnityEngine;

public static class GameStateResetter
{
    private static bool _isResetting = false;

    public static void FullReset()
    {
        if (_isResetting) { return; }
        _isResetting = true;
        try { FullResetInternal(); }
        catch (System.Exception e) { DebugHelper.LogError($"[GameStateResetter] {e}"); }
        finally { _isResetting = false; }
    }

    private static void FullResetInternal()
    {
        EventManager.ClearAll();
        MagnetMultiplierSystem.Reset();
        DotComboSystem.ResetEvolutionComboMultiplier();
        DotEffectRegistry.ClearAll();
        CurseSpreadSystem.ResetStaticState();
        GameReferences.Reset();
        GameSceneBootstrap.ResetCharacter();
        DamagePopup.FullCleanup();
        CharacterFactory.Clear();
        CharacterConfigLoader.ClearCache();
        VFXPool.ClearAll();

        // 清理静态列表（防止场景重载泄漏）
        DotBulletBase.ActiveDotBullets.Clear();
        SimpleBullet.ActiveBullets.Clear();
        Coin.All.Clear();
        XPGem.All.Clear();
        EnvironmentZone.All.Clear();
        Backpack.Clear();

        // 用 Destroy（延迟）而非 DestroyImmediate —— 避免 OnDestroy 回调级联
        // LoadScene 会销毁所有场景对象，DontDestroyOnLoad 对象在帧末延迟销毁
        MarkDestroy(SFXManager.Instance);
        MarkDestroy(BGMManager.Instance);
        MarkDestroy(DamageMeter.Instance);
        MarkDestroy(ObjectPool.Instance);
        MarkDestroy(CombatManager.Instance);
        MarkDestroy(SaveManager.Instance);
        MarkDestroy(OffScreenCuller.Instance);
    }

    private static void MarkDestroy(MonoBehaviour inst)
    {
        if (inst != null) Object.Destroy(inst.gameObject);
    }
}
