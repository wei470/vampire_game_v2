# AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 对话先读此文件。
> 最后更新：2026-06-17 | ~220 个 CS 文件 | ~250 个测试 | 护甲/腐蚀/侵蚀/装备系统已彻底删除

---

## 1. 项目概况

| 项 | 值 |
|----|-----|
| 引擎 | Unity 6 (URP) |
| 语言 | C# |
| 类型 | 2D 俯视角射击生存 |
| 场景 | `MenuScene` → `GameScene` |
| 角色 | Mage（DOT 法师）、Blue（蓝色战士） |
| 测试 | CoreSystemTests ~145 + AutomatedPlayModeTests ~85 + FiringSystemTests ~19 = ~250 |

---

## 2. 核心文件索引

### 角色系统

| 文件 | 职责 |
|------|------|
| `Player/ICharacterPassive.cs` | `ICharacterPassive`（通用）+ `IDotCharacterPassive`（DOT 子接口，含 WildWind） |
| `Player/CharacterPassiveBase.cs` | 角色基类：攻速/弹数/穿透/进化属性 + `GetFireDirection()` |
| `Player/MagePassive.cs` | Mage 专属：DOT 枪管理 + 全部升级字段（~390 行） |
| `Player/MagePassive.Firing.cs` | Mage 射击逻辑：累加器 + 雷暴双发 + 暴风三连 + 冰刃散弹 + 焚天火箭 |
| `Player/BlueCharacterPassive.cs` | 蓝色角色：继承 CharacterPassiveBase，只用 SimpleBullet |
| `Player/CharacterFactory.cs` | 角色工厂：`Register("xxx", go => go.AddComponent<XXX>())` |
| `Player/DetonateSystem.cs` | 引爆系统主文件（蓄力/核心引爆/狂风击退） |
| `Player/DetonateSystem.Chain.cs` | 连锁引爆 + 余烬 + 霜爆 |
| `Player/DetonateSystem.Charge.cs` | 蓄力引爆逻辑 |
| `Player/MageUpgradeApplier.cs` | Mage 升级应用：策略字典模式（~300 行） |
| `Player/EvolutionSystem.cs` | 进化系统：按等级解锁被动里程碑（已移除 ArmorBonus） |

### 子弹系统

| 文件 | 职责 |
|------|------|
| `Combat/ProjectileBase.cs` | 通用子弹基类：速度/方向/生命周期/穿透/反弹 + `GetDirection()` + `AddLifetime()` |
| `Combat/DotBulletBase.cs` | DOT 子弹基类：继承 ProjectileBase，命中调 `DotBulletHelper.EnsureStatusEffectManager()` |
| `Combat/SimpleBullet.cs` | 蓝色子弹：继承 ProjectileBase，命中直接扣血 |
| `Combat/DotBulletFactory.cs` | DOT 子弹工厂：7 种 DOT 子弹创建 + 龙卷风/速风替换逻辑 |
| `Combat/IGunState.cs` | `IGunState` 接口 + `GunState` + `DotGunState`（含 accumulator） |
| `Combat/DotBulletHelpers.cs` | `DotBulletHelper`（EnsureStatusEffectManager + ParalysisActive 同步）+ `PenetrateHandler` + `CritParams` |
| `Combat/BurnRocketBullet.cs` | **焚天**跟踪小火箭：追踪最近敌人，命中叠1层燃烧 |
| `Combat/HailBullet.cs` | **下雪天**冰雹：从屏幕顶部落下，命中叠2层霜冻 |
| `Combat/IceBladeBullet.cs` | **冰刃**散弹：速度2倍，命中叠1层霜冻 |
| `Combat/WallBounceHandler.cs` | **冷弹**墙壁反弹组件：支持最大反弹次数 + 存在时间加成 |
| `Combat/EnemySpeedBar.cs` | 敌人脚下移速百分比显示（TextMesh，减速时可见） |
| `Combat/SnowyDaySystem.cs` | **下雪天**系统：场地变蓝 + 每1秒落冰雹 |

### DOT 子弹类型（7 种，Mage 专属）

| 类型 | 类名 | 特点 |
|------|------|------|
| 中毒 | `PoisonBullet` | 命中留毒液池，固定 1s tick |
| 燃烧 | `BurnBullet` | 叠加层数，固定 0.5s tick |
| 霜冻 | `FrostBullet` | 永久减速 + 叠层（每层5%，最高50%，上限10层） |
| 雷电 | `LightningBullet` | 连锁 3 敌人，叠静电层 |
| 黑暗 | `DarkBullet` | 永久标记，死亡时 DOT 传播 |
| 光明 | `LightBulletController` | 蓄力激光扫射 |
| 风 | `WindBullet` | 高速 0.2s，随机 ±25° 偏射 |

### 状态效果系统

