# 🧠 AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 窗口先读此文件。最后更新：2026-06-10

## 1. 项目概述
- **引擎**：Unity 6 (URP) | **语言**：C# | **类型**：2D 俯视角射击生存
- **场景**：`MenuScene`(MenuSceneBootstrap) → `GameScene`(GameSceneBootstrap)
- **Assembly**：`Gameplay.asmdef`(运行时) / `Scripts.Editor.asmdef`(编辑器)
- **仓库**：https://github.com/wei470/vampire_game_v2.git
- **当前角色系统**：8 种角色（warrior/mage/necromancer/berserker/ranger/paladin/vampire/assassin），每个有独立 CharacterData.asset
- **Mage 为当前主角**，DOT 子弹系统是 Mage 专属机制

## 2. 核心架构

| 基类/系统 | 文件 | 说明 |
|-----------|------|------|
| `Singleton<T>` | `Core/Singleton.cs` | GameManager, ObjectPool, CombatManager, SaveManager, OffScreenCuller |
| `BaseEntity` | `Entities/BaseEntity.cs` | 实体基类（生命周期/死亡广播 OnDeath） |
| `Damageable` | `Entities/Damageable.cs` | 伤害组件（HP/护甲/无敌帧/死亡安全网 LateUpdate） |
| `EnemyBase` | `Enemies/EnemyBase.cs` | 敌人基类（移速/碰撞/LOD） |
| `GameReferences` | `Core/GameReferences.cs` | 静态引用缓存 Player/Camera/SpawnManager，**禁止 FindFirstObjectByType**。懒缓存 MagePassive/DetonateSystem/SkillManager/LevelSystem/WeaponCtrl/PlayerDamageable |
| `EventManager` | `Core/EventManager.cs` | 委托+泛型事件 `Subscribe<T>/Publish<T>`，修改必须同步 `ClearAll()` |
| `ObjectPool` + `PoolHelper` | `Core/ObjectPool.cs` / `Core/PoolHelper.cs` | 所有敌人/弹幕/掉落物必须走对象池，禁止 Instantiate/Destroy |
| `GameManager` | `Core/GameManager.cs` | 状态机 Menu→Playing→Paused→GameOver |
| `GameInputHandler` | `Core/GameInputHandler.cs` | 统一输入（InputSystem），WASD/鼠标/E引爆/F技能/Q切换/ESC暂停/Tab商店 |
| `GameStateResetter` | `Core/GameStateResetter.cs` | 轻量重置：事件清除+静态状态重置+标记单例延迟销毁（不使用 DestroyImmediate） |

**死亡流程**：TakeDamage → HP≤0 → Die() → OnDeath → Despawn（安全网：FixedUpdate+LateUpdate 双保险）

**返回菜单流程**（PauseMenuUI.ReturnToMenu）：
```
Time.timeScale = 1f
EventManager.ClearAll()
GameReferences.Reset()
GameSceneBootstrap.ResetCharacter()
DotEffectRegistry.ClearAll()
SceneManager.LoadScene("MenuScene")   ← LoadScene 统一销毁所有场景对象
```
**⚠️ 关键**：返回菜单 **禁止** 使用 `DestroyImmediate`（会触发 OnDestroy 回调级联，创建 DontDestroyOnLoad 垃圾对象导致卡死）。只做数据清理，让 `LoadScene` 处理对象销毁。

**R键重启流程**（GameInputHandler）：
```
Time.timeScale = 1f
EventManager.ClearAll()
GameReferences.Reset()
SceneManager.LoadScene(current scene)
```

## 3. DOT 子弹系统（Mage 专属，7种）

| 类型 | 类名 | 池键 | 特点 |
|------|------|------|------|
| 中毒 | PoisonBullet : DotBulletBase | DOT_POISON_BULLET | 无限距离，命中留毒液池(PoisonPuddle, DOT_POISON_PUDDLE) |
| 燃烧 | BurnBullet : DotBulletBase | DOT_BURN_BULLET | 快速子弹，叠加燃烧层数，风化触发扩散 |
| 霜冻 | FrostBullet : DotBulletBase | DOT_FROST_BULLET | 永久减速+叠层，霜电元素反应 |
| 雷电 | LightningBullet | DOT_LIGHTNING_BULLET | 连锁3敌人，叠静电层，定时放电（未迁移到 DotBulletBase） |
| 黑暗 | DarkBullet | — | 永久标记，敌人死亡时DOT按50%传播(范围3) |
| 光明 | LightBulletController | — | 蓄力3秒后激光扫射，每层受伤+0.5%，无上限 |
| 风 | WindBullet | DOT_WIND_BULLET | 30°扇形散射5发，叠风化层数(+伤害+击退) |
| ~~流血~~ | ~~BleedBullet~~ | — | 已弃用 [Obsolete]，保留 BleedEffect 组件（Dark/Curse/Detonate 仍引用） |

