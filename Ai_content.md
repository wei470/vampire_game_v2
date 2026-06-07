# 🧠 AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 窗口先读此文件。

## 1. 项目概述
- **引擎**：Unity 6 (URP) | **语言**：C# | **类型**：2D 俯视角射击生存
- **场景**：`MenuScene`(MenuSceneBootstrap) → `GameScene`(GameSceneBootstrap)
- **Assembly**：`Gameplay.asmdef`(运行时) / `Scripts.Editor.asmdef`(编辑器)

## 2. 目录结构
```
Assets/Scripts/
├ Core/        ← 单例、事件、对象池、引用、输入、存档、Debug
│  ├── GameSceneBootstrap.cs  协调器：组件组装+生命周期
│  ├── GameDataLoader.cs      数据加载：角色/武器/技能/Mage配置
│  ├── GameStarter.cs         游戏启动：应用配置+预热池+开始游戏
│  ├── GameHUDFactory.cs      HUD工厂：创建所有游戏内HUD组件
├ Combat/      ← 武器、弹幕、伤害、DOT子弹、状态效果
│ └ StatusEffects/
├ Entities/    ← 可伤害实体、掉落物、经验/金币
├ Enemies/     ← 14种敌人子类 + Boss + 能力框架
│  ├── SpawnManager.cs        波次管理协调器
│  ├── EnemyPrefabFactory.cs  敌人预制体创建+池键映射
│  ├── WaveConfigHelper.cs    波次配置+难度倍率计算
├ Player/      ← 控制器、等级、技能管理、MagePassive
│  ├── MagePassive.cs         DOT枪+升级+协同+进化
│  ├── DetonateSystem.cs      引爆系统（蓄力/连锁/余烬/碎裂）
├ Skills/      ← 8种主动技能
├ UI/          ← 所有UI组件
├ Map/         ← 地图主题、装饰、环境区域
├ Audio/       ← BGM + SFXManager
├ Data/        ← ConfigLoader
└ ScriptableObjects/ Config/ Characters/ Skills/
```

## 3. 核心架构

### 3.1 关键基类
- **`Singleton<T>`** → GameManager, ObjectPool, CombatManager, SaveManager, OffScreenCuller
- **`BaseEntity`** / **`Damageable`** / **`EnemyBase`** — 实体继承链

### 3.2 全局引用
- **`GameReferences`** — 静态类缓存 Player/Camera/SpawnManager 等，**禁止 FindFirstObjectByType**
- **返回菜单必须 `GameReferences.Reset()`**

### 3.3 事件系统
- **`EventManager`** — 静态委托 + 泛型事件 `Subscribe<T>/Publish<T>`
- 关键事件：OnEnemyKilled, OnXPGained, OnLevelUp, OnPlayerDeath, OnWaveStart/Complete, OnGameStateChanged
- **修改事件必须同步 `ClearAll()`**

### 3.4 对象池
- **`ObjectPool`** 单例 + **`PoolHelper`** 静态工具
- **所有敌人/弹幕/掉落物必须走对象池**，禁止 Instantiate/Destroy
- 池回收只调 `OnEnable()`，状态重置必须在 OnEnable

### 3.5 死亡流程
```
TakeDamage → HP≤0 → Die() → OnDeath事件 → EnemyBase.Despawn
安全网1: EnemyBase.FixedUpdate !IsAlive → Die()
安全网2: Damageable.LateUpdate HP≤0 → Die()
```

### 3.6 输入
- **`GameInputHandler`** — 统一输入（InputSystem），WASD移动/鼠标射击/E引爆/F技能/Q切换/ESC暂停/Tab商店

### 3.7 状态机
- **`GameManager`** — Menu→Playing→Paused→GameOver

### 3.8 存档
- **`SaveManager`** JSON序列化 / **`ConfigLoader`** 运行时加载配置

## 4. 武器系统（简化版）
- 所有角色默认获得 Bullet，Mage 不用 WeaponController
- 选择流程：角色 → 技能（无武器选择）

## 5. DOT 子弹系统（Mage 专属）

| 类型 | 类名 | 射速 | 特点 |
|------|------|------|------|
| 流血 | BleedBullet | 1/s | 命中附加流血，DPS:3/s，移动时受伤 |
| 中毒 | PoisonBullet | 0.5/s | 无限距离，命中留毒液池 |
| 燃烧 | BurnBullet | 1/s | 快速子弹，DPS:2/s，叠加燃烧层数 |
| 霜冻 | FrostBullet | 1/s | 命中不造成伤害，只施加永久减速30%+叠层，每层+5%，最高90%减速 |
| 雷电 | LightningBullet | 1/s | 命中不造成伤害，只叠静电层；连锁最多3个敌人；首次命中1秒静电，后续0.1秒静电；定时5秒放电(每层-0.2秒，最低2秒)0.5秒静电，伤害固定为0 |
| 黑暗 | DarkBullet | 0.33/s | 缓慢子弹(40%速度)，击中后消失，施加黑暗标记(永久)。命中的敌人略微变黑。敌人死亡时所有DOT按50%效果传播给3范围敌人(黑暗标记本身不传播)。不造成直接伤害 |
| 光明 | LightBulletController | 0.2/s | 蓄力3秒(玩家头上蓄力条，不减速)后，朝鼠标方向射出激光，顺时针扫45度，帧伤1点/次。命中施加光明标记：每层受伤+1%，无上限，敌人身上显示层数文字 |

