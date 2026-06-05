using UnityEngine;

/// <summary>
/// 经验宝石，敌人死亡后在地面生成，玩家靠近后自动拾取。
/// 对应 Python: entities/loot.py 中的 XPGem
/// 
/// 使用方式：通过 SpawnManager 或 KillRewarder 在敌人死亡位置生成
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class XPGem : MonoBehaviour
{
    [Header("经验宝石")]
    [SerializeField] private int _xpAmount = 10;
    [SerializeField] private float _magnetRange = 3f;      // 磁铁吸引范围
    [SerializeField] private float _magnetSpeed = 15f;       // 吸引速度（基础）
    [SerializeField] private float _pickupRange = 0.5f;      // 拾取范围
    [SerializeField] private float _magnetSpeedNear = 25f;   // #24 近距离快速吸引
    [SerializeField] private float _lifetime = 30f;         // 存在时间（秒）
    [SerializeField] private float _spawnFloatForce = 3f;   // 生成时的弹射力

    private Rigidbody2D _rb;
    private Transform _player;
    private float _spawnTime;
    private bool _isBeingMagnetized = false;
    private SpriteRenderer _spriteRenderer;

    // #24 帧跳过优化（非磁吸状态的宝石每3帧检测一次距离）
    private int _frameSkipCounter = 0;
    private const int FRAME_SKIP_INTERVAL = 3;

    public int XpAmount => _xpAmount;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _isBeingMagnetized = false;

        // 使用全局引用缓存
        var playerController = GameReferences.Player;
        if (playerController != null)
        {
            _player = playerController.transform;
        }

        // 设置触发器
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // 给一个随机方向的弹射力
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        _rb.linearVelocity = randomDir * _spawnFloatForce;

        // 设置宝石颜色（根据经验量不同颜色不同）
        if (_spriteRenderer != null)
        {
            if (_xpAmount >= 50)
                _spriteRenderer.color = new Color(0.3f, 0.5f, 1f); // 蓝色大宝石
            else if (_xpAmount >= 25)
                _spriteRenderer.color = new Color(0.2f, 0.8f, 0.2f); // 绿色中宝石
            else
                _spriteRenderer.color = new Color(0.5f, 1f, 0.5f); // 浅绿色小宝石
        }

        // 注册到屏幕外裁剪器
        if (OffScreenCuller.Instance != null)
        {
            OffScreenCuller.TrackLoot(gameObject, PoolHelper.XP_GEM);
        }
    }

    private void OnDisable()
    {
        OffScreenCuller.Untrack(gameObject);
    }

    private void Update()
    {
        // 超时回收
        if (Time.time - _spawnTime > _lifetime)
        {
            DespawnSelf();
            return;
        }

        if (_player == null) return;

        // #24 使用平方距离避免开方运算
        Vector3 delta = _player.position - transform.position;
        float distSqr = delta.sqrMagnitude;

        // #24 帧跳过优化：非磁吸状态每 3 帧检测一次（大量掉落物时减少 CPU 开销）
        if (!_isBeingMagnetized)
        {
            _frameSkipCounter++;
            if (_frameSkipCounter % FRAME_SKIP_INTERVAL != 0)
            {
                // 未激活时减速
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);
                return;
            }
        }

        // 进入磁铁范围，开始吸引（应用全局磁铁倍率 + 永久商店加成）
        float permanentMult = SaveManager.Instance?.GetPermanentMultiplier("magnet_radius") ?? 1f;
        float effectiveMagnetRange = _magnetRange * MagnetMultiplierSystem.MagnetRangeMultiplier * permanentMult;
        float effectiveMagnetRangeSqr = effectiveMagnetRange * effectiveMagnetRange;
        if (distSqr < effectiveMagnetRangeSqr)
        {
            _isBeingMagnetized = true;
        }

        // 磁铁吸引移动（#24 距离越近速度越快的递增曲线）
        if (_isBeingMagnetized)
        {
            float pickupRangeSqr = _pickupRange * _pickupRange;
            Vector2 dir = delta.normalized;

            // #24 距离越近速度越快：线性插值
            float dist = Mathf.Sqrt(distSqr); // 仅在磁吸时开方
            float speedT = 1f - Mathf.Clamp01(dist / effectiveMagnetRange);
            float speed = Mathf.Lerp(_magnetSpeed, _magnetSpeedNear, speedT);
            _rb.linearVelocity = dir * speed;

            // 到达拾取范围，拾取
            if (distSqr < pickupRangeSqr)
            {
                Pickup();
            }
        }
        else
        {
            // 减速（模拟摩擦）
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);
        }
    }

    /// <summary>
    /// 拾取经验宝石
    /// </summary>
    private void Pickup()
    {
        DebugHelper.Log($"[XPGem] Picked up {_xpAmount} XP gem");

        // 通知全局事件
        EventManager.TriggerItemPicked("XPGem", _xpAmount);

        // 直接给玩家添加经验（PlayerLevelSystem 已订阅 OnEnemyKilled，
        // 但经验宝石需要直接调用 AddExp）
        var levelSystem = _player.GetComponent<PlayerLevelSystem>();
        if (levelSystem != null)
        {
            levelSystem.AddExp(_xpAmount);
        }

        DespawnSelf();
    }

    /// <summary>
    /// 回收自身（优先对象池）
    /// </summary>
    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.XP_GEM);
    }

    /// <summary>
    /// 设置经验宝石属性
    /// </summary>
    public void Setup(int xpAmount)
    {
        _xpAmount = xpAmount;
    }

    /// <summary>
    /// 设置磁铁范围（来自玩家的磁铁升级）
    /// </summary>
    public void SetMagnetRange(float range)
    {
        _magnetRange = range;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _magnetRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _pickupRange);
    }
}