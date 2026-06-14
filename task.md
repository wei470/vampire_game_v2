# 子弹射击系统重构计划 v2

> 修复：while 循环导致单枪垄断帧预算，其他枪被饿死
> 创建时间：2026-06-14

---

## Bug 根因分析

上次的累加器方案有一个致命缺陷：**`while` 循环允许单枪在一帧内连射多发**。

```csharp
// 上次的代码（有 bug）
while (hasTarget && gun.accumulator >= effectiveCooldown && _bulletsThisFrame < MAX_BULLETS_PER_FRAME)
{
    gun.accumulator -= effectiveCooldown;
    SpawnDotBullet(gun, fireDir, dmgMult);  // 每次创建 3 发子弹
    _bulletsThisFrame += 3;
}
```

**场景**：7 枪，弹幕 2（每枪 3 发），急速 30 层（cooldown = 0.1s）
- Frame 1: 枪 A 累加器 = 0.5（积压 5 轮），while 连射 5 次 = 15 发，帧预算耗尽
- Frame 1: 枪 B~G 全部跳过（`_bulletsThisFrame >= 15`）
- Frame 2: 枪 A 累加器归零，枪 B~G 各射 1 发
- 结果：交替出现"一枪独占"和"多枪分食"，节奏不稳

---

## 重构方案

### 核心改动：`while` → `if`，每枪每帧最多射 1 发

```csharp
// 修复后
if (hasTarget && gun.accumulator >= effectiveCooldown && _bulletsThisFrame < MAX_BULLETS_PER_FRAME)
{
    gun.accumulator -= effectiveCooldown;  // 只扣一轮
    SpawnDotBullet(gun, fireDir, dmgMult);
    _bulletsThisFrame += bulletsPerShot;
}
```

**效果**：
- 7 枪交错开火，每帧最多 5 枪射击（15 / 3 = 5）
- 节奏均匀，无爆发无真空
- 累加器积压上限 1.5 轮（不是 3 轮），防止补射

---

## 任务清单

| # | 任务 | 文件 |
|---|------|------|
| 1 | `while` → `if`，每枪每帧最多 1 发 | `Player/MagePassive.Firing.cs` |
| 2 | 累加器上限从 3x → 1.5x | `Player/MagePassive.Firing.cs` |
| 3 | BlueCharacterPassive 同步修复 | `Player/BlueCharacterPassive.cs` |
| 4 | 射速测试：弹幕 1/2/3 × 急速 1~10 | `Tests/Editor/FiringSystemTests.cs` |

---

## 预期效果

| 指标 | 修复前（while） | 修复后（if） |
|------|----------------|-------------|
| 单枪每帧最多射出 | N 发（垄断帧预算） | 1 发 |
| 7 枪每帧分布 | 1 枪 × 5 发 + 6 枪 × 0 发 | 5 枪 × 1 发 + 2 枪等待 |
| 节奏稳定性 | 交替爆发+真空 | 均匀交错 |
| 5 秒总子弹数 | 不可预测 | 精确可控 |
