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
├── Combat/         ← 武器、弹幕、伤害管理
├── Entities/       ← 可伤害实体、掉落物、经验/金币
├── Enemies/        ← 14 种敌人子类 + SpawnManager
├── Player/         ← 玩家控制、等级、技能管理
├── Skills/         ← 8 种主动技能 + 被动技能
├── UI/             ← 所有 UI 组件（含 EnemyHealthBar）
├── Map/            ← 地图主题、边界、装饰、环境区域
├── Audio/          ← BGM 管理
├── Data/           ← 配置加载器
├── ScriptableObjects/
│   ├── Config/     ← GameConfig, EnemyWaveConfig, MapThemeData
│   ├── Characters/ ← CharacterData
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

### 3.2 全局引用缓存
- **`GameReferences`** — 静态类，缓存 `Player`, `MainCamera`, `SpawnManager`, `HUDManager` 等
- **严禁使用 `FindFirstObjectByType`**，所有全局引用必须通过 `GameReferences` 获取

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
  - WASD 移动、1-8/Q 武器切换、ESC 暂停、Tab 商店
  - 鼠标世界坐标统一计算（`MouseWorldPosition` 属性）

### 3.8 游戏状态机
- **`GameManager`** — 状态：`Menu → Playing → Paused → GameOver`
  - `ChangeState(newState)` → 先 `OnExitState(old)` 再 `OnEnterState(new)`
  - `PauseGame()` / `ResumeGame()` 控制 `Time.timeScale`

### 3.9 存档系统
- **`SaveManager`** — 单例，JSON 序列化
- **`ConfigLoader`** — 运行时加载 `GameConfig` + `EnemyWaveConfig`（Asset 缺失时静默创建默认）

---

## 4. 战斗系统（Combat 模块）

### 4.1 伤害公式
- **`CombatManager`** — 静态方法，所有伤害必须经过此处
  - `CalculateFinalDamage(baseDamage, multiplier, armor, out isCrit)` — 暴击/护甲计算
  - `DealDamage(target, baseDamage, multiplier)` — 单体伤害
  - `DealAoEDamage(center, radius, baseDamage, multiplier, knockback)` — 范围伤害
  - `ProcessPierce(ref pierce, target, base, mult, hitSet, knockback, transform)` — 穿透逻辑

### 4.2 弹幕继承体系
```
MonoBehaviour
└── Projectile (基础弹幕 — 移动/穿透/碰撞)
    ├── Bullet (基础子弹)
HomingProjectile (追踪弹)
LightningBolt (闪电链)
ShockwaveProjectile (冲击波)
MineTrap (地雷)
FireZone (火焰区域)
FrostOrb (冰霜球)
VenomDart (毒镖)
EnemyBullet (敌人子弹)
```

### 4.3 武器系统
- **`WeaponData`** (ScriptableObject) — 8 种武器类型
  - `ProjectileType`: Bullet, Lightning, Shockwave, Homing, Mine, Fire, Frost, Venom
  - 4 级升级（`UpgradeLevel` 0-4）：伤害/穿透/冷却/范围
- **`WeaponController`** — 武器发射控制
  - `SwitchWeapon(int index)` / `SetWeapon(WeaponData)`
  - `FireWeapon(direction)` → 根据类型调用对应 Spawn 方法

---

## 5. 实体系统（Entities 模块）

### 5.1 继承体系
```
MonoBehaviour
└── BaseEntity (标签/碰撞检测/OnEnable重置_alive)
    └── EnemyBase (敌人基类 — 追击/碰撞伤害/死亡/血条)
        ├── FastEnemy, TankEnemy, RangedEnemy
        ├── ChargerEnemy, ThrowerEnemy
        ├── HealerEnemy, ChainHealerEnemy, EnhancerEnemy
        ├── ShielderEnemy, StealthEnemy, BurstEnemy
        ├── SplitterEnemy, SummonerEnemy
        └── BossEnemy (5 阶段 Boss)
```

### 5.2 核心组件
- **`Damageable`** — 实现 `IDamageable`，HP/护甲/受伤/治疗/死亡 + LateUpdate 安全网
- **`KillRewarder`** — 实现 `IRewardable`，监听死亡事件生成掉落
- **`Coin`** / **`XPGem`** — 掉落物，移动拾取 + 对象池回收
- **`SpecialDrop`** — 特殊掉落（治疗/磁铁/攻击/护盾/经验）

### 5.3 物理质量（碰撞推力）
- **玩家 mass = 100** — 敌人无法推走玩家
- **敌人 mass = 1** — 玩家可轻松推开敌人

---

## 6. 敌人系统（Enemies 模块）

