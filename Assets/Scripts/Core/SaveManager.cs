using UnityEngine;
using System;
using System.IO;
using System.Text;

/// <summary>
/// 存档管理器，负责保存/加载玩家永久进度。
/// 对应 Python: playfile.py
///
/// #34 安全特性：
/// - CRC32 校验码防篡改
/// - 存档版本号，未来格式升级时自动迁移
/// - 最近 3 个存档备份，循环覆盖
///
/// 数据结构 → SaveData.cs
/// 升级/里程碑逻辑 → PermanentUpgradeStore.cs
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    // 向后兼容转发（ShopUI 等直接引用 SaveManager.SHOP_UPGRADES）
    public static ShopUpgradeDef[] SHOP_UPGRADES => PermanentUpgradeStore.SHOP_UPGRADES;
    public static MilestoneDef[] MILESTONES => PermanentUpgradeStore.MILESTONES;

    // 存档版本号（未来格式升级时递增）
    private const int CURRENT_SAVE_VERSION = 1;

    // 备份配置
    private const int MAX_BACKUPS = 3;
    private static readonly string[] BACKUP_SUFFIXES = { ".bak1", ".bak2", ".bak3" };

    // ============================================================
    // 运行时状态
    // ============================================================

    private SaveGameData _data = null;
    private string _savePath = null;
    private string _saveDir = null;

    /// <summary>
    /// 当前存档数据（保证非 null）
    /// </summary>
    public SaveGameData Data
    {
        get
        {
            if (_data == null) _data = CreateDefaultSave();
            return _data;
        }
    }

    public static event Action<int> OnCoinsChanged;
    public static event Action<SaveGameData> OnSaveLoaded;

    protected override void Awake()
    {
        base.Awake();
        _saveDir = Application.persistentDataPath;
        _savePath = Path.Combine(_saveDir, "save_data.json");
        if (_data == null) _data = CreateDefaultSave();
        Load();
    }

    // ============================================================
    // 存/读档
    // ============================================================

    public void Save()
    {
        if (string.IsNullOrEmpty(_savePath))
        {
            _saveDir = Application.persistentDataPath;
            _savePath = Path.Combine(_saveDir, "save_data.json");
        }
        try
        {
            // 保存前将 Dictionary 同步回数组
            Data.upgrades.Flush();
            Data.saveVersion = CURRENT_SAVE_VERSION;

            string dataJson = JsonUtility.ToJson(Data, true);
            string checksum = ComputeCRC32(dataJson);

            // 包装为 SaveWrapper（包含校验码）
            var wrapper = new SaveWrapper { data = dataJson, checksum = checksum };
            string wrapperJson = JsonUtility.ToJson(wrapper, true);

            // #34 先创建备份，再写入新存档
            RotateBackups();

            File.WriteAllText(_savePath, wrapperJson);
            DebugHelper.Log($"[SaveManager] Saved to {_savePath} (CRC32={checksum}, v{CURRENT_SAVE_VERSION})");
        }
        catch (Exception e)
        {
            DebugHelper.LogError($"[SaveManager] Save failed: {e.Message}");
        }
    }

    public void Load()
    {
        if (_savePath == null)
        {
            _saveDir = Application.persistentDataPath;
            _savePath = Path.Combine(_saveDir, "save_data.json");
        }

        if (File.Exists(_savePath))
        {
            try
            {
                string wrapperJson = File.ReadAllText(_savePath);
                _data = TryLoadFromWrapper(wrapperJson);

                if (_data == null)
                {
                    // 尝试从备份恢复
                    _data = TryLoadFromBackups();
                }

                if (_data != null)
                {
                    // 存档版本迁移
                    MigrateSaveData(_data);

                    if (_data.upgrades == null) _data.upgrades = new SerializableDictionary();
                    if (_data.claimedMilestones == null) _data.claimedMilestones = new string[0];
                    DebugHelper.Log($"[SaveManager] Loaded: coins={_data.coins}, waves={_data.highestWave}, kills={_data.totalKills}");
                }
                else
                {
                    DebugHelper.LogWarning("[SaveManager] All save files corrupted, creating new save");
                    _data = CreateDefaultSave();
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogWarning($"[SaveManager] Load failed: {e.Message}, attempting backup recovery");
                _data = TryLoadFromBackups();
                if (_data == null)
                {
                    DebugHelper.LogWarning("[SaveManager] No valid backup found, creating default save");
                    _data = CreateDefaultSave();
                }
            }
        }
        else
        {
            _data = CreateDefaultSave();
            DebugHelper.Log("[SaveManager] No save file found, created default save");
        }
        OnSaveLoaded?.Invoke(_data);
    }

    /// <summary>
    /// 尝试从 SaveWrapper 格式加载存档，校验 CRC32
    /// </summary>
    private SaveGameData TryLoadFromWrapper(string json)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
            if (wrapper != null && !string.IsNullOrEmpty(wrapper.data))
            {
                // 验证 CRC32 校验码
                string expectedCrc = ComputeCRC32(wrapper.data);
                if (wrapper.checksum == expectedCrc)
                {
                    DebugHelper.Log($"[SaveManager] CRC32 verified: {expectedCrc}");
                    return JsonUtility.FromJson<SaveGameData>(wrapper.data);
                }
                else
                {
                    DebugHelper.LogWarning($"[SaveManager] CRC32 mismatch! Expected={expectedCrc}, Got={wrapper.checksum}. Save file may be corrupted or tampered.");
                    return null;
                }
            }
        }
        catch
        {
            // 可能是旧格式（直接 SaveData JSON），尝试兼容加载
        }

        // 旧格式兼容：直接作为 SaveGameData 加载（无校验）
        try
        {
            var legacyData = JsonUtility.FromJson<SaveGameData>(json);
            if (legacyData != null)
            {
                DebugHelper.Log("[SaveManager] Loaded legacy save format (no checksum). Will upgrade on next save.");
                return legacyData;
            }
        }
        catch
        {
            // 完全无法解析
        }

        return null;
    }

    /// <summary>
    /// 尝试从备份文件加载存档
    /// </summary>
    private SaveGameData TryLoadFromBackups()
    {
        for (int i = 0; i < MAX_BACKUPS; i++)
        {
            string backupPath = _savePath + BACKUP_SUFFIXES[i];
            if (!File.Exists(backupPath)) continue;

            try
            {
                string json = File.ReadAllText(backupPath);
                var data = TryLoadFromWrapper(json);
                if (data != null)
                {
                    DebugHelper.Log($"[SaveManager] Recovered from backup #{i + 1}: {backupPath}");
                    return data;
                }
            }
            catch
            {
                // 备份也损坏，尝试下一个
            }
        }
        return null;
    }

    /// <summary>
    /// 循环覆盖备份文件（最旧的被覆盖）
    /// </summary>
    private void RotateBackups()
    {
        if (!File.Exists(_savePath)) return;

        try
        {
            // 删除最旧的备份，将当前存档移到备份位置
            string oldestBackup = _savePath + BACKUP_SUFFIXES[MAX_BACKUPS - 1];
            if (File.Exists(oldestBackup))
                File.Delete(oldestBackup);

            // 从后往前移动备份
            for (int i = MAX_BACKUPS - 1; i > 0; i--)
            {
                string src = _savePath + BACKUP_SUFFIXES[i - 1];
                string dst = _savePath + BACKUP_SUFFIXES[i];
                if (File.Exists(src))
                    File.Move(src, dst);
            }

            // 当前存档变为第一个备份
            File.Copy(_savePath, _savePath + BACKUP_SUFFIXES[0], true);
        }
        catch (Exception e)
        {
            DebugHelper.LogWarning($"[SaveManager] Backup rotation failed: {e.Message}");
        }
    }

    /// <summary>
    /// 存档版本迁移（未来格式升级时在此添加逻辑）
    /// </summary>
    private void MigrateSaveData(SaveGameData data)
    {
        if (data.saveVersion < CURRENT_SAVE_VERSION)
        {
            DebugHelper.Log($"[SaveManager] Migrating save from v{data.saveVersion} to v{CURRENT_SAVE_VERSION}");
            data.saveVersion = CURRENT_SAVE_VERSION;
        }
    }

    // ============================================================
    // CRC32 校验码计算
    // ============================================================

    private static uint[] _crc32Table;

    private static string ComputeCRC32(string input)
    {
        if (_crc32Table == null) InitCRC32Table();

        byte[] bytes = Encoding.UTF8.GetBytes(input);
        uint crc = 0xFFFFFFFF;
        for (int i = 0; i < bytes.Length; i++)
        {
            byte tableIndex = (byte)((crc & 0xFF) ^ bytes[i]);
            crc = (crc >> 8) ^ _crc32Table[tableIndex];
        }
        return (crc ^ 0xFFFFFFFF).ToString("X8");
    }

    private static void InitCRC32Table()
    {
        _crc32Table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
            _crc32Table[i] = crc;
        }
    }

    private SaveGameData CreateDefaultSave()
    {
        var data = new SaveGameData();
        data.coins = 0; data.highestWave = 0; data.totalKills = 0; data.totalGames = 0;
        data.claimedMilestones = new string[0];
        foreach (var upgrade in PermanentUpgradeStore.SHOP_UPGRADES)
            data.upgrades.Set(upgrade.attr, 0);
        return data;
    }

    // ============================================================
    // 金币操作（委托转发保持公共 API 不变）
    // ============================================================

    public int GetCoins() { return Data.coins; }

    public void AddCoins(int amount)
    {
        Data.coins += amount;
        OnCoinsChanged?.Invoke(Data.coins);
    }

    public bool SpendCoins(int amount)
    {
        if (Data.coins < amount) return false;
        Data.coins -= amount;
        OnCoinsChanged?.Invoke(Data.coins);
        return true;
    }

    // ============================================================
    // 升级/里程碑/加成（委托给 PermanentUpgradeStore）
    // ============================================================

    public int GetUpgradeLevel(string attr) { return PermanentUpgradeStore.GetUpgradeLevel(Data, attr); }
    public bool CanBuyUpgrade(int index) { return PermanentUpgradeStore.CanBuyUpgrade(Data, index); }
    public bool BuyUpgrade(int index)
    {
        if (PermanentUpgradeStore.BuyUpgrade(Data, index))
        {
            OnCoinsChanged?.Invoke(Data.coins);
            Save();
            return true;
        }
        return false;
    }

    public bool IsMilestoneClaimed(string id) { return PermanentUpgradeStore.IsMilestoneClaimed(Data, id); }
    public int GetStatForMilestone(string stat) { return PermanentUpgradeStore.GetStatForMilestone(Data, stat); }
    public bool ClaimMilestone(string milestoneId)
    {
        if (PermanentUpgradeStore.ClaimMilestone(Data, milestoneId))
        {
            Save();
            return true;
        }
        return false;
    }

    public float GetPermanentBonus(string attr) { return PermanentUpgradeStore.GetPermanentBonus(Data, attr); }
    public float GetPermanentMultiplier(string attr) { return PermanentUpgradeStore.GetPermanentMultiplier(Data, attr); }
    public void AddPermanentBonus(string key, float value) { PermanentUpgradeStore.AddPermanentBonus(Data, key, value); }
    public float GetPassiveBonus(string key) { return PermanentUpgradeStore.GetPassiveBonus(Data, key); }
    public void ClearPassiveBonuses() { PermanentUpgradeStore.ClearPassiveBonuses(Data); }

    // ============================================================
    // 统计更新
    // ============================================================

    public void RecordGameEnd(int finalWave, int killsThisGame)
    {
        Data.totalGames++;
        Data.totalKills += killsThisGame;
        if (finalWave > Data.highestWave) Data.highestWave = finalWave;
        Save();
        DebugHelper.Log($"[SaveManager] Game recorded: wave={finalWave}, kills={killsThisGame}, total_games={Data.totalGames}");
    }

    public void ResetData()
    {
        _data = CreateDefaultSave();
        Save();
        OnCoinsChanged?.Invoke(Data.coins);
        DebugHelper.Log("[SaveManager] Save data reset");
    }
}