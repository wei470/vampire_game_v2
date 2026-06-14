using UnityEngine;

/// <summary>
/// 蓝色角色被动能力 — 继承 CharacterPassiveBase，使用蓝色简单子弹。
/// 展示新角色架构的最简实现。
/// </summary>
public class BlueCharacterPassive : CharacterPassiveBase
{
    public override string CharacterId => "blue";
    public override string DisplayName => "蓝色战士";

    [Header("蓝色角色专属")]
    [SerializeField] private float _baseCooldown = 0.8f;
    [SerializeField] private int _baseDamage = 10;

    private BlueUpgradeConfig _upgradeConfig;

    public void SetUpgradeConfig(BlueUpgradeConfig config) { _upgradeConfig = config; }
    public BlueUpgradeConfig GetUpgradeConfig() => _upgradeConfig;

    private const int MAX_BULLETS_PER_FRAME = 15;
    private float _accumulator;

    protected override void Awake()
    {
        base.Awake();
        _accumulator = Random.Range(0f, _baseCooldown);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        Vector2 fireDir = GetFireDirection();
        bool hasTarget = fireDir.sqrMagnitude >= 0.01f;

        float attackSpeedMult = GetAttackSpeedMultiplier();
        float effectiveCooldown = Mathf.Max(0.1f, _baseCooldown * attackSpeedMult);

        _accumulator += Time.deltaTime;

        while (hasTarget && _accumulator >= effectiveCooldown)
        {
            _accumulator -= effectiveCooldown;
            SpawnBullets(fireDir);
        }

        if (_accumulator > effectiveCooldown * 3f)
            _accumulator = effectiveCooldown * 3f;
    }

    private void SpawnBullets(Vector2 direction)
    {
        float dmgMult = _weaponController != null ? _weaponController.DamageMultiplier : 1f;
        int bulletCount = Mathf.Min(1 + _bulletCountBonus, 3);
        float spreadAngle = 15f;

        for (int b = 0; b < bulletCount; b++)
        {
            Vector2 fireDir = direction;
            if (bulletCount > 1)
            {
                float angle = (b - (bulletCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(
                    direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad),
                    direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)
                ).normalized;
            }

            var bullet = SimpleBullet.Create(transform.position, fireDir, 12f,
                _baseDamage, dmgMult);

            if (bullet != null && _bulletSizeBonus > 0f)
            {
                bullet.transform.localScale *= (1f + _bulletSizeBonus);
            }
        }
    }

    public override bool ApplyUpgrade(string upgradeId)
    {
        if (_upgradeConfig == null) return false;

        var entry = _upgradeConfig.GetUpgradeEntry(upgradeId);
        if (!entry.HasValue) return false;

        var ue = entry.Value;
        switch (ue.category)
        {
            case CharacterUpgradeOption.UpgradeCategory.AttackSpeed:
                _attackSpeedBonus += ue.value1;
                break;
            case CharacterUpgradeOption.UpgradeCategory.BulletCount:
                _bulletCountBonus += (int)ue.value1;
                break;
            case CharacterUpgradeOption.UpgradeCategory.BulletSize:
                _bulletSizeBonus += ue.value1;
                break;
            case CharacterUpgradeOption.UpgradeCategory.Penetrate:
            case CharacterUpgradeOption.UpgradeCategory.Ricochet:
                _penetrateCount += (int)ue.value1;
                break;
            default:
                DebugHelper.Log($"[BlueCharacterPassive] Unhandled upgrade category: {ue.category}");
                break;
        }

        DebugHelper.Log($"[BlueCharacterPassive] Applied upgrade: {ue.upgradeName}");
        return true;
    }
}
