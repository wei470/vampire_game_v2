using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// #24 DOT 效果组件生命周期统一管理
/// 
/// 统一注册/注销所有活跃 DOT 效果组件（BurnStackEffect/PoisonStackEffect/FrostEffect/StaticStackEffect），
/// 支持批量操作（如引爆时遍历、场景清理）。
/// 
/// 使用方式：
///   - 组件在 OnEnable 调用 Register(this)，在 OnDisable 调用 Unregister(this)
///   - DetonateSystem 可通过 GetActiveEffects() 或 GetEffectsOnEnemy(enemy) 查询
/// </summary>
public static class DotEffectRegistry
{
    // ── 活跃效果集合（HashSet 查找 O(1)）──
    private static readonly HashSet<BurnStackEffect> _burnEffects = new HashSet<BurnStackEffect>();
    private static readonly HashSet<PoisonStackEffect> _poisonEffects = new HashSet<PoisonStackEffect>();
    private static readonly HashSet<FrostEffect> _frostEffects = new HashSet<FrostEffect>();
    private static readonly HashSet<StaticStackEffect> _staticEffects = new HashSet<StaticStackEffect>();

    // ── 统一基类注册（供 DotStatusBar 查询）──
    private static readonly HashSet<StackEffectBase> _stackEffects = new HashSet<StackEffectBase>();

    // ── 临时缓存（避免批量操作时分配）──
    private static readonly List<Component> _tempBuffer = new List<Component>(32);
    private static readonly List<IStackEffect> _tempStackBuffer = new List<IStackEffect>(8);

    // ═══ 注册/注销 ═══

    public static void Register(BurnStackEffect effect)
    {
        if (effect != null) _burnEffects.Add(effect);
    }

    public static void Unregister(BurnStackEffect effect)
    {
        _burnEffects.Remove(effect);
    }

    public static void Register(PoisonStackEffect effect)
    {
        if (effect != null) _poisonEffects.Add(effect);
    }

    public static void Unregister(PoisonStackEffect effect)
    {
        _poisonEffects.Remove(effect);
    }

    public static void Register(FrostEffect effect)
    {
        if (effect != null) _frostEffects.Add(effect);
    }

    public static void Unregister(FrostEffect effect)
    {
        _frostEffects.Remove(effect);
    }

    public static void Register(StaticStackEffect effect)
    {
        if (effect != null) _staticEffects.Add(effect);
    }

    public static void Unregister(StaticStackEffect effect)
    {
        _staticEffects.Remove(effect);
    }

    // ── StackEffectBase 统一注册/注销 ──

    public static void RegisterEffect(StackEffectBase effect)
    {
        if (effect != null) _stackEffects.Add(effect);
    }

    public static void UnregisterEffect(StackEffectBase effect)
    {
        _stackEffects.Remove(effect);
    }

    // ═══ 查询方法 ═══

    /// <summary>
    /// 获取指定敌人身上所有活跃的 DOT 效果组件
    /// </summary>
    public static List<Component> GetEffectsOnEnemy(GameObject enemy, List<Component> buffer = null)
    {
        var result = buffer ?? _tempBuffer;
        result.Clear();
        if (enemy == null) return result;

        var burn = enemy.GetComponent<BurnStackEffect>();
        if (burn != null && _burnEffects.Contains(burn)) result.Add(burn);

        var poison = enemy.GetComponent<PoisonStackEffect>();
        if (poison != null && _poisonEffects.Contains(poison)) result.Add(poison);

        var frost = enemy.GetComponent<FrostEffect>();
        if (frost != null && _frostEffects.Contains(frost)) result.Add(frost);

        var stat = enemy.GetComponent<StaticStackEffect>();
        if (stat != null && _staticEffects.Contains(stat)) result.Add(stat);

        return result;
    }

