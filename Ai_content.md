# 🧠 AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 窗口先读此文件。最后更新：2026-06-13

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
| `Damageable` | `Entities/Damageable.cs` | 伤害组件（float伤害/护甲/无敌帧/光明标记+黑暗标记受伤加深） |
| `EnemyBase` | `Enemies/EnemyBase.cs` | 敌人基类（移速/碰撞/LOD） |
| `ICharacterPassive` | `Player/ICharacterPassive.cs` | 角色被动接口（所有角色共享，替代 MagePassive 直接引用） |
| `IGunState` | `Combat/IGunState.cs` | 武器状态接口（DotGunState 实现，从 MagePassive 提取） |
| `IWeaponSystem` | `Combat/IWeaponSystem.cs` | 武器系统接口（Fire/OnUpgrade/GetDPS） |
| `IDetonatable` | `Combat/IDetonatable.cs` | 大招系统接口（CanDetonate/Detonate/GetCooldown） |
| `ICharacterConfig` | `ScriptableObjects/Config/ICharacterConfig.cs` | 角色配置接口（MageUpgradeConfig 实现） |
| `IEvolutionHandler` | `Player/IEvolutionHandler.cs` | 进化处理器接口 |
| `CharacterFactory` | `Player/CharacterFactory.cs` | 角色工厂（Register/Create，支持多角色） |
| `GameReferences` | `Core/GameReferences.cs` | 静态引用缓存，**新增 `CharacterPassive` 泛型属性** |
| `EventManager` | `Core/EventManager.cs` | 委托+泛型事件 `Subscribe<T>/Publish<T>` + 强类型事件结构体 |
| `ObjectPool` + `PoolHelper` | `Core/ObjectPool.cs` / `Core/PoolHelper.cs` | 所有敌人/弹幕/掉落物必须走对象池，**新增 SpawnOrFallback** |
| `PhysicsHelper` | `Core/Utils/PhysicsHelper.cs` | 零分配 Physics2D 查询（替代 OverlapCircleAll） |
| `DifficultyManager` | `Core/Managers/DifficultyManager.cs` | 10阶难度系统（解锁/倍率/存档） |
| `DamagePipeline` | `Combat/DamagePipeline.cs` | 伤害管线（Calculate/CalculateWithArmor） |
| `DotSpreadHelper` | `Combat/DotSpreadHelper.cs` | DOT 传播公共方法（DarkMarkEffect/CurseSpreadSystem 共用） |
| `CritParams` | `Combat/CritParams.cs` | 暴击参数结构体（替代10文件重复三字段） |
| `HitFlashEffect` | `Combat/HitFlashEffect.cs` | 敌人受击闪白效果 |
| `WaveAffixSystem` | `Combat/WaveAffixSystem.cs` | 波次词缀系统（6种负面词缀，难度6+解锁） |
| `TrainingDummy` | `Combat/TrainingDummy.cs` | DPS 测试木桩（20万HP，自动重生） |
| `DpsTracker` | `UI/Gameplay/DpsTracker.cs` | 实时 DPS 显示面板 |
| `DetonateWaveEffect` | `Combat/DetonateWaveEffect.cs` | 引爆冲击波（红色扩展圆环，时停期间扩散） |
| `EnvironmentZoneSpawner` | `Map/EnvironmentZoneSpawner.cs` | 环境区域生成器（毒沼/冰面/火地/圣光） |
| `LootDropSystem` | `Combat/LootDropSystem.cs` | 掉落系统（概率+品质+词缀） |
| `Backpack` | `Combat/Backpack.cs` | 背包系统（装备/分解） |
| `UpgradeTooltip` | `UI/Systems/UpgradeTooltip.cs` | 升级选项工具提示 |
| `TutorialSystem` | `UI/Systems/TutorialSystem.cs` | 新手引导（波次1/3/5/10/15/20教学） |
| `DynamicCamera` | `Combat/DynamicCamera.cs` | 动态相机（Boss聚焦/引爆缩放） |
| `MenuButtonFactory` | `Core/Bootstrap/MenuButtonFactory.cs` | 菜单按钮工厂（Test/Boss/DPS按钮） |
| `GameManager` | `Core/GameManager.cs` | 状态机 Menu→Playing→Paused→GameOver |
| `GameInputHandler` | `Core/GameInputHandler.cs` | 统一输入（InputSystem），WASD/鼠标/E引爆/F技能/Q切换/ESC暂停/Tab商店 |
| `GameStateResetter` | `Core/GameStateResetter.cs` | 轻量重置：事件清除+静态状态重置+标记单例延迟销毁 |

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
- 霜冻×燃烧→融化(1s, DOT伤害×2) — BurnBullet 检查 FrostEffect，消耗霜冻层，MeltEffect 组件

