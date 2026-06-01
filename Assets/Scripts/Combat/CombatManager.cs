#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 战斗管理器 - 统一处理穿透计算、AoE伤害、爆炸效果。
/// 对应 Python: combat manager 中的穿透/AoE 逻辑
/// 
/// 功能：
/// - 穿透计算（pierce 递减，记录已命中敌人）
/// - AoE 伤害（Physics2D.OverlapCircleAll）
/// - 爆炸效果生成
/// 
/// 使用方式：Singleton，场景中自动创建
/// </summary>
public class CombatManager : Singleton<CombatManager>
{
    [Header("调试设置")]
    [SerializeField] private bool _debugLog = false;

    // 全局伤害倍率（用于全局增益）
    private float _globalDamageMultiplier = 1f;
    public float GlobalDamageMultiplier { get => _globalDamageMultiplier; set => _globalDamageMultiplier = value; }

    /// <summary>
    /// 计算最终伤害（考虑护甲、倍率、暴击等）
    /// </summary>
    public static int CalculateFinalDamage(int baseDamage, float multiplier, int armor, out bool isCrit)
    {
        // 暴击判定（P1-25）
        float critChance = SaveManager.Instance?.GetPermanentBonus("crit_chance") ?? 0f;
        isCrit = Random.value < critChance;
        float critMult = isCrit ? 1.5f : 1f;

        int rawDamage = Mathf.RoundToInt(baseDamage * multiplier * Instance._globalDamageMultiplier * critMult);
        return Mathf.Max(1, rawDamage - armor);
    }

    /// <summary>
    /// 计算最终伤害（兼容旧签名，不输出暴击标志）
    /// </summary>
    public static int CalculateFinalDamage(int baseDamage, float multiplier, int armor)
    {
        return CalculateFinalDamage(baseDamage, multiplier, armor, out _);
    }

    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    public static void DealDamage(Damageable target, int baseDamage, float multiplier = 1f)
    {
        if (target == null || target.CurrentHp <= 0) return;

        int finalDamage = CalculateFinalDamage(baseDamage, multiplier, target.Armor, out bool isCrit);
        target.TakeDamage(finalDamage);

        // 吸血系统（P1-26）
        float lifesteal = SaveManager.Instance?.GetPermanentBonus("lifesteal") ?? 0f;
        if (lifesteal > 0)
        {
            int healAmount = Mathf.RoundToInt(finalDamage * lifesteal);
            var playerDmg = GameReferences.Player?.Damageable;
            playerDmg?.Heal(healAmount);
        }

        if (Instance._debugLog)
        {
            string critTag = isCrit ? " CRIT!" : "";
            DebugHelper.Log($"[CombatManager] Dealt {finalDamage} damage to {target.gameObject.name}{critTag}");
        }
    }

    /// <summary>
    /// AoE 伤害 - 对区域内所有敌人造成伤害
    /// </summary>
    public static int DealAoEDamage(Vector2 center, float radius, int baseDamage, float multiplier = 1f, float knockbackForce = 0f)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        int hitCount = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                DealDamage(dmg, baseDamage, multiplier);
                hitCount++;

                // 击退
                if (knockbackForce > 0)
                {
                    var rb = hit.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 pushDir = ((Vector2)hit.transform.position - center).normalized;
                        rb.linearVelocity += pushDir * knockbackForce;
                    }
                }
            }
        }

        if (Instance._debugLog)
        {
            DebugHelper.Log($"[CombatManager] AoE at {center}, radius {radius}, hit {hitCount} enemies");
        }

        return hitCount;
    }

    /// <summary>
    /// 穿透伤害处理 - 检查穿透次数，返回是否应该销毁投射物
    /// </summary>
    public static bool ProcessPierce(ref int currentPierce, Damageable target, int baseDamage, float multiplier, HashSet<int> hitEnemies, float knockbackForce = 0f, Transform projectileTransform = null)
    {
        if (target == null || target.CurrentHp <= 0) return false;

        int targetId = target.gameObject.GetInstanceID();

        // 防止重复命中
        if (hitEnemies.Contains(targetId)) return false;
        hitEnemies.Add(targetId);

        // 造成伤害
        DealDamage(target, baseDamage, multiplier);

        // 击退
        if (knockbackForce > 0 && projectileTransform != null)
        {
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 pushDir = (target.transform.position - projectileTransform.position).normalized;
                rb.linearVelocity += pushDir * knockbackForce;
            }
        }

        // 穿透递减
        currentPierce--;

        // 返回 true 如果应该销毁
        return currentPierce <= 0;
    }

    /// <summary>
    /// 生成爆炸视觉效果
    /// </summary>
    public static void CreateExplosionEffect(Vector2 position, float radius, Color color, float duration = 0.3f)
    {
        var go = new GameObject("ExplosionVFX");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = color;
        sr.sortingOrder = 25;

        var effect = go.AddComponent<ExplosionVFX>();
        effect.Setup(radius, duration);
    }

    /// <summary>
    /// 绘制闪电视觉效果（两段之间的连线）
    /// </summary>
    public static void CreateLightningLine(Vector2 from, Vector2 to, float duration = 0.15f)
    {
        var go = new GameObject("LightningLine");
        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.8f, 0.8f, 1f, 1f);
        lr.endColor = new Color(0.5f, 0.5f, 1f, 0.5f);
        lr.sortingOrder = 20;

        // 锯齿形闪电路径
        int segments = 5;
        lr.positionCount = segments + 1;
        lr.SetPosition(0, from);
        for (int i = 1; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector2 point = Vector2.Lerp(from, to, t);
            point += Random.insideUnitCircle * 0.3f;
            lr.SetPosition(i, point);
        }
        lr.SetPosition(segments, to);

        // 自动销毁
        Destroy(go, duration);
    }

    private static Sprite _cachedCircleSprite;
    private static Sprite CreateCircleSprite()
    {
        if (_cachedCircleSprite != null) return _cachedCircleSprite;

        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                if (dist <= 1f)
                {
                    tex.SetPixel(x, y, new Color(1, 1, 1, 1f - dist));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedCircleSprite;
    }
}

/// <summary>
/// 爆炸视觉效果组件
/// </summary>
public class ExplosionVFX : MonoBehaviour
{
    private float _maxRadius;
    private float _duration;
    private float _spawnTime;
    private SpriteRenderer _sr;

    public void Setup(float maxRadius, float duration)
    {
        _maxRadius = maxRadius;
        _duration = duration;
        _spawnTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed > _duration)
        {
            Destroy(gameObject);
            return;
        }

        float t = elapsed / _duration;
        float scale = Mathf.Lerp(0.5f, _maxRadius * 2f, t);
        transform.localScale = Vector3.one * scale;

        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            _sr.color = c;
        }
    }
}