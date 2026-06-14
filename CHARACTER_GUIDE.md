# 🎮 新角色开发指南

> 重构后的角色系统让新角色开发像"填表"一样简单。

## 快速开始

### 步骤 1：创建角色被动类

创建 `Assets/Scripts/Player/XXXCharacterPassive.cs`：

```csharp
using UnityEngine;

public class XXXCharacterPassive : CharacterPassiveBase
{
    public override string CharacterId => "xxx";
    public override string DisplayName => "XXX 角色";

    // 角色专属字段
    [Header("XXX 专属")]
    [SerializeField] private float _baseCooldown = 0.8f;
    [SerializeField] private int _baseDamage = 10;

    private XXXUpgradeConfig _upgradeConfig;
    public void SetUpgradeConfig(XXXUpgradeConfig config) { _upgradeConfig = config; }

    private float _lastFireTime;

    protected override void Awake()
    {
        base.Awake();
        _lastFireTime = Time.time;
    }

    private void Update()
    {
        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        float deltaTime = Time.deltaTime;
        if (deltaTime > 0.05f) return;

        // 使用通用射击方向计算
        Vector2 fireDir = FireDirectionHelper.GetFireDirection(transform);
        bool hasTarget = fireDir.sqrMagnitude >= 0.01f;

        float attackSpeedMult = GetAttackSpeedMultiplier();
        float effectiveCooldown = Mathf.Max(0.1f, _baseCooldown * attackSpeedMult);

        if (hasTarget && Time.time >= _lastFireTime + effectiveCooldown)
        {
            _lastFireTime = Time.time;
            SpawnBullets(fireDir);
        }
    }

    private void SpawnBullets(Vector2 direction)
    {
        float dmgMult = _weaponController != null ? _weaponController.DamageMultiplier : 1f;
        int bulletCount = Mathf.Min(1 + _bulletCountBonus, 3);

        for (int b = 0; b < bulletCount; b++)
        {
            Vector2 fireDir = direction;
            if (bulletCount > 1)
            {
                float spreadAngle = 15f;
                float angle = (b - (bulletCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(
                    direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad),
                    direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)
                ).normalized;
            }

            // 使用通用子弹工厂
            BulletFactory.Create("simple", transform.position, fireDir,
                _baseDamage, dmgMult);
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
            // ... 其他升级类型
        }

        return true;
    }
}
```

### 步骤 2：创建升级配置

创建 `Assets/ScriptableObjects/Config/XXXUpgradeConfig.cs`：

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "XXXUpgradeConfig", menuName = "VampireGame/XXX Upgrade Config")]
public class XXXUpgradeConfig : CharacterUpgradeConfig
{
    private void OnEnable()
    {
        characterId = "xxx";
        displayName = "XXX 角色";
        description = "角色描述";
        characterColor = Color.white;

        if (upgradeEntries == null || upgradeEntries.Length == 0)
        {
            upgradeEntries = new UpgradeEntry[]
            {
                new UpgradeEntry
                {
                    upgradeId = "haste",
                    upgradeName = "急速 (Haste)",
                    description = "攻速+15%",
                    category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed,
                    value1 = 0.15f,
                    maxStacks = 0
                },
                // ... 更多升级选项
            };
        }
    }
}
```

### 步骤 3：创建子弹（可选）

如果默认的 `SimpleBullet` 不满足需求，创建自定义子弹：

```csharp
using UnityEngine;

public class XXXBullet : ProjectileBase
{
    protected override void OnHitEnemy(GameObject enemy)
    {
        var dmg = enemy.GetComponent<Damageable>();
        if (dmg == null || dmg.CurrentHp <= 0) return;

        float damage = _impactDamage * _damageMultiplier;
        if (_canCrit && Random.value < _critChance)
            damage *= _critMult;

        dmg.TakeDamage(damage);
        // 自定义命中效果...
    }

    public static XXXBullet Create(Vector2 pos, Vector2 dir, float speed,
        int impactDamage, float dmgMult)
    {
        var go = new GameObject("XXXBullet");
        // ... 创建 GameObject 和组件
        var bullet = go.AddComponent<XXXBullet>();
        bullet.SetupBullet(speed, 4f, impactDamage, dmgMult, false, 0f, 2f);
        bullet.SetDirection(dir);
        return bullet;
    }
}
```

### 步骤 4：创建角色数据

在 Unity Editor 中：
1. 右键 `Assets/ScriptableObjects/Characters/` → `Create` → `VampireGame` → `Character Data`
2. 设置角色属性（HP/移速/护甲/颜色/图标）

### 步骤 5：注册到角色工厂

在 `CharacterFactory.cs` 的 `EnsureInitialized()` 方法中添加：

```csharp
Register("xxx", go => go.AddComponent<XXXCharacterPassive>());
```

### 步骤 6：创建升级配置资源

1. 在 Unity Editor 中右键 `Assets/Resources/Configs/` → `Create` → `VampireGame` → `XXX Upgrade Config`
2. 命名为 `xxxUpgradeConfig`（必须匹配 `{characterId}UpgradeConfig` 格式）

## 架构说明

### 接口层次

```
ICharacterPassive (通用接口)
├── CharacterPassiveBase (抽象基类)
│   ├── MagePassive : IDotCharacterPassive (DOT 角色)
│   ├── BlueCharacterPassive (简单角色)
│   └── XXXCharacterPassive (你的角色)
```

### 子弹层次

```
ProjectileBase (通用子弹基类)
├── DotBulletBase (DOT 子弹基类)
│   ├── PoisonBullet
│   ├── BurnBullet
│   ├── FrostBullet
│   └── WindBullet
├── SimpleBullet (蓝色子弹)
└── XXXBullet (你的子弹)
```

### 升级配置层次

```
CharacterUpgradeConfig (ScriptableObject 基类)
├── MageUpgradeConfig (Mage 专属)
├── BlueUpgradeConfig (蓝色角色)
└── XXXUpgradeConfig (你的角色)
```

### 全局引用

```csharp
GameReferences.CharacterPassive   // ICharacterPassive (通用)
GameReferences.DotCharacterPassive // IDotCharacterPassive (DOT 角色专用)
GameReferences.MagePassive         // MagePassive (仅兼容，新代码勿用)
```

## 最佳实践

1. **优先使用通用接口**：`ICharacterPassive` 而非具体类
2. **复用现有组件**：`SimpleBullet`、`BulletFactory`、`FireDirectionHelper`
3. **配置驱动**：升级数据放在 `CharacterUpgradeConfig` 子类中
4. **最小化修改**：新角色只需创建新文件，不修改现有系统

## 参考实现

- **简单角色**：`BlueCharacterPassive` + `BlueUpgradeConfig` + `SimpleBullet`
- **DOT 角色**：`MagePassive` + `MageUpgradeConfig` + `DotBulletFactory`
