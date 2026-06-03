# 🎯 项目优化清单 V3 — Vampire Survivors Unity 移植版

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

### #31 🟠 P1 — 敌人子类重复代码消除（EnemyAbilityBase）
**文件**：`Enemies/` 目录下 14 个敌人子类
**问题**：14 种敌人子类中有大量重复代码（光环检测、射击逻辑、冷却计时器等）。例如 HealerEnemy 和 EnhancerEnemy 的光环逻辑几乎相同，RangedEnemy 和 ThrowerEnemy 的射击逻辑相似。
**建议**：
- 新建 `EnemyAbilityBase` 抽象类，封装通用能力模式：
  - `AuraAbility`（光环型：Healer/Enhancer/Shielder）
  - `RangedAbility`（射击型：Ranged/Thrower/Burst）
  - `MeleeAbility`（近战型：Basic/Fast/Tank/Charger）
  - `SpecialAbility`（特殊型：Stealth/Splitter/Summoner/ChainHealer）
- 每种能力类型提供 `OnUpdate()`、`OnDetectPlayer()`、`OnCooldownReady()` 等虚方法
- 敌人子类只需实现差异化逻辑
- 预计减少 40-50% 的敌人代码量

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

### #33 🟡 P2 — 配置数据热加载
**文件**：`Data/ConfigLoader.cs`、`ScriptableObjects/Config/GameConfig.cs`
**问题**：GameConfig 和 EnemyWaveConfig 的参数修改需要重新编译或重启游戏。开发调试阶段频繁修改配置效率低下。
**建议**：
- ConfigLoader 支持运行时重新加载 JSON 配置文件（按 F5 键热加载）
- GameConfig 序列化为 JSON 文件，编辑器修改后自动同步
- 提供 Debug 面板（按 F1 打开），可实时修改游戏参数（敌人血量倍率、DOT 伤害倍率、波次间隔等）
- Debug 面板使用 ImGui 或 OnGUI 实现，仅在 Development Build 中可用
- 参数修改后立即生效，无需重启

### #34 🟡 P2 — SaveManager 数据完整性校验
**文件**：`Core/SaveManager.cs`
**问题**：SaveManager 使用 JSON 序列化存档，但没有数据完整性校验。玩家可以手动编辑 JSON 文件作弊，或存档损坏时无法检测。
**建议**：
- 存档时计算 JSON 内容的 CRC32/MD5 校验码，附加到文件末尾
- 加载时验证校验码，不匹配时提示"存档损坏"并提供删除选项
- 保留最近 3 个存档备份（autosave_1.json ~ autosave_3.json），循环覆盖
- 存档版本号字段，未来升级存档格式时自动迁移

### #35 🟢 P3 — 对象池监控 Debug 面板
**文件**：`Core/ObjectPool.cs`、`Core/DebugHelper.cs`
**问题**：对象池运行状态不可见。开发时无法判断哪些池预热不足、哪些池膨胀过度。
**建议**：
- Debug 面板显示每个池的统计：总容量、活跃数、空闲数、峰值使用数
- 颜色标记：绿色=正常、黄色=接近容量上限、红色=已溢出
- 自动检测"池泄漏"（活跃数持续增长不回收）
- 按 F2 键切换显示/隐藏
- 仅在 Development Build 中可用

---

## 二、内容扩展与新功能

### #36 🟠 P1 — 新敌人类型：CorrosiveEnemy（腐蚀者）
**文件**：`Enemies/` (新建)、`Enemies/SpawnManager.cs`、`Core/PoolHelper.cs`
**问题**：当前 14 种敌人覆盖了基础近战/远程/辅助/特殊类型，但缺少"反 DOT"型敌人。Mage 的 DOT 流派在后期过于强势，缺少克制。
**建议**：
- 新建 `CorrosiveEnemy`：接触玩家后施加"DOT 免疫护盾"（3 秒内清除所有 DOT）
  - 形状：八角形，颜色：暗绿色
  - 血量 35，速度 2.5，伤害 8
  - 死亡时释放毒雾（2 格范围，清除范围内所有敌人的 DOT）
  - 第 10+ 波开始出现
- 新建 `PurifierEnemy`：光环范围内敌人获得 DOT 抗性 +50%
  - 形状：十字形，颜色：白色
  - 血量 25，速度 2.0
  - 光环半径 4 格
  - 第 12+ 波开始出现
- 这两种敌人迫使 Mage 玩家优先击杀特定目标，增加策略深度

### #37 🟠 P1 — 新 Boss 阶段专属机制
**文件**：`Enemies/BossEnemy.cs`
**问题**：4 种 Boss 类型已有差异化行为，但每个 Boss 的 5 个阶段切换只是数值提升（血量/伤害），缺少阶段专属技能。
**建议**：
- **Juggernaut（重装）**：
  - 阶段 3：召唤 2 个 TankEnemy 作为护盾
  - 阶段 5：进入"狂暴"状态，速度 ×2，冲锋无冷却
- **Sorcerer（法师）**：
  - 阶段 3：释放"反魔法区域"（区域内玩家技能 CD 翻倍）
  - 阶段 5：同时释放环形弹幕 + 召唤 + 震波
- **Phantom（幽灵）**：
  - 阶段 3：隐身时间延长至 3 秒，闪现距离增大
  - 阶段 5：分裂为 2 个幽灵分身（血量各 50%）
