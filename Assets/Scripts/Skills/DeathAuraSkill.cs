using UnityEngine;

/// <summary>
/// 死亡光环技能 - 持续对周围敌人造成范围伤害。
/// 对应 Python: DeathAura skill
/// 
/// 效果：
/// - 持续时间内，每隔一段时间对周围敌人造成伤害
/// - 以玩家为中心的圆形范围
/// - 视觉：玩家周围暗紫色光环
/// </summary>
public class DeathAuraSkill : BaseSkill
{
    private float _tickTimer = 0f;
    private float _tickInterval = 0.5f;
    private GameObject _auraVisual;

    protected override void Activate()
    {
        _tickTimer = 0f;

        // 创建持续的光环视觉效果
        CreateAuraVisual();

        DebugHelper.Log($"[DeathAura] Activated! radius={_skillData.effectRadius}, damage/tick={GetDamage()}");
    }

    protected override void Update()
    {
        base.Update();

        if (!_isActive) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickInterval;
            DealAuraDamage();
        }

        // 更新光环位置
        if (_auraVisual != null && _playerTransform != null)
        {
            _auraVisual.transform.position = _playerTransform.position;
        }
    }

    private void DealAuraDamage()
    {
        if (_playerTransform == null) return;

        Vector2 center = _playerTransform.position;
        int damage = GetDamage();
        float radius = _skillData.effectRadius;

        int hitCount = CombatManager.DealAoEDamage(center, radius, damage);

        if (hitCount > 0)
        {
            DebugHelper.Log($"[DeathAura] Tick damage to {hitCount} enemies");
        }
    }

    private void CreateAuraVisual()
    {
        if (_auraVisual != null) return;

        _auraVisual = new GameObject("DeathAuraVisual");
        var sr = _auraVisual.AddComponent<SpriteRenderer>();
        
        // 创建圆形纹理
        int size = 128;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                if (dist <= 1f)
                {
                    tex.SetPixel(x, y, new Color(0.5f, 0f, 0.8f, 0.3f * (1f - dist)));
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
            _auraVisual.transform.position = _playerTransform.position;
        }
        _auraVisual.transform.localScale = Vector3.one * _skillData.effectRadius * 2f;
    }

    protected override void Deactivate()
    {
        base.Deactivate();

        if (_auraVisual != null)
        {
            Destroy(_auraVisual);
            _auraVisual = null;
        }

        DebugHelper.Log("[DeathAura] Deactivated.");
    }
}