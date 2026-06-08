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
        var allMono = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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

        // 10. 清除 DamagePopup 对象池
        DamagePopup.ResetPool();

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

    private static T GetSingletonInstance<T>() where T : MonoBehaviour
    {
        var prop = typeof(T).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (prop != null)
            return prop.GetValue(null) as T;
        return null;
    }
}