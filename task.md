# 子弹射速 & 弹幕系统 Bug 修复

> 创建时间：2026-06-13
> 问题：弹幕数量和子弹数量过大时，发射频率变化甚至不发射，弹幕数量不对

## 已发现的 Bug

| # | Bug | 位置 | 状态 | 说明 |
|---|-----|------|------|------|
| 1 | **SpawnDotBullet 无子弹数上限** | `MagePassive.Firing.cs` | ✅ 已修复 | `Mathf.Min(1 + _bulletCountBonus, 3)`，上限 3 |
| 2 | **ApplyUpgradeVisual 每弹创建 Glow** | `MagePassive.Firing.cs` | ✅ 已修复 | Glow 对象池化，`_glowPool` 复用 |
| 3 | **effectiveCooldown 下限太小** | `MagePassive.Firing.cs` | ✅ 已修复 | `Max(0.01f, ...)` → `Max(0.1f, ...)` |
| 4 | **帧率雪崩** | `MagePassive.Firing.cs` | ✅ 已修复 | `deltaTime > 0.05f` 时跳过射击 |
| 5 | **GetFireDirection GetComponent** | `MagePassive.Firing.cs` | ✅ 已修复 | 改用 `EnemyBase.AllAlive` 静态列表 |
| 6 | **DotEffectConfig.GetDefault() 每帧调用** | `MagePassive.Firing.cs` | ✅ 已修复 | 硬编码 `spreadAngle = 15f` |
| 7 | **DotFrequency 链路断裂：顺序** | `DotBulletBase.cs` | ✅ 已修复 | EnsureStatusEffectManager 移到 OnHitEnemy 之后 |
| 8 | **DotFrequency 链路断裂：DPS 不变** | `BurnStackEffect.cs` | ✅ 已修复 | 伤害改用 baseInterval（频率只影响 tick 速率，不影响单次伤害） |
| 9 | **DPS 木桩层错误** | `TrainingDummy.cs` | ✅ 已修复 | `Default(0)` → `Enemy(9)`，修复子弹不碰撞 |

## DotFrequency 痛苦升级全链路

```
ApplyDotFrequency → mage.DotFrequencyBonus += 0.10 (×20 → 2.0)
    ↓
EnsureStatusEffectManager (每次子弹命中)
    ↓
GetDotFrequencyMultiplier → Max(0.1, 1.0 - 2.0) = 0.1
    ↓
StackEffectBase.FrequencyMultiplier = 0.1
    ↓
BurnStackEffect.Update:
    tickInterval = Max(0.2, 1.0/stacks) * 0.1  ← tick 更快
    dmg = baseDps * Max(0.2, 1.0/stacks)        ← 伤害不变
    DPS = dmg / tickInterval = 10× baseDps       ← DPS 提升
```

## 测试覆盖

| 测试 | 验证 |
|------|------|
| `DotFrequency_MagePassive_BonusAccumulates` | 20次升级后 DotFrequencyBonus=2.0 |
| `DotFrequency_GetDotFrequencyMultiplier_ReturnsCorrectValue` | 0%/10%/50%/200% 加成倍率 |
| `DotFrequency_StackEffectBase_FrequencyMultiplierPropagates` | StackEffectBase 属性传播 |
| `DotFrequency_BurnTickInterval_DecreasesWithMultiplier` | tick 间隔随倍率缩短 |
| `DotFrequency_BurnDPS_IncreasesWithFrequency` | DPS 随频率真正提升 |
| `DotFrequency_EnsureStatusEffectManager_SetsFrequencyMultiplier` | EnsureStatusEffectManager 设置倍率 |
| `DotFrequency_Applier_UpdatesBonus` | ApplyUpgrade 链路验证 |