**基类**：`DotBulletBase`（速度/生命周期/穿透/反弹/通用 OnEnable 重置）

**元素反应**：
- 燃烧×风化→燃烧扩散(r=配置化, 默认5) — BurnBullet 检查 WindErosionEffect
- 霜冻×静电→霜电冰场(r=配置化, 默认1, 2s) — FrostLightningField

**关键文件**：
| 文件 | 作用 |
|------|------|
| `DotBulletFactory.cs` | DOT子弹工厂（从Config读取参数创建子弹，**OnConfigChanged 清除 `_config` 缓存**） |
| `DotBulletBase.cs` | DOT子弹基类（速度/生命周期/穿透/反弹） |
| `DotBulletConfig.cs` | DOT子弹配置 ScriptableObject（40+字段，已中文化Tooltip，OnConfigChanged 信号） |
| `DotBulletHelpers.cs` | 工具方法（EnsureStatusEffectManager/GlowReturnHelper） |
| `DotEffectRegistry.cs` | DOT效果统一注册表（4个 HashSet: Burn/Poison/Frost/Static） |
| `DotColorBlender.cs` | DOT 视觉颜色混合 |

## 4. Mage 升级系统（33种）
- **实现链**：`MageUpgradeConfig`(配置) → `MagePassive`(属性, partial class) → `MageUpgradeApplier`(应用)
- **MagePassive.cs**（属性+DOT枪管理, ~370行）+ **MagePassive.Firing.cs**（Update+子弹发射+视觉, ~160行）
- **升级类别**：DOT子弹(7) + DOT增强(4) + 引爆增强(2) + 子弹增强(3) + P0强化(3) + P1深度(1) + P2协同(7) + 子弹扩展(2) + 生存(2) + P3终极(3)
- **联动**：StatusEffectSystem / DetonateSystem / CurseSpreadSystem / DotComboSystem / DotFusionSystem
- **DotFusionSystem**：5种融合（熔岩/毒冰/等离子/电磁/腐蚀），被 MagePassive 和 EvolutionSystem 引用

## 5. 引爆系统
- `DetonateSystem.cs` — 蓄力/连锁/余烬/碎裂
- 已用 **SpatialGrid** 空间分区加速范围查询（10单元格，覆盖-200~+200）
- 霜爆和末日审判已合并到主循环内（消除3次重复遍历）
- TryChainDetonate 使用类级缓存列表消除 GC alloc
- 输入：E键蓄力（Keyboard API），移速惩罚50%
- **连锁引爆协程使用 `WaitForSecondsRealtime`**（不受 Time.timeScale 影响）

## 6. 敌人系统
- 14种 + 4种Boss变体，由 SpriteFactory 运行时生成
- 出怪：1-2波Basic → 3-4波+Ranged/Fast → 5-7波+Tank/Thrower → 8+全种类
- Boss每5波出现，DOT抗性：EnemyDotResistance
- **精英词缀**：EliteModifierSystem，第5波起概率生成，8种子能力
- **SpawnManager** 波次管理：WaveConfigHelper(配置) + EnemyPrefabFactory(预制体) + SpawnManager(协调)
- **SpawnManager 使用 `WaitForSecondsRealtime`** 出怪（暂停时不冻结出怪流程）
- **SpawnManager 使用 `Time.unscaledTime`** 做清理计时器（暂停时仍清理死敌人）
- **CurseSpreadSystem**：敌人死亡时传播DOT，已用 SpatialGrid 加速

## 7. 关键文件索引

