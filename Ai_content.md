# 🧠 AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 窗口先读此文件。最后更新：2026-06-14

## 1. 项目概述
- **引擎**：Unity 6 (URP) | **语言**：C# | **类型**：2D 俯视角射击生存
- **场景**：`MenuScene`(MenuSceneBootstrap) → `GameScene`(GameSceneBootstrap)
- **仓库**：https://github.com/wei470/vampire_game_v2.git
- **当前角色系统**：8 种角色（warrior/mage/necromancer/berserker/ranger/paladin/vampire/assassin），全部使用 MagePassive
- **Mage 为当前主角**，DOT 子弹系统是 Mage 专属机制
- **已删除**：技能系统、难度选择系统、弹速升级、痛苦(DotFrequency)、凋零(DotCritBurst)

## 2. 核心架构

| 系统 | 文件 | 说明 |
|------|------|------|
| `Damageable` | `Entities/Damageable.cs` | 伤害组件（float伤害/护甲减伤/HP回复） |
| `EnemyBase` | `Enemies/EnemyBase.cs` | 敌人基类（AllAlive静态列表/缓存组件） |
| `MagePassive` | `Player/MagePassive.cs` | Mage专属（DOT枪管理+属性，partial class） |
| `MagePassive.Firing` | `Player/MagePassive.Firing.cs` | Update+子弹发射+视觉（DotGunState class，nextAllowedFireTime） |
| `GameReferences` | `Core/Managers/GameReferences.cs` | 全局引用缓存（Player/DetonateSystem等） |
| `AdminConfig` | `ScriptableObjects/Config/AdminConfig.cs` | 管理员配置（admin=0只显示Start，admin=1显示全部） |
| `PlayerConfig` | `ScriptableObjects/Config/PlayerConfig.cs` | 角色配置（按characterId设置移速） |
| `BGMManager` | `Audio/BGMManager.cs` | 背景音乐（PlayClip方法，DontDestroyOnLoad） |
| `MapBoundary` | `Map/MapBoundary.cs` | 屏幕边界空气墙（跟随摄像机，FixedUpdate+LateUpdate钳制） |

## 3. 护甲系统

```
减伤% = min(护甲 × 2%, 90%)
实际伤害 = RoundToInt(原始伤害 × (1 - 减伤%))
```

- 每波敌人 +1 护甲（EnemyScalingHelper）
- 腐蚀：护甲 = Floor(护甲 × 90%)，最多8层
- 侵蚀：护甲 = 护甲 - 侵蚀层数（无限叠加）
- 计算顺序：先腐蚀 → 再侵蚀 → 结算伤害

## 4. DOT 子弹系统（Mage 专属，7种）

| 类型 | 类名 | 池键 | 特点 |
|------|------|------|------|
| 中毒 | PoisonBullet : DotBulletBase | DOT_POISON_BULLET | 命中留毒液池，固定1s tick |
| 燃烧 | BurnBullet : DotBulletBase | DOT_BURN_BULLET | 叠加燃烧层数，固定0.5s tick，层数越高伤害越高 |
| 霜冻 | FrostBullet : DotBulletBase | DOT_FROST_BULLET | 永久减速+叠层 |
| 雷电 | LightningBullet | DOT_LIGHTNING_BULLET | 连锁3敌人，叠静电层 |
| 黑暗 | DarkBullet | — | 永久标记，死亡时DOT传播 |
| 光明 | LightBulletController | — | 蓄力激光扫射 |
| 风 | WindBullet : DotBulletBase | DOT_WIND_BULLET | 高速单发0.25s，随机±25°偏射 |

**DOT tick 公式**：
- 燃烧：`dmg = baseDps × 0.5 × (1 + (stacks-1) × 10%)`，间隔固定0.5s
- 毒素：`dmg = PoisonDamagePerTick + (stacks-1)`，间隔固定1.0s

**元素反应**：
- 燃烧×风化→燃烧扩散 | 霜冻×静电→冰场 | 霜冻×燃烧→融化(DOT×2)

## 5. 引爆系统（E键）

```
引爆伤害 = DPS × 5 × detonateMultiplier（上限800）
detonateMultiplier = 3.0 × 1.15^辐射层数（最多10层）
引爆冷却 = 12s × max(0.1, 1 - 污染层数×0.1)（最多6层）
```

- 蓄力 → 冲击波扩展 → 接触敌人触发引爆 → 时停 → 显示总伤害
- 连锁引爆：10 × multiplier × chainRatio，每次衰减50%
- 霜爆：50 × multiplier（上限800）
- 末日审判：3种DOT + HP≤阈值 → 直接击杀

## 6. 升级系统（MageUpgradeConfig）

**DOT 增强**：
- 腐蚀(Corrosion)：护甲×90%，最多8层
- 侵蚀(Erosion)：无视1点护甲/层，无限叠加

**引爆增强**：
- 辐射(Radiation)：引爆伤害×115%/层，最多10层
- 污染(Contaminate)：引爆冷却-10%/层，最多6层

**子弹增强**：急速(攻速+15%) / 弹幕(子弹+1，上限3) / 贯穿弹(穿透+1)

**协同强化**：光明审判 / 静电领域 / 霜爆 / 末日审判 / 暗影标记

**一般强化**：移速+10%（全角色通用）