- **Berserker（狂战）**：
  - 阶段 3：每次冲锋后留下火焰路径（FireZone）
  - 阶段 5：血量低于 20% 时进入"不死"状态 5 秒（血量锁定在 1）

### #38 🟡 P2 — 游戏内成就永久奖励系统
**文件**：`UI/AchievementUI.cs`、`Core/SaveManager.cs`
**问题**：task.md #35 已创建 24 个成就并关联永久加成，但永久加成的实际应用效果可能不明显。玩家不知道解锁成就获得了什么具体提升。
**建议**：
- 成就解锁时显示具体的永久加成数值（如 "暴击率 +1%，永久生效"）
- SaveManager 永久加成在游戏启动时汇总显示（Loading Screen 或主菜单）
- 主菜单新增"永久加成总览"面板，列出所有已解锁加成
- 永久加成上限控制：单个加成不超过 20%，总加成不超过 100%
- 加成图标在 LevelUpUI 的升级选项旁显示（提示玩家已有加成）

### #39 🟡 P2 — 波次挑战系统（可选高难度模式）
**文件**：`Enemies/SpawnManager.cs`、`ScriptableObjects/Config/EnemyWaveConfig.cs`
**问题**：当前难度曲线固定，高玩觉得太简单，新手觉得太难。缺少难度选择。
**建议**：
- 新增 **波次挑战系统**：每 5 波（Boss 波前）提供可选挑战
  - 挑战内容：下一波敌人血量 ×2、速度 ×1.5、或额外 Boss
  - 接受挑战奖励：额外 100 金币 + 永久加成 +1%
  - 拒绝挑战：正常进行
- 挑战通过 EventManager 广播，UI 显示挑战卡片（暂停时弹出）
- 挑战记录保存到 SaveManager（统计接受/拒绝次数）

### #40 🟡 P2 — 环境区域系统增强
**文件**：`Map/EnvironmentZone.cs`、`Map/MapThemeManager.cs`
**问题**：EnvironmentZone 已存在但可能功能有限。地图缺少环境交互元素，每局游戏地图只是视觉背景。
**建议**：
- 新增环境区域类型：
  - 🟢 治疗区域：玩家站在上面每秒恢复 2% HP
  - 🔴 伤害区域：每秒造成 5 点伤害（Boss 战时 Boss 创建）
  - 🔵 加速区域：站在上面移速 +50%
  - 🟣 DOT 增强区域：DOT 伤害 +30%（Mage 专属优势）
- 环境区域每 30-60 秒随机出现在地图上，持续 15 秒
- 视觉效果：半透明圆形区域 + 粒子特效
- 区域走对象池，预热 5-10 个

### #41 🟢 P3 — 游戏统计页面（Game Over 后）
**文件**：`UI/GameOverUI.cs`、`UI/DamageMeter.cs`
**问题**：Game Over 界面只显示基础信息（存活时间、击杀数），缺少详细统计。
**建议**：
- Game Over 后显示详细统计面板：
  - 伤害分布饼图（各 DOT 类型占比）
  - DPS 时间曲线（简化版折线图）
  - 最高单次引爆伤害
  - 最高 DPS 峰值
  - 总 DOT 层数叠加次数
  - 成就解锁列表（本局新解锁的）
  - Boss 击杀数 / 总波次数
- 统计数据通过 DamageMeter 已有数据生成
- "分享"按钮：复制统计文本到剪贴板

### #42 🟢 P3 — 多角色解锁与角色专属成就
**文件**：`UI/SelectionUI.cs`、`ScriptableObjects/Characters/CharacterData.cs`
**问题**：8 种角色可能全部可选，缺少解锁机制。玩家没有"收集所有角色"的动力。
**建议**：
- 角色解锁条件：
  - Warrior：默认解锁
  - Mage：默认解锁（或击杀 100 敌人后解锁）
  - Archer：存活 5 分钟后解锁
  - Tank：承受 10000 伤害后解锁
  - Assassin：单局击杀 500 敌人后解锁
  - Healer：单局治疗 5000 HP 后解锁（或使用 Mage DOT 杀死后解锁）
  - Necromancer：击败 10 个 Boss 后解锁
  - Berserker：单局引爆 50 次后解锁（Mage 专属成就关联）
- 未解锁角色在 SelectionUI 中显示为灰色剪影 + 解锁条件提示
- 解锁时全屏特效 + 角色介绍弹窗

### #43 🟢 P3 — 每日挑战模式（Daily Challenge）
**文件**：新建 `DailyChallenge/` 目录
**问题**：游戏缺少重复游玩的动力。每日挑战可以增加长期留存。
**建议**：
- 每日生成固定种子的挑战配置：
  - 固定的敌人波次序列（不可随机）
  - 特殊规则：如"只有 DOT 伤害有效"、"引爆 CD 减半"、"敌人速度 ×2"
  - 全球排行榜（可选，需要后端支持）
- 挑战配置使用日期作为种子，同一天所有玩家面对相同挑战
- 主菜单新增"每日挑战"按钮
- 挑战结果保存到 SaveManager（最佳排名、完成时间）
- 完成每日挑战奖励：额外金币 + 限定成就

---

## 总结

| 优先级 | 数量 | 说明 |
|--------|------|------|
| 🟠 P1 | 3 项 | 架构基础/内容扩展 |
| 🟡 P2 | 7 项 | 内容扩展/系统完善 |
| 🟢 P3 | 3 项 | 长期规划/锦上添花 |
| ✅ 已完成 | 1 项 | #32 事件系统增强 |
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
