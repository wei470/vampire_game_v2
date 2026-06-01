using UnityEngine;

/// <summary>
/// 死亡奖励组件，实体死亡时通过 EventManager 广播奖励事件。
/// 对应 Python: entities/base.py 中的 KillRewarder mixin
/// 
/// 使用方式：挂载到敌人 GameObject 上，与 BaseEntity + Damageable 配合使用
/// </summary>
public class KillRewarder : MonoBehaviour, IRewardable
{
    [Header("击杀奖励")]
    [SerializeField] private int _xpReward = 10;
    [SerializeField] private int _coinReward = 5;

    /// <summary>
    /// 经验奖励
    /// </summary>
    public int XpReward => _xpReward;

    /// <summary>
    /// 金币奖励
    /// </summary>
    public int CoinReward => _coinReward;

    private BaseEntity _baseEntity;
    private bool _subscribed = false;

    private void OnEnable()
    {
        // 懒加载 BaseEntity — 在 SpawnCodeEnemy 中 KillRewarder 可能先于 EnemyBase 添加
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (_subscribed && _baseEntity != null)
        {
            _baseEntity.OnDeath -= OnEntityDeath;
            _subscribed = false;
        }
    }

    /// <summary>
    /// 尝试订阅死亡事件（延迟到 BaseEntity 可用时）
    /// </summary>
    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (_baseEntity == null) _baseEntity = GetComponent<BaseEntity>();
        if (_baseEntity != null)
        {
            _baseEntity.OnDeath += OnEntityDeath;
            _subscribed = true;
        }
    }

    [Header("掉落物预制体（可选）")]
    [SerializeField] private GameObject _xpGemPrefab;
    [SerializeField] private GameObject _coinPrefab;

    [Header("特殊掉落")]
    [SerializeField] private float _specialDropChance = 0.05f; // 5% 概率掉落特殊物品

    /// <summary>
    /// 实体死亡时触发奖励
    /// </summary>
    private void Start()
    {
        // Start 时再次尝试订阅（确保 EnemyBase 已添加）
        TrySubscribe();
    }

    private void OnEntityDeath(Vector3 deathPosition)
    {
        DebugHelper.Log($"[KillRewarder] {gameObject.name} killed at {deathPosition}, XP: {_xpReward}, Coin: {_coinReward}");

        // 通过 EventManager 广播敌人击杀事件
        EventManager.TriggerEnemyKilled(deathPosition, _xpReward, _coinReward);

        // 生成掉落物
        SpawnLoot(deathPosition);

        // 特殊掉落（概率触发）
        if (_specialDropChance > 0f && Random.value < _specialDropChance)
        {
            SpawnSpecialDrop(deathPosition);
        }
    }

    /// <summary>
    /// 在死亡位置生成掉落物（优先使用对象池）
    /// </summary>
    private void SpawnLoot(Vector3 position)
    {
        // 生成经验宝石（对象池优先）
        SpawnXPGemPooled(position);

        // 生成金币（对象池优先）
        SpawnCoinPooled(position);
    }

    /// <summary>
    /// 通过对象池生成经验宝石
    /// </summary>
    private void SpawnXPGemPooled(Vector3 position)
    {
        GameObject xpGem;
        if (_xpGemPrefab != null)
        {
            xpGem = PoolHelper.SpawnOrInstantiate(PoolHelper.XP_GEM, _xpGemPrefab, position, Quaternion.identity);
        }
        else
        {
            xpGem = SpawnDefaultXPGem(position, _xpReward);
        }

        if (xpGem != null)
        {
            var gem = xpGem.GetComponent<XPGem>();
            if (gem != null) gem.Setup(_xpReward);
        }
    }

    /// <summary>
    /// 通过对象池生成金币
    /// </summary>
    private void SpawnCoinPooled(Vector3 position)
    {
        GameObject coin;
        if (_coinPrefab != null)
        {
            coin = PoolHelper.SpawnOrInstantiate(PoolHelper.COIN, _coinPrefab, position, Quaternion.identity);
        }
        else
        {
            coin = SpawnDefaultCoin(position, _coinReward);
        }

        if (coin != null)
        {
            var coinComp = coin.GetComponent<Coin>();
            if (coinComp != null) coinComp.Setup(_coinReward);
        }
    }

    /// <summary>
    /// 用代码创建默认经验宝石（无预制体时的备用方案）
    /// </summary>
    private GameObject SpawnDefaultXPGem(Vector3 position, int xpAmount)
    {
        var go = new GameObject("XPGem");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
        sr.color = xpAmount >= 50 ? new Color(0.3f, 0.5f, 1f) 
                 : xpAmount >= 25 ? new Color(0.2f, 0.8f, 0.2f) 
                 : new Color(0.5f, 1f, 0.5f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.4f, 0.4f);

        var gem = go.AddComponent<XPGem>();
        gem.Setup(xpAmount);
        return go;
    }

    /// <summary>
    /// 用代码创建默认金币（无预制体时的备用方案）
    /// </summary>
    private GameObject SpawnDefaultCoin(Vector3 position, int amount)
    {
        var go = new GameObject("Coin");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
        sr.color = new Color(1f, 0.85f, 0f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.3f, 0.3f);

        var coin = go.AddComponent<Coin>();
        coin.Setup(amount);
        return go;
    }

    /// <summary>
    /// 生成特殊掉落物（随机类型）
    /// </summary>
    private void SpawnSpecialDrop(Vector3 position)
    {
        var types = System.Enum.GetValues(typeof(SpecialDrop.DropType));
        var randomType = (SpecialDrop.DropType)types.GetValue(Random.Range(0, types.Length));
        float value = randomType == SpecialDrop.DropType.HealthPotion ? 0.25f
                    : randomType == SpecialDrop.DropType.DamageBoost ? 0.5f
                    : randomType == SpecialDrop.DropType.ShieldOrb ? 3f
                    : randomType == SpecialDrop.DropType.XPMultiplier ? 2f
                    : 0f;
        float duration = randomType == SpecialDrop.DropType.MagnetBurst ? 0f : 10f;
        SpecialDrop.Create(position + Vector3.up * 0.5f, randomType, value, duration);
        DebugHelper.Log($"[KillRewarder] Special drop: {randomType} at {position}");
    }

    /// <summary>
    /// 设置奖励值（用于动态调整，如 Boss 等级越高奖励越多）
    /// </summary>
    public void SetRewards(int xp, int coin)
    {
        _xpReward = xp;
        _coinReward = coin;
    }
}