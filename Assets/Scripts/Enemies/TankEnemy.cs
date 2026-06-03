#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// 高血量敌人（Tank），3 倍 HP，移动较慢。
/// 对应 Python 版的 Tank 敌人类型
/// 
/// 行为：缓慢追踪玩家，碰撞造成较高伤害
/// 使用方式：挂载到敌人 GameObject，替代 EnemyBase
/// </summary>
public class TankEnemy : EnemyBase
{
    [Header("Tank 特殊属性")]
    [SerializeField] private float _tankSpeedMultiplier = 0.5f;  // 移动速度倍率（较慢）
    [SerializeField] private int _tankHpMultiplier = 3;           // HP 倍率
    [SerializeField] private int _tankContactDamage = 20;         // 较高的碰撞伤害

    protected override void Awake()
    {
        base.Awake();
        // #11 TankEnemy DOT 抗性预设：流血抗性+50%，霜冻弱点-30%
        var res = GetComponent<EnemyDotResistance>();
        if (res == null) res = gameObject.AddComponent<EnemyDotResistance>();
        EnemyDotResistance.ApplyTankPreset(res);
        // Tank 敌人移动较慢
        MoveSpeed *= _tankSpeedMultiplier;
    }

    /// <summary>
    /// 获取 Tank 的 HP 倍率
    /// </summary>
    public int TankHpMultiplier => _tankHpMultiplier;

    /// <summary>
    /// 初始化 Tank 敌人（由 SpawnManager 调用）
    /// </summary>
    public void SetupTank(int baseHp)
    {
        var dmg = GetComponent<Damageable>();
        if (dmg != null)
        {
            dmg.SetMaxHp(baseHp * _tankHpMultiplier);
            dmg.ResetHp();
        }

        // 设置碰撞伤害
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            // 碰撞伤害在 EnemyBase 的 OnCollisionStay2D 中处理
            // 通过 Setup 方法修改 contactDamage
        }
    }
}