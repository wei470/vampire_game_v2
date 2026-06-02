# 🧠 AI 项目速查手册 — Vampire Survivors Unity 移植版

> **用途**：每次开新 AI 窗口时，先读此文件，30 秒内掌握整个项目架构、修改规范和关键依赖关系。

---

## 1. 项目概述

- **引擎**：Unity 6 (URP 渲染管线)
- **语言**：C#，目标平台 2D 俯视角射击生存
- **玩法核心**：类 Vampire Survivors — 自动射击 + 被动移动 + 技能 + 波次敌人 + Boss 战 + 升级选择
- **场景结构**：
  - `MenuScene` → 主菜单（`MenuSceneBootstrap` 驱动）
  - `GameScene` → 游戏主场景（`GameSceneBootstrap` 驱动初始化）
- **Assembly 定义**：
  - `Gameplay.asmdef` — 所有游戏运行时代码
  - `Scripts.Editor.asmdef` — 编辑器扩展代码

---

## 2. 目录结构速览

```
Assets/Scripts/
├── Core/           ← 基础设施（单例、事件、对象池、引用、输入、存档）
├── Combat/         ← 武器、弹幕、伤害管理、DOT 子弹系统、状态效果
│   └── StatusEffects/ ← 统一状态效果管理器
├── Entities/       ← 可伤害实体、掉落物、经验/金币
├── Enemies/        ← 14 种敌人子类 + SpawnManager
├── Player/         ← 玩家控制、等级、技能管理、MagePassive
├── Skills/         ← 8 种主动技能 + 被动技能
├── UI/             ← 所有 UI 组件（含 EnemyHealthBar）
├── Map/            ← 地图主题、边界、装饰、环境区域
├── Audio/          ← BGM 管理
├── Data/           ← 配置加载器
├── ScriptableObjects/
│   ├── Config/     ← GameConfig, EnemyWaveConfig, MapThemeData
│   ├── Characters/ ← CharacterData, CharacterUpgradeData
│   └── Skills/     ← SkillData
└── Gameplay.asmdef
Assets/Editor/      ← 编辑器工具（场景创建器）
```

---

## 3. 核心架构（Core 模块）

### 3.1 单例基类
- **`Singleton<T>`** — MonoBehaviour 单例基类，`Awake()` 中设置 `Instance`
- 继承自它的：`GameManager`, `ObjectPool`, `CombatManager`, `ComboManager`, `SaveManager`, `OffScreenCuller`
- 应用退出时 `_applicationIsQuitting` 静默返回 null，不打印警告
- ⚠️ **使用 `DontDestroyOnLoad`，返回菜单时必须手动销毁所有单例**

### 3.2 全局引用缓存
- **`GameReferences`** — 静态类，缓存 `Player`, `MainCamera`, `SpawnManager`, `HUDManager` 等
- **严禁使用 `FindFirstObjectByType`**，所有全局引用必须通过 `GameReferences` 获取
- **返回菜单时必须调用 `GameReferences.Reset()` 清空所有引用**

### 3.3 事件系统
- **`EventManager`** — 纯静态类，基于 `System.Action` 委托
- 关键事件：
  - `OnEnemyKilled(Vector3, int xp, int coin)`
  - `OnXPGained(int)`, `OnLevelUp(int)`
  - `OnPlayerDeath`, `OnPlayerDamaged`, `OnPlayerHealed`
  - `OnWaveStart(int)`, `OnWaveComplete(int)`
  - `OnGameStateChanged(oldState, newState)`
  - `OnComboChanged(int)`
  - `OnSelectionComplete(CharacterData, WeaponData, SkillData)`
- 修改事件时必须同步修改 `EventManager.ClearAll()`

### 3.4 对象池
- **`ObjectPool`** — 单例，`WarmUp/Spawn/Despawn`
- **`PoolHelper`** — 静态工具类，封装常用池操作
  - `PoolHelper.SpawnOrInstantiate()` — 优先从池取，无池则 Instantiate
  - `PoolHelper.DespawnOrDestroy()` — 优先回池，无池则 Destroy
  - 14 种敌人池键常量 + 掉落物池
- **所有敌人、弹幕、掉落物都必须走对象池**，禁止直接 Instantiate/Destroy

### 3.5 对象池回收状态重置（关键）
- **`BaseEntity.OnEnable()`** — 重置 `_alive = true`
- **`Damageable.OnEnable()`** — 重置 `_currentHp = _maxHp` + `_dead = false`
- **`EnemyBase.OnEnable()`** — 调用 `base.OnEnable()` + 注册死亡事件 + 创建血条
- ⚠️ 对象池回收时只调用 `OnEnable()`，不调用 `Awake()`，所以所有状态重置必须在 `OnEnable()`

