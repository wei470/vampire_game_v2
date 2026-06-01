using UnityEngine;
using System;

/// <summary>
/// 实体基类，所有游戏实体的父类。
/// 对应 Python: entities/base.py 中的 BaseEntity
/// </summary>
public class BaseEntity : MonoBehaviour
{
    [Header("实体基础属性")]
    [SerializeField] protected bool _alive = true;
    [SerializeField] protected float _collisionRadius = 0.5f;

    /// <summary>
    /// 实体是否存活
    /// </summary>
    public bool Alive => _alive;

    /// <summary>
    /// 碰撞半径
    /// </summary>
    public float CollisionRadius => _collisionRadius;

    /// <summary>
    /// 死亡事件，参数：(死亡位置)
    /// </summary>
    public event Action<Vector3> OnDeath;

    protected virtual void Awake()
    {
        _alive = true;
    }

    /// <summary>
    /// 每次从对象池取出或首次激活时，重置存活状态
    /// </summary>
    protected virtual void OnEnable()
    {
        _alive = true;
    }

    /// <summary>
    /// 实体死亡
    /// </summary>
    public virtual void Die()
    {
        if (!_alive) return;

        _alive = false;
        DebugHelper.Log($"[BaseEntity] {gameObject.name} died at {transform.position}");
        OnDeath?.Invoke(transform.position);
    }

    /// <summary>
    /// 检测与另一个实体是否碰撞
    /// </summary>
    public bool IsCollidingWith(BaseEntity other)
    {
        if (other == null || !other.Alive) return false;
        float distance = Vector3.Distance(transform.position, other.transform.position);
        return distance < (_collisionRadius + other.CollisionRadius);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _collisionRadius);
    }
}