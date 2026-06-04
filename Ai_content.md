# 🧠 AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 窗口先读此文件，快速掌握项目架构和修改规范。

## 1. 项目概述
- **引擎**：Unity 6 (URP) | **语言**：C# | **类型**：2D 俯视角射击生存
- **场景**：`MenuScene`(MenuSceneBootstrap) → `GameScene`(GameSceneBootstrap)
- **Assembly**：`Gameplay.asmdef`(运行时) / `Scripts.Editor.asmdef`(编辑器)

## 2. 目录结构
```
Assets/Scripts/
├ Core/        ← 单例、事件、对象池、引用、输入、存档、Debug
├ Combat/      ← 武器、弹幕、伤害、DOT子弹、状态效果
│ └ StatusEffects/
├ Entities/    ← 可伤害实体、掉落物、经验/金币
├ Enemies/     ← 14种敌人子类 + Boss + 能力框架
├ Player/      ← 控制器、等级、技能管理、MagePassive
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
| 霜冻 | FrostBullet | 1/s | 冰冻1秒 + 永久减速30%，每层+5%，最低90%（无霜伤） |
| 雷电 | LightningBullet | 1/s | 连锁附近最多3个敌人，施加静电层数 |

- **中毒叠加**：基础2+每层+1，间隔1s×0.9^(n-1)，最低0.2s
- **毒液池**：每秒叠一层中毒
- **霜冻**：永久减速30%基础，每层+5%，上限90%（不再有霜伤）
- **静电**：每层降低0.1秒触发间隔（初始5秒，最低2秒），被连锁暂停移动0.5秒

## 6. Mage 升级系统（15种）
- DOT子弹(4)：流血/中毒/燃烧/霜冻
- DOT增强(4)：腐蚀/诅咒/痛苦/凋零
- 引爆增强(2)：辐射/污染
- DOT时间(1)：侵蚀
- 子弹增强(3)：急速/弹幕/反弹

## 7. 状态效果系统
- **StatusEffectManager** 挂敌人身上，管理所有DOT/Debuff
- 独立组件：BleedEffect, BurnStackEffect, PoisonStackEffect, FrostEffect
- 诅咒传播：敌人死亡时自动传播DOT给附近敌人

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
| GameSceneBootstrap | 游戏启动+升级注入 |
| EventManager | 全局事件+泛型事件 |
| GameReferences | 全局引用缓存 |
| CombatManager | 伤害管理 |
| SpawnManager | 敌人生成 |
| MagePassive | DOT枪系统+引爆 |
| DotProjectile | 4种DOT子弹+效果 |
| StatusEffectSystem | DOT管理+诅咒传播 |
| LevelUpUI | 升级界面 |
| Damageable | 可伤害实体 |
| PoolHelper | 对象池辅助 |

### 不应轻易修改
Singleton.cs, BaseEntity.cs, ObjectPool.cs, Interfaces.cs, EnemyBase.cs

## 13. 修改规范

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
返回菜单必须：EventManager.ClearAll() → LevelUpUI.ResetMagnetMultiplier() → GameReferences.Reset() → ResetCharacter() → 销毁所有单例

### 关键 Bug 注意
- PoisonBullet/FrostBullet 必须有 _lifetime 超时销毁
- FrostEffect.OnEnable() 必须 RestoreSpeed + 重置状态
- DOT子弹枪去重：LevelUpUI 检查已拥有则跳过
- 攻速公式：`1f - bonus`，最低0.2
- DOT子弹命中必须调 DotBulletHelper.EnsureStatusEffectManager()
- InputSystem：使用 `Keyboard.current.xxxKey.wasPressedThisFrame`，禁止 `Input.GetKeyDown`
- **SpawnManager.StartFirstWave() 必须重置状态**：调用前必须 `StopAllCoroutines()` + 重置 `_currentWave=0, _enemiesAlive=0, _isSpawning=false, _waveInProgress=false` + 清除 `_activeEnemies`。否则重启/新局会导致：①上一局敌人残留场景朝远处移动 ②新敌人不生成 ③永远卡在第一波。这是 LoadScene 不销毁场景内 SpawnManager 残留状态导致的。
- **所有重启路径必须调用 `GameStateResetter.FullReset()`**：R键(GameInputHandler)、GameOverUI重启、PauseMenuUI返回菜单等。`FullReset()` 会在销毁 ObjectPool 之前 `DestroyImmediate` 所有敌人。**仅调用 `EventManager.ClearAll()` + `LoadScene()` 是不够的！**
- **ObjectPool 是 DontDestroyOnLoad 单例**：`LoadScene(buildIndex)` 重建同一场景时，ObjectPool 会跨场景存活，池中的旧敌人会残留。必须通过 `GameStateResetter.FullReset()` 销毁 ObjectPool 单例。