### 3.6 死亡流程（三层保障）
```
TakeDamage() → HP≤0 → _dead=true → Die() → BaseEntity.Die() → OnDeath事件
                                                                    ↓
                                              EnemyBase.OnEnemyDeathHandler → Destroy/Despawn

安全网 1: EnemyBase.FixedUpdate() 检测 !IsAlive && Alive → Die()
安全网 2: Damageable.LateUpdate() 检测 HP≤0 && !_dead → Die() → Destroy
```

### 3.7 输入系统
- **`GameInputHandler`** — 统一输入入口
  - WASD 移动、鼠标自动射击、ESC 暂停、Tab 商店
  - 鼠标世界坐标统一计算（`MouseWorldPosition` 属性）
  - **武器切换已删除** — 每局锁定初始选择的武器
- **键位布局**：
  | 键位 | 功能 |
  |------|------|
  | WASD | 移动 |
  | 鼠标 | 自动射击（主武器 + DOT 子弹） |
  | E | Mage 引爆（专属） |
  | F | 使用主动技能 |
  | Q | 切换技能 |
  | R | 重开 |
  | T | 跳波 |
  | Tab | 商店 |
  | ESC | 暂停 |

### 3.8 游戏状态机
- **`GameManager`** — 状态：`Menu → Playing → Paused → GameOver`
  - `ChangeState(newState)` → 先 `OnExitState(old)` 再 `OnEnterState(new)`
  - `PauseGame()` / `ResumeGame()` 控制 `Time.timeScale`

### 3.9 存档系统
- **`SaveManager`** — 单例，JSON 序列化
- **`ConfigLoader`** — 运行时加载 `GameConfig` + `EnemyWaveConfig`（Asset 缺失时静默创建默认）

---

## 4. 武器系统（已简化）

### 4.1 武器分配逻辑
- **所有角色**默认获得 Bullet（基础子弹武器）
- **Mage 角色**不使用 WeaponController，攻击完全由 MagePassive 的 DOT 枪系统驱动
- 选择流程已简化为 2 步：选择角色 → 选择技能（不再选择武器）

### 4.2 武器数据（运行时创建）
- 8 种 WeaponData 仍保留在代码中用于非 Mage 角色
- Mage 的 WeaponController 在游戏开始时被禁用（`wc.enabled = false`）

---

## 5. DOT 子弹系统（Mage 专属）

### 5.1 子弹类型
| 类型 | 类名 | 颜色 | 特点 |
|------|------|------|------|
| 毒子弹 | `PoisonBullet` | 🟢绿色 | 无限距离，命中留毒液池(2秒) |
| 流血 | `BleedBullet` | 🔴红色 | 命中附加流血，移动时受伤 |
| 燃烧 | `BurnBullet` | 🟠橙色 | 快速圆形子弹(12f)，叠加燃烧层数 |
| 霜冻 | `FrostBullet` | 🔵冰蓝 | 快速子弹(20f)，冰冻+永久减速 |
| 中毒药瓶 | `PoisonPotion` | 🟢药瓶 | 投掷药瓶，爆炸生成毒液池 |

### 5.2 中毒叠加效果（PoisonStackEffect）
- **每tick伤害**：基础2 + 每层+1（1层=2，5层=6，10层=11）
- **初始间隔**：x = 1秒
- **每层加速**：x = x × 0.9（每层缩短10%）
- **最低间隔**：0.2秒
- **中毒 debuff 持续到敌人死亡**（不再有时间限制）
- 敌人外观变绿闪烁
- **毒液池范围**（毒子弹命中留 0.5 半径，毒药瓶留 1.5 半径）
- **处于毒液池的敌人每秒叠一层中毒**（1.0秒tick间隔）
- **所有持续伤害效果显示在敌人血条上方**（EnemyHealthBar 动态显示所有 DOT 类型）

### 5.3 Mage 被动参数
- DOT 持续时间 +20%
- DOT 可暴击（暴击率 5%+，暴击倍率 2x）
- E 键引爆（12秒冷却）
- 默认自带毒子弹（1.5秒射速）
- 子弹速度受 `GetBulletSpeedMultiplier()` 影响（急速升级生效）
- 攻速受 `GetAttackSpeedMultiplier()` 影响（急速升级缩短射击间隔）

---

## 6. 角色专属升级系统（15个，共振已删除）