### 6.1 14 种敌人 + 1 种 Boss
| # | 类型 | 行为特点 |
|---|------|---------|
| 1 | BasicEnemy | 基础近战，直冲玩家 |
| 2 | FastEnemy | 高速追击，低血量 |
| 3 | TankEnemy | 高血量(80)，慢速 |
| 4 | RangedEnemy | 保持距离射击 |
| 5 | ThrowerEnemy | 投掷炸弹 |
| 6 | HealerEnemy | 治疗附近敌人 |
| 7 | ChainHealerEnemy | 链式治疗 |
| 8 | EnhancerEnemy | 增强附近敌人移速 |
| 9 | ShielderEnemy | 为附近敌人添加护盾 |
| 10 | StealthEnemy | 隐身/显形切换 |
| 11 | BurstEnemy | 蓄力后爆发射击 |
| 12 | SplitterEnemy | 死亡分裂 |
| 13 | SummonerEnemy | 召唤小兵 |
| 14 | ChargerEnemy | 蓄力冲锋 |
| 15 | BossEnemy | 5阶段Boss（每5波） |

### 6.2 出怪逻辑（SpawnManager）
- **第 1-2 波**：只出 BasicEnemy
- **第 3-4 波**：Basic + Ranged(30%) + Fast(30%)
- **第 5-7 波**：+ Tank(10%) + Thrower(15%)
- **第 8+ 波**：全部 14 种敌人随机出现
- **Boss**：每 5 波出现，同时生成 ≤3 个小怪
- **波次公式**：敌人数 = 3 + (wave-1) × 2
- **难度曲线**：S 曲线递增（前10波慢，15波加速，后期平稳）

### 6.3 敌人血条（EnemyHealthBar）
- 纯 2D Sprite 实现（不依赖 Canvas）
- 深灰背景 + 彩色填充条（绿→黄→红）
- 满血时隐藏，受伤后显示
- 由 EnemyBase.OnEnable() 自动创建

---

## 7. 技能系统（Skills 模块）

### 7.1 主动技能（8 种）
- **`BaseSkill`** — 抽象基类：冷却/伤害/效果强度/升级
- 实现：WindWave, Berserk, TheWorld, Teleport, DeathAura, LightningStorm, GravityWell, FrostNova

### 7.2 被动技能
- **`PassiveSkillData`** (ScriptableObject) — 10 种被动类型
- 类型：MaxHP/HPRegen/MoveSpeed/AttackDamage/Cooldown/Armor/CritChance/CritDamage/PickupRange/Luck

### 7.3 技能管理
- **`PlayerSkillManager`** — `CreateSkillComponent()` 中必须调用 `skill.SetSkillData(data)`

---

## 8. UI 系统

### 8.1 全局主题 (UIColorTheme.cs)
- 五色配色：暗青 `#012326` / 深蓝青 `#025373` / 荧光青 `#05F2DB` / 洋红 `#D9048E` / 亮粉 `#F205CB`
- 所有 UI 组件统一从 `UIColorTheme` 取色，确保视觉统一
- 工具方法：`MakeTexture(Color)` 创建 1px 纹理、`DrawButtonGlow(Rect)` 绘制荧光边框发光

### 8.2 IMGUI 缩放 (GUIScaleHelper.cs)
- 参考分辨率 1920×1080，所有 IMGUI `OnGUI()` 通过 `GUI.matrix` 等比缩放
- 用法：`BeginScale()` → 绘制 → `EndScale()`
- 已集成入口：`GameSceneBootstrap.OnGUI()`, `PlayerHealthBarHUD`, `BossHealthBarHUD`, `SkillHUD`, `WaveRewardUI`

### 8.3 字体管理 (UIFontProvider.cs)
- 统一字体入口：`UIFontProvider.DefaultFont`
- 优先级：`Resources.Load("Fonts/Default")` → `LegacyRuntime.ttf`
- 动态 Canvas 自动添加 `CanvasScaler(1920×1080)`

### 8.4 UI 组件表

| 组件 | 渲染 | 功能 |
|------|------|------|
| `HUDManager` | UGUI | 游戏内 HUD（等级/波次/金币） |
| `LevelUpUI` | UGUI | 升级选择界面 |
| `SelectionUI` | IMGUI | 角色/武器/技能初始选择（三栏布局） |
| `WaveRewardUI` | IMGUI | 波次间奖励选择 |
| `ShopUI` | UGUI | 商店界面 |
| `GameOverUI` | UGUI | 游戏结束界面 |
| `PauseMenuUI` | IMGUI | 暂停菜单 |
| `SettingsUI` | IMGUI | 设置面板 |
| `AchievementUI` | IMGUI | 成就系统 UI |
| `MinimapUI` | IMGUI | 小地图 |
| `DamagePopup` | 3D Text | 浮动伤害数字 |
| `ScreenShake` | Transform | 屏幕抖动效果 |
| `EnemyHealthBar` | Sprite | 敌人头顶血条 |
| `PlayerHealthBarHUD` | **IMGUI 新增** | **左上角玩家血量条** |
| `BossHealthBarHUD` | **IMGUI 新增** | **中上方 Boss 血量条** |
| `SkillHUD` | **IMGUI 新增** | **左下角技能冷却 + 操作提示** |

---

## 9. 地图系统（Map 模块）

- **`MapBoundary`** — 碰撞墙 + 边界线
- **`MapThemeManager`** — 主题切换
- **`GridBackground`** — 网格背景
- **`EnvironmentZone`** / **`DecorationSpawner`** — 环境区域 + 装饰

---

## 10. 场景引导（Bootstrap）