- **中毒叠加**：基础2+每层+1，间隔1s×0.9^(n-1)，最低0.2s
- **毒液池**：每秒叠一层中毒
- **霜冻**：永久减速30%基础，每层+5%，上限90%（不造成伤害，只减速，不再冰冻敌人）
- **静电**：雷电子弹不造成直接伤害，只叠层+连锁；首次命中触发1秒静电，后续命中触发0.1秒静电；定时基础5秒放电(每层-0.2秒，最低2秒)暂停0.5秒；静电不造成伤害，纯控制效果
- **黑暗标记**：永久标记，命中的敌人略微变黑。敌人死亡时传播所有DOT(DarkMarkEffect)。传播效率50%，范围3。DarkBullet无穿透，不造成直接伤害
- **光明标记**：每层受到伤害+1%，无上限(公式：1.0+stack×0.01)。LightBulletController蓄力3秒(头部蓄力条)后朝鼠标方向射出激光，顺时针扫45度，帧伤1点/次。敌人身上用TextMesh显示"+N%"层数

## 6. Mage 升级系统（17种）
- DOT子弹(7)：流血/中毒/燃烧/霜冻/雷电/黑暗/光明
- DOT增强(4)：腐蚀/诅咒/痛苦/凋零
- 引爆增强(2)：辐射/污染
- DOT时间(1)：侵蚀
- 子弹增强(3)：急速/弹幕/反弹

## 7. 状态效果系统
- **StatusEffectManager** 挂敌人身上，管理所有DOT/Debuff
- **CurseSpreadSystem** 静态类，敌人死亡时传播DOT
- **DotComboSystem** DOT组合效果：碎冰/爆燃/脓毒
- 独立组件：BleedEffect, BurnStackEffect, PoisonStackEffect, FrostEffect

## 8. 敌人系统
- 14种 + 4种Boss变体，由 SpriteFactory 运行时生成形状
- 出怪：1-2波Basic → 3-4波+Ranged/Fast → 5-7波+Tank/Thrower → 8+全种类
- Boss每5波出现，敌人数=3+(wave-1)×2
- DOT抗性系统：EnemyDotResistance

## 9. 技能系统
- 8种主动技能继承 BaseSkill，F使用/Q切换
- 被动技能：PassiveSkillData 10种类型

## 10. 选择流程
- Step1 选角色(8种) → Step2 选技能(8种)

## 11. Debug 系统
- **DebugConfigPanel** — F1键，热加载JSON + 全局倍率
- **DebugPoolMonitor** — F2键，对象池监控面板

## 12. 关键文件索引

### 最常修改
| 文件 | 说明 |
|------|------|
| GameSceneBootstrap | 协调器（~236行）：组件组装+生命周期 |
| GameDataLoader | 数据加载（~160行）：角色/武器/技能/Mage配置 |
| GameStarter | 游戏启动（~212行）：应用配置+预热池+开始游戏 |
| GameHUDFactory | HUD工厂（~110行）：创建游戏内HUD组件 |
| SpawnManager | 波次管理（~342行）：协调波次+生成+状态 |
| EnemyPrefabFactory | 敌人工厂（~231行）：预制体创建+池键映射 |
| WaveConfigHelper | 波次配置（~120行）：难度曲线+配置加载 |
| MagePassive | DOT枪（~428行）：DOT枪管理+升级+协同+进化 |
| DetonateSystem | 引爆系统（~359行）：蓄力/连锁/余烬/碎裂 |
| LevelUpUI | 升级UI协调（~220行）：显示+选择+应用 |
| LevelUpOptionGenerator | 升级选项生成（~332行）：选项生成+Build路线+推荐 |
| StatusEffectSystem | DOT管理（~230行）：核心管理+引爆+视觉 |
| CurseSpreadSystem | 诅咒传播（~100行）：死亡时传播DOT给附近敌人 |
| DotComboSystem | DOT组合（~100行）：碎冰/爆燃/脓毒协同 |
| EventManager | 全局事件（~120行）：事件声明+触发+清理 |
| Damageable | 可伤害实体（~330行，未拆分：职责紧密） |

### 不应轻易修改
Singleton.cs, BaseEntity.cs, ObjectPool.cs, Interfaces.cs, EnemyBase.cs

## 14. 修改规范

### 必须遵守
1. 所有伤害经 `CombatManager`，生命周期走 `ObjectPool`/`PoolHelper`
2. 全局引用经 `GameReferences`，事件经 `EventManager`
3. Debug 用 `DebugHelper`，状态重置在 `OnEnable()`

### 代码风格
- 中文注释、`///` XML文档、`[Header]` 标记
- PascalCase 方法/类，_camelCase 私有字段

