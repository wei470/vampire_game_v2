using UnityEngine;

/// <summary>
/// 时停技能 - 暂停所有敌人的行动。
/// 对应 Python: TheWorld skill
/// 
/// 效果：
/// - 持续时间内，所有敌人停止移动
/// - 敌人AI暂停（速度设为0）
/// - 技能结束后恢复正常
/// </summary>
public class TheWorldSkill : BaseSkill
{
    private EnemyBase[] _frozenEnemies;
    private float[] _originalSpeeds;

    protected override void Activate()
    {
        // 冻结所有敌人
        _frozenEnemies = FindObjectsByType<EnemyBase>();
        _originalSpeeds = new float[_frozenEnemies.Length];

        int frozenCount = 0;
        for (int i = 0; i < _frozenEnemies.Length; i++)
        {
            if (_frozenEnemies[i] != null && _frozenEnemies[i].Alive)
            {
                _originalSpeeds[i] = _frozenEnemies[i].MoveSpeed;
                _frozenEnemies[i].MoveSpeed = 0f;
                frozenCount++;

                // 停止刚体速度
                var rb = _frozenEnemies[i].GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                // 视觉效果 - 变蓝
                var sr = _frozenEnemies[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(0.5f, 0.5f, 1f, 0.8f);
                }
            }
        }

        // 暂停所有敌人子弹
        var bullets = FindObjectsByType<EnemyBullet>();
        foreach (var bullet in bullets)
        {
            if (bullet != null)
            {
                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        DebugHelper.Log($"[TheWorld] Time stopped! Frozen {frozenCount} enemies for {_skillData.duration}s");
    }

    protected override void Deactivate()
    {
        base.Deactivate();

        // 恢复所有敌人
        if (_frozenEnemies != null)
        {
            for (int i = 0; i < _frozenEnemies.Length; i++)
            {
                if (_frozenEnemies[i] != null && _frozenEnemies[i].Alive)
                {
                    _frozenEnemies[i].MoveSpeed = _originalSpeeds[i];

                    // 恢复颜色
                    var sr = _frozenEnemies[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = Color.white;
                    }
                }
            }
        }

        _frozenEnemies = null;
        _originalSpeeds = null;

        DebugHelper.Log("[TheWorld] Time resumed!");
    }
}