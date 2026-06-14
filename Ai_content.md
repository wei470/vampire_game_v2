# AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 对话先读此文件。
> 最后更新：2026-06-14 | 218 个 CS 文件 | 178 个测试

---

## 1. 项目概况

| 项 | 值 |
|----|-----|
| 引擎 | Unity 6 (URP) |
| 语言 | C# |
| 类型 | 2D 俯视角射击生存 |
| 场景 | `MenuScene` → `GameScene` |
| 角色 | Mage（DOT 法师）、Blue（蓝色战士） |
| 测试 | CoreSystemTests 91 + AutomatedPlayModeTests 87 = 178 |

---

## 2. 核心文件索引

### 角色系统

| 文件 | 职责 |
|------|------|
| `Player/ICharacterPassive.cs` | `ICharacterPassive`（通用）+ `IDotCharacterPassive`（DOT 子接口） |
| `Player/CharacterPassiveBase.cs` | 角色基类：攻速/弹数/穿透/进化属性 + `GetFireDirection()` |
| `Player/MagePassive.cs` | Mage 专属：DOT 枪管理 + 属性，继承 CharacterPassiveBase，实现 IDotCharacterPassive |
| `Player/MagePassive.Firing.cs` | Mage 射击逻辑：Update + SpawnDotBullet + 视觉 |
| `Player/BlueCharacterPassive.cs` | 蓝色角色：继承 CharacterPassiveBase，只用 SimpleBullet |
| `Player/CharacterFactory.cs` | 角色工厂：`Register("xxx", go => go.AddComponent<XXX>())` |
| `Player/DetonateSystem.cs` | 引爆系统：蓄力/连锁/余烬/霜爆/末日审判 |
| `Player/MageUpgradeApplier.cs` | Mage 升级应用：策略字典模式 |
| `Player/EvolutionSystem.cs` | 进化系统：按等级解锁被动里程碑 |

### 子弹系统

| 文件 | 职责 |
|------|------|
| `Combat/ProjectileBase.cs` | 通用子弹基类：速度/方向/生命周期/穿透/反弹 |
| `Combat/DotBulletBase.cs` | DOT 子弹基类：继承 ProjectileBase，命中调 `DotBulletHelper.EnsureStatusEffectManager()` |
| `Combat/SimpleBullet.cs` | 蓝色子弹：继承 ProjectileBase，命中直接扣血 |
| `Combat/DotBulletFactory.cs` | DOT 子弹工厂：7 种 DOT 子弹创建 |
| `Combat/IGunState.cs` | `IGunState` 接口 + `GunState` + `DotGunState` |
| `Combat/DotBulletHelpers.cs` | `DotBulletHelper`（EnsureStatusEffectManager + 腐蚀/侵蚀）+ `PenetrateHandler` + `CritParams` |

### DOT 子弹类型（7 种，Mage 专属）

| 类型 | 类名 | 特点 |
|------|------|------|
| 中毒 | `PoisonBullet` | 命中留毒液池，固定 1s tick |
| 燃烧 | `BurnBullet` | 叠加层数，固定 0.5s tick |
| 霜冻 | `FrostBullet` | 永久减速 + 叠层 |
| 雷电 | `LightningBullet` | 连锁 3 敌人，叠静电层 |
| 黑暗 | `DarkBullet` | 永久标记，死亡时 DOT 传播 |
| 光明 | `LightBulletController` | 蓄力激光扫射 |
| 风 | `WindBullet` | 高速 0.2s，随机 ±25° 偏射 |

### 状态效果系统

| 文件 | 职责 |
|------|------|
| `Combat/StatusEffects/StatusEffectSystem.cs` | StatusEffectManager + StatusEffectType 枚举 + StatusEffect 类 + DetonateResult + DotComboSystem |
| `Combat/StatusEffects/CurseSpreadSystem.cs` | 诅咒传播：敌人死亡时 DOT 扩散 |
| `Combat/StatusEffects/DotVisualEffectManager.cs` | DOT 视觉效果管理 |

### 配置系统

| 文件 | 职责 |
|------|------|
| `ScriptableObjects/Config/CharacterUpgradeConfig.cs` | 配置基类 + `ICharacterConfig` 接口 + `DotGunEntry`/`UpgradeEntry` 结构体 + `CharacterConfigLoader` |
| `ScriptableObjects/Config/MageUpgradeConfig.cs` | Mage 升级配置：7 DOT 枪 + 10 增强 |
| `ScriptableObjects/Config/BlueUpgradeConfig.cs` | 蓝色角色升级配置 |
| `ScriptableObjects/Config/DotEffectConfig.cs` | DOT 效果配置：所有数值参数（595 行） |
| `ScriptableObjects/Config/PlayerConfig.cs` | 角色移速配置 |
| `ScriptableObjects/Config/AdminConfig.cs` | 管理员模式配置 |

