using UnityEngine;

/// <summary>
/// Boss 毒区组件 — 玩家站在区域内持续受到伤害。
/// 由 BossEnemy 第 4 阶段生成，生命周期结束后自动销毁。
/// </summary>
public class BossPoisonZone : MonoBehaviour
{
    private int _tickDamage;
    private float _duration;
    private float _tickInterval;
    private float _lastTickTime;
    private float _createTime;

    /// <summary>
    /// 配置毒区参数
    /// </summary>
    public void Setup(int tickDamage, float duration, float tickInterval)
    {
        _tickDamage = tickDamage;
        _duration = duration;
        _tickInterval = tickInterval;
        _createTime = Time.time;
        _lastTickTime = Time.time;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time - _createTime > _duration) return;
        if (Time.time - _lastTickTime < _tickInterval) return;

        if (other.CompareTag("Player"))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                dmg.TakeDamage(_tickDamage);
                _lastTickTime = Time.time;
            }
        }
    }
}