### GameSceneBootstrap 初始化顺序
1. `Start()` → 加载数据 → 显示 `SelectionUI`
2. `OnSelectionConfirmed()` → `ApplySelectionAndStartGame()`
3. 初始化 Player / SpawnManager / ObjectPool / MapBoundary / 各 UI
4. `WarmUpObjectPools()` — 预热所有对象池（含运行时创建的敌人预制体）
5. `SpawnManager.EnsureEnemyPrefabs()` — Inspector 未赋值时自动创建 14 种敌人预制体

---

## 11. 接口定义（Interfaces.cs）

```csharp
interface IDamageable  { void TakeDamage(int damage); }
interface IPoolable    { void OnSpawnFromPool(); void OnDespawnToPool(); }
interface IRewardable  { /* KillRewarder 实现 */ }
```

---

## 12. ScriptableObject 数据结构

| 类型 | 路径 | 关键字段 |
|------|------|---------|
| `GameConfig` | Config/GameConfig.cs | 全局游戏参数 |
| `EnemyWaveConfig` | Config/EnemyWaveConfig.cs | 波次配置 |
| `MapThemeData` | Config/MapThemeData.cs | 主题配置 |
| `CharacterData` | Characters/CharacterData.cs | 角色属性 |
| `WeaponData` | WeaponData.cs | 武器属性（8种×4级） |
| `SkillData` | Skills/SkillData.cs | 技能属性 |
| `PassiveSkillData` | Skills/PassiveSkill.cs | 被动技能（10种） |

---

## 13. ⚠️ 修改规范与约束

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
- [ ] 新增敌人 → 池键常量 + SpawnManager 预制体字段 + ChooseEnemyPrefab
- [ ] 新增武器 → `WeaponData.ProjectileType` + `WeaponController.FireWeaponData()`
- [ ] 新增技能 → 继承 `BaseSkill` + `SetSkillData()` + 注册到 `PlayerSkillManager`
- [ ] 新增被动 → `PassiveSkillData.PassiveType` + `Apply()` 方法
- [ ] 新增 UI → 集成到 `GameSceneBootstrap` 或 `HUDManager`

---

## 14. 关键文件快速索引

### 最常修改
| 文件 | 路径 |
|------|------|
| GameSceneBootstrap | Core/GameSceneBootstrap.cs |
| EventManager | Core/EventManager.cs |
| GameReferences | Core/GameReferences.cs |
| CombatManager | Combat/CombatManager.cs |
| SpawnManager | Enemies/SpawnManager.cs |
| WeaponController | Combat/WeaponController.cs |
| PlayerController | Player/PlayerController.cs |
| Damageable | Entities/Damageable.cs |
| GameConfig | ScriptableObjects/Config/GameConfig.cs |
| PoolHelper | Core/PoolHelper.cs |

### 不应轻易修改
| 文件 | 原因 |
|------|------|
| Singleton.cs | 基类，影响所有单例 |
| BaseEntity.cs | 基类，影响所有实体 |
| ObjectPool.cs | 底层池系统 |
| Interfaces.cs | 接口定义 |
| EnemyBase.cs | 敌人基类，影响所有14种敌人 |

---

## 15. 新增文件清单

```
Assets/Scripts/Core/PoolHelper.cs          ← 对象池工具
Assets/Scripts/Core/ComboManager.cs        ← 连击系统
Assets/Scripts/Core/GameReferences.cs      ← 全局引用缓存
Assets/Scripts/Core/DebugHelper.cs         ← 调试日志
Assets/Scripts/Core/GameInputHandler.cs    ← 输入管理
Assets/Scripts/UI/SelectionUI.cs           ← 角色选择
Assets/Scripts/UI/DebugOverlay.cs          ← 调试覆盖
Assets/Scripts/UI/DamagePopup.cs           ← 伤害数字
Assets/Scripts/UI/ScreenShake.cs           ← 屏幕抖动
Assets/Scripts/UI/PauseMenuUI.cs           ← 暂停菜单
Assets/Scripts/UI/SettingsUI.cs            ← 设置面板
Assets/Scripts/UI/WaveRewardUI.cs          ← 波次奖励
Assets/Scripts/UI/AchievementUI.cs         ← 成就UI
Assets/Scripts/UI/MinimapUI.cs             ← 小地图
Assets/Scripts/UI/EnemyHealthBar.cs        ← 敌人血条
Assets/Scripts/Skills/PassiveSkill.cs      ← 被动技能
Assets/Scripts/Map/MapBoundary.cs          ← 地图边界
Assets/Scripts/Entities/SpecialDrop.cs     ← 特殊掉落
```

---

## 16. Git 协作规范

### 分支策略
- `main` — 稳定可运行版本
- `dev` — 开发分支
- `feature/xxx` — 功能分支

### 提交规范
```
feat: 新增敌人类型 XXX
fix: 修复敌人死亡后不移除的问题
refactor: 重构伤害计算公式
docs: 更新 Ai_content.md
```

### 协作流程
1. 从 `dev` 创建 `feature/xxx` 分支
2. 完成后提 PR 合并回 `dev`
3. 测试通过后合并到 `main`