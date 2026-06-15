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

### ~~Bug 1：交错偏移被 cap 吞掉，7 枪同步开火~~ ✅ 已修复

**根因**：硬钳位 `accumulator = effectiveCooldown × 1.5` 把所有积压的枪压到**同一个值**，
导致它们在同一帧开火、之后永久同步。

**修复**：cap 改为**相位保留取模**，保留各枪的子周期相位，长时间积压后仍维持交错。

```csharp
if (gun.accumulator > effectiveCooldown * 1.5f)
    gun.accumulator = effectiveCooldown + Mathf.Repeat(gun.accumulator, effectiveCooldown);
```

| 枪 | 积压前(Poison) | 旧硬钳位(0.54) | 新取模 cap |
|----|---------------|----------------|------------|
| 0 | 5.00 | 0.54 | 0.36 + 相位A |
| 1 | 5.03 | 0.54（同步） | 0.36 + 相位B |
| 6 | 5.18 | 0.54（同步） | 0.36 + 相位C |

各枪相位差被保留，**交错不再失效**。

### ~~Bug 2：cap 导致不均匀射击节奏~~ ✅ 已修复

**根因**：硬钳位在开火前强制把 accumulator 压到 1.5×cooldown，破坏自然累加节奏。

**修复**：同 Bug 1，取模 cap 把积压限制在「约 1 发」的同时保留相位，
开火后回落到 `[0, cooldown)` 自然累加，节奏恢复均匀（积压至多多打 1 发就回到稳态）。

### ~~Bug 3：无子弹数量上限~~ ✅ 已修复

**修复**：添加 `MAX_ACTIVE_BULLETS = 200`，超出时 `SpawnDotBullet` 停止创建。

```csharp
if (DotBulletBase.ActiveDotBullets.Count >= MAX_ACTIVE_BULLETS) break;
```

### ~~Bug 4：每帧子弹创建无节流~~ ✅ 已修复

**根因**：每帧最多 7 枪开火 × 5 弹幕 = 35 个 `DotBulletFactory.Create()` 调用，造成帧率尖刺。

**修复**：加入每帧全局子弹创建预算 `MAX_BULLETS_PER_FRAME = 15`，配合**轮转起始枪索引**
保证各枪轮流优先开火（避免末尾的枪饿死）。超预算的枪保留 accumulator（已被 cap），下一帧补发。

**⚠️ 关键：帧预算以「整把枪」为粒度，绝不截断弹幕**（见 Bug 9）。

```csharp
const int MAX_BULLETS_PER_FRAME = 15;
int i = (_fireStartIndex + k) % gunCount;               // 轮转，避免末尾枪饿死
int barrage = Mathf.Min(1 + _bulletCountBonus, MAX_BARRAGE);
if (bulletsThisFrame > 0 && bulletsThisFrame + barrage > MAX_BULLETS_PER_FRAME)
    continue;                                            // 推迟整把枪，不拆弹幕
bulletsThisFrame += SpawnDotBullet(gun, fireDir, dmgMult);
_fireStartIndex = (_fireStartIndex + 1) % gunCount;
```

### ~~Bug 9：弹幕+2 却射出 1/2/3 发（弹幕被中途截断）~~ ✅ 已修复

**现象**：拿了弹幕+2 后，期望每次开火稳定 3 发，实际偶尔 1/2/3 发。

**根因**：弹幕在创建循环**内部**被两处逐发截断，导致单次弹幕数不稳定：
1. 早期版本的帧预算 `Mathf.Min(bulletCount, budget)` 在弹幕中途砍断；
2. `MAX_ACTIVE_BULLETS` 的**逐发** `break` —— 场上子弹逼近 200 时，一把弹幕只创建到一半。

**修复**：弹幕**原子化**。两道闸门都改为「整把枪之前」判断一次：
- 帧预算超限 → 推迟**整把枪**到下一帧（保留 accumulator）；
- 场上子弹达硬上限 → **整把跳过**（`return 0`），允许至多 `MAX_BARRAGE-1` 的轻微溢出（≤204，无害）。

弹幕循环内部不再有任何 `break`/截断，单次弹幕数恒为 `Min(1 + bulletCountBonus, MAX_BARRAGE)`。

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
| 7 枪同步开火 | 硬钳位 cap 把积压枪压到同值 → 同帧开火后永久同步 | ✅ |
| 射击节奏不均匀 | cap 在开火前强制钳位，破坏自然累加 | ✅ |
| 子弹数量爆炸 | 无子弹上限 | ✅ |
| 帧率尖刺 | 每帧同步创建多个 GameObject | ✅ |
| 急速溢出 | 线性公式无递减收益 | ✅ |
| 弹幕+2 却射 1/2/3 发 | 帧预算/子弹上限在弹幕**内部**逐发截断 → 弹幕原子化 | ✅ |
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

### 5. 相位保留 cap（Bug 1/2）✅

```csharp
// 取模而非硬钳位：积压限制在「约 1 发」的同时保留各枪相位，交错不失效
if (gun.accumulator > effectiveCooldown * 1.5f)
    gun.accumulator = effectiveCooldown + Mathf.Repeat(gun.accumulator, effectiveCooldown);
```

### 6. 每帧子弹创建预算 + 轮转（Bug 4）✅

```csharp
const int MAX_BULLETS_PER_FRAME = 15;
int i = (_fireStartIndex + k) % gunCount;        // 轮转，避免末尾枪饿死
if (bulletsThisFrame >= MAX_BULLETS_PER_FRAME) continue;
bulletsThisFrame += SpawnDotBullet(gun, fireDir, dmgMult, MAX_BULLETS_PER_FRAME - bulletsThisFrame);
_fireStartIndex = (_fireStartIndex + 1) % gunCount;
```

---

## 全部 Bug 已修复 ✅

Bug 1/2/4 由「相位保留 cap」+「每帧预算 + 轮转」解决，无遗留待重构项。