    /// <summary>
    /// 检查指定敌人是否有任何活跃的 DOT 效果
    /// </summary>
    public static bool HasAnyEffect(GameObject enemy)
    {
        if (enemy == null) return false;
        return enemy.GetComponent<BurnStackEffect>() != null
            || enemy.GetComponent<PoisonStackEffect>() != null
            || enemy.GetComponent<FrostEffect>() != null
            || enemy.GetComponent<StaticStackEffect>() != null
            || enemy.GetComponent<LightMarkEffect>() != null
            || enemy.GetComponent<DarkMarkEffect>() != null
            || enemy.GetComponent<WindErosionEffect>() != null;
    }

    /// <summary>
    /// 获取指定敌人身上所有活跃的 IStackEffect（查找所有 MonoBehaviour 组件中的 IStackEffect）
    /// </summary>
    public static List<IStackEffect> GetStackEffectsOnEnemy(GameObject enemy, List<IStackEffect> buffer = null)
    {
        var result = buffer ?? _tempStackBuffer;
        result.Clear();
        if (enemy == null) return result;

        var components = enemy.GetComponents<MonoBehaviour>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IStackEffect effect && effect.IsActive)
                result.Add(effect);
        }
        return result;
    }

    /// <summary>
    /// 统计指定敌人身上的 DOT 效果类型数量
    /// </summary>
    public static int CountEffectsOnEnemy(GameObject enemy)
    {
        if (enemy == null) return 0;
        int count = 0;
        if (enemy.GetComponent<BurnStackEffect>() != null) count++;
        if (enemy.GetComponent<PoisonStackEffect>() != null) count++;
        if (enemy.GetComponent<FrostEffect>() != null) count++;
        if (enemy.GetComponent<StaticStackEffect>() != null) count++;
        return count;
    }

    /// <summary>
    /// 获取所有活跃的燃烧效果数量
    /// </summary>
    public static int ActiveBurnCount => _burnEffects.Count;

    /// <summary>
    /// 获取所有活跃的中毒效果数量
    /// </summary>
    public static int ActivePoisonCount => _poisonEffects.Count;

    /// <summary>
    /// 获取所有活跃的霜冻效果数量
    /// </summary>
    public static int ActiveFrostCount => _frostEffects.Count;

    /// <summary>
    /// 获取所有活跃的静电效果数量
    /// </summary>
    public static int ActiveStaticCount => _staticEffects.Count;

    /// <summary>
    /// 获取所有活跃 DOT 效果总数
    /// </summary>
    public static int ActiveTotalCount => _burnEffects.Count + _poisonEffects.Count + _frostEffects.Count + _staticEffects.Count;

    // ═══ 批量操作 ═══

    /// <summary>
    /// 清除所有注册（场景切换时调用）
    /// </summary>
    public static void ClearAll()
    {
        _burnEffects.Clear();
        _poisonEffects.Clear();
        _frostEffects.Clear();
        _staticEffects.Clear();
        _stackEffects.Clear();
    }

    /// <summary>
    /// 强制移除指定敌人身上所有 DOT 效果
    /// </summary>
    public static void RemoveAllEffectsOnEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        var burn = enemy.GetComponent<BurnStackEffect>();
        if (burn != null) { _burnEffects.Remove(burn); Object.Destroy(burn); }

        var poison = enemy.GetComponent<PoisonStackEffect>();
        if (poison != null) { _poisonEffects.Remove(poison); Object.Destroy(poison); }

        var frost = enemy.GetComponent<FrostEffect>();
        if (frost != null) { _frostEffects.Remove(frost); Object.Destroy(frost); }

        var stat = enemy.GetComponent<StaticStackEffect>();
        if (stat != null) { _staticEffects.Remove(stat); Object.Destroy(stat); }
    }

    /// <summary>
    /// 清理已销毁的引用（防御性维护，可在低频路径调用）
    /// </summary>
    public static void CleanupNulls()
    {
        _burnEffects.RemoveWhere(e => e == null);
        _poisonEffects.RemoveWhere(e => e == null);
        _frostEffects.RemoveWhere(e => e == null);
        _staticEffects.RemoveWhere(e => e == null);
        _stackEffects.RemoveWhere(e => e == null);
    }
}