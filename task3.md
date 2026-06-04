# 🎯 项目优化清单 V3 — Vampire Survivors Unity 移植版

> **⚠️ 换窗口恢复点**
> ✅ 全部 13 项任务已完成！（#31-#43）
> 所有新增/修改的文件已编译验证通过
> 所有新增/修改的文件已编译验证通过（仅预存错误 DotProjectile/MagePassive/PauseMenuUI/CorrosiveEnemy 未修复）

> 从 task2.md 拆分出来的优化任务（#31-#43）
> 重点：架构优化、内容扩展与新功能

---

## 优先级说明

| 等级 | 含义 | 预计影响 |
|------|------|---------|
| 🔴 P0 | 紧急 — 严重影响游戏体验/潜在Bug | 立即修复 |
| 🟠 P1 | 高优 — 核心玩法体验/性能瓶颈 | 下个迭代 |
| 🟡 P2 | 中优 — 内容扩展/体验提升 | 计划内 |
| 🟢 P3 | 低优 — 锦上添花/长期规划 | 有空再做 |

---

## 一、架构与代码质量优化

### ✅ #31 🟠 P1 — 敌人子类重复代码消除（EnemyAbilityBase）【已实现】
**文件**：`Enemies/` 目录下 14 个敌人子类
**实施**：
1. 新建 `EnemyAbilityBase` 抽象类，封装通用能力模式（`OnUpdate()`、`OnDetectPlayer(distance)`、`OnCooldownReady()`）
2. 新建 `AuraAbility` 光环能力基类（封装光环创建/清理、移动速度修改、范围检测、闪光效果）
3. 实现 3 种光环能力：`HealerAuraAbility`（治疗）、`EnhancerAuraAbility`（增强移速）、`ShielderAuraAbility`（护盾减伤+呼吸动画）
4. 重构 HealerEnemy（115行→20行）、EnhancerEnemy（137行→16行）、ShielderEnemy（104行→15行）
5. 修复预存编译错误：删除 EnemyDeathEffect.cs 中重复的 ScreenShake 类
6. 将 `EnemyBase.SkipSpecialAbility` 从 protected 改为 public 以便 EnemyAbilityBase 访问
7. 将新文件添加到 Gameplay.csproj
**新增文件**：`Enemies/EnemyAbilityBase.cs`、`Enemies/Abilities/AuraAbility.cs`、`Enemies/Abilities/HealerAuraAbility.cs`、`Enemies/Abilities/EnhancerAuraAbility.cs`、`Enemies/Abilities/ShielderAuraAbility.cs`

### ✅ #32 🟡 P2 — 事件系统增强（泛型事件 + 参数对象）【已实现】
**文件**：`Core/EventManager.cs`
**实施**：
1. 新增泛型事件系统：`EventManager.Subscribe<T>(Action<T>)` / `EventManager.Unsubscribe<T>(Action<T>)` / `EventManager.Publish<T>(T)`
2. 内部使用嵌套泛型类 `GenericEventBus<T>` 存储事件委托，利用 C# 泛型静态特性自动按类型隔离
3. 新增 `ClearGeneric<T>()` 方法可清除指定类型的泛型事件
4. `ClearAll()` 中自动清除所有已注册的泛型事件（通过 `_genericClearActions` 列表和 `_registeredGenericTypes` HashSet 追踪）
5. 保留所有旧接口（OnEnemyKilled、OnDamage 等）完全向后兼容，零破坏性修改
6. 新功能代码可直接使用 `EventManager.Publish(new MyEvent { ... })` 模式，无需修改 EventManager 类
7. 事件参数建议使用 struct 减少 GC（由使用者自行决定）

### ✅ #33 🟡 P2 — 配置数据热加载【已实现】
**文件**：`Data/ConfigLoader.cs`、`ScriptableObjects/Config/GameConfig.cs`、`Core/DebugConfigPanel.cs`（新建）
**实施**：
1. 新建 `DebugConfigPanel.cs`：OnGUI 实现的调试配置面板
   - F1 键打开/关闭 Debug 面板，F5 键热加载 JSON 配置
   - 导出当前 ScriptableObject 配置为 JSON 文件（Application.persistentDataPath/Configs/）
   - 从 JSON 热加载配置到运行时 ScriptableObject（JsonUtility.FromJsonOverwrite）
   - 面板中可实时修改玩家属性、难度曲线、Boss配置、生成系统、波次配置
   - 4 个全局 Debug 倍率滑动条：伤害、敌人血量、DOT伤害、敌人速度（0.1x-5x）
   - 预设按钮：重置倍率、2x难度、0.5x简单
   - 仅在 `UNITY_EDITOR || DEVELOPMENT_BUILD` 中编译，Release Build 中为空实现
