using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 引爆冲击波 — 从玩家身上炸出一圈红色扩展圆环，
/// 接触到的敌人触发引爆伤害（仅一次）。
///
/// 使用方式：由 DetonateSystem 在引爆时创建。
/// </summary>
public class DetonateWaveEffect : MonoBehaviour
{
    private float _maxRadius;
    private float _expandSpeed;
    private float _currentRadius;
    private float _detonateMultiplier;
    private float _critChance;
    private float _critMult;
    private ICharacterPassive _character;
    private HashSet<GameObject> _hitEnemies = new HashSet<GameObject>();
    private SpriteRenderer _sr;
    private float _totalDamage;
    private int _enemiesHit;
    private System.Action<float, int> _onComplete;
    private float _bleedHpPct;
    private float _burnHpPct;
    private float _poisonHpPct;
    private bool _finished;

    // 延迟特效（时停结束后播放）
    private struct PendingEffect
    {
        public Vector3 pos;
        public float dmg;
    }
    private List<PendingEffect> _pendingEffects = new List<PendingEffect>(16);

    private static Sprite _ringSprite;

    public void Init(float maxRadius, float expandSpeed, float detonateMultiplier,
        float critChance, float critMult, ICharacterPassive character,
        float bleedHpPct, float burnHpPct, float poisonHpPct,
        System.Action<float, int> onComplete)
    {
        _maxRadius = maxRadius;
        _expandSpeed = expandSpeed;
        _detonateMultiplier = detonateMultiplier;
        _critChance = critChance;
        _critMult = critMult;
        _character = character;
        _bleedHpPct = bleedHpPct;
        _burnHpPct = burnHpPct;
        _poisonHpPct = poisonHpPct;
        _onComplete = onComplete;
        _currentRadius = 0f;
        _totalDamage = 0;
        _enemiesHit = 0;
        _hitEnemies.Clear();
        _finished = false;
        _pendingEffects.Clear();
    }

    private void Awake()
    {
        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = GetRingSprite();
        _sr.color = new Color(1f, 0.15f, 0.1f, 0.7f);
        _sr.sortingOrder = 50;
        transform.localScale = Vector3.one * 0.1f;
    }

    private void Update()
    {
        _currentRadius += _expandSpeed * Time.unscaledDeltaTime;
        float scale = _currentRadius * 2f;
        transform.localScale = new Vector3(scale, scale, 1f);

        float alpha = Mathf.Lerp(0.7f, 0f, _currentRadius / _maxRadius);
        _sr.color = new Color(1f, 0.15f, 0.1f, alpha);

        DetectEnemies();

        if (!_finished && _currentRadius >= _maxRadius)
        {
            _finished = true;

            // 时停期间显示总伤害数字
            if (_enemiesHit > 0 && _totalDamage > 0)
            {
                DetonateFlashEffect.Show(0.15f);
                DamagePopup.CreateDetonateTotal(transform.position, _totalDamage, _enemiesHit);
            }

            // 恢复时间 + 后处理
            _onComplete?.Invoke(_totalDamage, _enemiesHit);

            // 时间恢复后播放所有爆炸特效
            for (int i = 0; i < _pendingEffects.Count; i++)
            {
                var e = _pendingEffects[i];
                CombatManager.CreateExplosionEffect(e.pos, 2f, new Color(1f, 0.3f, 0.2f), 0.4f);
                DamagePopup.Create(e.pos, e.dmg, new Color(1f, 0.2f, 0.2f), false);
            }
            _pendingEffects.Clear();

            Destroy(gameObject);
        }
    }

    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(32);

    private void DetectEnemies()
    {
        int count = PhysicsHelper.OverlapCircle(transform.position, _currentRadius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (!col.CompareTag("Enemy")) continue;
            if (_hitEnemies.Contains(col.gameObject)) continue;

            var dmg = col.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;

            _hitEnemies.Add(col.gameObject);
            ApplyDetonateToEnemy(col.gameObject, dmg);
        }
    }

    private void ApplyDetonateToEnemy(GameObject enemy, Damageable dmg)
    {
        float enemyDmg = 0;
        bool hadEffect = false;

        DetonateResult detResult = default;
        if (enemy.TryGetComponent<StatusEffectManager>(out var sem) && sem.HasAnyDot)
        {
            float d = sem.Detonate(_detonateMultiplier, _critChance, _critMult, out detResult);
            if (d > 0) { _totalDamage += d; enemyDmg += d; hadEffect = true; }
        }

        if (enemy.TryGetComponent<BleedEffect>(out var bleed))
        {
            float extra = dmg.MaxHp * _bleedHpPct * _detonateMultiplier;
            dmg.TakeDamage(extra); _totalDamage += extra; enemyDmg += extra; hadEffect = true;
        }

        if (enemy.TryGetComponent<BurnStackEffect>(out var burn))
        {
            float extra = dmg.MaxHp * _burnHpPct * _detonateMultiplier;
            dmg.TakeDamage(extra); _totalDamage += extra; enemyDmg += extra; hadEffect = true;
        }

        if (enemy.TryGetComponent<PoisonStackEffect>(out var poison))
        {
            float extra = dmg.MaxHp * _poisonHpPct * _detonateMultiplier;
            dmg.TakeDamage(extra); _totalDamage += extra; enemyDmg += extra; hadEffect = true;
        }

        if (hadEffect)
        {
            _enemiesHit++;
            _pendingEffects.Add(new PendingEffect { pos = enemy.transform.position, dmg = enemyDmg });
        }
    }

    private static Sprite GetRingSprite()
    {
        if (_ringSprite != null) return _ringSprite;

        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float outerR = size / 2f;
        float innerR = outerR * 0.75f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= outerR && dist >= innerR)
                {
                    float edge = Mathf.Min(dist - innerR, outerR - dist) / (outerR - innerR);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, edge));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        return _ringSprite;
    }
}
