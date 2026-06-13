using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 冰霜新星技能 - 释放冰霜冲击波，减速并伤害周围敌人。
/// 对应 Python: FrostNova skill
/// 
/// 效果：
/// - 以玩家为中心释放冰霜冲击波
/// - 对范围内所有敌人造成伤害
/// - 减速范围内所有敌人
/// - 视觉：冰蓝色扩散圆环
/// </summary>
public class FrostNovaSkill : BaseSkill
{
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);

    private System.Collections.Generic.List<(EnemyBase enemy, float originalSpeed)> _slowedEnemies
        = new System.Collections.Generic.List<(EnemyBase, float)>();

    protected override void Activate()
    {
        if (_playerTransform == null) return;

        Vector2 center = _playerTransform.position;
        float radius = _skillData.effectRadius;
        int damage = GetDamage();
        float slowAmount = _skillData.effectStrength; // 0.0 ~ 1.0

        // 对范围内所有敌人造成伤害和减速
        int count = PhysicsHelper.OverlapCircle(center, radius, _overlapBuffer);
        int hitCount = 0;

        for (int i = 0; i < count; i++)
        { var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;

            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                // 造成伤害
                CombatManager.DealDamage(dmg, damage);
                hitCount++;

                // 减速
                var enemyBase = hit.GetComponent<EnemyBase>();
                if (enemyBase != null)
                {
                    float originalSpeed = enemyBase.MoveSpeed;
                    float newSpeed = originalSpeed * (1f - slowAmount);
                    enemyBase.MoveSpeed = newSpeed;
                    _slowedEnemies.Add((enemyBase, originalSpeed));

                    // 冰冻视觉 - 变蓝
                    var sr = hit.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(0.6f, 0.8f, 1f, 0.9f);
                    }

                    // 延迟恢复速度
                    StartCoroutine(RestoreSpeedAfterDelay(enemyBase, sr, _skillData.effectDuration));
                }
            }
        }

        // 视觉效果 - 冰霜扩散
        CombatManager.CreateExplosionEffect(center, radius, new Color(0.5f, 0.8f, 1f, 0.8f), 0.5f);

        DebugHelper.Log($"[FrostNova] Hit {hitCount} enemies, damage={damage}, slow={slowAmount * 100}%");
    }

    private System.Collections.IEnumerator RestoreSpeedAfterDelay(EnemyBase enemy, SpriteRenderer sr, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (enemy != null && enemy.Alive)
        {
            // 查找原始速度
            for (int i = _slowedEnemies.Count - 1; i >= 0; i--)
            {
                if (_slowedEnemies[i].enemy == enemy)
                {
                    enemy.MoveSpeed = _slowedEnemies[i].originalSpeed;
                    _slowedEnemies.RemoveAt(i);
                    break;
                }
            }

            // 恢复颜色
            if (sr != null)
            {
                sr.color = Color.white;
            }
        }
    }

    protected override void Deactivate()
    {
        base.Deactivate();

        // 恢复所有被减速的敌人
        foreach (var (enemy, originalSpeed) in _slowedEnemies)
        {
            if (enemy != null && enemy.Alive)
            {
                enemy.MoveSpeed = originalSpeed;
                var sr = enemy.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.white;
            }
        }
        _slowedEnemies.Clear();

        DebugHelper.Log("[FrostNova] All slows expired.");
    }
}