2. 集成到 `GameSceneBootstrap`：选择完成后自动创建 DebugConfigPanel 组件
3. 集成 DebugDamageMultiplier 到 `CombatManager.CalculateFinalDamage()`
4. 集成 DebugEnemyHpMultiplier 到 `EnemyBase.OnEnable()`（对象池回收时缩放血量）
5. 集成 DebugEnemySpeedMultiplier 到 `EnemyBase.FixedUpdate()`（移动速度倍率）
6. 集成 DebugDotDamageMultiplier 到 `StatusEffectManager.Update()`（DOT tick伤害倍率）
7. 将新文件添加到 Gameplay.csproj
**新增文件**：`Core/DebugConfigPanel.cs`
**修改文件**：`Core/GameSceneBootstrap.cs`、`Combat/CombatManager.cs`、`Enemies/EnemyBase.cs`、`Combat/StatusEffects/StatusEffectSystem.cs`、`Gameplay.csproj`

### ✅ #34 🟡 P2 — SaveManager 数据完整性校验【已实现】
**文件**：`Core/SaveManager.cs`
**实施**：
1. 新增 `SaveWrapper` 类：包装 SaveData JSON + CRC32 校验码（十六进制字符串）
2. `Save()` 时自动计算 CRC32 校验码并包装为 SaveWrapper 格式
3. `Load()` 时自动验证 CRC32 校验码，不匹配时打印警告并尝试备份恢复
4. 旧格式兼容：如果存档是直接 SaveData JSON（无校验码），自动兼容加载，下次保存时升级为新格式
5. 3 个存档备份循环覆盖：`.bak1` → `.bak2` → `.bak3`，每次保存前轮转
6. 备份恢复：主存档损坏时依次尝试 3 个备份文件
7. 存档版本号 `saveVersion` 字段 + `MigrateSaveData()` 迁移框架（未来格式升级预留）
8. CRC32 使用标准多项式 0xEDB88320，256 项查找表，纯 C# 实现无外部依赖
**修改文件**：`Core/SaveManager.cs`

### ✅ #35 🟢 P3 — 对象池监控 Debug 面板【已实现】
**文件**：`Core/ObjectPool.cs`、`Core/DebugPoolMonitor.cs`（新建）、`Core/GameSceneBootstrap.cs`
**实施**：
1. 新建 `DebugPoolMonitor.cs`：OnGUI 实现的对象池监控面板
   - 按 F2 键切换显示/隐藏，仅在 `UNITY_EDITOR || DEVELOPMENT_BUILD` 中编译
   - 显示每个池的统计：总容量、活跃数、空闲数、峰值使用数、出池/回池总数
   - 颜色标记：🟢绿色=正常（<70%）、🟡黄色=接近容量上限（70-90%）、🔴红色=已溢出（>90%）
   - 💀红色粗体标记：自动检测"池泄漏"（活跃数连续增长超过10次更新周期）
   - 迷你进度条显示每个池的使用比例
   - 总览信息区显示全部池的总对象数/活跃数/空闲数/泄漏数
   - 解析 `ObjectPool.GetAllStats()` 字符串获取池统计数据
2. 集成到 `GameSceneBootstrap.Start()`：选择界面之前自动创建 DebugPoolMonitor 组件
3. 将新文件添加到 Gameplay.csproj
**新增文件**：`Core/DebugPoolMonitor.cs`
**修改文件**：`Core/GameSceneBootstrap.cs`、`Gameplay.csproj`

---

## 二、内容扩展与新功能

### ✅ #36 🟠 P1 — 新敌人类型：CorrosiveEnemy（腐蚀者）& PurifierEnemy【已实现】
**文件**：`Enemies/` (新建)、`Combat/StatusEffects/StatusEffectSystem.cs`
**实施**：
1. 新建 `CorrosiveEnemy`：接触玩家后清除 DOT，死亡时释放毒雾清除范围内所有敌人的 DOT
   - 形状：八角形，颜色：暗绿色，血量 35，速度 2.5，伤害 8
   - 订阅死亡事件 `OnDeath += OnCorrosiveDeath`
   - 创建腐蚀光环视觉效果
