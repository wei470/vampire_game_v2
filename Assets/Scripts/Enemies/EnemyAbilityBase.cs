using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人能力基类 — 封装通用能力模式，减少敌人子类重复代码。
/// 作为独立 MonoBehaviour 组件挂载到敌人 GameObject 上。
/// 
/// 提供统一的能力生命周期：
/// - OnUpdate()：每帧调用（不受 LOD 影响）
/// - OnDetectPlayer(distance)：玩家在检测范围内时调用
/// - OnCooldownReady()：冷却就绪时调用
/// 
/// 子类只需实现差异化逻辑。
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public abstract class EnemyAbilityBase : MonoBehaviour
{
    [Header("能力基础参数")]
    [SerializeField] protected float _cooldown = 3f;
    [SerializeField] protected Color _effectColor = Color.white;
    [SerializeField] protected float _colorFlashDuration = 0.3f;

    /// <summary>所属敌人组件</summary>
    protected EnemyBase Owner;

    /// <summary>玩家目标</summary>
    protected Transform Target;

    /// <summary>缓存的 Rigidbody2D</summary>
    protected Rigidbody2D CachedRb;

    /// <summary>缓存的 SpriteRenderer</summary>
    protected SpriteRenderer CachedSr;

    /// <summary>原始颜色（用于闪光恢复）</summary>
    protected Color OriginalColor;

    private float _lastActivateTime = -999f;

    /// <summary>是否应跳过能力更新（远距离 LOD）</summary>
    protected bool ShouldSkip => Owner != null && Owner.SkipSpecialAbility;

    /// <summary>是否存活</summary>
    protected bool IsAlive => Owner != null && Owner.Alive;

    protected static readonly List<Collider2D> OverlapBuffer = new List<Collider2D>(16);

    protected virtual void Awake()
    {
        Owner = GetComponent<EnemyBase>();
        CachedRb = GetComponent<Rigidbody2D>();
        CachedSr = GetComponent<SpriteRenderer>();
        if (CachedSr != null) OriginalColor = CachedSr.color;
    }

    protected virtual void Start()
    {
        var player = GameReferences.Player;
        if (player != null) Target = player.transform;
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;

        // 每帧更新（不受 LOD 影响）
        OnUpdate();

        // 重新获取目标（对象池兼容：Start 只调用一次）
        if (Target == null)
        {
            var player = GameReferences.Player;
            if (player != null) Target = player.transform;
            if (Target == null) return;
        }

        // 检测玩家距离
        float dist = Vector3.Distance(transform.position, Target.position);
        OnDetectPlayer(dist);

        // 远距离 LOD 跳过冷却能力
        if (ShouldSkip) return;

        // 冷却就绪检查
        if (Time.time - _lastActivateTime >= _cooldown)
        {
            OnCooldownReady();
            _lastActivateTime = Time.time;
        }
    }

    /// <summary>
    /// 每帧更新（不受 LOD 影响）。
    /// 用于持续性效果如护盾呼吸动画。
    /// </summary>
    protected virtual void OnUpdate() { }

    /// <summary>
    /// 玩家在检测范围内时每帧调用。
    /// </summary>
    /// <param name="distance">与玩家的距离</param>
    protected virtual void OnDetectPlayer(float distance) { }

    /// <summary>
    /// 冷却就绪时调用，子类实现具体能力逻辑。
    /// </summary>
    protected virtual void OnCooldownReady() { }

    /// <summary>
    /// 闪光效果 — 临时改变自身颜色后自动恢复。
    /// </summary>
    protected void FlashColor(Color color, float duration = -1f)
    {
        if (duration < 0) duration = _colorFlashDuration;
        if (CachedSr != null)
        {
            CachedSr.color = color;
            Invoke(nameof(RestoreColor), duration);
        }
    }

    private void RestoreColor()
    {
        if (CachedSr != null) CachedSr.color = OriginalColor;
    }

    /// <summary>
    /// 在范围内搜索友军。
    /// </summary>
    protected int FindNearbyAllies(float radius)
    {
        return PhysicsHelper.OverlapCircle(transform.position, radius, OverlapBuffer);
    }

    /// <summary>
    /// 恢复指定 SpriteRenderer 的颜色（协程版）。
    /// </summary>
    protected System.Collections.IEnumerator RestoreColorCoroutine(SpriteRenderer sr, Color original, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sr != null) sr.color = original;
    }

    /// <summary>
    /// 恢复指定敌人的速度（协程版）。
    /// </summary>
    protected System.Collections.IEnumerator RestoreSpeedCoroutine(EnemyBase enemy, float originalSpeed, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemy != null) enemy.MoveSpeed = originalSpeed;
    }
}