**关键文件**：
| 文件 | 作用 |
|------|------|
| `DotBulletFactory.cs` | DOT子弹工厂（SpawnOrFallback + CreateBullet\<T\> 泛型方法） |
| `DotBulletBase.cs` | DOT子弹基类（速度/生命周期/穿透/反弹） |
| `DotBulletConfig.cs` | DOT子弹配置 ScriptableObject（50+字段，已中文化Tooltip，OnConfigChanged 信号） |
| `DotBulletHelpers.cs` | 工具方法（EnsureStatusEffectManager/GlowReturnHelper） |
| `DotEffectRegistry.cs` | DOT效果统一注册表（4个 HashSet: Burn/Poison/Frost/Static） |
| `DotColorBlender.cs` | DOT 视觉颜色混合 |

## 4. Mage 升级系统（30种）
- **实现链**：`MageUpgradeConfig`(配置) → `MagePassive`(属性, partial class) → `MageUpgradeApplier`(策略字典)
- **MagePassive.cs**（属性+DOT枪管理）+ **MagePassive.Firing.cs**（Update+子弹发射+视觉）
- **升级分类**：子弹(7) + DOT增强(3) + 引爆增强(2) + 子弹增强(3) + 协同(5) + 一般强化(10)
- **一般强化**：移速/护甲/HP/暴击率/暴击伤害/磁力/回复/弹速/击退/弹体（全角色通用）
- **已删除**：饱和/元素引爆/吸血法术/连锁反应/共鸣/相位移动/灵魂虹吸/余烬强化/湮灭领域/诅咒/腐化之触/元素风暴/暗影链接/弹药精通/元素亲和
- **联动**：StatusEffectSystem / DetonateSystem / CurseSpreadSystem / DotComboSystem

## 5. 引爆系统
- `DetonateSystem.cs` — 蓄力/冲击波/时停/连锁/余烬
- 引爆流程：按E蓄力 → 屏幕晃动0.5s → Time.timeScale=0 → 红色冲击波扩展 → 接触敌人触发引爆伤害 → 时停结束显示DETONATE总伤害 → 后处理（连锁/余烬/霜爆）
- `DetonateWaveEffect.cs` — 红色扩展圆环，PhysicsHelper零分配检测，HashSet确保每敌只触发一次
- 引爆参数全部可配置（`DotEffectConfig` 的引爆设置章节，26个字段）
- 已用 **SpatialGrid** 空间分区加速范围查询（10单元格，覆盖-200~+200）
- TryChainDetonate 使用类级缓存列表消除 GC alloc
- 输入：E键蓄力（Keyboard API），移速惩罚可配置

## 6. 敌人系统
- 14种 + 4种Boss变体，由 SpriteFactory 运行时生成
- 出怪：1-2波Basic → 3-4波+Ranged/Fast → 5-7波+Tank/Thrower → 8+全种类
- Boss每5波出现，DOT抗性：EnemyDotResistance
- **精英词缀**：EliteModifierSystem，第5波起概率生成，15种子能力（含磁力/重力/瘟疫）
- **敌人AI**：EnemyAIBehavior 4种行为（追踪/逃跑/冲锋/环绕）
- **每波护甲+1**：EnemyScalingHelper.ApplyScaling 接收 currentWave 参数
- **护甲公式**：混合减伤（固定减伤上限50% + 百分比减伤递减收益）
- **SpawnManager** 波次管理：WaveConfigHelper(配置) + EnemyPrefabFactory(预制体) + SpawnManager(协调)
- **SpawnManager 使用 `WaitForSecondsRealtime`** 出怪（暂停时不冻结出怪流程）
- **SpawnManager 使用 `Time.unscaledTime`** 做清理计时器（暂停时仍清理死敌人）
- **CurseSpreadSystem**：敌人死亡时传播DOT，已用 SpatialGrid 加速

## 7. 关键文件索引