| 文件 | 职责 |
|------|------|
| `Combat/StatusEffects/StatusEffectSystem.cs` | StatusEffectManager + StatusEffectType 枚举（已移除 Corrosion/Erosion）+ StatusEffect 类 + DetonateResult + DotComboSystem + ParalysisActive |
| `Combat/StatusEffects/CurseSpreadSystem.cs` | 诅咒传播：敌人死亡时 DOT 扩散 |
| `Combat/StatusEffects/DotVisualEffectManager.cs` | DOT 视觉效果管理 |
| `Combat/FrostEffect.cs` | 霜冻效果：减速 + DOT（冷酷之拥） + 最大层数 + EnemySpeedBar 挂载 |
| `Combat/WindErosionEffect.cs` | 风化效果：叠层 + 击退（含 KnockbackMultiplier 龙卷风加成） |
| `Combat/WindBullet.cs` | 风子弹：含 IsTornado 标志（龙卷风击退×1.5） |

### 配置系统

| 文件 | 职责 |
|------|------|
| `ScriptableObjects/Config/CharacterUpgradeConfig.cs` | 配置基类 + `ICharacterConfig` 接口 + `DotGunEntry`/`UpgradeEntry` 结构体 + `CharacterConfigLoader`（已移除 ArmorBonus/DotTrigger） |
| `ScriptableObjects/Config/MageUpgradeConfig.cs` | Mage 升级配置：7 DOT 枪 + ~30 增强（含全部新升级） |
| `ScriptableObjects/Config/BlueUpgradeConfig.cs` | 蓝色角色升级配置 |
| `ScriptableObjects/Config/DotEffectConfig.cs` | DOT 效果配置：所有数值参数（已移除 armor 相关，FrostBaseSlowPct=0.05, FrostMaxSlow=0.5, FrostMaxStacks=10） |
| `ScriptableObjects/Config/GameConfig.cs` | 游戏配置（已移除 basePlayerArmor/dpsDummyArmor/equipment 字段） |
| `ScriptableObjects/Config/PlayerConfig.cs` | 角色移速配置 |
| `ScriptableObjects/Config/AdminConfig.cs` | 管理员模式配置 |

### 核心管理器

| 文件 | 职责 |
|------|------|
| `Core/Managers/GameReferences.cs` | 全局引用缓存：`Player`/`CharacterPassive`/`DotCharacterPassive`/`DetonateSystem`/`LevelSystem`/`WeaponCtrl`/`PlayerDamageable` |
| `Core/Managers/GameStateResetter.cs` | 场景重置：`FullReset()` 清理所有静态状态 + 单例销毁（已移除 Backpack.Clear） |
| `Core/Managers/PermanentUpgradeStore.cs` | 永久升级商店（已移除 Armor+1 和 armor 里程碑） |
| `Core/Managers/SpriteFactory.cs` | 程序化 Sprite 生成：Square/Circle/Triangle/Diamond/Star 等 |
| `Core/Utils/PoolHelper.cs` | 对象池键常量（含 BURN_ROCKET/HAIL_BULLET/ICE_BLADE_BULLET） |
| `Core/Bootstrap/GameStarter.cs` | 游戏启动：加载配置 → 创建角色 → 开始波次（已移除 armor 初始化） |
| `Core/Bootstrap/GameSceneBootstrap.cs` | GameScene 引导（已移除 dpsDummyArmor） |

### VFX / UI 工具

| 文件 | 职责 |
|------|------|
| `Combat/VFXUtils.cs` | `MaterialCache` + `VFXPool` + `GlowReturnHelper` + `DotSpriteCache` + `DotBulletVisualEffects` + VFX MonoBehaviour 组件 |
| `Combat/TextTickers.cs` | `FloatingText`：统一浮动文字组件 |
| `Combat/CombatManager.cs` | `CreateExplosionEffect` + `ExplosionVFX`（已修复 alpha 初始值） |
| `UI/UIUtils.cs` | `UIFormatUtils` + `UIFontProvider` + `GUIScaleHelper` |

---

## 3. 关键公式

### 急速系统（对数递减）

```
attackSpeedMult = Max(0.2, 1.0 / (1.0 + attackSpeedBonus))
effectiveCooldown = Max(0.1, gun.cooldown × attackSpeedMult)
```

每层急速 +0.15，每层都有递减但持续的收益。

### 射击系统（累加器）

```
每帧：gun.accumulator += dt
上限：if (accumulator > effectiveCooldown × 1.5)
         accumulator = effectiveCooldown + Repeat(accumulator, effectiveCooldown)
开火：if (accumulator >= effectiveCooldown) { accumulator -= cooldown; fire(); }
```

