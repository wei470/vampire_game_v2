# 子弹射击系统重构计划

> 修复：高攻速下子弹倾巢而出 + 真空期问题
> 创建时间：2026-06-14

---

## Bug 根因分析

当前射击代码有 3 个设计缺陷共同导致"爆发 + 真空"模式：

### 缺陷 1：攻速变化时的冷却缩放（MagePassive.Firing.cs:19-28）

```csharp
// 每帧检测攻速变化，缩放所有枪的剩余冷却
if (Mathf.Abs(attackSpeedMult - _lastAttackSpeedMult) > 0.001f)
{
    _dotGuns[j].nextAllowedFireTime = Time.time + oldRemaining * (attackSpeedMult / _lastAttackSpeedMult);
}
```

**问题**：当玩家一次性拾取 30+ 层急速时，`attackSpeedMult` 从 1.0 瞬间降到 0.2。
`oldRemaining * (0.2 / 1.0)` = 剩余冷却缩到 20%，所有枪的 `nextAllowedFireTime` 同时落入过去 → 全部同时开火。

### 缺陷 2：所有枪共享 Time.time 基准

```csharp
gun.nextAllowedFireTime = Time.time + effectiveCooldown;
```

所有枪在同一帧设置 `nextAllowedFireTime = 同一个 Time.time + cooldown`。
如果它们的冷却相同（都受 `Mathf.Max(0.1f, ...)` 钳制），它们永远同步开火。

### 缺陷 3：最低冷却 0.1s + 多枪 = 子弹洪流

7 种 DOT 枪 × 3 发弹幕 = 每轮 21 发子弹。
最低冷却 0.1s = 每秒 10 轮 = **每秒 210 发子弹**。
这在一帧内爆发式创建，导致帧率崩溃，然后所有枪再次同步 → 恶性循环。

---

## 重构方案

### 方案：累加器 + 枪交错 + 帧预算

用**时间累加器**替代 `nextAllowedFireTime` 检查，每枪有独立偏移，每帧限制子弹创建数。

```
设计：
1. 每枪维护 _timeAccumulator（累计经过时间）
2. 每枪初始化时有随机偏移（0 ~ cooldown），避免同步
3. 每帧：_timeAccumulator += deltaTime
4. 当 _timeAccumulator >= effectiveCooldown 时开火，减去 cooldown
5. 全局帧预算：每帧最多创建 MAX_BULLETS_PER_FRAME 发子弹
6. 移除攻速缩放逻辑（不需要了，累加器自然适配）
```

---

## 任务清单

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| 1 | **移除攻速缩放逻辑** | `Player/MagePassive.Firing.cs` | 删除 `_lastAttackSpeedMult` 追踪和冷却缩放代码 |
| 2 | **改用时间累加器** | `Player/MagePassive.Firing.cs` | 每枪 `_accumulator += deltaTime`，超过冷却就开火 |
| 3 | **枪交错初始化** | `Player/MagePassive.cs` | `UnlockDotGun` 时给 `accumulator` 随机初始值 `[0, cooldown)` |
| 4 | **帧预算限制** | `Player/MagePassive.Firing.cs` | `MAX_BULLETS_PER_FRAME = 15`，超出排队到下一帧 |
| 5 | **BlueCharacterPassive 同步重构** | `Player/BlueCharacterPassive.cs` | 同样改用累加器 |
| 6 | **测试验证** | `Tests/` | 高攻速 + 弹幕场景测试 |

---

## 伪代码

### MagePassive.Firing.cs 重构后

```csharp
private const int MAX_BULLETS_PER_FRAME = 15;
private int _bulletsThisFrame;

private void Update()
{
    _detonateSystem.UpdateChargeInput();
    if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        return;

    float dt = Time.deltaTime;
    Vector2 fireDir = GetFireDirection();
    bool hasTarget = fireDir.sqrMagnitude >= 0.01f;
    float dmgMult = _weaponController != null ? _weaponController.DamageMultiplier : 1f;
    float attackSpeedMult = GetAttackSpeedMultiplier();

    _bulletsThisFrame = 0;

    for (int i = 0; i < _dotGuns.Count; i++)
    {
        if (_bulletsThisFrame >= MAX_BULLETS_PER_FRAME) break;

        var gun = _dotGuns[i];
        float effectiveCooldown = Mathf.Max(0.1f, gun.cooldown * attackSpeedMult);

        // 累加器模式：不受帧率和攻速变化影响
        gun.accumulator += dt;

        while (hasTarget && gun.accumulator >= effectiveCooldown && _bulletsThisFrame < MAX_BULLETS_PER_FRAME)
        {
            gun.accumulator -= effectiveCooldown;
            SpawnDotBullet(gun, fireDir, dmgMult);
            _bulletsThisFrame += Mathf.Min(1 + _bulletCountBonus, 3);
        }

        // 防止长时间不射击后累加器积压过大
        if (gun.accumulator > effectiveCooldown * 3f)
            gun.accumulator = effectiveCooldown * 3f;
    }
}
```

### DotGunState 新增字段

```csharp
public class DotGunState : GunState
{
    // ... 现有字段 ...
    public float accumulator;  // 时间累加器
}
```

### UnlockDotGun 交错初始化

```csharp
_guns.Add(new DotGunState
{
    effectType = type, color = color, cooldown = cooldown,
    impactDamage = impactDmg, dotDps = dotDps, dotDuration = dotDuration,
    upgradeLevel = 1,
    accumulator = Random.Range(0f, cooldown)  // 随机偏移，避免同步
});
```

---

## 预期效果

| 指标 | 重构前 | 重构后 |
|------|--------|--------|
| 子弹同步爆发 | 7 枪同时开火 | 各枪交错开火 |
| 攻速变化时的冷却行为 | 缩放导致瞬间全部就绪 | 累加器自然适配 |
| 每帧子弹上限 | 无限（21+ 发/帧） | 15 发/帧 |
| 真空期 | 爆发后帧率崩溃导致 | 均匀分布，无真空 |
| 高攻速稳定性 | 不稳定 | 稳定 |
