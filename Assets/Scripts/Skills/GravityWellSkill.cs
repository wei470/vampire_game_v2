using UnityEngine;

/// <summary>
/// 引力井技能 - 在玩家周围创建引力场，吸引并伤害敌人。
/// 对应 Python: GravityWell skill
/// 
/// 效果：
/// - 持续时间内，吸引范围内所有敌人向玩家靠近
/// - 持续对被吸引的敌人造成伤害
/// - 视觉：玩家周围蓝色漩涡
/// </summary>
public class GravityWellSkill : BaseSkill
{
    private float _tickTimer = 0f;
    private float _tickInterval = 0.5f;
    private GameObject _wellVisual;

    protected override void Activate()
    {
        _tickTimer = 0f;
        CreateWellVisual();
        DebugHelper.Log($"[GravityWell] Activated! radius={_skillData.effectRadius}, pull strength={GetEffectStrength()}");
    }

    protected override void Update()
    {
        base.Update();

        if (!_isActive) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickInterval;
            PullAndDamage();
        }

        // 更新视觉位置
        if (_wellVisual != null && _playerTransform != null)
        {
            _wellVisual.transform.position = _playerTransform.position;
            // 旋转效果
            _wellVisual.transform.Rotate(0, 0, 120f * Time.deltaTime);
        }
    }

    private void PullAndDamage()
    {
        if (_playerTransform == null) return;

        Vector2 center = _playerTransform.position;
        float radius = _skillData.effectRadius;
        float pullStrength = GetEffectStrength() * 3f;
        int damage = GetDamage();

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        int hitCount = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            var rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 吸引方向：从敌人指向玩家
                Vector2 pullDir = (center - (Vector2)hit.transform.position).normalized;
                float dist = Vector2.Distance(center, hit.transform.position);
                
                // 距离越近吸引力越强
                float distFactor = 1f - (dist / radius);
                rb.linearVelocity += pullDir * pullStrength * distFactor;
            }

            // 造成伤害
            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                CombatManager.DealDamage(dmg, damage);
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            DebugHelper.Log($"[GravityWell] Pulled and damaged {hitCount} enemies");
        }
    }

    private void CreateWellVisual()
    {
        if (_wellVisual != null) return;

        _wellVisual = new GameObject("GravityWellVisual");
        var sr = _wellVisual.AddComponent<SpriteRenderer>();

        // 创建漩涡纹理
        int size = 128;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                if (dist <= 1f)
                {
                    // 螺旋效果
                    float angle = Mathf.Atan2(y - center, x - center);
                    float spiral = Mathf.Sin(angle * 3f + dist * 10f) * 0.5f + 0.5f;
                    float alpha = (1f - dist) * 0.4f * spiral;
                    tex.SetPixel(x, y, new Color(0.3f, 0.5f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        sr.color = _skillData.skillColor;
        sr.sortingOrder = -1;

        if (_playerTransform != null)
        {
            _wellVisual.transform.position = _playerTransform.position;
        }
        _wellVisual.transform.localScale = Vector3.one * _skillData.effectRadius * 2f;
    }

    protected override void Deactivate()
    {
        base.Deactivate();

        if (_wellVisual != null)
        {
            Destroy(_wellVisual);
            _wellVisual = null;
        }

        DebugHelper.Log("[GravityWell] Deactivated.");
    }
}