### 核心系统
| 文件 | 说明 |
|------|------|
| `Core/GameSceneBootstrap.cs` | 协调器：组件组装+生命周期 |
| `Core/GameDataLoader.cs` | 数据加载：角色/武器/技能/Mage配置 |
| `Core/GameStarter.cs` | 游戏启动：应用配置+预热池+开始游戏 |
| `Core/GameReferences.cs` | 全局引用缓存（6个懒缓存属性） |
| `Core/EventManager.cs` | 事件系统 |
| `Core/ObjectPool.cs` | 对象池（DontDestroyOnLoad） |
| `Core/PoolHelper.cs` | 池辅助（SpawnOrInstantiate/DespawnOrDestroy/RegisterVirtualPrefab） |
| `Core/GameStateResetter.cs` | 轻量重置（事件+静态状态+标记销毁，~40行） |
| `Core/GameManager.cs` | 状态机 |
| `Core/GameInputHandler.cs` | 统一输入 |
| `Core/SaveManager.cs` | 存档管理 |
| `Core/SpatialGrid.cs` | 空间分区加速范围查询（10单元格） |
| `Core/DebugHelper.cs` | Debug 日志工具 |
| `Core/OffScreenCuller.cs` | 屏幕外敌人优化 |

### 战斗系统
| 文件 | 说明 |
|------|------|
| `Combat/DotBulletFactory.cs` | DOT子弹工厂 |
| `Combat/DotBulletBase.cs` | DOT子弹基类 |
| `Combat/DotBulletHelpers.cs` | 工具方法 |
| `Combat/StatusEffects/StatusEffectSystem.cs` | DOT管理+引爆+视觉 |
| `Combat/StatusEffects/DotEffectRegistry.cs` | DOT效果组件统一注册表 |
| `Combat/StatusEffects/CurseSpreadSystem.cs` | 死亡时传播DOT |
| `Combat/StatusEffects/DotComboSystem.cs` | DOT 组合系统 |
| `Combat/CombatManager.cs` | 战斗管理（伤害/特效/DespawnAllDotBullets） |
| `Combat/FrostLightningField.cs` | 霜电冰场（元素反应，**已接 Config 信号刷新**） |

### DOT 子弹
| 文件 | 说明 |
|------|------|
| `Combat/PoisonBullet.cs` | 毒子弹 + PoisonPuddle + PoisonStackEffect + PoisonBurstTextTicker |
| `Combat/BurnBullet.cs` | 燃烧子弹 + BurnStackEffect |
| `Combat/FrostBullet.cs` | 霜冻子弹 + FrostEffect |
| `Combat/LightningBullet.cs` | 雷电子弹 + StaticStackEffect |
| `Combat/DarkBullet.cs` | 暗影子弹 + DarkMarkEffect |
| `Combat/LightBulletController.cs` | 光明蓄力激光 + LightMarkEffect |
| `Combat/WindBullet.cs` | 风蚀子弹 + WindErosionEffect |
| `Combat/HomingProjectile.cs` | 追踪子弹（DotHomingBullet 预挂在模板上） |

### 玩家系统
| 文件 | 说明 |
|------|------|
| `Player/MagePassive.cs` | DOT枪管理+属性（partial class, ~370行） |
| `Player/MagePassive.Firing.cs` | Update+子弹发射+视觉（partial class, ~160行） |
| `Player/DetonateSystem.cs` | 引爆系统（~359行，连锁使用 WaitForSecondsRealtime） |
| `Player/MageUpgradeApplier.cs` | 升级应用 |
| `Player/PlayerController.cs` | 玩家控制 |
| `Player/EvolutionSystem.cs` | 进化系统 |
| `Player/PlayerSkillManager.cs` | 技能管理 |
| `Player/PlayerLevelSystem.cs` | 等级系统（while 循环有 100 次安全上限） |

### 敌人系统
| 文件 | 说明 |
|------|------|
| `Enemies/SpawnManager.cs` | 波次管理（使用 WaitForSecondsRealtime + Time.unscaledTime） |
| `Enemies/EnemyPrefabFactory.cs` | 敌人工厂：预制体创建+池键映射 |
| `Enemies/WaveConfigHelper.cs` | 波次配置加载+难度倍率 |
| `Enemies/EnemyBase.cs` | 敌人基类 |
| `Enemies/EliteModifierSystem.cs` | 精英词缀（8种子能力） |
| `Enemies/BossEnemy.cs` | Boss 敌人 |
| `Enemies/BossFactory.cs` | Boss 工厂 |
| `Enemies/EnemyDotResistance.cs` | DOT 抗性 |
| `Enemies/EnemyDeathEffect.cs` | 死亡特效 |

