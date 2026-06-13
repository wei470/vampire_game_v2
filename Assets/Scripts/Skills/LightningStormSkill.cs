using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 雷暴技能 - 随机对多个敌人释放闪电攻击。
/// 对应 Python: LightningStorm skill
/// 
/// 效果：
/// - 随机选择范围内多个敌人
/// - 对每个目标释放闪电，造成伤害
/// - 闪电之间有链式视觉效果
/// </summary>
public class LightningStormSkill : BaseSkill
{
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    [SerializeField] private int _maxTargets = 6;

    protected override void Activate()
    {
        if (_playerTransform == null) return;

        Vector2 center = _playerTransform.position;
        float radius = _skillData.effectRadius;
        int damage = GetDamage();

        // 获取范围内的所有敌人
        int count = PhysicsHelper.OverlapCircle(center, radius, _overlapBuffer);
        List<Damageable> enemies = new List<Damageable>();

        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                enemies.Add(dmg);
            }
        }

        // 随机选择目标（最多 _maxTargets 个）
        int targetCount = Mathf.Min(_maxTargets, enemies.Count);
        
        // Fisher-Yates 洗牌
        for (int i = enemies.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = enemies[i];
            enemies[i] = enemies[j];
            enemies[j] = temp;
        }

        // 对选中的敌人造成伤害并绘制闪电
        Vector2 lastPos = center;
        int hitCount = 0;
        for (int i = 0; i < targetCount; i++)
        {
            if (enemies[i] == null) continue;

            CombatManager.DealDamage(enemies[i], damage);
            
            // 闪电视觉效果
            Vector2 targetPos = enemies[i].transform.position;
            CombatManager.CreateLightningLine(lastPos, targetPos, 0.3f);

            // 小范围AoE
            CombatManager.CreateExplosionEffect(targetPos, 0.8f, new Color(0.8f, 0.8f, 1f), 0.2f);

            lastPos = targetPos;
            hitCount++;
        }

        DebugHelper.Log($"[LightningStorm] Struck {hitCount} targets, damage={damage}");
    }
}