### 核心系统
| 文件 | 说明 |
|------|------|
| `Core/Bootstrap/GameSceneBootstrap.cs` | 协调器：组件组装+生命周期+难度选择+DPS测试 |
| `Core/Managers/GameDataLoader.cs` | 数据加载：角色/武器/技能/Mage配置 |
| `Core/Bootstrap/GameStarter.cs` | 游戏启动：CharacterFactory创建角色+预热池+开始游戏 |
| `Core/Managers/GameReferences.cs` | 全局引用缓存（CharacterPassive/MagePassive/DetonateSystem等） |
| `Core/Managers/EventManager.cs` | 事件系统+强类型事件结构体（DamageEvent等） |
| `Core/Managers/ObjectPool.cs` | 对象池（DontDestroyOnLoad） |
| `Core/Utils/PoolHelper.cs` | 池辅助（SpawnOrFallback/SpawnOrInstantiate/DespawnOrDestroy） |
| `Core/Utils/PhysicsHelper.cs` | 零分配 Physics2D 查询 |
| `Core/Managers/DifficultyManager.cs` | 10阶难度系统（解锁/倍率/存档） |
| `Core/Managers/SaveManager.cs` | 存档管理（RotateBackups备份机制） |
| `Core/Utils/SpatialGrid.cs` | 空间分区加速范围查询（10单元格） |
| `Core/Bootstrap/MenuButtonFactory.cs` | 菜单按钮工厂（Test/Boss/DPS） |
| `Core/GameStateResetter.cs` | 轻量重置（事件+静态状态+标记销毁） |

### 战斗系统
| 文件 | 说明 |
|------|------|
| `Combat/DotBulletFactory.cs` | DOT子弹工厂（SpawnOrFallback + CreateBullet\<T\>） |
| `Combat/DotBulletBase.cs` | DOT子弹基类（速度/生命周期/穿透/反弹） |
| `Combat/DotBulletHelpers.cs` | 工具方法（EnsureStatusEffectManager） |
| `Combat/StatusEffects/StatusEffectSystem.cs` | DOT管理+引爆+视觉 |
| `Combat/StatusEffects/DotEffectRegistry.cs` | DOT效果组件统一注册表（StackEffectBase + IStackEffect） |
| `Combat/StatusEffects/CurseSpreadSystem.cs` | 死亡时传播DOT |
| `Combat/DetonateWaveEffect.cs` | 引爆冲击波（红色扩展圆环+时停） |
| `Combat/DetonateSubEffects.cs` | 引爆子效果（余烬+霜爆） |
| `Combat/DotSpreadHelper.cs` | DOT传播公共方法 |
| `Combat/CritParams.cs` | 暴击参数结构体 |
| `Combat/WaveAffixSystem.cs` | 波次词缀系统（6种） |
| `Combat/HitFlashEffect.cs` | 受击闪白 |
| `Combat/DamagePipeline.cs` | 伤害管线 |
| `Combat/IGunState.cs` | 武器状态接口 + DotGunState |
| `Combat/IWeaponSystem.cs` | 武器系统接口 |
| `Combat/IDetonatable.cs` | 大招接口 |
| `Combat/IEquipment.cs` | 装备接口+稀有度+词缀 |
| `Combat/LootDropSystem.cs` | 掉落系统 |
| `Combat/Backpack.cs` | 背包系统 |
| `Combat/TrainingDummy.cs` | DPS测试木桩 |
| `Combat/CombatColorTheme.cs` | 战斗颜色常量 |

### DOT 子弹
| 文件 | 说明 |
|------|------|
| `Combat/PoisonBullet.cs` | 毒子弹 |
| `Combat/PoisonStackEffect.cs` | 毒叠加效果 |
| `Combat/PoisonPuddle.cs` | 毒液池 |
| `Combat/BurnBullet.cs` | 燃烧子弹 |
| `Combat/BurnStackEffect.cs` | 燃烧叠加效果 + 融化反应触发 |
| `Combat/FrostBullet.cs` | 霜冻子弹 |
| `Combat/FrostEffect.cs` | 霜冻效果 |
| `Combat/LightningBullet.cs` | 雷电子弹 |
| `Combat/StaticStackEffect.cs` | 静电叠加效果 |
| `Combat/DarkBullet.cs` | 暗影子弹 |
| `Combat/DarkMarkEffect.cs` | 黑暗标记效果（叠层+传播） |
| `Combat/LightBulletController.cs` | 光明蓄力激光 |
| `Combat/LightMarkEffect.cs` | 光明标记效果 |
| `Combat/WindBullet.cs` | 风蚀子弹（固定3发0.5s） |
| `Combat/WindErosionEffect.cs` | 风化叠加效果 |
| `Combat/WindErosionVortex.cs` | 风蚀漩涡 |
| `Combat/PenetrateHandler.cs` | 穿透处理器 |
| `Combat/RicochetHandler.cs` | 反弹处理器 |
| `Combat/DotHomingBullet.cs` | 追踪弹桥接组件 |
| `Combat/HomingProjectile.cs` | 追踪子弹 |

