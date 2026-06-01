using UnityEngine;

/// <summary>
/// Boss AoE 震波组件 — 从 Boss 位置向外扩散的冲击波。
/// 碰到玩家时造成击退+伤害，震波半径逐渐扩大后消失。
/// </summary>
public class BossShockwave : MonoBehaviour
{
    private int _damage;
    private float _maxRadius;
    private float _expandDuration;
    private float _createTime;
    private float _startRadius = 0.5f;
    private CircleCollider2D _collider;
    private SpriteRenderer _sr;

    /// <summary>
    /// 配置震波参数
    /// </summary>
    public void Setup(int damage, float maxRadius, float expandDuration)
    {
        _damage = damage;
        _maxRadius = maxRadius;
        _expandDuration = expandDuration;
        _createTime = Time.time;

        _collider = GetComponent<CircleCollider2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float elapsed = Time.time - _createTime;
        float t = Mathf.Clamp01(elapsed / _expandDuration);

        // 震波半径逐渐扩大
        float currentRadius = Mathf.Lerp(_startRadius, _maxRadius, t);
        transform.localScale = Vector3.one * currentRadius;

        // 透明度逐渐降低
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(0.6f, 0f, t);
            _sr.color = c;
        }

        // 到达最大半径后销毁
        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                dmg.TakeDamage(_damage);
            }

            // 击退效果
            var rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 knockback = (other.transform.position - transform.position).normalized * 8f;
                rb.linearVelocity = knockback;
            }
        }
    }
}