### 6.1 DOT 子弹解锁（4种）
- 🔴 流血、🟢 中毒、🟠 燃烧、🔵 霜冻

### 6.2 DOT 增强（4种）
- **腐蚀** — 拥有DOT的敌人护甲-10%（可叠加）
- **诅咒** — DOT敌人死亡时扩散DOT给附近敌人（每层+1目标），**必须拥有诅咒升级才触发传播**
- **痛苦** — DOT触发间隔-10%（可叠加）
- **凋零** — DOT生效时10%几率双倍伤害

### 6.3 引爆增强（2种）
- **辐射** — 引爆伤害+30%
- **污染** — 引爆冷却-30%

### 6.4 DOT 时间增强（1种）
- **侵蚀** — 每5次DOT生效额外冲击，造成当次DOT总伤50%的额外伤害（默认不生效，选了升级才触发）

### 6.5 子弹增强（3种）
- **急速** — 攻速+15%，子弹速度+10%
- **弹幕** — 子弹数量+1（散射发射）
- **反弹** — 30%几率反弹

---

## 7. 状态效果系统（StatusEffectSystem）

### 7.1 StatusEffectManager
- 挂载到敌人身上，管理所有活跃 DOT/Debuff
- 支持 14 种 StatusEffectType
- DOT 持续时间倍率、DOT 伤害倍率
- 引爆（Detonate）、诅咒传播、辐射、凋零禁回血

### 7.2 独立 DOT 组件
- `BleedEffect` — 移动时受伤
- `BurnStackEffect` — 叠加燃烧层数
- `PoisonStackEffect` — 叠加中毒层数（基础2伤/tick + 每层+1，间隔随层数加速）
- `FrostEffect` — 冰冻+永久减速

### 7.3 诅咒传播机制
- `StatusEffectManager.OnEnable()` 注册 `BaseEntity.OnDeath` 事件
- **必须拥有诅咒升级才触发传播**（`CurseSpreadTargets > 1` 才生效）
- DOT 子弹命中敌人时自动添加 `StatusEffectManager`（通过 `DotBulletHelper.EnsureStatusEffectManager()`）
- 敌人死亡时自动调用 `OnEnemyDeath_SpreadContaminate()`
- 传播给范围内**所有敌人**，继承 **10%** 的原 DOT 层数/伤害/持续时间
- 中毒/燃烧按 `StackCount × 0.1` 继承层数（最少 1 层）
- 传播时显示**灰色锁链连线**特效（LineRenderer，0.5秒自动消失）

### 7.4 侵蚀机制
- 每 N 次 DOT 生效触发额外冲击伤害（初始 N=5，升级后减少，最低 2）
- 冲击伤害 = 当次 DOT 总伤 × ErosionDamagePercent（默认 0，选了侵蚀升级后 +0.5）
- `StatusEffectManager._erosionDotHitCount` 在 `OnEnable()` 中重置为 0

---

## 8. 敌人系统（Enemies 模块）

### 8.1 14 种敌人 + 1 种 Boss（每种独立形状+颜色+特效）

| # | 类型 | 形状 | 颜色 | 特效 |
|---|------|------|------|------|
| 1 | BasicEnemy | █ 方形 | 🔴 红色 | - |
| 2 | FastEnemy | ▲ 三角 | 🟣 紫色 | 高速追击 |
| 3 | TankEnemy | ⬡ 六边形 | 🩶 灰色 | 高血量(80)，慢速 |
| 4 | RangedEnemy | ▲ 三角 | 🩷 浅红 | 🔴 圆形子弹射击 |
| 5 | ThrowerEnemy | ◆ 菱形 | 🟠 橙色 | 🟠 圆形投掷弹 |
| 6 | HealerEnemy | ✚ 十字 | 🟢 绿色 | 🟢 治疗光环 + 脉冲 |
| 7 | ChainHealerEnemy | ✚ 十字 | 🩵 青绿 | 链式治疗 |
| 8 | EnhancerEnemy | ⬠ 五边形 | 🟡 黄色 | 🟠 增强光环 + 脉冲 |
| 9 | ShielderEnemy | ⬡ 六边形 | 🔵 蓝色 | 🔵 护盾光环 + 呼吸 |
| 10 | StealthEnemy | ◆ 菱形 | 🩶 暗灰 | 隐身/显形 |
| 11 | BurstEnemy | ★ 星形 | 🟠 亮橙 | 蓄力爆发 |
| 12 | SplitterEnemy | ◆ 菱形 | 🍷 暗红 | 死亡分裂 |
| 13 | SummonerEnemy | ⬠ 五边形 | 🟣 深紫 | 召唤小兵 |
| 14 | ChargerEnemy | ▲ 三角 | 🟤 橙棕 | 蓄力冲锋 |
| 15 | BossEnemy | █ 方形 | 动态 | 5阶段Boss（每5波） |