### 配置 ScriptableObject
| 文件 | 说明 |
|------|------|
| `ScriptableObjects/Config/DotBulletConfig.cs` | DOT子弹配置（40+字段，中文化Tooltip，OnConfigChanged） |
| `ScriptableObjects/Config/EnemySpawnConfig.cs` | 敌人生成配置（40+字段，8个分组） |
| `ScriptableObjects/Config/MageUpgradeConfig.cs` | Mage升级配置 |
| `ScriptableObjects/Config/EnemyWaveConfig.cs` | 波次配置（S曲线难度、特殊波次） |
| `ScriptableObjects/Config/GameConfig.cs` | 全局游戏配置 |
| `ScriptableObjects/Characters/CharacterData.cs` | 角色数据 |

### UI
| 文件 | 说明 |
|------|------|
| `UI/DamagePopup.cs` | 伤害数字弹出（内部对象池, DontDestroyOnLoad, FullCleanup()） |
| `UI/EnemyHealthBar.cs` | 敌人血条 |
| `UI/LevelUpUI.cs` + `LevelUpOptionGenerator.cs` | 升级UI+选项生成 |
| `UI/GameOverUI.cs` | 游戏结束 |
| `UI/PauseMenuUI.cs` | 暂停菜单（**直接 LoadScene，不做 DestroyImmediate**） |
| `UI/WaveChallengeSystem.cs` | 波次挑战（**WaitForChallengeResolution 有 120 秒超时**） |
| `UI/DetonateHUD.cs` | 引爆 HUD |
| `UI/WaveIntermissionUI.cs` | 波次间歇 UI |
| `UI/BossHealthBarUI.cs` | Boss 血条 |

## 8. Editor 工具

| 菜单 | 文件 | 功能 |
|------|------|------|
| Mage → Validate Upgrade Config | `Assets/Editor/MageUpgradeValidator.cs` | 升级配置验证（300行，检查ID唯一性/数值范围/弃用检测） |
| Mage → Dot Bullet Config Editor | `Assets/Editor/DotBulletConfigEditor.cs` | 子弹参数可视化编辑器（400行，DPS曲线/参数表格/元素反应） |
| Mage → Create Dot Bullet Config Asset | `Assets/Editor/CreateDotBulletConfigAsset.cs` | 创建 DotBulletConfig.asset |
| Mage → Create Enemy Spawn Config Asset | `Assets/Editor/CreateEnemySpawnConfigAsset.cs` | 创建 EnemySpawnConfig.asset |
| Mage → Dot Bullet Config Inspector | `Assets/Editor/DotBulletConfigInspector.cs` | DotBulletConfig 自定义 Inspector（中文标签） |
| — | `Assets/Editor/BuildTool.cs` | 构建工具 |

## 9. 修改规范

**必须遵守**：
1. 所有伤害经 `CombatManager`，生命周期走 `ObjectPool`/`PoolHelper`
2. 全局引用经 `GameReferences`，事件经 `EventManager`
3. Debug 用 `DebugHelper`，状态重置在 `OnEnable()`
4. InputSystem 用 `Keyboard.current.xxxKey.wasPressedThisFrame`，禁止 `Input.GetKeyDown`
5. Config 使用 `OnConfigChanged` 信号刷新模式（OnValidate 触发）

**检查清单**：
- 修改 EventManager → 更新 ClearAll()
- 新增敌人 → 池键+SpawnManager+形状+特效
- 新增技能 → 继承 BaseSkill + SetSkillData()
- 新增 DOT 子弹 → 注册到 MagePassive + 更新 LevelUpUI + DotBulletFactory
- 返回菜单 → EventManager.ClearAll() + GameReferences.Reset() + LoadScene（**禁止 FullReset/DestroyImmediate**）

**Config OnConfigChanged 模式**（已完成全部 11 个组件的信号刷新）：
```csharp
// Config 文件中：
public static System.Action OnConfigChanged;
#if UNITY_EDITOR
private void OnValidate() { OnConfigChanged?.Invoke(); }
#endif

// 消费者组件中：
private void OnEnable() { DotBulletConfig.OnConfigChanged += RefreshFromConfig; }
private void OnDisable() { DotBulletConfig.OnConfigChanged -= RefreshFromConfig; }
private void RefreshFromConfig() { /* 从 GetDefault() 重新读取字段 */ }
```