### 玩家系统
| 文件 | 说明 |
|------|------|
| `Player/MagePassive.cs` | DOT枪管理+属性（partial class）+ ICharacterPassive 实现 |
| `Player/MagePassive.Firing.cs` | Update+子弹发射+视觉（风子弹固定3发0.5s） |
| `Player/DetonateSystem.cs` | 引爆系统（冲击波+时停+连锁） |
| `Player/MageUpgradeApplier.cs` | 升级应用（策略字典，30种升级） |
| `Player/ICharacterPassive.cs` | 角色被动接口 |
| `Player/CharacterFactory.cs` | 角色工厂 |
| `Player/IEvolutionHandler.cs` | 进化处理器接口 |
| `Player/EvolutionSystem.cs` | 进化系统（优先分发到 IEvolutionHandler） |

### 敌人系统
| 文件 | 说明 |
|------|------|
| `Enemies/SpawnManager.cs` | 波次管理（WaitForSecondsRealtime + 每波护甲+1） |
| `Enemies/EnemyScalingHelper.cs` | 敌人属性缩放（HP/速度/护甲×波次） |
| `Enemies/EnemyPrefabFactory.cs` | 敌人工厂 |
| `Enemies/EnemyBase.cs` | 敌人基类 |
| `Enemies/EliteModifierSystem.cs` | 精英词缀（15种） |
| `Enemies/EnemyAIBehavior.cs` | 敌人AI行为（追踪/逃跑/冲锋/环绕） |
| `Enemies/BossEnemy.cs` | Boss 敌人 |
| `Enemies/BossPhaseHelper.cs` | Boss 阶段逻辑（提取自 BossEnemy） |
| `Enemies/BossFactory.cs` | Boss 工厂 |

### 配置 ScriptableObject
| 文件 | 说明 |
|------|------|
| `ScriptableObjects/Config/DotEffectConfig.cs` | DOT效果配置（50+字段，中文化Tooltip，OnConfigChanged） |
| `ScriptableObjects/Config/EnemySpawnConfig.cs` | 敌人生成配置（40+字段，8个分组） |
| `ScriptableObjects/Config/MageUpgradeConfig.cs` | Mage升级配置（30种：7子弹+13专属+10一般） |
| `ScriptableObjects/Config/DifficultyConfig.cs` | 难度配置（10阶：敌人倍率+奖励+特殊规则） |
| `ScriptableObjects/Config/GameConfig.cs` | 全局游戏配置（含DPS测试木桩参数） |
| `ScriptableObjects/Config/ICharacterConfig.cs` | 角色配置接口 |
| `ScriptableObjects/Config/CharacterConfigLoader.cs` | 角色配置加载器 |
| `ScriptableObjects/Characters/CharacterData.cs` | 角色数据 |

### UI
| 文件 | 说明 |
|------|------|
| `UI/Gameplay/DamagePopup.cs` | 伤害数字弹出（内部对象池, float伤害, F2格式） |
| `UI/Gameplay/EnemyHealthBar.cs` | 敌人血条（EnemyUIRoot统一缩放） |
| `UI/Gameplay/DotStatusBar.cs` | DOT叠层状态栏（图标+xN，每行最多2个） |
| `UI/Gameplay/DpsTracker.cs` | DPS实时追踪面板 |
| `UI/Systems/LevelUpUI.cs` + `LevelUpOptionGenerator.cs` | 升级UI+选项生成 |
| `UI/Systems/DifficultySelectUI.cs` | 难度选择UI（10阶+确认按钮） |
| `UI/Systems/TestBulletSelectUI.cs` | 测试模式选择UI（子弹+一般强化+专属强化） |
| `UI/Systems/TutorialSystem.cs` | 新手引导 |
| `UI/Systems/UpgradeTooltip.cs` | 升级工具提示 |
| `UI/Menus/PauseMenuUI.cs` | 暂停菜单 |
| `UI/Menus/GameOverUI.cs` | 游戏结束 |

