using UnityEngine;

/// <summary>
/// 天照效果 — 元素反应"天照"（燃烧 × 黑暗）产生的 Debuff。
///
/// 触发条件：火场 tick 时检测敌人身上有 DarkMarkEffect
/// 效果：
///   - 敌人变黑（视觉）
///   - 每 2 秒，所有已有非 0 层 DOT +1 层
///   - 不可叠加，只触发一次
/// </summary>
public class AmaterasuEffect : MonoBehaviour
{
    private const float TICK_INTERVAL = 2f;
    private float _lastTickTime;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private bool _initialized = false;

    private void OnEnable()
    {
        _lastTickTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            _originalColor = _sr.color;
            _sr.color = new Color(0.05f, 0.05f, 0.05f);
        }
        _initialized = true;
    }

    private void OnDisable()
    {
        if (_sr != null)
            _sr.color = _originalColor;
    }

    private void Update()
    {
        if (!_initialized) return;

        if (Time.time - _lastTickTime < TICK_INTERVAL) return;
        _lastTickTime = Time.time;

        // 对所有已有非 0 层 DOT +1 层
        var burn = GetComponent<BurnStackEffect>();
        if (burn != null && burn.StackCount > 0)
            burn.AddStack(1f, 1f, false, 0f, 0f);

        var poison = GetComponent<PoisonStackEffect>();
        if (poison != null && poison.StackCount > 0)
            poison.AddStack(1f, 1f, false, 0f, 0f);

        var frost = GetComponent<FrostEffect>();
        if (frost != null && frost.FrostStacks > 0)
            frost.AddStack();

        var staticEff = GetComponent<StaticStackEffect>();
        if (staticEff != null && staticEff.StackCount > 0)
            staticEff.AddStack();

        var wind = GetComponent<WindErosionEffect>();
        if (wind != null && wind.WindStacks > 0)
            wind.RegisterHit();

        CombatManager.CreateExplosionEffect(transform.position, 0.5f,
            new Color(0.1f, 0.0f, 0.0f, 0.4f), 0.3f);
    }

    /// <summary>
    /// 检查敌人是否已有天照效果
    /// </summary>
    public static bool HasEffect(GameObject enemy)
    {
        return enemy.GetComponent<AmaterasuEffect>() != null;
    }

    /// <summary>
    /// 施加天照效果（如果还没有）
    /// </summary>
    public static void Apply(GameObject enemy)
    {
        if (HasEffect(enemy)) return;
        enemy.AddComponent<AmaterasuEffect>();
    }
}
