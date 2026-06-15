# 高急速 + 高弹幕 Bug 盘点

> 创建时间：2026-06-14

---

## 急速机制现状

```
attackSpeedMult = Max(0.2, 1.0 - attackSpeedBonus)
effectiveCooldown = Max(0.1, gun.cooldown × attackSpeedMult)
```

每层急速 +0.15，6 层后触底（attackSpeedMult = 0.2），之后不再加速。

| 急速层数 | attackSpeedMult | Poison(1.8s) | Burn(0.5s) | Wind(0.2s) |
|---------|-----------------|---------------|------------|------------|
| 0 | 1.00 | 1.80s | 0.50s | 0.20s |
| 3 | 0.55 | 0.99s | 0.28s | 0.11s |
| 6+ | 0.20 | 0.36s | **0.10s** | **0.10s** |

---

## Bug 列表

### Bug 1：交错偏移被 cap 吞掉，7 枪同步开火

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

### Bug 2：cap 导致不均匀射击节奏

cap = `effectiveCooldown × 1.5`。以 60FPS、effectiveCooldown=0.1 为例：

```
Frame 1: accumulator = 0.15(cap) → 开火 → 0.05
Frame 2: accumulator = 0.0667 → 不开火
Frame 3: accumulator = 0.0834 → 不开火
Frame 4: accumulator = 0.1001 → 开火 → 0.0001
Frame 5: accumulator = 0.0168 → 不开火
Frame 6: accumulator = 0.0335 → 不开火
Frame 7: accumulator = 0.0502 → 不开火
Frame 8: accumulator = 0.0669 → 不开火
Frame 9: accumulator = 0.0836 → 不开火
Frame 10: accumulator = 0.1003 → 开火 → 0.0003
```

节奏：开火、等 2 帧、开火、等 5 帧、开火、等 2 帧... **不均匀**。

### Bug 3：无子弹数量上限

7 枪 × 3 弹幕 = 每轮 21 发子弹。
effectiveCooldown = 0.1s → 每秒 10 轮 → **每秒 210 发子弹**。
子弹 lifetime = 4s → 场上同时存在 **840 发子弹**。
每个子弹有 TrailRenderer + SpriteRenderer + Collider + Glow 子对象。

### Bug 4：每帧子弹创建无节流

每帧最多 7 枪开火 × 3 弹幕 = 21 个 `DotBulletFactory.Create()` 调用。
每个调用涉及：
- 对象池 Spawn 或 new GameObject
- TrailRenderer/SpriteRenderer 配置
- Glow 子对象创建（upgrade ≥ 3）
- Collider 缩放

21 个 GameObject 创建/激活 = 帧率尖刺。

### Bug 7：弹幕数量不稳定（1/2/3 随机）

`SpawnDotBullet` 创建 `bulletCount` 个子弹，但：
1. `DotBulletFactory.Create` 返回 null 时静默跳过（不报错）
2. 子弹在敌人碰撞体内生成 → `OnTriggerEnter2D` 同帧触发 → 立即回收
3. 对象池耗尽时 fallback 创建的子弹可能缺少组件

```csharp
// 当前代码：创建失败静默跳过
GameObject bullet = DotBulletFactory.Create(...);
ApplyBulletSizeBonus(bullet);   // null 时跳过
ApplyUpgradeVisual(bullet, gun); // null 时跳过
```

### Bug 8：弹幕上限 3 但代码不统一

```csharp
int bulletCount = Mathf.Min(1 + _bulletCountBonus, 3);  // SpawnDotBullet
int bulletsPerShot = Mathf.Min(1 + _bulletCountBonus, 3); // BlueCharacterPassive
```

两处重复计算，如果只改一处会不一致。

### Bug 6：急速 6 层后完全无效

```csharp
attackSpeedMult = Max(0.2, 1.0 - attackSpeedBonus)
```

6 层 × 0.15 = 0.9 → Max(0.2, 0.1) = 0.2。
30 层 × 0.15 = 4.5 → Max(0.2, -3.5) = 0.2。
**24 层急速完全浪费**。

---

## 根因总结

| 问题 | 根因 |
|------|------|
| 7 枪同步开火 | 交错初始化被 cap 吞掉 + 二轮起 accumulator 重置到同值 |
| 射击节奏不均匀 | cap 在开火前强制钳位，破坏自然累加 |
| 子弹数量爆炸 | 无子弹上限 + 无每秒开火上限 + 无场上子弹上限 |
| 帧率尖刺 | 每帧同步创建 21 个 GameObject |
| 急速溢出 | 线性公式无递减收益，6 层后无效 |

---

## 重构方案

### 1. 移除累加器 cap，改用子弹数上限

```csharp
// 旧：cap accumulator（破坏节奏）
if (gun.accumulator > effectiveCooldown * 1.5f)
    gun.accumulator = effectiveCooldown * 1.5f;

// 新：不限制 accumulator，限制每帧总子弹数
const int MAX_BULLETS_PER_FRAME = 12;
int bulletsThisFrame = 0;
// ...
if (bulletsThisFrame + bulletsPerShot <= MAX_BULLETS_PER_FRAME)
{
    bulletsThisFrame += bulletsPerShot;
    // 开火
}
```

### 2. 交错用相位偏移而非累加器初始值

```csharp
// 旧：accumulator 初始化（被 cap 吞掉）
accumulator = index * cooldown / (index + 1)

// 新：每枪有固定的相位偏移，不被 cap 影响
float phaseOffset = (float)gunIndex / totalGuns * effectiveCooldown;
// 开火条件：accumulator >= effectiveCooldown + phaseOffset
```

### 3. 急速改为递减收益

```csharp
// 旧：线性，6 层触底
attackSpeedMult = Max(0.2, 1.0 - bonus)

// 新：对数递减，每层都有收益
attackSpeedMult = Max(0.2, 1.0 / (1.0 + bonus))
// 1层=0.87, 3层=0.71, 6层=0.57, 10层=0.47, 30层=0.24
```

### 4. 弹幕上限改为 5

```csharp
int bulletCount = Mathf.Min(1 + _bulletCountBonus, 5);
```

### 5. 弹幕数量稳定性

```csharp
// 旧：创建失败静默跳过
GameObject bullet = DotBulletFactory.Create(...);

// 新：创建失败时重试一次，仍失败则跳过并日志
GameObject bullet = DotBulletFactory.Create(...);
if (bullet == null) bullet = DotBulletFactory.Create(...); // 重试
if (bullet == null) { Debug.LogWarning("Bullet create failed"); continue; }
```
