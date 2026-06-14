using UnityEngine;

/// <summary>
/// 全局引用缓存，避免热路径中使用 FindAnyObjectByType 等昂贵查找。
/// 在场景初始化时注入引用，场景卸载时清除。
/// #23 新增：常用组件缓存属性，减少 GetComponent 调用（12处 → 懒缓存）
/// </summary>
public static class GameReferences
{
    /// <summary>
    /// 玩家控制器引用
    /// </summary>
    private static PlayerController _player;
    public static PlayerController Player
    {
        get => _player;
        set
        {
            _player = value;
            InvalidateComponentCache();
        }
    }

    /// <summary>
    /// 敌人生成管理器引用
    /// </summary>
    public static SpawnManager SpawnManager { get; set; }

    /// <summary>
    /// 主摄像机引用
    /// </summary>
    public static Camera MainCamera { get; set; }

    /// <summary>
    /// 测试模式标志 — 跳过选择流程，使用默认配置直接开始游戏
    /// </summary>
    public static bool TestMode { get; set; } = false;
    public static bool BossTestMode { get; set; } = false;
    public static bool DpsTestMode { get; set; } = false;

    // ── #23: 常用组件懒缓存（Player 设置时自动重置）──
    private static MagePassive _cachedMagePassive;
    private static ICharacterPassive _cachedCharacterPassive;
    private static IDotCharacterPassive _cachedDotCharacterPassive;
    private static DetonateSystem _cachedDetonateSystem;
    private static PlayerLevelSystem _cachedLevelSystem;
    private static WeaponController _cachedWeaponController;
    private static Damageable _cachedDamageable;

    /// <summary>
    /// 获取玩家的 ICharacterPassive 组件（泛型接口，适用于所有角色）
    /// </summary>
    public static ICharacterPassive CharacterPassive
    {
        get
        {
            if (_cachedCharacterPassive == null && Player != null)
                _cachedCharacterPassive = Player.GetComponent<ICharacterPassive>();
            return _cachedCharacterPassive;
        }
    }

    /// <summary>
    /// 获取玩家的 IDotCharacterPassive 组件（DOT 角色专属接口）
    /// </summary>
    public static IDotCharacterPassive DotCharacterPassive
    {
        get
        {
            if (_cachedDotCharacterPassive == null && Player != null)
                _cachedDotCharacterPassive = Player.GetComponent<IDotCharacterPassive>();
            return _cachedDotCharacterPassive;
        }
    }

    /// <summary>
    /// 获取玩家的 MagePassive 组件（懒缓存，Player 变更时自动失效）
    /// 保留兼容性：新代码应使用 CharacterPassive 或 DotCharacterPassive
    /// </summary>
    public static MagePassive MagePassive
    {
        get
        {
            if (_cachedMagePassive == null && Player != null)
                _cachedMagePassive = Player.GetComponent<MagePassive>();
            return _cachedMagePassive;
        }
    }

    /// <summary>
    /// 获取玩家的 DetonateSystem 组件（懒缓存）
    /// </summary>
    public static DetonateSystem DetonateSystem
    {
        get
        {
            if (_cachedDetonateSystem == null && Player != null)
                _cachedDetonateSystem = Player.GetComponent<DetonateSystem>();
            return _cachedDetonateSystem;
        }
    }

    /// <summary>
    /// 获取玩家的 PlayerLevelSystem 组件（懒缓存）
    /// </summary>
    public static PlayerLevelSystem LevelSystem
    {
        get
        {
            if (_cachedLevelSystem == null && Player != null)
                _cachedLevelSystem = Player.GetComponent<PlayerLevelSystem>();
            return _cachedLevelSystem;
        }
    }

    /// <summary>
    /// 获取玩家的 WeaponController 组件（懒缓存）
    /// </summary>
    public static WeaponController WeaponCtrl
    {
        get
        {
            if (_cachedWeaponController == null && Player != null)
                _cachedWeaponController = Player.GetComponent<WeaponController>();
            return _cachedWeaponController;
        }
    }

    /// <summary>
    /// 获取玩家的 Damageable 组件（懒缓存）
    /// </summary>
    public static Damageable PlayerDamageable
    {
        get
        {
            if (_cachedDamageable == null && Player != null)
                _cachedDamageable = Player.GetComponent<Damageable>();
            return _cachedDamageable;
        }
    }

    /// <summary>
    /// 清除组件缓存（Player 变更时调用）
    /// </summary>
    private static void InvalidateComponentCache()
    {
        _cachedMagePassive = null;
        _cachedCharacterPassive = null;
        _cachedDotCharacterPassive = null;
        _cachedDetonateSystem = null;
        _cachedLevelSystem = null;
        _cachedWeaponController = null;
        _cachedDamageable = null;
    }

    /// <summary>
    /// 清除所有引用（场景卸载时调用）
    /// </summary>
    public static void Clear()
    {
        Player = null;
        SpawnManager = null;
        MainCamera = null;
        TestMode = false;
        BossTestMode = false;
        DpsTestMode = false;
        InvalidateComponentCache();
    }

    /// <summary>
    /// 完全重置所有引用（返回菜单时调用，防止残留）
    /// </summary>
    public static void Reset()
    {
        Player = null;
        SpawnManager = null;
        MainCamera = null;
        TestMode = false;
        BossTestMode = false;
        DpsTestMode = false;
        InvalidateComponentCache();
    }
}
