using UnityEngine;

/// <summary>
/// 敌人子弹，由远程敌人发射，碰到玩家造成伤害。
/// 对应 Python: entities/projectiles.py 中的敌人投射物
/// 
/// 使用方式：由 RangedEnemy 在射击时实例化
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("子弹属性")]
    [SerializeField] private int _damage = 15;
    [SerializeField] private float _lifetime = 5f;

    private float _spawnTime;
    private static readonly Collider2D[] _enemyBuffer = new Collider2D[1];

    private void OnEnable()
    {
        _spawnTime = Time.time;

        // 确保 Collider 是触发器
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 注册到屏幕外裁剪器
        if (OffScreenCuller.Instance != null)
        {
            OffScreenCuller.TrackProjectile(gameObject, PoolHelper.ENEMY_BULLET);
        }

        ApplyFrozenHandsSlow();
    }

    private void OnDisable()
    {
        OffScreenCuller.Untrack(gameObject);
    }

    private void Update()
    {
        // 超时回收
        if (Time.time - _spawnTime > _lifetime)
        {
            DespawnSelf();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 碰到玩家，造成伤害
        if (other.CompareTag("Player"))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                dmg.TakeDamage(_damage);
                DebugHelper.Log($"[EnemyBullet] Hit player for {_damage} damage");
            }
            DespawnSelf();
        }
    }

    /// <summary>
    /// 回收自身（优先对象池）
    /// </summary>
    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.ENEMY_BULLET);
    }

    /// <summary>
    /// 设置子弹属性
    /// </summary>
    public void Setup(int damage, float lifetime)
    {
        _damage = damage;
        _lifetime = lifetime;
    }

    private void ApplyFrozenHandsSlow()
    {
        var mage = GameReferences.DotCharacterPassive as MagePassive;
        if (mage == null || !mage.FrozenHands) return;
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return;
        float bestDistSqr = float.MaxValue;
        GameObject nearest = null;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;
            float dSqr = ((Vector2)e.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (dSqr < bestDistSqr) { bestDistSqr = dSqr; nearest = e; }
        }
        if (nearest != null)
        {
            var frost = nearest.GetComponent<FrostEffect>();
            if (frost != null && frost.FrostStacks > 0)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity *= 0.67f;
            }
        }
    }
}