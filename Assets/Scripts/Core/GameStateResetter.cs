using UnityEngine;

/// <summary>
/// #37 游戏状态重置工具类 — 封装所有返回菜单/重启时的重置步骤。
/// PauseMenuUI 和 GameOverUI 都调用 GameStateResetter.FullReset()。
/// 新增重置项只需修改一处。
/// </summary>
public static class GameStateResetter
{
    /// <summary>
    /// 完整重置所有游戏状态（事件、引用、单例、子系统）
    /// </summary>
    public static void FullReset()
    {
        // 0. 先冻结时间
        Time.timeScale = 0f;

        // 0.5 停止场景中所有 MonoBehaviour 协程（防止 WaitForSeconds 卡死）
        var allMono = Object.FindObjectsByType<MonoBehaviour>();
        if (allMono != null)
        {
            for (int i = 0; i < allMono.Length; i++)
            {
                if (allMono[i] != null)
                    allMono[i].StopAllCoroutines();
            }
        }

        // 1. 清除所有事件订阅，防止 Destroy 期间回调级联
        EventManager.ClearAll();

        // 1.5 禁用所有 MonoBehaviour，防止销毁期间回调
        if (allMono != null)
        {
            for (int i = 0; i < allMono.Length; i++)
            {
                if (allMono[i] != null)
                    allMono[i].enabled = false;
            }
        }

        // 2. 重置静态状态
        MagnetMultiplierSystem.Reset();
        DotComboSystem.ResetEvolutionComboMultiplier();
        DotEffectRegistry.ClearAll();

        // 3. 重置全局引用缓存
        GameReferences.Reset();

        // 4. 重置角色选择静态数据
        GameSceneBootstrap.ResetCharacter();

        // 5. 销毁 SFX 系统
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopAll();
            Object.DestroyImmediate(SFXManager.Instance.gameObject);
        }

        // 6. 销毁 BGM
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.Stop();
            Object.DestroyImmediate(BGMManager.Instance.gameObject);
        }

        // 7. 销毁伤害统计
        if (DamageMeter.Instance != null)
        {
            DamageMeter.Instance.ResetStats();
            Object.DestroyImmediate(DamageMeter.Instance.gameObject);
        }

        // 8. 销毁所有 DontDestroyOnLoad 单例
        DestroySingletonImmediate<ObjectPool>();
        DestroySingletonImmediate<CombatManager>();
        SaveAndDestroySingletonImmediate<SaveManager>();
        DestroySingletonImmediate<OffScreenCuller>();

        // 8.5 清理场景中残留的战斗对象（毒雾池、DOT子弹、火焰区域等）
        CleanupLingeringCombatObjects();

        // 9. 清理场景中残留的敌人
        int enemyCount = 0;
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>())
        {
            if (enemy != null)
            {
                Object.DestroyImmediate(enemy.gameObject);
                enemyCount++;
            }
        }
        foreach (var boss in Object.FindObjectsByType<BossEnemy>())
        {
            if (boss != null)
            {
                Object.DestroyImmediate(boss.gameObject);
                enemyCount++;
            }
        }
        if (enemyCount > 0)
            DebugHelper.Log($"[GameStateResetter] Destroyed {enemyCount} enemies");

        // 9.5 清除 ChainLine 等临时特效对象
        CleanupTempEffects();

        // 10. 完全清理 DamagePopup 对象池（销毁 DontDestroyOnLoad 池父级）
        DamagePopup.FullCleanup();

        // 11. 恢复时间缩放
        Time.timeScale = 1f;
    }

    private static void DestroySingletonImmediate<T>() where T : MonoBehaviour
    {
        var instance = GetSingletonInstance<T>();
        if (instance != null)
            Object.DestroyImmediate(instance.gameObject);
    }

    private static void SaveAndDestroySingletonImmediate<T>() where T : SaveManager
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
            Object.DestroyImmediate(SaveManager.Instance.gameObject);
        }
    }

    /// <summary>
    /// 清理所有战斗残留物：毒雾池、DOT子弹、火焰区域等
    /// </summary>
    private static void CleanupLingeringCombatObjects()
    {
        int count = 0;

        // 清理毒液池
        foreach (var obj in Object.FindObjectsByType<PoisonPuddle>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理DOT子弹（毒/雷/冰/火/暗）
        foreach (var obj in Object.FindObjectsByType<PoisonBullet>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }
        foreach (var obj in Object.FindObjectsByType<LightningBullet>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }
        foreach (var obj in Object.FindObjectsByType<FrostBullet>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }
        foreach (var obj in Object.FindObjectsByType<BurnBullet>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }
        foreach (var obj in Object.FindObjectsByType<DarkBullet>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理火焰区域
        foreach (var obj in Object.FindObjectsByType<FireZone>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理毒药瓶
        foreach (var obj in Object.FindObjectsByType<PoisonPotion>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理其他通用投射物（子弹、弹幕）
        foreach (var obj in Object.FindObjectsByType<Projectile>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理追踪投射物
        foreach (var obj in Object.FindObjectsByType<HomingProjectile>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理敌人子弹
        foreach (var obj in Object.FindObjectsByType<EnemyBullet>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理地雷陷阱
        foreach (var obj in Object.FindObjectsByType<MineTrap>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        // 清理毒液飞镖
        foreach (var obj in Object.FindObjectsByType<VenomDart>())
        {
            if (obj != null) { Object.DestroyImmediate(obj.gameObject); count++; }
        }

        if (count > 0)
            DebugHelper.Log($"[GameStateResetter] Destroyed {count} lingering combat objects");
    }

    /// <summary>
    /// 清理临时特效对象（连锁闪电线条等）
    /// </summary>
    private static void CleanupTempEffects()
    {
        // 清理场景中所有名为 "ChainLine" 或 "PoisonBurstText" 的临时特效
        int count = 0;
        foreach (var obj in Object.FindObjectsByType<GameObject>())
        {
            if (obj != null && (obj.name == "ChainLine" || obj.name == "PoisonBurstText"))
            {
                Object.DestroyImmediate(obj);
                count++;
            }
        }
        if (count > 0)
            DebugHelper.Log($"[GameStateResetter] Destroyed {count} temp effect objects");
    }

    private static T GetSingletonInstance<T>() where T : MonoBehaviour
    {
        var prop = typeof(T).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (prop != null)
            return prop.GetValue(null) as T;
        return null;
    }
}