> ⚠️ 所有敌人默认速度已调整为原来的50%
> 形状由 SpriteFactory 运行时生成（Triangle/Diamond/Pentagon/Hexagon/Star/Cross）
> 光环特效由 EnemyEffectHelper 统一管理

### 8.2 出怪逻辑（SpawnManager）
- **第 1-2 波**：只出 BasicEnemy
- **第 3-4 波**：Basic + Ranged(30%) + Fast(30%)
- **第 5-7 波**：+ Tank(10%) + Thrower(15%)
- **第 8+ 波**：全部 14 种敌人随机出现
- **Boss**：每 5 波出现，同时生成 ≤3 个小怪
- **波次公式**：敌人数 = 3 + (wave-1) × 2
- **难度曲线**：S 曲线递增（前10波慢，15波加速，后期平稳）

---

## 9. 技能系统（Skills 模块）

### 9.1 主动技能（8 种，按 F 使用）
- **`BaseSkill`** — 抽象基类：冷却/伤害/效果强度/升级
- 实现：WindWave, Berserk, TheWorld, Teleport, DeathAura, LightningStorm, GravityWell, FrostNova

### 9.2 被动技能
- **`PassiveSkillData`** (ScriptableObject) — 10 种被动类型
- 类型：MaxHP/HPRegen/MoveSpeed/AttackDamage/Cooldown/Armor/CritChance/CritDamage/PickupRange/Luck

### 9.3 技能管理
- **`PlayerSkillManager`** — `CreateSkillComponent()` 中必须调用 `skill.SetSkillData(data)`
- **键位**：F 使用技能，Q 切换技能

---

## 10. 选择流程（已简化为2步）

### SelectionUI
- **Step 1/2**：选择角色（8种可选）
- **Step 2/2**：选择技能（8种可选）
- 武器不再需要选择，所有角色自动获得默认子弹
- Mage 的攻击由 MagePassive DOT 枪系统驱动，不使用 WeaponController
- ⚠️ DOT 子弹枪（bleed/poison/burn/frostbite）一经选择即从牌库移除，不可重复拾取

---

## 11. 关键文件快速索引

### 最常修改
| 文件 | 路径 | 说明 |
|------|------|------|
| GameSceneBootstrap | Core/GameSceneBootstrap.cs | 游戏启动 + Mage 升级注入 |
| EventManager | Core/EventManager.cs | 全局事件 |
| GameReferences | Core/GameReferences.cs | 全局引用缓存 |
| GameInputHandler | Core/GameInputHandler.cs | 输入处理 |
| CombatManager | Combat/CombatManager.cs | 伤害管理 |
| SpawnManager | Enemies/SpawnManager.cs | 敌人生成 + 形状分配 |
| PlayerController | Player/PlayerController.cs | 玩家移动 |
| MagePassive | Player/MagePassive.cs | DOT 枪系统 |
| DotProjectile | Combat/DotProjectile.cs | 4种DOT子弹 + 效果组件 |
| DotBulletHelper | Combat/DotProjectile.cs | DOT子弹通用工具（EnsureStatusEffectManager） |
| StatusEffectSystem | Combat/StatusEffects/StatusEffectSystem.cs | DOT 管理 + 诅咒传播 + 侵蚀 |
| LevelUpUI | UI/LevelUpUI.cs | 升级界面 + DOT 枪过滤 |
| Damageable | Entities/Damageable.cs | 可伤害实体 |
| EnemyHealthBar | UI/EnemyHealthBar.cs | 敌人血条 + DOT 指示器 |
| EnemyEffectHelper | Enemies/EnemyEffectHelper.cs | 光环/脉冲特效工具 |
| SpriteFactory | Core/SpriteFactory.cs | 运行时几何图形生成 |
| KillRewarder | Entities/KillRewarder.cs | 掉落物生成 |
| PlayerHealthBarHUD | UI/PlayerHealthBarHUD.cs | 左上 HUD (HP/XP/金币) |
| SkillHUD | UI/SkillHUD.cs | 左下技能冷却条 + Mage引爆CD条 |
| PauseMenuUI | UI/PauseMenuUI.cs | 暂停菜单（含游戏状态重置） |
| GameOverUI | UI/GameOverUI.cs | 游戏结束界面（含游戏状态重置） |
| Coin | Entities/Coin.cs | 金币拾取逻辑 |
| GameConfig | ScriptableObjects/Config/GameConfig.cs | 游戏配置 |
| PoolHelper | Core/PoolHelper.cs | 对象池辅助 |

