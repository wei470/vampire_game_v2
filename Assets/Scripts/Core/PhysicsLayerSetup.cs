using UnityEngine;

/// <summary>
/// #17 物理 Layer 常量定义 + Physics2D 碰撞矩阵运行时配置。
/// 在 GameSceneBootstrap 中调用 SetupCollisionMatrix() 一次即可。
///
/// Layer 定义（见 TagManager.asset）：
///   8  = Bullet    — 玩家子弹（DOT 子弹、普通子弹）
///   9  = Enemy     — 敌人
///   10 = Player    — 玩家
///   11 = Pickup    — 掉落物（XP/金币）
///   12 = Environment — 环境/装饰物
/// </summary>
public static class PhysicsLayerSetup
{
    // ═══ Layer 索引常量 ═══
    public const int LAYER_DEFAULT    = 0;
    public const int LAYER_BULLET     = 8;
    public const int LAYER_ENEMY      = 9;
    public const int LAYER_PLAYER     = 10;
    public const int LAYER_PICKUP     = 11;
    public const int LAYER_ENVIRONMENT = 12;

    // ═══ LayerMask 便捷属性 ═══
    /// <summary>子弹应该碰撞的目标层（敌人 + 玩家 + 环境）</summary>
    public static int BulletTargetMask =>
        (1 << LAYER_ENEMY) | (1 << LAYER_PLAYER) | (1 << LAYER_ENVIRONMENT);

    /// <summary>敌人子弹应该碰撞的目标层（玩家 + 环境）</summary>
    public static int EnemyBulletTargetMask =>
        (1 << LAYER_PLAYER) | (1 << LAYER_ENVIRONMENT);

    /// <summary>掉落物应该碰撞的目标层（玩家）</summary>
    public static int PickupTargetMask =>
        (1 << LAYER_PLAYER);

    /// <summary>
    /// 配置 Physics2D 碰撞矩阵。
    /// 只在 GameScene 启动时调用一次。
    ///
    /// 矩阵规则：
    /// - Bullet(8) × Enemy(9)   = 碰撞 ✓（玩家子弹打敌人）
    /// - Bullet(8) × Player(10) = 不碰撞（玩家子弹不打玩家）
    /// - Bullet(8) × Pickup(11) = 不碰撞
    /// - Bullet(8) × Environment(12) = 碰撞 ✓（子弹撞墙销毁）
    /// - Enemy(9) × Player(10)  = 碰撞 ✓（敌人接触玩家造成伤害）
    /// - Enemy(9) × Enemy(9)    = 不碰撞（敌人之间不碰撞）
    /// - Pickup(11) × Player(10) = 碰撞 ✓（拾取）
    /// - Pickup(11) × Enemy(9)  = 不碰撞
    /// - Environment(12) × Everything = 碰撞（环境是障碍物）
    /// </summary>
    public static void SetupCollisionMatrix()
    {
        // 先全部禁用自定义 Layer 之间的碰撞
        for (int i = LAYER_BULLET; i <= LAYER_ENVIRONMENT; i++)
        {
            for (int j = i; j <= LAYER_ENVIRONMENT; j++)
            {
                Physics2D.IgnoreLayerCollision(i, j, true);
            }
        }

        // 重新启用需要碰撞的组合
        // 玩家子弹(Bullet) ↔ 敌人(Enemy)
        Physics2D.IgnoreLayerCollision(LAYER_BULLET, LAYER_ENEMY, false);

        // 玩家子弹(Bullet) ↔ 环境(Environment)
        Physics2D.IgnoreLayerCollision(LAYER_BULLET, LAYER_ENVIRONMENT, false);

        // 敌人(Enemy) ↔ 玩家(Player) — 敌人接触伤害
        Physics2D.IgnoreLayerCollision(LAYER_ENEMY, LAYER_PLAYER, false);

        // 掉落物(Pickup) ↔ 玩家(Player) — 磁吸拾取
        Physics2D.IgnoreLayerCollision(LAYER_PICKUP, LAYER_PLAYER, false);

        // 掉落物(Pickup) ↔ 环境(Environment)
        Physics2D.IgnoreLayerCollision(LAYER_PICKUP, LAYER_ENVIRONMENT, false);

        // 环境(Environment) ↔ 玩家(Player)
        Physics2D.IgnoreLayerCollision(LAYER_ENVIRONMENT, LAYER_PLAYER, false);

        // 环境(Environment) ↔ 敌人(Enemy)
        Physics2D.IgnoreLayerCollision(LAYER_ENVIRONMENT, LAYER_ENEMY, false);

        Debug.Log("[PhysicsLayerSetup] 碰撞矩阵已配置：Bullet→Enemy/Env, Enemy→Player, Pickup→Player");
    }

    /// <summary>
    /// 为 GameObject 设置 Bullet Layer（玩家子弹用）
    /// </summary>
    public static void SetAsBullet(GameObject go)
    {
        go.layer = LAYER_BULLET;
    }

    /// <summary>
    /// 为 GameObject 设置 Enemy Layer
    /// </summary>
    public static void SetAsEnemy(GameObject go)
    {
        go.layer = LAYER_ENEMY;
    }

    /// <summary>
    /// 为 GameObject 设置 Player Layer
    /// </summary>
    public static void SetAsPlayer(GameObject go)
    {
        go.layer = LAYER_PLAYER;
    }

    /// <summary>
    /// 为 GameObject 设置 Pickup Layer
    /// </summary>
    public static void SetAsPickup(GameObject go)
    {
        go.layer = LAYER_PICKUP;
    }

    /// <summary>
    /// 为 GameObject 设置 Environment Layer
    /// </summary>
    public static void SetAsEnvironment(GameObject go)
    {
        go.layer = LAYER_ENVIRONMENT;
    }
}