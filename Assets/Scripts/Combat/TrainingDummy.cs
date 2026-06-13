using UnityEngine;

/// <summary>
/// DPS 测试木桩 — 超高HP，死亡后自动重生，配合 DpsTracker 显示实时 DPS。
/// </summary>
public class TrainingDummy : MonoBehaviour
{
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private long _maxHp = 2000000000;
    private int _armor = 0;
    private float _regen = 0f;
    private bool _invincible = false;
    private float _respawnDelay = 1f;
    private float _deathTime;
    private bool _dead = false;

    private static readonly Color ALIVE_COLOR = new Color(0.6f, 0.4f, 0.3f);
    private static readonly Color HIT_COLOR = new Color(1f, 1f, 1f);
    private float _hitFlashEnd;

    public void Init(long hp, int armor, float regen, bool invincible, float respawnDelay)
    {
        _maxHp = hp;
        _armor = armor;
        _regen = regen;
        _invincible = invincible;
        _respawnDelay = respawnDelay;
        SetupDummy();
    }

    private void Awake()
    {
        SetupDummy();
    }

    private void SetupDummy()
    {
        _damageable = GetComponent<Damageable>();
        if (_damageable == null) _damageable = gameObject.AddComponent<Damageable>();

        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = HealthBarSpriteHelper.GetWhiteSprite();
            _sr.color = ALIVE_COLOR;
            _sr.sortingOrder = 5;
        }

        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Default");

        var col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 1.5f);
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        transform.localScale = Vector3.one * 1.5f;

        // 设置 HP
        _damageable.SetMaxHp((int)Mathf.Min(_maxHp, int.MaxValue));
        _damageable.Heal((int)Mathf.Min(_maxHp, int.MaxValue));
        _damageable.enabled = true;
        _dead = false;

        // 注册受伤事件用于闪白
        _damageable.OnDamaged -= OnDamaged;
        _damageable.OnDamaged += OnDamaged;
    }

    private void OnDamaged(int hp, int maxHp)
    {
        _hitFlashEnd = Time.time + 0.08f;
    }

    private void Update()
    {
        if (_dead)
        {
            if (Time.time - _deathTime >= _respawnDelay)
                Respawn();
            return;
        }

        // 受击闪白
        if (_sr != null)
        {
            _sr.color = Time.time < _hitFlashEnd ? HIT_COLOR : ALIVE_COLOR;
        }

        // 回血
        if (_regen > 0 && _damageable != null && _damageable.CurrentHp < _damageable.MaxHp)
        {
            int heal = Mathf.Max(1, Mathf.RoundToInt(_regen * Time.deltaTime));
            _damageable.Heal(heal);
        }

        // 无敌模式：HP 永远不低于 1
        if (_invincible && _damageable != null && _damageable.CurrentHp <= 0)
        {
            _damageable.Heal(1);
        }

        // 死亡检测
        if (_damageable != null && _damageable.CurrentHp <= 0 && !_invincible)
        {
            _dead = true;
            _deathTime = Time.time;
            _sr.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
    }

    private void Respawn()
    {
        _damageable.Heal((int)Mathf.Min(_maxHp, int.MaxValue));
        _sr.color = ALIVE_COLOR;
        _dead = false;

        // 通知 DpsTracker 木桩重生
        DpsTracker.Instance?.OnDummyRespawn();
    }

    private void OnDestroy()
    {
        if (_damageable != null)
            _damageable.OnDamaged -= OnDamaged;
    }
}