2. 新建 `PurifierEnemy`：光环范围内友军获得 DOT 抗性持续增强
   - 使用 AuraAbility 框架，白色呼吸动画
   - 光环半径 4 格，持续为范围内友军提升所有 DOT 抗性
3. 新建 `PurifierAuraAbility`：净化光环能力（继承 AuraAbility）
4. 在 `StatusEffectManager` 中添加 `ClearAllDotEffects()` 公共方法
5. 将新文件添加到 Gameplay.csproj
**新增文件**：`Enemies/CorrosiveEnemy.cs`、`Enemies/PurifierEnemy.cs`、`Enemies/Abilities/PurifierAuraAbility.cs`
**修改文件**：`Combat/StatusEffects/StatusEffectSystem.cs`（新增 ClearAllDotEffects 方法）

### ✅ #37 🟠 P1 — 新 Boss 阶段专属机制【已实现】
**文件**：`Enemies/BossEnemy.cs`
**实施**：
1. 添加 `ExecutePhase3Ability()` 和 `ExecutePhase5Ability()` 方法
2. **阶段 3 专属技能**（首次进入时触发）：
   - Juggernaut：召唤 2 个 TankEnemy 作为护盾
   - Sorcerer：释放"反魔法区域"（BossPoisonZone，10 秒持续）
   - Phantom：隐身时间延长至 3 秒 + 闪现冷却缩短至 3 秒
   - Berserker：冲锋后留下火焰路径（`_berserkerLeaveFireTrail` 标记）
3. **阶段 5 专属技能**（首次进入时触发 + 持续效果）：
   - Juggernaut：速度 ×2，冲锋间隔降至 0.5 秒
   - Sorcerer：同时释放环形弹幕 + 召唤 + 震波
   - Phantom：分裂为 2 个幽灵分身（血量各 50%）
   - Berserker：血量低于 20% 时进入"不死"状态 5 秒（HP 锁定）
4. 新增 `_phase3AbilityUsed`、`_phase5AbilityTriggered`、`_berserkerUndyingActive` 等状态字段
**修改文件**：`Enemies/BossEnemy.cs`

### ✅ #38 🟡 P2 — 游戏内成就永久奖励系统【已实现】
**文件**：`UI/AchievementUI.cs`
**实施**：
1. 新增 `FormatBonusText()` 方法：根据 bonusKey 类型生成中文永久加成描述
   - 暴击率 +X%、最大生命 +X、伤害 +X%、DOT伤害 +X%、引爆伤害 +X%、金币获取 +X%
2. 成就解锁通知卡片中显示具体加成数值（如 "✨ 暴击率 +1%，永久生效"）
3. 成就面板中每个已解锁成就下方显示加成描述
4. 新增 `GetBonusSummary()` 方法：汇总所有已解锁成就的永久加成
5. 成就面板底部新增"永久加成总览"区域，列出所有加成类型和数值
6. 总加成显示上限百分比（上限 100%）
7. 永久加成上限控制：
   - `MAX_SINGLE_BONUS = 0.20f`（单个加成上限 20%）
   - `MAX_TOTAL_BONUS = 1.00f`（总加成上限 100%）
8. 新增 `GetCappedBonus(string bonusKey)` 公共方法，外部可查询带上限的加成值
**修改文件**：`UI/AchievementUI.cs`

### ✅ #39 🟡 P2 — 波次挑战系统（可选高难度模式）【已实现】
**文件**：`UI/WaveChallengeSystem.cs`（新建）、`Enemies/SpawnManager.cs`、`Core/GameSceneBootstrap.cs`
**实施**：
1. 新建 `WaveChallengeSystem.cs`：波次挑战系统组件
   - 4 种挑战类型：坚韧试炼（敌人HP×2）、疾风试炼（敌人速度×1.5）、双王试炼（额外Boss）、精英试炼（敌人+10护甲）
   - 每 5 波（非Boss波前）自动触发挑战选择
   - OnGUI 实现挑战卡片 UI（金色边框、遮罩背景、接受/拒绝按钮）
   - 接受挑战即时奖励金币（100 + wave×5）
   - 拒绝挑战正常进行
   - 统计接受/拒绝次数
