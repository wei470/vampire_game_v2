using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色配置加载器 — 根据 characterId 加载对应的 ICharacterConfig。
/// 每个角色的升级配置通过 Resources.Load 加载。
/// </summary>
public static class CharacterConfigLoader
{
    private static readonly Dictionary<string, ICharacterConfig> _cache = new();

    /// <summary>
    /// 加载角色配置（带缓存）
    /// </summary>
    public static ICharacterConfig Load(string characterId)
    {
        if (_cache.TryGetValue(characterId, out var cached))
            return cached;

        // 按约定路径加载：Configs/{characterId}UpgradeConfig
        var config = Resources.Load<MageUpgradeConfig>($"Configs/{characterId}UpgradeConfig");
        // TODO: 当新角色添加时，扩展此方法支持其他配置类型
        
        if (config != null)
        {
            _cache[characterId] = config;
            return config;
        }

        DebugHelper.LogWarning($"[CharacterConfigLoader] No config found for '{characterId}'");
        return null;
    }

    /// <summary>
    /// 清除缓存（场景切换时调用）
    /// </summary>
    public static void ClearCache()
    {
        _cache.Clear();
    }
}
