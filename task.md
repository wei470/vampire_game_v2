# 高急速 + 高弹幕 Bug 盘点与修复记录

> 创建时间：2026-06-14
> 最后更新：2026-06-14

---

## 急速机制（当前）

```
attackSpeedMult = Max(0.2, 1.0 / (1.0 + attackSpeedBonus))
effectiveCooldown = Max(0.1, gun.cooldown × attackSpeedMult)
```

每层急速 +0.15，对数递减，每层都有收益。

| 急速层数 | attackSpeedMult | Poison(1.8s) | Burn(0.5s) | Wind(0.2s) |
|---------|-----------------|---------------|------------|------------|
| 0 | 1.00 | 1.80s | 0.50s | 0.20s |
| 1 | 0.87 | 1.57s | 0.43s | 0.17s |
| 3 | 0.69 | 1.24s | 0.34s | 0.14s |
| 6 | 0.53 | 0.95s | 0.26s | 0.11s |
| 10 | 0.47 | 0.85s | 0.24s | **0.10s** |
| 30 | 0.24 | 0.43s | **0.10s** | **0.10s** |

---

## Bug 列表

### ~~Bug 1：交错偏移被 cap 吞掉，7 枪同步开火~~ ⏳ 待重构

```csharp
accumulator = index × cooldown / (index + 1)   // 初始化
```

| 枪 | 初始值(Poison) | cap(0.54) | cap 后 |
|----|---------------|-----------|--------|
| 0 | 0.00 | 0.54 | 0.00 |
| 1 | 0.90 | 0.54 | **0.54** |
| 2 | 1.20 | 0.54 | **0.54** |
| 6 | 1.54 | 0.54 | **0.54** |

枪 1~6 全部被 cap 到 0.54，**交错完全失效**。
第一轮：枪 0 先开火（0.36s），枪 1~6 同时开火（0.54s）。
第二轮起：所有枪 accumulator 都从 ~0 开始，**完全同步**。

### ~~Bug 2：cap 导致不均匀射击节奏~~ ⏳ 待重构

cap = `effectiveCooldown × 1.5`。以 60FPS、effectiveCooldown=0.1 为例：

```
Frame 1: accumulator = 0.15(cap) → 开火 → 0.05
Frame 2: accumulator = 0.0667 → 不开火
Frame 3: accumulator = 0.0834 → 不开火
Frame 4: accumulator = 0.1001 → 开火 → 0.0001
Frame 5~9: 不开火
Frame 10: 开火
```

节奏：开火、等 2 帧、开火、等 5 帧、开火、等 2 帧... **不均匀**。

### ~~Bug 3：无子弹数量上限~~ ✅ 已修复

**修复**：添加 `MAX_ACTIVE_BULLETS = 200`，超出时 `SpawnDotBullet` 停止创建。

```csharp
if (DotBulletBase.ActiveDotBullets.Count >= MAX_ACTIVE_BULLETS) break;
```

### ~~Bug 4：每帧子弹创建无节流~~ ⏳ 待重构

每帧最多 7 枪开火 × 5 弹幕 = 35 个 `DotBulletFactory.Create()` 调用。
需要更深层的重构（帧预算或时间切片）。

### ~~Bug 5/8：弹幕上限 3 但代码不统一~~ ✅ 已修复

**修复**：统一常量 `MAX_BARRAGE = 5`，所有引用处使用同一常量。

### ~~Bug 6：急速 6 层后完全无效~~ ✅ 已修复

**修复**：公式改为 `1/(1+bonus)`，对数递减，每层都有收益。

```
旧：6层=0.20, 10层=0.20, 30层=0.20（6层后无收益）
新：6层=0.53, 10层=0.47, 30层=0.24（每层递减但持续收益）
```

### ~~Bug 7：弹幕数量不稳定（1/2/3 随机）~~ ✅ 已修复

**修复**：
1. 创建失败重试一次
2. 仍失败则跳过并输出警告日志
3. 场上子弹超 200 时停止创建

```csharp
GameObject bullet = DotBulletFactory.Create(...);
if (bullet == null) bullet = DotBulletFactory.Create(...);
if (bullet == null) { Debug.LogWarning("..."); continue; }
```

---

## 根因总结

| 问题 | 根因 | 状态 |
|------|------|------|
| 7 枪同步开火 | 交错初始化被 cap 吞掉 + 二轮起 accumulator 重置到同值 | ⏳ |
| 射击节奏不均匀 | cap 在开火前强制钳位，破坏自然累加 | ⏳ |
| 子弹数量爆炸 | 无子弹上限 | ✅ |
| 帧率尖刺 | 每帧同步创建多个 GameObject | ⏳ |
| 急速溢出 | 线性公式无递减收益 | ✅ |
| 弹幕数量不稳定 | 创建失败静默跳过 | ✅ |
| 弹幕上限不统一 | 两处重复计算 | ✅ |

---

## 已完成的修复

### 1. 场上子弹上限 ✅

```csharp
const int MAX_ACTIVE_BULLETS = 200;
if (DotBulletBase.ActiveDotBullets.Count >= MAX_ACTIVE_BULLETS) break;
```

### 2. 弹幕上限统一为 5 ✅

```csharp
const int MAX_BARRAGE = 5;
int bulletCount = Mathf.Min(1 + _bulletCountBonus, MAX_BARRAGE);
```

### 3. 急速递减收益 ✅

```csharp
// 对数递减：1/(1+bonus)
attackSpeedMult = Max(0.2, 1.0 / (1.0 + attackSpeedBonus))
```

### 4. 子弹创建重试 + 日志 ✅

```csharp
GameObject bullet = DotBulletFactory.Create(...);
if (bullet == null) bullet = DotBulletFactory.Create(...);
if (bullet == null) { Debug.LogWarning($"[MagePassive] Bullet create failed for {gun.effectType}"); continue; }
```

---

## 待重构（Bug 1/2/4）

需要更深层的重构来解决：
1. 交错偏移被 cap 吞掉 → 改用相位偏移或移除 cap
2. 射击节奏不均匀 → 改用固定时间步或帧预算
3. 每帧创建无节流 → 时间切片或子弹合并