- 每枪每帧最多 1 发（`if` 不是 `while`）
- 弹幕上限 `MAX_BARRAGE = 5`
- 场上子弹上限 `MAX_ACTIVE_BULLETS = 200`
- 每帧子弹预算 `MAX_BULLETS_PER_FRAME = 15`，以**整把枪**为粒度
- 轮转起始枪 `_fireStartIndex`：预算耗尽时各枪轮流优先开火

### 霜冻减速（加法叠加）

```
slowPercent = min(maxSlow, baseSlow + (stacks-1) × perStackSlow)
FrostSlowMultiplier = 1 - slowPercent
effectiveSpeed = BaseMoveSpeed × FrostSlowMultiplier × PoisonSwampMultiplier
```

- 默认：baseSlow=5%, perStackSlow=5%, maxSlow=50%, maxStacks=10
- 每层命中：100% → 95% → 90% → ... → 50%
- **冬天**：maxSlow 每层 +5%（敌人最低速 45%/40%/35%）
- **寒冬**：perStackSlow 每层 +1%
- 敌人脚下显示移速百分比（`EnemySpeedBar`）

### DOT tick

```
燃烧：dmg = baseDps × 0.5 × (1 + (stacks-1) × 10%)，间隔 0.5s
毒素：dmg = PoisonDamagePerTick + (stacks-1)，间隔 1.0s
霜冻DOT（冷酷之拥）：dmg = 4 × stacks，间隔 1.0s，参与引爆
```

### 引爆（E 键）

```
引爆伤害 = DPS × 5 × multiplier（上限 800）
multiplier = 3.0 × 1.15^辐射层数（最多 10 层）
冷却 = 12s × max(0.1, 1 - 污染层数 × 0.1)（最多 6 层）
```

- **冷酷之拥**：霜冻 DOT 伤害计入引爆（`EffectiveFrostDps × stacks × 5 × mult`）
- **瘫痪**：带雷电层的敌人 DOT 伤害 +10%
- **狂风**：引爆时击退范围内所有敌人（4 距离）

---

## 4. 关键 Bug 与注意事项

| 问题 | 说明 |
|------|------|
| **返回菜单** | `ReturnToMenu` 不调 `FullReset`，只做定向清理（避免 ObjectPool 级联销毁卡死） |
| **护甲系统已删除** | `Damageable` 无 `Armor`/`SetArmor`/`AddArmor`。`StatusEffectType` 已移除 `Corrosion`/`Erosion`。`CharacterData.armor`、`GameConfig.basePlayerArmor` 已删除。伤害直接穿透无减伤 |
| **DOT 命中** | 必须调 `DotBulletHelper.EnsureStatusEffectManager()` |
| **场景重置** | 所有静态列表在 `GameStateResetter.FullReset()` 中清理 |
| **子弹颜色** | `DotBulletBase.OnEnable` 重置 SpriteRenderer/TrailRenderer 颜色 + trail.material |
| **VFX 销毁** | 用 `TimedSelfDestruct`（unscaledTime），不用 `Object.Destroy(delay)` |
| **ExplosionVFX alpha** | `Setup()` 时读取 `_initialAlpha = _sr.color.a`，淡出从初始值开始（不再硬编码 1.0） |
| **DOT 引爆时 frostDOT** | `Detonate` 方法中 `hasFrostDot` 检查避免 `_activeEffects.Count == 0` 早返回跳过冰冻 DOT |

---

## 5. 全部升级一览

### 焚天（燃烧 🟢）
- 额外发射跟踪小火箭，命中叠1层燃烧，攻速为燃烧子弹的2倍
- 火箭：`BurnRocketBullet`，追踪 `_homingRadius=20`，速度 18，lifetime = 燃烧×50%

### 冰冻系列
| ID | 名称 | 稀有度 | 效果 |
|---|---|---|---|
| `frost_winter` | 冬天 | ⚪ | 霜冻减速上限+5%/层（敌人最低速 45%/40%/35%） |
| `frost_deep_winter` | 寒冬 | ⚪ | 冰冻子弹每层减速+1% |
| `frost_snowy_day` | 下雪天 | 🟣 | 场地变蓝 + 每1秒从屏幕顶部落冰雹，命中叠2层霜冻 |
| `frost_frozen_hands` | 冻手 | 🟢 | 被减速敌人射出的子弹弹速降低33% |
| `frost_ice_blade` | 冰刃 | 🟢 | 每射出5发霜冻子弹追加一波5发散弹冰弹，速度2倍，命中叠1层霜冻 |
| `frost_cold_bullet` | 冷弹 | 🟣 | 霜冻子弹和冰刃可通过墙壁反弹2次，子弹存在时间+5秒 |
| `frost_cold_embrace` | 冷酷之拥 | 🟢 | 冰冻增加DOT伤害（4×层数），参与引爆 |

