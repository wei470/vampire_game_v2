using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 角色工厂 — 根据 CharacterData 创建对应的角色被动组件。
///
/// 新角色只需在初始化时注册：
///   CharacterFactory.Register("warrior", go => go.AddComponent<BloodWarriorPassive>());
/// </summary>
public static class CharacterFactory
{
    private static readonly Dictionary<string, Func<GameObject, ICharacterPassive>> _creators
        = new Dictionary<string, Func<GameObject, ICharacterPassive>>();

    private static bool _initialized = false;

    /// <summary>
    /// 注册角色创建器（游戏启动时调用一次）
    /// </summary>
    public static void Register(string characterId, Func<GameObject, ICharacterPassive> creator)
    {
        _creators[characterId] = creator;
    }

    /// <summary>
    /// 创建角色被动组件
    /// </summary>
    public static ICharacterPassive Create(string characterId, GameObject player)
    {
        EnsureInitialized();

        if (_creators.TryGetValue(characterId, out var creator))
        {
            var passive = creator(player);
            DebugHelper.Log($"[CharacterFactory] Created '{characterId}' passive: {passive.GetType().Name}");
            return passive;
        }

        // 回退：默认创建 MagePassive
        DebugHelper.Log($"[CharacterFactory] Unknown characterId '{characterId}', using MagePassive");
        return player.AddComponent<MagePassive>();
    }

    /// <summary>
    /// 检查角色是否已注册
    /// </summary>
    public static bool IsRegistered(string characterId)
    {
        EnsureInitialized();
        return _creators.ContainsKey(characterId);
    }

    /// <summary>
    /// 获取所有已注册的角色ID
    /// </summary>
    public static IEnumerable<string> GetRegisteredIds()
    {
        EnsureInitialized();
        return _creators.Keys;
    }

    /// <summary>
    /// 清除所有注册（场景切换时调用）
    /// </summary>
    public static void Clear()
    {
        _creators.Clear();
        _initialized = false;
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        Register("mage", go => go.AddComponent<MagePassive>());
        Register("warrior", go => go.AddComponent<MagePassive>());
        Register("ranger", go => go.AddComponent<MagePassive>());
        Register("vampire", go => go.AddComponent<MagePassive>());
        Register("assassin", go => go.AddComponent<MagePassive>());
        Register("paladin", go => go.AddComponent<MagePassive>());
        Register("necromancer", go => go.AddComponent<MagePassive>());
        Register("berserker", go => go.AddComponent<MagePassive>());
    }
}