### 修改检查清单
- 修改 EventManager → 更新 ClearAll()
- 新增敌人 → 池键+SpawnManager+形状+特效
- 新增技能 → 继承 BaseSkill + SetSkillData()
- 新增 DOT 子弹 → 注册到 MagePassive + 更新 LevelUpUI
- 新增掉落物 → KillRewarder + GameSceneBootstrap 池预热

### 场景重置规范
返回菜单必须：EventManager.ClearAll() → MagnetMultiplierSystem.Reset() → GameReferences.Reset() → ResetCharacter() → 销毁所有单例

### 关键 Bug 注意
- PoisonBullet/FrostBullet 必须有 _lifetime 超时销毁
- FrostEffect.OnEnable() 必须 RestoreSpeed + 重置状态
- DOT子弹枪去重：LevelUpUI 检查已拥有则跳过
- 攻速公式：`1f - bonus`，最低0.2
- DOT子弹命中必须调 DotBulletHelper.EnsureStatusEffectManager()
- InputSystem：使用 `Keyboard.current.xxxKey.wasPressedThisFrame`，禁止 `Input.GetKeyDown`
- **SpawnManager.StartFirstWave() 必须重置状态**：调用前必须 `StopAllCoroutines()` + 重置 `_currentWave=0, _enemiesAlive=0, _isSpawning=false, _waveInProgress=false` + 清除 `_activeEnemies`
- **所有重启路径必须调用 `GameStateResetter.FullReset()`**：R键、GameOverUI重启、PauseMenuUI返回菜单等。仅 `EventManager.ClearAll()` + `LoadScene()` 不够！
- **ObjectPool 是 DontDestroyOnLoad 单例**：LoadScene 重建场景时池中旧敌人会残留，必须通过 `GameStateResetter.FullReset()` 销毁。
- **重启后对象池必须重新预热**：`FullReset()` 销毁 ObjectPool 后，`SpawnManager.StartFirstWave()` 必须在 `EnsureEnemyPrefabs()` 之后调用 `EnsureEnemyPoolsWarmedUp()` 确保池中有可激活的敌人实例。

## 15. 重构进度 — 全部完成 ✅

> **V1+V2 重构已全部完成**，V3 性能优化已全部完成，fixme.md 已删除。

### V3 性能优化总结
- **1.1 DOT子弹 FixedUpdate GetComponent 缓存**：BleedBullet/BurnBullet/FrostBullet/LightningBullet 在 OnEnable 中缓存 Rigidbody2D
- **1.2 DetonateSystem GetComponent 优化**：所有 `GetComponent` → `TryGetComponent`，缓存 ScreenShake 引用
- **1.3 CurseSpreadSystem GetComponent 优化**：源DOT组件单次获取（检查+缓存合并），目标组件改用 TryGetComponent

### V4 文件拆分总结
- **MageStatsHUD(509)** → MageStatsHUD(~170) + MageStatsHUDRenderer(~280) 静态绘制类
- **DotStatusIndicator(493)** → DotStatusIndicator(~310) + DotStatusIconManager(~180) 图标管理器
- **AchievementUI(387)** → AchievementUI(~190) + AchievementNotificationRenderer(~190) 通知渲染器

### 重构总结

**V1 阶段**（7次拆分）：
- GameSceneBootstrap → Bootstrap + GameDataLoader + GameStarter + GameHUDFactory
- SpawnManager → SpawnManager + EnemyPrefabFactory + WaveConfigHelper
- MagePassive → MagePassive + DetonateSystem
- LevelUpUI → LevelUpUI + LevelUpOptionGenerator + MagnetMultiplierSystem
- DotProjectile → BleedBullet + PoisonBullet + BurnBullet + FrostBullet + LightningBullet + DotBulletFactory
- StatusEffectSystem → StatusEffectSystem + CurseSpreadSystem + DotComboSystem
- EventManager → EventManager + GenericEventBus

**V2 阶段**（P4-P6，14次拆分）：
- BossEnemy → BossEnemy + BossAbilities + BossFactory
- EnemyHealthBar → EnemyHealthBar + HealthBarSpriteHelper + DotStatusIndicator
- SaveManager → SaveManager + SaveData + PermanentUpgradeStore
- MagePassive → MagePassive + MageUpgradeApplier
- StatusEffectSystem → StatusEffectSystem + StatusEffectData + DotVisualEffectManager
- SpawnManager → SpawnManager + EnemyScalingHelper
- LevelUpOptionGenerator → LevelUpOptionGenerator + BuildPathRecommender
- MageStatsHUD → MageStatsHUD + MageStatsDataCollector
- DecorationSpawner → DecorationSpawner + DecorationSpriteHelper
- SelectionFlowManager → SelectionFlowManager + SelectionDataLoader
- EnvironmentZone → EnvironmentZone + EnvironmentZoneEffect
- DamageMeter → DamageMeter + DamageBreakdownUI
- WeaponController → WeaponController + WeaponProjectileFactory
- SFXManager → SFXManager + SFXPoolHelper

**跳过的文件**：ObjectPool(核心单例)、DebugPoolMonitor(编辑器代码)、P7文件(250-330行，结构清晰无需拆分)