### 雷电系列
| ID | 名称 | 稀有度 | 效果 |
|---|---|---|---|
| `storm_multi` | 雷暴 | 🟣 | 每发雷电子弹都变成双发，延迟0.1秒 |
| `storm_chain` | 静电爆炸 | 🟣 | 每射出5发雷电子弹，下一发命中后产生半个屏幕的爆炸，范围内所有敌人+1层雷电印记 |
| `paralysis` | 瘫痪 | 🟢 | 身上带有雷电层数的敌人受到的所有DOT伤害+10% |

### 风元素系列
| ID | 名称 | 稀有度 | 效果 |
|---|---|---|---|
| `wind_typhoon` | 台风 | 🟢 | 风子弹击退距离+20% |
| `wind_tornado` | 龙卷风 | 🟣 | 20%概率替换为龙卷风，速度30%，击退200% |
| `wind_wild` | 狂风 | 🟢 | DOT引爆时击退所有敌人 |
| `wind_storm` | 暴风 | 🟣 | 33%概率替换为三连发（延迟0.2秒） |
| `wind_swift` | 速风 | 🟢 | 30%概率子弹速度200%并穿透所有敌人 |

---

## 6. 代码规范

### 接口使用

```csharp
// 通用角色能力
ICharacterPassive passive = GameReferences.CharacterPassive;
passive.GetAttackSpeedMultiplier();
passive.ApplyUpgrade("haste");

// DOT 专属能力（仅 Mage 等 DOT 角色）
IDotCharacterPassive dot = GameReferences.DotCharacterPassive;
dot?.DotGuns.Count;
dot?.GetDotDamageMultiplier();
```

### 射击系统常量

```csharp
const int MAX_BARRAGE = 5;              // 弹幕上限
const int MAX_ACTIVE_BULLETS = 200;     // 场上子弹上限
float effectiveCooldown = Max(0.1, gun.cooldown * attackSpeedMult);
```

### VFX 工具

```csharp
Material mat = MaterialCache.GetDefault();
var go = VFXPool.Get("CurseLine");
VFXPool.ReturnImmediate(go);
var glow = GlowReturnHelper.GetOrCreate();
Sprite s = DotSpriteCache.Get();
Sprite c = DotSpriteCache.CircleSprite();
go.AddComponent<TimedSelfDestruct>().Setup(0.5f);
```

### 测试

```csharp
// EditMode 测试（纯计算，无 GameObject）
Tests/Editor/CoreSystemTests.cs — ~145 个

// PlayMode 测试（GameObject 交互）
Tests/Editor/AutomatedPlayModeTests.cs — ~85 个

// 射速系统测试（累加器模拟）
Tests/Editor/FiringSystemTests.cs — ~19 个
```

---

## 7. 元素反应

> 完整矩阵见仓库根目录 `matrix.md`。触发逻辑分散在各 DOT 子弹的命中/ tick 中。

已实现 9 个：毒爆、融化、天照、燃烧扩散、霜电冰场、紫电、球状闪电、静电扩散、暗影传播。

| 反应 | 组合 | 代码位置 |
|------|------|---------|
| 毒爆 | 中毒 × 黑暗 | `PoisonBullet.LeavePuddle()` |
| 融化 | 燃烧 × 霜冻 | `BurnBullet.TriggerMelt()` |
| 天照 | 燃烧 × 黑暗 | `BurnBullet.ApplyZoneBurnStacks()` + `AmaterasuEffect.cs` |
| 燃烧扩散 | 燃烧 × 风化 | `BurnBullet.TriggerBurnSpread()` |
| 霜电冰场 | 霜冻 × 雷电 | `FrostBullet.cs` + `FrostLightningField.cs`（持续时间1秒） |
| **紫电** | 雷电 × 黑暗 | `LightningBullet.OnTriggerEnter2D/EnterPurpleMode/HandlePurpleHit` |
| 球状闪电 | 风化 × 雷电 | `WindBullet.OnHitEnemy()` + `BallLightning.cs` |
| 静电扩散 | 雷电叠满层 | `StaticStackEffect.cs` |
| 暗影传播 | 黑暗标记死亡 | `DarkMarkEffect.cs` + `CurseSpreadSystem.cs` |

### 紫电（雷电 × 黑暗）

- 雷电子弹命中带黑暗标记（`DarkMarkEffect.IsActive`）的敌人 → 变黑紫、速度 ×3、无限贯穿
- 沿途每穿过一个敌人：+1 雷电层（触发静电）；**不叠加黑暗层**
- 血量 < 20% 直接处决：调 `BaseEntity.Die()`（走完整死亡管线 → 击杀奖励 + 暗影传播）
- 对象池注意：`_isPurple`/速度/生命/颜色/缩放必须在 `OnEnable` 重置

### 黑暗标记

- **层数固定上限 1 层**（`DarkMarkEffect.MAX_STACK`），多次命中不叠加