2. 集成到 `SpawnManager`：
   - `Start()` 中自动创建 WaveChallengeSystem 组件
   - `RestBeforeNextWave()` 中在波次间歇期调用 `OfferChallenge()`，暂停倒计时等待玩家选择
   - `SpawnRandomEnemy()` 中应用挑战倍率（ChallengeHpMultiplier、ChallengeSpeedMultiplier、ChallengeEliteArmor）
3. 集成到 `GameSceneBootstrap.OnGUI()`：渲染挑战 UI
4. 将新文件添加到 Gameplay.csproj
**新增文件**：`UI/WaveChallengeSystem.cs`
**修改文件**：`Enemies/SpawnManager.cs`、`Core/GameSceneBootstrap.cs`、`Gameplay.csproj`

### ✅ #40 🟡 P2 — 环境区域系统增强【已实现】
**文件**：`Map/EnvironmentZone.cs`、`ScriptableObjects/Config/MapThemeData.cs`
**实施**：
1. 在 `MapThemeData.EnvironmentZoneType` 枚举中新增 2 种区域类型：
   - `Speed` — 加速区域（玩家移速+50%）
   - `DotEnhance` — DOT增强区域（DOT伤害+30%，Mage专属优势）
2. 在 `EnvironmentZone.cs` 中实现新区域类型的完整逻辑：
   - `InitializeVisuals()`：Speed=蓝色，DotEnhance=紫色
   - `OnTriggerEnter2D/Exit2D`：进入/离开时应用/移除效果
   - `ApplySpeed()`：修改 PlayerController.MoveSpeed（+50%）
   - `ApplyDotEnhance()`：修改 StatusEffectManager.DotDamageMultiplier（+30%）
   - `OnDestroy()` 清理时正确移除 Speed 和 DotEnhance 效果
   - `OnDrawGizmosSelected()` 支持新类型颜色显示
3. 原有 4 种类型（Slow/Damage/Heal/Lava）保持不变
**修改文件**：`Map/EnvironmentZone.cs`、`ScriptableObjects/Config/MapThemeData.cs`

### ✅ #41 🟢 P3 — 游戏统计页面（Game Over 后）【已实现】
**文件**：`UI/GameOverUI.cs`、`UI/DamageMeter.cs`
**实施**：
1. 增强 `DamageMeter.cs`：
   - 新增 `_maxSingleDetonateDamage`（最高单次引爆伤害）
   - 新增 `_maxDpsPeak`（最高 DPS 峰值）
   - 新增 `_totalDotTicks`（总 DOT 生效次数）
   - 新增 `_totalKills` / `_bossKills`（击杀/Boss 击杀统计）
   - `RecordDetonate()` 自动追踪最高引爆伤害
   - `RecordDotDamage()` 自动追踪 DOT 触发次数
   - `RecordKill(isBoss)` 公共 API 记录击杀
   - `CombatTime` 属性暴露战斗时长
   - `GenerateStatsSummary()` 生成分享文本
   - `ResetStats()` 重置所有新增字段
2. 重写 `GameOverUI.cs`：
   - 重新布局：标题(0.9) → 基础统计(0.82) → 金币(0.78) → 详细面板(0.32-0.75) → 按钮(0.22向下)
   - 新增详细统计面板（半透明黑色背景）：
     - 标题"⚔ 战斗统计 ⚔"
     - 总伤害 + 平均 DPS
     - 击杀数 + Boss 击杀
     - 最高引爆 + 最高 DPS
     - DOT 总触发次数
     - 伤害分布列表（ASCII 进度条 + 百分比）
   - 新增"📋 复制统计"按钮（绿色，复制统计到剪贴板）
   - 按钮改为中文：重新开始/升级商店/返回菜单
**修改文件**：`UI/GameOverUI.cs`、`UI/DamageMeter.cs`

### ✅ #42 🟢 P3 — 多角色解锁与角色专属成就【已实现】
**文件**：`UI/CharacterUnlockManager.cs`（新建）、`UI/SelectionUI.cs`、`UI/GameOverUI.cs`、`UI/DamageMeter.cs`
**实施**：
1. 新建 `CharacterUnlockManager.cs`：静态角色解锁管理器
   - 8 个角色的解锁条件定义（Warrior/Mage 默认解锁，其他6个需要成就）
   - Ranger: 存活 300 秒，Vampire: 击杀 300，Assassin: 击杀 500
   - Paladin: 承受 10000 伤害，Necromancer: 累计 10 Boss，Berserker: 引爆 50 次
   - `IsCharacterUnlocked()` / `GetUnlockProgress()` / `GetUnlockConditionText()` 查询接口
   - `TryUnlockCharacter()` 尝试解锁并保存到 SaveManager
   - `UpdateStatsOnGameEnd()` 游戏结束时更新统计值
   - 使用 `SaveManager.Data.upgrades` 存储解锁状态和统计值