### 不应轻易修改
| 文件 | 原因 |
|------|------|
| Singleton.cs | 基类，影响所有单例 |
| BaseEntity.cs | 基类，影响所有实体 |
| ObjectPool.cs | 底层池系统 |
| Interfaces.cs | 接口定义 |
| EnemyBase.cs | 敌人基类，影响所有14种敌人 |

---

## 12. ⚠️ 修改规范与约束

### 必须遵守
1. **所有伤害必须经过 `CombatManager`**
2. **所有对象生命周期必须走 `ObjectPool`/`PoolHelper`**
3. **全局引用必须通过 `GameReferences`**
4. **事件必须通过 `EventManager`**
5. **Debug 日志必须用 `DebugHelper`**
6. **状态重置必须在 `OnEnable()`**（对象池回收时只调用 OnEnable，不调用 Awake）

### 代码风格
- 中文注释、`///` XML 文档注释、`[Header]` 特性标记
- PascalCase 方法/类，camelCase 字段，_camelCase 私有字段

### 修改检查清单
- [ ] 修改 `EventManager` → 更新 `ClearAll()`
- [ ] 新增敌人 → 池键常量 + SpawnManager 预制体字段 + ChooseEnemyPrefab + 形状分配 + 特效
- [ ] 新增技能 → 继承 `BaseSkill` + `SetSkillData()` + 注册到 `PlayerSkillManager`
- [ ] 新增被动 → `PassiveSkillData.PassiveType` + `Apply()` 方法
- [ ] 新增 UI → 集成到 `GameSceneBootstrap` 或 `HUDManager`
- [ ] 新增 DOT 子弹 → 继承对应基类 + 注册到 `MagePassive` + 更新 `LevelUpUI` 和 `GameSceneBootstrap`
- [ ] 新增敌人形状 → `SpriteFactory` 中添加对应的 `Create*()` 方法 + `ClearCache()` 中注册清理
- [ ] 新增掉落物 → `KillRewarder.SpawnDefault*()` + `GameSceneBootstrap` 对象池预热模板同步更新

### 场景重置规范（返回菜单/重启必须遵守）
- `PauseMenuUI.ReturnToMenu()` 和 `GameOverUI.ResetGameState()` 必须：
  1. `EventManager.ClearAll()` — 清除所有事件
  2. `LevelUpUI.ResetMagnetMultiplier()` — 重置磁铁倍率
  3. `GameReferences.Reset()` — 清空全局引用缓存
  4. `GameSceneBootstrap.ResetCharacter()` — 清空静态角色数据
  5. 销毁所有 DontDestroyOnLoad 单例（GameManager, ObjectPool, CombatManager, SaveManager, OffScreenCuller）
- ⚠️ Singleton 使用 DontDestroyOnLoad，不手动销毁会导致下一局复用旧状态

### 关键 Bug 注意事项
- **PoisonBullet/FrostBullet 超时清理**：必须添加 `_lifetime` + `Update()` 超时 `Destroy(gameObject)`
- **FrostEffect 对象池重置**：必须在 `OnEnable()` 中调用 `RestoreSpeed()` + 重置 `_frozen/_freezeEndTime/_speedCaptured`
- **DOT 子弹枪去重**：`LevelUpUI.GenerateOptions()` 中 `IsDotGunUpgrade()` 检查 `MagePassive.DotGuns` 已拥有则跳过
- **攻速公式**：`GetAttackSpeedMultiplier()` 使用 `1f - bonus` 而非 `1/(1+bonus)`，最低 0.2
- **子弹速度**：`SpawnDotBullet()` 中必须应用 `GetBulletSpeedMultiplier()` 到所有子弹速度
- **DOT 子弹命中敌人**：必须调用 `DotBulletHelper.EnsureStatusEffectManager()` 确保诅咒传播的死亡事件注册
- **敌人血条左对齐**：填充条使用左 pivot Sprite 或居中与背景重叠，避免每帧动态计算位置
- **金币对象池模板**：`GameSceneBootstrap` 中 Coin 模板 Sprite 必须与 `KillRewarder.SpawnDefaultCoin()` 一致（圆形+缩放）
- **技能CD条**：`SkillHUD` 左下角额外显示 Mage 引爆(E技能)CD条（紫色系，仅 Mage 角色可见）