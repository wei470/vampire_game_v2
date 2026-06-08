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
        // 0. 【最高优先级】先冻结时间，防止后续 Update/FixedUpdate 触发
        Time.timeScale = 0f;

        // 1. 【关键】先清除所有事件订阅，防止 Destroy 期间触发回调级联（导致冻结/崩溃）
        EventManager.ClearAll();

        // 2. 重置升级相关静态状态
        MagnetMultiplierSystem.Reset();

        // 3. 重置全局引用缓存（防止旧玩家引用残留）
        GameReferences.Reset();

        // 4. 重置角色选择静态数据
        GameSceneBootstrap.ResetCharacter();

        // 5. 重置并销毁 SFX 系统（先于 ObjectPool，避免播放音效引用已销毁对象）
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopAll();
            Object.Destroy(SFXManager.Instance.gameObject);
        }

        // 6. 重置并销毁 BGM
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.Stop();
            Object.Destroy(BGMManager.Instance.gameObject);
        }

        // 7. 重置并销毁伤害统计
        if (DamageMeter.Instance != null)
        {
            DamageMeter.Instance.ResetStats();
            Object.Destroy(DamageMeter.Instance.gameObject);
        }

        // 8. 销毁所有 DontDestroyOnLoad 单例，确保下次进入干净重建
        DestroySingleton<ObjectPool>();
        DestroySingleton<CombatManager>();
        SaveAndDestroySingleton<SaveManager>();
        DestroySingleton<OffScreenCuller>();

        // 9. 清理场景中残留的敌人/子弹（用 Destroy 而非 DestroyImmediate，避免回调级联）
        int enemyCount = 0;
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>())
        {
            if (enemy != null)
            {
                Object.Destroy(enemy.gameObject);
                enemyCount++;
            }
        }
        foreach (var boss in Object.FindObjectsByType<BossEnemy>())
        {
            if (boss != null)
            {
                Object.Destroy(boss.gameObject);
                enemyCount++;
            }
        }
        if (enemyCount > 0)
            DebugHelper.Log($"[GameStateResetter] Destroyed {enemyCount} enemies");
    }

    private static void DestroySingleton<T>() where T : MonoBehaviour
    {
        var instance = GetSingletonInstance<T>();
        if (instance != null)
            Object.Destroy(instance.gameObject);
    }

    private static void SaveAndDestroySingleton<T>() where T : SaveManager
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
            Object.Destroy(SaveManager.Instance.gameObject);
        }
    }

    private static T GetSingletonInstance<T>() where T : MonoBehaviour
    {
        // 使用反射获取 Instance 属性（所有 Singleton<T> 都有）
        var prop = typeof(T).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (prop != null)
            return prop.GetValue(null) as T;
        return null;
    }
}