### Map
| 文件 | 说明 |
|------|------|
| `Map/EnvironmentZoneSpawner.cs` | 环境区域生成器 |
| `Map/EnvironmentZone.cs` | 环境区域组件 |

### Editor 工具
| 菜单 | 文件 | 功能 |
|------|------|------|
| Mage → Validate Upgrade Config | `Editor/MageUpgradeValidator.cs` | 升级配置验证 |
| Mage → Dot Effect Config Editor | `Editor/DotEffectConfigEditor.cs` | 子弹参数可视化编辑器 |
| Mage → Create Dot Effect Config Asset | `Editor/CreateDotEffectConfigAsset.cs` | 创建 DotEffectConfig.asset |
| Mage → Create MageUpgradeConfig Asset | `Editor/CreateMageUpgradeConfigAsset.cs` | 创建 MageUpgradeConfig.asset |
| Mage → Create Enemy Spawn Config Asset | `Editor/CreateEnemySpawnConfigAsset.cs` | 创建 EnemySpawnConfig.asset |
| Mage → Dot Effect Config Inspector | `Editor/DotEffectConfigInspector.cs` | 自定义 Inspector（中文标签+引爆设置） |
| Mage → Generate DOT Icons | `Editor/DotIconGenerator.cs` | 预烘焙 DOT 图标 Sprite |

## 9. 修改规范

**必须遵守**：
1. 所有伤害经 `CombatManager`，生命周期走 `ObjectPool`/`PoolHelper`
2. 全局引用经 `GameReferences`，事件经 `EventManager`
3. Debug 用 `DebugHelper`，状态重置在 `OnEnable()`
4. InputSystem 用 `Keyboard.current.xxxKey.wasPressedThisFrame`，禁止 `Input.GetKeyDown`
5. Config 使用 `OnConfigChanged` 信号刷新模式（OnValidate 触发）
6. **每次修改完代码后，必须在 `Assets/Tests/Editor/AutomatedPlayModeTests.cs` 中添加对应的自动化测试用例**（见下方测试规范）

**检查清单**：
- 修改 EventManager → 更新 ClearAll()
- 新增敌人 → 池键+SpawnManager+形状+特效
- 新增技能 → 继承 BaseSkill + SetSkillData()
- 新增 DOT 子弹 → 注册到 MagePassive + 更新 LevelUpUI + DotBulletFactory
- 返回菜单 → EventManager.ClearAll() + GameReferences.Reset() + LoadScene（**禁止 FullReset/DestroyImmediate**）
- **修改任何代码 → 添加/更新对应的测试用例**

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