## 7. 射击系统（MagePassive.Firing.cs）

```csharp
// DotGunState 是 class（非struct），无写回
float effectiveCooldown = Mathf.Max(0.01f, gun.cooldown * attackSpeedMult);
if (hasTarget && Time.time >= gun.nextAllowedFireTime)
{
    gun.nextAllowedFireTime = Time.time + effectiveCooldown;
    SpawnDotBullet(gun, fireDir, dmgMult);
}
```

- 弹幕上限 3 发（`Mathf.Min(1 + _bulletCountBonus, 3)`）
- 散射角度从 `DotEffectConfig.BarrageSpreadAngle` 读取
- 攻速变化时按比例缩放 nextAllowedFireTime
- 帧率雪崩保护：`deltaTime > 0.05f` 跳过射击
- Glow 对象池化（`_glowPool`）

## 8. 敌人系统

- 14种 + 4种Boss变体
- 每波护甲+1（EnemyScalingHelper）
- 精英词缀：EliteModifierSystem，第5波起概率生成
- `EnemyBase.AllAlive` 静态列表（替代 FindObjectsByType）
- SpawnManager.StartFromWave(int wave) 支持指定波次开始

## 9. 空气墙系统（MapBoundary）

- 4面 BoxCollider2D + Static Rigidbody2D，Environment层
- 跟随摄像机（FixedUpdate + LateUpdate 更新位置）
- margin = -1（屏幕内侧 1 单位）
- 双重钳制：FixedUpdate + LateUpdate 强制所有实体在屏幕内
- 敌人刷新位置钳制在屏幕内（距边缘 2 单位）

## 10. BGM 系统

- 主菜单：`BGMManager.Instance.PlayClip("music1")`（music1.ogg）
- 游戏内：`BGMManager.Instance.PlayClip("music2")`（music2.ogg）
- BGMManager 使用 DontDestroyOnLoad 跨场景
- 音频文件在 `Assets/Resources/Audio/`（.ogg 格式）

## 11. AdminConfig 管理员模式

- `admin=0`：主菜单只显示 Start Game
- `admin=1`：显示全部按钮（Shop/Test/BossTest/Daily）和快捷键（T/B/G/D）
- 菜单 `Mage → Create AdminConfig Asset` 创建配置

## 12. PlayerConfig 角色配置

- `GetMoveSpeed(characterId)` 按角色查询移速
- 默认 mage 移速 10f（原20f的50%）
- 菜单 `Mage → Create PlayerConfig Asset` 创建配置

## 13. Test 模式

- 主菜单按 T 进入 Test 模式
- 三个标签页：子弹 / 一般强化 / 专属强化
- 底部可选择起始波次（默认1）
- DPS 测试模式按 G，木桩 200 万 HP，Static 刚体

## 14. 关键 Bug 注意

**返回菜单卡死**：禁止 `DestroyImmediate`，禁止 `OnGUI` 内 `LoadScene`

**DOT 效果**：命中必须调 `DotBulletHelper.EnsureStatusEffectManager()`

**GameReferences.Player**：GameStarter 构造函数中赋值

**腐蚀计算**：用 `FloorToInt`（非 RoundToInt），否则 1 护甲无效

**护甲减伤**：用 `RoundToInt`（非 CeilToInt），避免浮点误差

## 15. 测试规范

| 文件 | 说明 |
|------|------|
| `Tests/Editor/CoreSystemTests.cs` | EditMode 单元测试（纯计算） |
| `Tests/Editor/AutomatedPlayModeTests.cs` | PlayMode 集成测试（GameObject 交互） |

当前测试数量：CoreSystemTests 42 个 + AutomatedPlayModeTests 68 个 = 110 个

## 16. 关键文件索引

| 文件 | 说明 |
|------|------|
| `Player/MagePassive.cs` | DOT枪管理+属性 |
| `Player/MagePassive.Firing.cs` | Update+子弹发射 |
| `Player/DetonateSystem.cs` | 引爆系统 |
| `Player/MageUpgradeApplier.cs` | 升级应用（策略字典） |
| `Combat/DotBulletFactory.cs` | DOT子弹工厂 |
| `Combat/DotBulletBase.cs` | DOT子弹基类 |
| `Combat/StatusEffects/StatusEffectSystem.cs` | DOT管理+引爆 |
| `Combat/DotBulletHelpers.cs` | EnsureStatusEffectManager + 腐蚀/侵蚀计算 |
| `Combat/BurnStackEffect.cs` | 燃烧叠加（固定0.5s tick） |
| `Combat/PoisonStackEffect.cs` | 毒素叠加（固定1.0s tick） |
| `Combat/WindBullet.cs` | 风子弹（继承DotBulletBase） |
| `Enemies/SpawnManager.cs` | 波次管理+StartFromWave |
| `Enemies/EnemyBase.cs` | 敌人基类+AllAlive列表 |
| `Map/MapBoundary.cs` | 屏幕空气墙 |
| `ScriptableObjects/Config/MageUpgradeConfig.cs` | 升级配置 |
| `ScriptableObjects/Config/DotEffectConfig.cs` | DOT效果配置 |
| `ScriptableObjects/Config/AdminConfig.cs` | 管理员配置 |
| `ScriptableObjects/Config/PlayerConfig.cs` | 角色配置 |