2. 修改 `SelectionUI.cs`：
   - 角色选择列表中检查每个角色的解锁状态
   - 未解锁角色显示 🔒 图标 + 灰色文字样式
   - 未解锁角色下方显示解锁条件进度（如"存活 5 分钟 (120/300)"）
   - 锁定角色点击无效，不会被选中
3. 修改 `GameOverUI.cs`：
   - 游戏结束时调用 `CharacterUnlockManager.UpdateStatsOnGameEnd()` 更新统计
4. 将新文件添加到 Gameplay.csproj
**新增文件**：`UI/CharacterUnlockManager.cs`
**修改文件**：`UI/SelectionUI.cs`、`UI/GameOverUI.cs`、`Gameplay.csproj`

### ✅ #43 🟢 P3 — 每日挑战模式（Daily Challenge）【已实现】
**文件**：`UI/DailyChallengeSystem.cs`（新建）、`Core/MenuSceneBootstrap.cs`
**实施**：
1. 新建 `DailyChallengeSystem.cs`：每日挑战系统（静态类）
   - 10 种挑战规则：DotOnly/DetonateCdhalf/EnemySpeedx2/EnemyHpx1_5/NoHeal/DoubleCoins/HalfPlayerHp/TripleEnemies/BossRush/GlassCannon
   - 使用日期作为种子（`YYYYMMDD`），同一天所有玩家面对相同3个随机规则组合
   - `DailyChallengeConfig` 结构体包含种子+3个规则+描述文本
   - `ActivateDailyChallenge()` / `Deactivate()` 激活/停用挑战模式
   - 运行时倍率查询API：`GetEnemyHpMultiplier()`、`GetEnemySpeedMultiplier()`、`GetCoinMultiplier()`、`GetPlayerDamageMultiplier()` 等
   - `IsDotOnly()`、`IsNoHeal()`、`IsBossRush()` 等布尔查询
   - 挑战完成记录保存到 `SaveManager.Data.upgrades`（`daily_YYYYMMDD` key 存波次）
   - `GetTotalCompletedCount()` 查询累计完成次数
2. 修改 `MenuSceneBootstrap.cs`：
   - 新增 `StartDailyChallenge()` 方法：激活挑战模式后加载 GameScene
   - 绑定 "DailyButton" 按钮点击事件
   - 按 D 键快捷启动每日挑战
   - 启动时打印今日挑战的3条规则到控制台
3. 将新文件添加到 Gameplay.csproj
**新增文件**：`UI/DailyChallengeSystem.cs`
**修改文件**：`Core/MenuSceneBootstrap.cs`、`Gameplay.csproj`

---

## 总结

| 优先级 | 数量 | 说明 |
|--------|------|------|
| 🟠 P1 | 3 项 | 架构基础/内容扩展 |
| 🟡 P2 | 7 项 | 内容扩展/系统完善 |
| 🟢 P3 | 3 项 | 长期规划/锦上添花 |
| ✅ 已完成 | 13 项 | #31 敌人代码重构, #32 事件系统增强, #33 配置热加载, #34 存档校验, #35 对象池监控, #36 新敌人类型, #37 Boss阶段机制, #38 成就永久奖励, #39 波次挑战, #40 环境区域增强, #41 游戏统计页面, #42 多角色解锁, #43 每日挑战 |
| **合计** | **13 项** | — |

### 分类统计

| 类别 | 数量 | 说明 |
|------|------|------|
| 架构与代码质量优化 | 5 项 | #31-#35 |
| 内容扩展与新功能 | 8 项 | #36-#43 |

### 建议执行顺序

**第一轮（核心架构与内容）**：
1. #31 敌人代码重构 — 架构基础
2. #36 新敌人类型（反DOT）— 平衡性
3. #37 Boss 阶段机制 — 内容深度

**第二轮（系统完善）**：
4. #33 配置热加载
5. #34 存档校验
6. #38 成就永久奖励
7. #39 波次挑战系统
8. #40 环境区域增强

**第三轮（长期规划）**：
9. #35 对象池监控
10. #41 游戏统计页面
11. #42 多角色解锁
12. #43 每日挑战
