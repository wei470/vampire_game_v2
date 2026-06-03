using UnityEngine;

/// <summary>
/// 金币拾取物，敌人死亡后在地面生成，玩家靠近后自动拾取。
/// 对应 Python: entities/loot.py 中的 Coin
/// 
/// 使用方式：通过 KillRewarder 在敌人死亡位置生成
/// 暂存数值，UI 后续显示
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [Header("金币")]
    [SerializeField] private int _coinAmount = 5;
    [SerializeField] private float _magnetRange = 4f;
    [SerializeField] private float _magnetSpeed = 18f;
    [SerializeField] private float _magnetSpeedNear = 28f;  // #24 近距离快速吸引
    [SerializeField] private float _pickupRange = 0.5f;
    [SerializeField] private float _lifetime = 30f;
    [SerializeField] private float _spawnFloatForce = 4f;

    private Rigidbody2D _rb;
    private Transform _player;
    private float _spawnTime;
    private bool _isBeingMagnetized = false;
    private SpriteRenderer _spriteRenderer;

    // #24 帧跳过优化（非磁吸状态的金币每3帧检测一次距离）
    private int _frameSkipCounter = 0;
    private const int FRAME_SKIP_INTERVAL = 3;

    /// <summary>
    /// 全局金币计数器（局内金币，拾取时同时存入 SaveManager）
    /// </summary>
    public static int TotalCoins { get; private set; }

    public int CoinAmount => _coinAmount;

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

        // 随机弹射
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        _rb.linearVelocity = randomDir * _spawnFloatForce;

        // 金币颜色（金黄色）
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = new Color(1f, 0.85f, 0f); // 金色
        }

        // 注册到屏幕外裁剪器
        if (OffScreenCuller.Instance != null)
        {
            OffScreenCuller.TrackLoot(gameObject, PoolHelper.COIN);
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

        // #24 帧跳过优化：非磁吸状态每 3 帧检测一次
        if (!_isBeingMagnetized)
        {
            _frameSkipCounter++;
            if (_frameSkipCounter % FRAME_SKIP_INTERVAL != 0)
            {
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);
                return;
            }
        }

        // 应用全局磁铁倍率 + 永久商店加成
        float permanentMult = SaveManager.Instance?.GetPermanentMultiplier("magnet_radius") ?? 1f;
        float effectiveMagnetRange = _magnetRange * LevelUpUI.MagnetRangeMultiplier * permanentMult;
        float effectiveMagnetRangeSqr = effectiveMagnetRange * effectiveMagnetRange;
        if (distSqr < effectiveMagnetRangeSqr)
        {
            _isBeingMagnetized = true;
        }

        if (_isBeingMagnetized)
        {
            float pickupRangeSqr = _pickupRange * _pickupRange;
            Vector2 dir = delta.normalized;

            // #24 距离越近速度越快
            float dist = Mathf.Sqrt(distSqr);
            float speedT = 1f - Mathf.Clamp01(dist / effectiveMagnetRange);
            float speed = Mathf.Lerp(_magnetSpeed, _magnetSpeedNear, speedT);
            _rb.linearVelocity = dir * speed;

            if (distSqr < pickupRangeSqr)
            {
                Pickup();
            }
        }
        else
        {
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);
        }
    }

    /// <summary>
    /// 拾取金币（同时存入 SaveManager 持久化）
    /// </summary>
    private void Pickup()
    {
        TotalCoins += _coinAmount;

        // 存入存档系统（局外货币）
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddCoins(_coinAmount);
        }

        DebugHelper.Log($"[Coin] Picked up {_coinAmount} coins, total: {TotalCoins}");

        // 通知全局事件
        EventManager.TriggerItemPicked("Coin", _coinAmount);
        EventManager.TriggerCoinChanged(TotalCoins);

        DespawnSelf();
    }

    /// <summary>
    /// 回收自身（优先对象池）
    /// </summary>
    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.COIN);
    }

    /// <summary>
    /// 设置金币数量
    /// </summary>
    public void Setup(int amount)
    {
        _coinAmount = amount;
    }

    /// <summary>
    /// 重置金币总数（游戏重新开始时调用）
    /// </summary>
    public static void ResetTotalCoins()
    {
        TotalCoins = 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _magnetRange);
    }
}