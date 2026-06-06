using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 存档数据结构定义 — 包含所有存档相关的数据类。
/// 从 SaveManager 中提取，减少主文件行数。
/// </summary>

/// <summary>
/// 商店升级定义
/// </summary>
[Serializable]
public class ShopUpgradeDef
{
    public string name;
    public string desc;
    public string attr;
    public float amount;
    public float multiplier;
    public int cost;
    public int maxLevel;

    public ShopUpgradeDef(string name, string desc, string attr, float amount, float multiplier, int cost, int maxLevel = 20)
    {
        this.name = name; this.desc = desc; this.attr = attr;
        this.amount = amount; this.multiplier = multiplier;
        this.cost = cost; this.maxLevel = maxLevel;
    }
}

/// <summary>
/// 里程碑定义
/// </summary>
[Serializable]
public class MilestoneDef
{
    public string id; public string name; public string desc;
    public string stat; public int threshold; public string rewardDesc;
    public string effectAttr; public float effectValue;

    public MilestoneDef(string id, string name, string desc, string stat, int threshold,
        string rewardDesc, string effectAttr, float effectValue)
    {
        this.id = id; this.name = name; this.desc = desc;
        this.stat = stat; this.threshold = threshold;
        this.rewardDesc = rewardDesc; this.effectAttr = effectAttr; this.effectValue = effectValue;
    }
}

/// <summary>
/// 存档主数据
/// </summary>
[Serializable]
public class SaveGameData
{
    public int saveVersion = 1;
    public int coins;
    public SerializableDictionary upgrades = new SerializableDictionary();
    public int highestWave;
    public int totalKills;
    public int totalGames;
    public string[] claimedMilestones = new string[0];
}

/// <summary>
/// 存档包装器 — 包含实际数据 + CRC32 校验码
/// </summary>
[Serializable]
public class SaveWrapper
{
    public string data;       // SaveGameData 的 JSON 字符串
    public string checksum;   // CRC32 校验码（十六进制字符串）
}

/// <summary>
/// 可序列化字典 — JSON 不支持 Dictionary，用平行数组代替。
/// 运行时缓存 Dictionary 避免 O(n) 线性搜索。
/// </summary>
[Serializable]
public class SerializableDictionary
{
    public string[] keys = new string[0];
    public int[] values = new int[0];

    /// <summary>
    /// 运行时 Dictionary 缓存
    /// </summary>
    [NonSerialized] private Dictionary<string, int> _dict;
    [NonSerialized] private bool _dirty = false;

    private void EnsureBuilt()
    {
        if (_dict == null) RebuildDict();
    }

    private void RebuildDict()
    {
        _dict = new Dictionary<string, int>(keys.Length);
        for (int i = 0; i < keys.Length; i++)
            _dict[keys[i]] = values[i];
    }

    private void SyncToArrays()
    {
        keys = new string[_dict.Count];
        values = new int[_dict.Count];
        int idx = 0;
        foreach (var kvp in _dict)
        {
            keys[idx] = kvp.Key;
            values[idx] = kvp.Value;
            idx++;
        }
    }

    public int Get(string key, int defaultVal = 0)
    {
        EnsureBuilt();
        return _dict.TryGetValue(key, out var v) ? v : defaultVal;
    }

    public void Set(string key, int value)
    {
        EnsureBuilt();
        _dict[key] = value;
        _dirty = true;
    }

    /// <summary>
    /// 在 Save 前调用，将 Dictionary 同步回数组以便 JSON 序列化
    /// </summary>
    public void Flush()
    {
        if (_dirty && _dict != null)
        {
            SyncToArrays();
            _dirty = false;
        }
    }
}