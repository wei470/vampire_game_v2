using UnityEngine;

/// <summary>
/// 高速敌人（Fast），2 倍移动速度，但 HP 较低。
/// 对应 Python 版的 Fast 敌人类型
/// 
/// 行为：快速冲向玩家，碰撞造成伤害
/// 使用方式：挂载到敌人 GameObject，替代 EnemyBase
/// </summary>
public class FastEnemy : EnemyBase
{
    [Header("Fast 特殊属性")]
    [SerializeField] private float _fastSpeedMultiplier = 2f;   // 速度倍率
    [SerializeField] private float _fastHpMultiplier = 0.6f;    // HP 倍率（较低）

    protected override void Awake()
    {
        base.Awake();
        // #11 FastEnemy DOT 抗性预设：霜冻抗性+30%，流血弱点-20%
        var res = GetComponent<EnemyDotResistance>();
        if (res == null) res = gameObject.AddComponent<EnemyDotResistance>();
        EnemyDotResistance.ApplyFastPreset(res);
        // Fast 敌人移动更快
        MoveSpeed *= _fastSpeedMultiplier;
    }

    /// <summary>
    /// 初始化 Fast 敌人（由 SpawnManager 调用）
    /// </summary>
    public void SetupFast(int baseHp)
    {
        var dmg = GetComponent<Damageable>();
        if (dmg != null)
        {
            int scaledHp = Mathf.RoundToInt(baseHp * _fastHpMultiplier);
            dmg.SetMaxHp(scaledHp);
            dmg.ResetHp();
        }
    }
}