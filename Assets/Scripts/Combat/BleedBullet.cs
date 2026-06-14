using UnityEngine;

/// <summary>
/// 流血被动效果 — 挂载到敌人身上，敌人移动时受伤
/// BleedBullet（流血子弹）已从游戏中移除，BleedEffect 组件仍被 DarkBullet/CurseSpreadSystem/DetonateSystem 引用。
/// </summary>
public class BleedEffect : MonoBehaviour
{
    public float dps;
    public float duration;
    private float _startTime;
    public bool canCrit; public float critChance, critMult;
    private Vector3 _lastPosition;
    private float _damageAccumulator;
    private const float MOVE_THRESHOLD = 0.1f;
    private Damageable _damageable;

    /// <summary>
    /// #19 脓毒组合加成
    /// </summary>
    [System.NonSerialized] public float comboSepsisBonus = 0f;

    public void Refresh(float dps, float duration, bool canCrit, float critChance, float critMult)
    {
        this.dps = Mathf.Max(this.dps, dps);
        this.duration = duration;
        _startTime = Time.time;
        this.canCrit = canCrit; this.critChance = critChance; this.critMult = critMult;
    }

    private void Start()
    {
        _startTime = Time.time;
        _lastPosition = transform.position;
        _damageable = GetComponent<Damageable>();
    }

    private void Update()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0) { Destroy(this); return; }

        float moved = Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        if (moved > MOVE_THRESHOLD && _damageable != null && _damageable.CurrentHp > 0)
        {
            float effectiveDps = dps * (1f + comboSepsisBonus);
            float dmg = effectiveDps * Time.deltaTime * 3f;
            if (canCrit && Random.value < critChance) dmg *= critMult;
            _damageAccumulator += dmg;

            if (_damageAccumulator >= 1f)
            {
                int intDmg = Mathf.FloorToInt(_damageAccumulator);
                _damageable.TakeDamage(intDmg, new Color(0.9f, 0.15f, 0.15f));
                _damageAccumulator -= intDmg;
            }
        }
    }

    private void OnDestroy()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }
}