**对象池模式**（所有 DOT 子弹使用 PoolHelper.SpawnOrFallback）：
```csharp
var go = PoolHelper.SpawnOrFallback(poolKey, BuildTemplate,
    () => { /* fallback: new GameObject */ }, pos);
var b = go.GetComponent<XXXBullet>();
if (b == null) b = go.AddComponent<XXXBullet>();
b.Setup(...);
b.SetDirection(dir);
return b;
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

### 2026-06-13: 代码结构大规模重构
**P0~P3 全部完成**（详见 code.md）：
- PhysicsHelper 零分配迁移（19处）
- GUIStyle 缓存（12个文件 ~60处）
- GetComponent 移到 Awake（3个文件）
- CritParams 结构体（替代10文件重复三字段）
- MenuSceneBootstrap 拆分（465→346行，提取 MenuButtonFactory）
- DotSpreadHelper DOT传播去重
- 命名修复（12处 public _xxx → public xxx）

### 2026-06-13: 角色框架抽象层建立
- `ICharacterPassive` 接口（替代18个文件 MagePassive 直接引用）
- `IGunState` 接口（DotGunState 从 MagePassive 提取）
- `ICharacterConfig` 接口（MageUpgradeConfig 实现）
- `IEvolutionHandler` 接口（EvolutionSystem 优先分发）
- `CharacterFactory` 角色工厂
- `GameReferences.CharacterPassive` 泛型属性
- `DetonateSystem` 改用 `ICharacterPassive`（20处引用替换）

### 2026-06-13: 难度系统
- `DifficultyConfig` ScriptableObject（10阶参数）
- `DifficultyManager` 静态管理器（选择/倍率/解锁/存档）
- `DifficultySelectUI` 难度选择界面（点击+Enter确认）
- `WaveAffixSystem` 波次词缀（6种负面词缀，难度6+）
- SpawnManager 注入难度倍率 + 每波护甲+1
- GameSceneBootstrap 正常模式先显示难度选择

### 2026-06-13: DPS测试模式
- 主菜单按 G 进入 DPS Test
- 屏幕正中生成不动Boss（20万HP，GameConfig可调）
- DpsTracker 实时显示DPS/峰值/总伤/命中数
- R键重置统计，Tab隐藏面板

### 2026-06-13: 引爆系统重写
- 引爆改为冲击波扩散模式（红色圆环从玩家扩展）
- 时停机制（Time.timeScale=0，冲击波用 unscaledDeltaTime）
- 总伤害数字在冲击波结束时立刻显示
- 冲击波参数全部可配置（DotEffectConfig 引爆设置章节，26字段）

### 2026-06-13: 伤害float化
- `Damageable.TakeDamage` 改为 float 参数
- `DamagePopup` 所有 Create 方法改为 float + F2 格式
- `EventManager.OnDamage` 改为 Action<GameObject, float, Vector3>
- `StatusEffectManager.Detonate` 返回 float
- DOT tick 去除 Mathf.RoundToInt

### 2026-06-13: 护甲公式重做
- 混合减伤：固定减伤（上限50%）+ 百分比减伤（递减收益）
- 公式：`actualDamage = Max(0.01, (damage - Min(armor, damage×0.5)) × (1 - armor/(armor+100)))`
- 每波敌人+1护甲

### 2026-06-13: 强化系统精简
- 删除15种强化（饱和/元素引爆/吸血法术/连锁反应/共鸣/相位移动/灵魂虹吸/余烬强化/湮灭领域/诅咒/腐化之触/元素风暴/暗影链接/弹药精通/元素亲和）
- 新增10种一般强化（移速/护甲/HP/暴击率/暴击伤害/磁力/回复/弹速/击退/弹体）
- 修复腐蚀/凋零强化无效果的bug
- 子弹>5变追踪效果已删除

### 2026-06-13: Unity包依赖修复
- manifest.json 添加 com.unity.ugui + com.unity.textmeshpro + com.unity.test-framework
- InputSystem 使用本地 tgz 路径
- **注意：不要删除 Library 文件夹，用 Ctrl+R 强制重编译**

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

## 13. 自动化测试规范

**规则：每次修改代码后，必须在 `Assets/Tests/Editor/AutomatedPlayModeTests.cs` 中添加/更新对应的测试用例。**

### 测试文件

| 文件 | 说明 |
|------|------|
| `Assets/Tests/Editor/CoreSystemTests.cs` | EditMode 单元测试（纯计算、无场景依赖） |
| `Assets/Tests/Editor/AutomatedPlayModeTests.cs` | PlayMode 集成测试（创建 GameObject、验证组件交互） |
| `Assets/Tests/Editor/Tests.Editor.asmdef` | 测试程序集定义（引用 Gameplay） |

### 测试运行方式

Unity 中：`Window → General → Test Runner → PlayMode → Run All`

### 测试分类规范

| 分类 | 前缀 | 说明 | 示例 |
|------|------|------|------|
| P0 启动 | `P0_` | Config 加载、工厂注册、引用存在 | `P0_BurnBullet_Create_ReturnsNotNull` |
| P0 核心 | `P0_` | 子弹创建、贯穿、伤害数字 | `P0_PenetrateHandler_Setup_ResetsState` |
| P1 系统 | `P1_` | 难度、精英、Boss | `P1_DifficultyManager_InitializesCorrectly` |
| P2 新功能 | `P2_` | 装备、环境、UI | `P2_LootDropSystem_DropsEquipment` |
| P3 接口 | `P3_` | 接口一致性、无泄漏 | `P3_MagePassive_ImplementsAllInterfaces` |

### 测试编写规则

1. **每个新增/修改的公共方法** → 至少 1 个测试
2. **每个新组件** → 验证 `Create`/`Init` 不返回 null
3. **每个新接口实现** → 验证接口方法存在
4. **每个 Config 字段** → 验证默认值在合理范围
5. **每个 Bug 修复** → 添加回归测试（修复前会失败的用例）
6. **使用 `Assert.Ignore()`** 跳过依赖场景的测试（如无 Player 时）
7. **使用 `Object.DestroyImmediate()`** 清理测试创建的临时对象
8. **浮点比较** 使用 tolerance：`Assert.AreEqual(expected, actual, 0.001f)`
9. **测试命名**：`{分类}_{被测系统}_{场景}_{期望结果}`