### 核心管理器

| 文件 | 职责 |
|------|------|
| `Core/Managers/GameReferences.cs` | 全局引用缓存：`Player`/`CharacterPassive`/`DotCharacterPassive`/`DetonateSystem`/`LevelSystem`/`WeaponCtrl`/`PlayerDamageable` |
| `Core/Managers/GameStateResetter.cs` | 场景重置：`FullReset()` 清理所有静态状态 + 单例销毁 |
| `Core/Bootstrap/GameStarter.cs` | 游戏启动：加载配置 → 创建角色 → 开始波次 |
| `Core/Bootstrap/GameSceneBootstrap.cs` | GameScene 引导 |

### VFX / UI 工具

| 文件 | 职责 |
|------|------|
| `Combat/VFXUtils.cs` | `MaterialCache` + `VFXPool` + `GlowReturnHelper` + `DotSpriteCache` + `DotBulletVisualEffects` + VFX MonoBehaviour 组件 |
| `Combat/TextTickers.cs` | `FloatingText`：统一浮动文字组件 |
| `UI/UIUtils.cs` | `UIFormatUtils` + `UIFontProvider` + `GUIScaleHelper` |

---

## 3. 关键公式

### 护甲系统

```
减伤% = min(护甲 × 2%, 90%)
实际伤害 = RoundToInt(原始伤害 × (1 - 减伤%))
```

- 每波敌人 +1 护甲（EnemyScalingHelper）
- 腐蚀：`FloorToInt(护甲 × 0.9)`，最多 8 层
- 侵蚀：`护甲 - 层数`，无限叠加
- 计算顺序：先腐蚀 → 再侵蚀 → 结算伤害

### DOT tick

```
燃烧：dmg = baseDps × 0.5 × (1 + (stacks-1) × 10%)，间隔 0.5s
毒素：dmg = PoisonDamagePerTick + (stacks-1)，间隔 1.0s
```

### 引爆（E 键）

```
引爆伤害 = DPS × 5 × multiplier（上限 800）
multiplier = 3.0 × 1.15^辐射层数（最多 10 层）
冷却 = 12s × max(0.1, 1 - 污染层数 × 0.1)（最多 6 层）
```

---

## 4. 关键 Bug 与注意事项

| 问题 | 说明 |
|------|------|
| **返回菜单卡死** | 禁止 `DestroyImmediate`，禁止 `OnGUI` 内 `LoadScene` |
| **DOT 命中** | 必须调 `DotBulletHelper.EnsureStatusEffectManager()` |
| **腐蚀计算** | 用 `FloorToInt`（非 RoundToInt），否则 1 护甲无效 |
| **护甲减伤** | 用 `RoundToInt`（非 CeilToInt），避免浮点误差 |
| **场景重置** | 所有静态列表必须在 `GameStateResetter.FullReset()` 中清理 |
| **MagePassive.Awake** | 自动创建 DetonateSystem + 解锁 Poison DOT 枪 |
| **GameReferences** | Player 赋值时自动失效所有缓存，使用 `CharacterPassive` 而非 `MagePassive` |

---

## 5. 新角色开发流程

```
1. 创建 XXXCharacterPassive : CharacterPassiveBase
     - 实现 CharacterId / DisplayName
     - 实现 Update() 中的射击逻辑
     - 重写 ApplyUpgrade() 处理升级

2. 创建 XXXUpgradeConfig : CharacterUpgradeConfig
     - 在 OnEnable() 中设置 characterId / upgradeEntries

3. 复用 SimpleBullet 或创建 XXXBullet : ProjectileBase
     - 重写 OnHitEnemy() 定义命中效果

4. CharacterFactory.Register("xxx", go => go.AddComponent<XXXCharacterPassive>())

5. 创建 XXXCharacterData.asset（Unity Editor 中）
```

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
dot?.CorrosionArmorReduction;
```

### VFX 工具

```csharp
Material mat = MaterialCache.GetDefault();          // 缓存的 Sprites/Default Material
var go = VFXPool.Get("CurseLine");                  // 从池获取 VFX
VFXPool.Return(go, 0.5f);                          // 延迟回收
var glow = GlowReturnHelper.GetOrCreate();          // Glow 对象池
Sprite s = DotSpriteCache.Get();                    // 椭圆 Sprite
Sprite c = DotSpriteCache.CircleSprite();           // 圆形 Sprite
```

### 测试

```csharp
// EditMode 测试（纯计算，无 GameObject）
Tests/Editor/CoreSystemTests.cs — 91 个

// PlayMode 测试（GameObject 交互）
Tests/Editor/AutomatedPlayModeTests.cs — 87 个
```