**对象池模式**（所有 DOT 子弹使用）：
```csharp
// 在 Create() 静态方法中：
var pool = ObjectPool.Instance;
GameObject go = null;
if (pool != null && pool.HasPool(PoolHelper.DOT_xxx_BULLET))
    go = pool.Spawn(PoolHelper.DOT_xxx_BULLET, pos, Quaternion.identity);
else {
    PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_xxx_BULLET, BuildTemplate, N);
    go = pool != null ? pool.Spawn(PoolHelper.DOT_xxx_BULLET, pos, Quaternion.identity) : null;
}
if (go == null) { /* fallback: new GameObject */ }
```

**WaitForSecondsRealtime 使用规则**：
- 需要在暂停时继续运行的协程（出怪、波次间歇、连锁引爆）→ 使用 `WaitForSecondsRealtime`
- 暂停时应冻结的效果（DOT tick、击退、飘字）→ 使用 `WaitForSeconds`

## 10. 关键 Bug 注意 & 已知陷阱

**⚠️ 返回菜单卡死 — 核心禁忌**：
- **禁止从 `OnGUI()` 内调用 `SceneManager.LoadScene()`** — Unity 的 OnGUI 在同一帧被多次调用（Layout/Repaint），LoadScene 销毁旧场景后后续 OnGUI 操作已销毁对象导致卡死
- **禁止在返回菜单时使用 `DestroyImmediate`** — 会触发 OnDestroy 回调级联：单例 OnDestroy → 访问其他已销毁单例 → Singleton getter 自动重建新实例 → 创建 DontDestroyOnLoad 垃圾对象 → 后续 LoadScene 遇到状态不一致的残留对象死锁
- **正确做法**：只做数据清理（ClearAll/Reset），让 LoadScene 统一销毁

**DontDestroyOnLoad 相关**：
- ObjectPool 是 DontDestroyOnLoad，**不需要手动销毁**（LoadScene 后 Singleton getter 自动处理）
- **DamagePopup.FullCleanup()** 必须在重启时调用（非 ResetPool），否则 DontDestroyOnLoad 池父级永久残留
- Singleton 的 `Instance` getter 在 `_instance == null` 时会自动创建新实例 — **销毁单例时注意回调级联**

**敌人生命周期**：
- **SpawnManager.ForceDestroyAllEnemies** 必须用 DestroyImmediate（非 SetActive(false)），否则 zombie 敌人占池
- PoisonBullet/FrostBullet 必须有 _lifetime 超时销毁
- FrostEffect.OnEnable() 必须 RestoreSpeed + 重置状态

**DOT 效果**：
- DOT子弹命中必须调 DotBulletHelper.EnsureStatusEffectManager()
- **DotEffectRegistry.ClearAll()** 必须在重启时调用，否则残留 PoisonStackEffect 引用

**OnDisable 安全**：
- **PoisonBurstTextTicker.OnDisable** 和 **ReactionTextTicker.OnDisable** 和 **BurnSpreadTextTicker.OnDisable** 禁止 Destroy(gameObject)，否则 FullReset 禁用阶段级联崩溃
- OnDisable 中避免 Destroy/Instantiate

**协程安全**：
- **WaveChallengeSystem.WaitForChallengeResolution** 必须有超时（120秒），否则挑战 UI 未响应时永久卡死
- **PlayerLevelSystem** 的 `while (_currentExp >= ExpToLevelUp)` 循环必须有安全上限（100次）

## 11. Bug 修复记录

### 2026-06-10: 返回菜单卡死（多轮修复）
**症状**：ESC → Return to Menu → Unity 卡死（100% 或间歇性复现）
**根因链**：
1. `FullReset()` 使用 `DestroyImmediate` 销毁 DontDestroyOnLoad 单例
2. `DestroyImmediate` 触发 `OnDestroy` 回调 → 回调访问其他已销毁单例
3. `Singleton.Instance` getter 自动重建新实例（`FindAnyObjectByType` → `new GameObject` + `DontDestroyOnLoad`）
4. 创建了 DontDestroyOnLoad 垃圾对象，状态不一致
5. `LoadScene` 遇到残留对象 → 卡死
**修复**：
- `GameStateResetter` 重写：去掉所有 `DestroyImmediate`，只做数据清理 + `Destroy`（延迟销毁）
- `PauseMenuUI.ReturnToMenu()` 简化：去掉 `FullReset()`，直接 `EventManager.ClearAll()` + `GameReferences.Reset()` + `LoadScene()`
- 去掉从 `OnGUI()` 内调用 `LoadScene` 的方案（协程/DeferredSceneLoad 都有生命周期问题），改为直接调用

