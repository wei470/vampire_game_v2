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

    private void Awake()
    {
        _baseEntity = GetComponent<BaseEntity>();
    }

    private void OnEnable()
    {
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
    [SerializeField] private float _specialDropChance = 0.05f;

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEntityDeath(Vector3 deathPosition)
    {
        // 计算连击倍率
        int finalXP = _xpReward;
        int finalCoin = _coinReward;

        if (ComboSystem.Instance != null)
        {
            float xpMult = ComboSystem.Instance.GetXPMultiplier();
            float coinMult = ComboSystem.Instance.GetCoinMultiplier();
            finalXP = Mathf.RoundToInt(_xpReward * xpMult);
            finalCoin = Mathf.RoundToInt(_coinReward * coinMult);
        }

        DebugHelper.Log($"[KillRewarder] {gameObject.name} killed at {deathPosition}, XP: {finalXP}, Coin: {finalCoin}");

        EventManager.TriggerEnemyKilled(deathPosition, finalXP, finalCoin);

        SpawnLoot(deathPosition, finalXP, finalCoin);

        if (_specialDropChance > 0f && Random.value < _specialDropChance)
        {
            SpawnSpecialDrop(deathPosition);
        }
    }

    private void SpawnLoot(Vector3 position, int xpAmount, int coinAmount)
    {
        SpawnXPGemPooled(position, xpAmount);
        SpawnCoinPooled(position, coinAmount);
    }

    private void SpawnXPGemPooled(Vector3 position, int xpAmount)
    {
        GameObject xpGem;
        if (_xpGemPrefab != null)
        {
            xpGem = PoolHelper.SpawnOrInstantiate(PoolHelper.XP_GEM, _xpGemPrefab, position, Quaternion.identity);
        }
        else
        {
            xpGem = SpawnDefaultXPGem(position, xpAmount);
        }

        if (xpGem != null)
        {
            var gem = xpGem.GetComponent<XPGem>();
            if (gem != null) gem.Setup(xpAmount);
        }
    }

    private void SpawnCoinPooled(Vector3 position, int coinAmount)
    {
        GameObject coin;
        if (_coinPrefab != null)
        {
            coin = PoolHelper.SpawnOrInstantiate(PoolHelper.COIN, _coinPrefab, position, Quaternion.identity);
        }
        else
        {
            coin = SpawnDefaultCoin(position, coinAmount);
        }

        if (coin != null)
        {
            var coinComp = coin.GetComponent<Coin>();
            if (coinComp != null) coinComp.Setup(coinAmount);
        }
    }

    /// <summary>
    /// 用代码创建默认经验宝石（无预制体时的备用方案）
    /// 无碰撞体积，纯视觉绿色小圆球
    /// </summary>
    private GameObject SpawnDefaultXPGem(Vector3 position, int xpAmount)
    {
        var go = new GameObject("XPGem");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.color = new Color(0.1f, 0.9f, 0.2f);
        go.transform.localScale = Vector3.one * 0.6f;

        // 碰撞体仅用于触发器检测（自动吸取），isTrigger = true 不阻碍移动
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

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
        sr.sprite = SpriteFactory.Circle;
        sr.color = new Color(1f, 0.85f, 0f);
        go.transform.localScale = Vector3.one * 0.5f;

        // 碰撞体仅用于触发器检测，isTrigger = true 不阻碍移动
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        var coin = go.AddComponent<Coin>();
        coin.Setup(amount);
        return go;
    }

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

    public void SetRewards(int xp, int coin)
    {
        _xpReward = xp;
        _coinReward = coin;
    }
}