### 2026-06-10: 暂停后游戏冻结
**症状**：Unity Editor pause 后游戏系统全部冻结
**根因**：所有关键协程使用 `WaitForSeconds`（受 `Time.timeScale` 影响），暂停时 `Time.timeScale=0` 导致出怪/波次间歇/连锁引爆永久冻结
**修复文件**：SpawnManager.cs / DetonateSystem.cs / BossRushManager.cs
**关键变更**：
- SpawnManager: `WaitForSeconds` → `WaitForSecondsRealtime`（出怪+间歇），`Time.time` → `Time.unscaledTime`（清理计时器）
- DetonateSystem: `WaitForSeconds` → `WaitForSecondsRealtime`（连锁引爆）
- BossRushManager: `WaitForSeconds` → `WaitForSecondsRealtime`（Boss 生成延迟）
- WaveChallengeSystem: 添加 120 秒超时 + 自动拒绝
- PlayerLevelSystem: while 循环添加 100 次安全上限

### 2026-06-10: Config 实时信号刷新全部完成
**已完成信号刷新的组件**（11个）：
| 组件 | 刷新的字段 |
|------|-----------|
| PoisonPuddle | `_duration` (PoisonPuddleDuration) |
| BurnStackEffect | `_duration` (BurnDuration) |
| FrostLightningField | `_duration`, `_radius`, `_frostTickInterval`, `_slowPercent` |
| MagePassive.Firing | 光明子弹发射后立即蓄力（LightPostFireDelay） |
| **FrostEffect** | `_maxSlow`, `_baseSlow`, `_perStackSlow` |
| **StaticStackEffect** | `_baseInterval`, `_stackReduction`, `_minInterval`, `_stunDurationHit/Discharge/First`, `_maxStacks` |
| **WindErosionEffect** | `_knockbackDistance`, `_maxStacks` |
| **LightMarkEffect** | `_damagePerStack` |
| **DotBulletFactory** | 清除 `_config` 缓存 |
| **DarkMarkEffect** | `_spreadRadius`, `_spreadEfficiency` |
| **LightBulletController** | 全部 8 个蓄力/扫射/标记参数 |

### 2026-06-10: 毒子弹 DontDestroyOnLoad 泄漏 + 敌人消失 + 重启不生成
**症状**：DamagePopup 永久残留 DontDestroyOnLoad；敌人偶尔永久消失；重启后敌人不生成
**根因**：
1. DamagePopup 对象池使用 DontDestroyOnLoad 但 ResetPool() 从不销毁池父级
2. SpawnManager.ForceDestroyAllEnemies() 用 SetActive(false) 隐藏敌人而非 DestroyImmediate
3. DotEffectRegistry 静态集合在 FullReset 时未清空
4. PoisonBurstTextTicker.OnDisable 中 Destroy(gameObject) 触发级联销毁
**修复文件**：DamagePopup.cs / GameStateResetter.cs / SpawnManager.cs / PoisonBullet.cs

## 12. V2 优化任务完成状态

**全部 25 项已完成** ✅（详见 task2.md）

| 优先级 | 完成项数 | 关键优化 |
|--------|---------|---------|
| P0 性能 | 4/4 | DetonateSystem 遍历合并+GC消除、DOT组件OnEnable缓存、HomingProjectile预挂 |
| P1 代码质量 | 8/8 | BleedBullet清理、DotFusionSystem审计、DOT子弹迁移DotBulletBase、MagePassive属性POCO、WindErosion优化、元素反应配置化 |
| P2 可维护性 | 8/8 | DOT数值配置化、Glow回收、SpatialGrid空间分区、GetComponent缓存审计、50+测试、MageUpgradeValidator |
| P3 架构改进 | 5/5 | MagePassive partial class、DotHomingBullet策略字典、GameReferences懒缓存、DotEffectRegistry、DotBulletConfigEditor |
