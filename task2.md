# 🎯 项目优化清单 V2 — Vampire Survivors Unity 移植版

> 更新时间：2026-06-02
> 基于 task.md 已完成的 41 项优化后的第二轮深度优化
> 重点：游戏体验打磨、Mage 深度优化、性能极限优化、内容扩展

---

## 优先级说明

| 等级 | 含义 | 预计影响 |
|------|------|---------|
| 🔴 P0 | 紧急 — 严重影响游戏体验/潜在Bug | 立即修复 |
| 🟠 P1 | 高优 — 核心玩法体验/性能瓶颈 | 下个迭代 |
| 🟡 P2 | 中优 — 内容扩展/体验提升 | 计划内 |
| 🟢 P3 | 低优 — 锦上添花/长期规划 | 有空再做 |

---

## 一、Mage 角色深度优化（12项）

### ✅ #1 🟠 P1 — Mage 专属 HUD 统计面板【已实现】
**文件**：`UI/MageStatsHUD.cs`（新建）、`Core/GameSceneBootstrap.cs`
**实施**：
1. 新建 `MageStatsHUD` 组件，使用 OnGUI 实现（与 SkillHUD 一致的设计模式）
2. 非暂停时：左下角简化版显示（⚔ MAGE 标签 + 当前总 DPS + 引爆 CD 状态 + DOT 子弹图标条）
3. 暂停时：展开完整统计面板，包含：
   - DOT GUNS 区域：每种子弹显示色块图标 + 类型名 + DPS + 等级 + 有效冷却
   - OVERVIEW 区域：总 DPS（带暴击期望计算）、暴击率、暴击倍率、DOT 持续时间加成
   - DETONATE [E] 区域：引爆倍率、冷却、就绪状态
   - DOT ENHANCEMENTS 区域：频率、腐蚀、诅咒传播、凋零、侵蚀参数
   - 子弹增强：攻速、弹速、子弹数、反弹率
   - MILESTONES 区域：元素大师（4/4进度条）、连锁引爆（激活状态）
4. DPS 计算缓存（0.5秒刷新间隔），避免每帧开销
5. 紫色系 Mage 专属 UI 主题
6. GameSceneBootstrap 在选择 Mage 时自动创建 MageStatsHUD

### ✅ #2 🟠 P1 — DOT 子弹穿透机制（急速>100%后）【已优化】
**文件**：`Combat/DotProjectile.cs`
**实施**：
1. BleedBullet OnTriggerEnter2D 已有穿透检测（task.md #45 已集成）
2. BurnBullet OnTriggerEnter2D 新增 `PenetrateHandler.TryPenetrate()` 检测，在反弹检测之前
3. FrostBullet OnTriggerEnter2D 新增 `PenetrateHandler.TryPenetrate()` 检测，在反弹检测之前
4. PoisonBullet/PoisonPotion 不适用穿透（命中后爆炸生成毒液池，穿透无意义）
5. PenetrateHandler 通过 DotBulletFactory.AttachRicochetIfAvailable() 在子弹创建时自动附加（急速>100%时）
6. 穿透次数 = FloorToInt(BulletSpeedBonus)，每多100%多穿透1个敌人

### ✅ #3 🟠 P1 — 反弹(Ricochet)逻辑已集成到 DOT 子弹碰撞【已确认】
**文件**：`Combat/DotProjectile.cs`
**状态**：反弹逻辑已在所有直射型 DOT 子弹的 OnTriggerEnter2D 中集成：
1. BleedBullet：L99 `RicochetHandler.TryRicochet()` ✓
2. BurnBullet：`RicochetHandler.TryRicochet()` ✓
3. FrostBullet：`RicochetHandler.TryRicochet()` ✓
4. PoisonBullet/PoisonPotion：不适用（AOE爆炸型子弹）
5. RicochetHandler 通过 DotBulletFactory.AttachRicochetIfAvailable() 在子弹创建时自动附加
6. 反弹后子弹重定向到最近敌人，保留DOT效果，视觉显示白色爆炸特效

### ✅ #4 🟡 P2 — DOT 子弹枪 Synergy（协同）升级系统【已实现】
**文件**：`Player/MagePassive.cs`
**实施**：
1. 新增 `_activeSynergies` HashSet 追踪已激活的协同效果
2. 新增 `SynergyType` 枚举：Plague（瘟疫）、FrozenBlade（冰封血刃）、BulletStorm（弹雨风暴）、JudgmentDay（末日审判）
3. `CheckSynergies()` 方法在每次升级后自动调用，检测 4 种协同条件：
   - 瘟疫：中毒子弹 + 燃烧子弹 + 腐蚀升级（ArmorReduction > 0.101）
   - 冰封血刃：流血子弹 + 霜冻子弹 + 诅咒升级（CurseSpreadTargets > 1）
   - 弹雨风暴：弹幕（BulletCountBonus > 0）+ 急速（AttackSpeedBonus > 0）+ 反弹（RicochetChance > 0）
   - 末日审判：辐射（DetonateMultiplier > 3.01）+ 污染（Cooldown < 11.99）+ 侵蚀（ErosionDamagePercent > 0）
4. `TryActivateSynergy()` 首次激活时：金色/绿色/冰蓝/火红独特弹字 "✦ SYNERGY: [名称]!" + 升级音效
5. `ApplySynergyEffect()` 记录日志，实际运行时效果通过公共查询接口供外部组件检查
6. 末日审判已集成到 `Detonate()`：引爆命中后调用 `ActivateJudgmentDay()` 激活 3 秒 DOT 频率翻倍
7. `GetDotFrequencyMultiplier()` 公共方法：末日审判期间间隔减半
8. 4 个公共查询属性：`HasPlagueSynergy`、`HasFrozenBladeSynergy`、`HasBulletStormSynergy`、`HasJudgmentDaySynergy`
9. DOT 子弹枪解锁路径也调用 `CheckSynergies()`（解锁新子弹可能触发协同）

### ✅ #5 🟡 P2 — Mage 引爆蓄力机制【已实现】
**文件**：`Player/MagePassive.cs`
**实施**：
1. 新增 `_isCharging`/`_chargeStartTime` 蓄力状态追踪
2. E 键逻辑改为：按下开始蓄力（`wasPressedThisFrame`），松开执行引爆（`wasReleasedThisFrame`），满 3 秒自动引爆
3. 4 档蓄力倍率系统（`GetChargeMultiplier`）：
   - 0-1 秒：1.0x 基础倍率
   - 1-2 秒：1.5x 倍率 + 范围 +20%
   - 2-3 秒：2.0x 倍率 + 范围 +50% + 敌人减速 50%（通过 Frostbite 效果）
   - 3 秒+：3.0x 倍率 + 范围 +50% + 留下 FireZone 辐射区域（5秒，8 DPS）
4. `DetonateWithCharge()` 临时倍增引爆参数后调用 `Detonate()`，引爆后恢复原始值
5. 蓄力时玩家移速 -30%（`_chargeMoveSpeedPenalty=0.3f`，`GetChargeMoveSpeedMultiplier()` 公共接口）
6. 3 个公共查询属性供 SkillHUD 使用：`IsCharging`、`ChargeProgress`(0-1)、`ChargeMultiplier`(1-3)
7. 蓄力中按 E 不会重复触发（`!_isCharging` 检查）

### ✅ #6 🟡 P2 — DOT 效果在敌人血条上的图标系统【已实现】
**文件**：`UI/EnemyHealthBar.cs`
**实施**：
1. 新增 `_dotIconContainer` 在血条上方（DOT 条之上）创建独立的图标层
2. 4 种 DOT 图标使用运行时生成的几何形状 Sprite（静态缓存）：
   - 🔴 流血 = 三角形（3边多边形）
   - 🟢 中毒 = 菱形（4边多边形）
   - 🟠 燃烧 = 五边形（5边多边形，带脉冲闪烁）
   - 🔵 霜冻 = 六边形（6边多边形）
3. `CreatePolygonSprite()` 使用像素填充法生成精确多边形纹理，`IsInsidePolygon()` 使用扇区角度检测
4. 每个图标旁显示层数 TextMesh（`x5` 格式），使用 `CreateIconCountText()` 创建
5. `UpdateDotIcons()` 在 `UpdateDotIndicators()` 末尾调用，实时更新图标显示/隐藏/位置/层数
6. 图标按固定间距横向排列（DOT_ICON_GAP=0.35），自动跟随 DOT 激活状态
7. 燃烧图标带 8Hz 脉冲色效
8. `HideAllIndicators()` 扩展为同时隐藏所有图标和层数文字
9. 所有 Sprite 静态缓存（`_triangleSprite` 等），所有敌人实例共享，零额外 GC

### ✅ #7 🟡 P2 — Mage 升级选择缺少"Build 路线"提示【已实现】

### ✅ #8 🟢 P3 — DOT 子弹弹道缺少视觉多样性【已实现】
**文件**：`Combat/DotProjectile.cs`
**实施**：
1. 新增 `DotBulletVisualEffects` 静态工具类，提供 4 种轻量级视觉特效方案（TrailRenderer / 子物体 / 旋转）
2. 🔴 流血子弹：TrailRenderer 红色拖尾（0.6秒消失，0.04宽度）
3. 🟢 中毒子弹：TrailRenderer 绿色拖尾（0.4秒消失，0.03宽度）
4. 🟠 燃烧子弹：FlamePulseEffect 子物体橙色发光球，12Hz 脉冲缩放+透明度闪烁
5. 🔵 霜冻子弹：TrailRenderer 冰蓝宽拖尾 + FrostGhostSpawner 每0.08秒生成半透明冰晶残影，0.25秒淡出销毁
6. 🟢 中毒药瓶：SpinEffect 360°/秒旋转弹道
7. 新增辅助组件：FlamePulseEffect、FrostGhostSpawner、GhostFadeOut、SpinEffect
8. 所有特效零 GC 分配（静态 Sprite 缓存，残影自动销毁）

### ✅ #9 🟢 P3 — Mage 被动缺少"进化"机制【已实现】
**文件**：`Player/MagePassive.cs`
**实施**：
1. 新增 `_evolvedTypes` HashSet 追踪已进化类型 + `IsEvolved(type)` / `EvolvedTypes` 公共查询接口
2. `UnlockDotGun()` 中当 `upgradeLevel >= 5` 时自动触发 `TriggerEvolution()`
3. 4 种进化效果：
   - 🔴 血之狂潮（Blood Tide）：流血 DPS ×2
   - 🟢 瘟疫之源（Plague Source）：中毒 DPS +50%
   - 🟠 地狱之火（Hellfire）：燃烧 DPS +80%
   - 🔵 绝对零度（Absolute Zero）：霜冻 DPS ×2
4. 进化触发时：金色弹字 "✦ EVOLVED: [名称]!" + 升级音效 + 日志
5. `ApplyEvolutionBonus()` 直接修改对应 DotGunState 的 dotDps
6. `GetEvolutionInfo()` 返回进化名称和描述（中文+英文双语）

### ✅ #10 🟢 P3 — Mage 与其他角色的差异化 UI 主题【已实现】
**文件**：`UI/UIColorTheme.cs`、`UI/SkillHUD.cs`、`Core/GameSceneBootstrap.cs`
**实施**：
1. UIColorTheme 新增 Mage 主题色：紫色边框(MagePanelBorder)、深紫背景(MageBackground)、绿色HP(MageHpGreen)、紫色XP(MageXpPurple)、DOT绿(MageAccentDot)、引爆紫红(MageAccentDetonate)
2. 新增主题切换系统：`SetTheme("mage")` / `GetAccentColor()` / `GetHighlightColor()` / `GetHpColor()` / `GetXpColor()`
3. SkillHUD 改用 `UIColorTheme.GetAccentColor()` 替代硬编码的 AccentCyan，根据角色动态切换配色
4. GameSceneBootstrap 在选择完成后根据角色类型自动调用 `UIColorTheme.SetTheme("mage" 或 "default")`
5. 所有 UI 组件可通过 `UIColorTheme.IsMageTheme` 查询当前主题

### ✅ #11 🟢 P3 — DOT 伤害类型细分与抗性系统【已实现】
**文件**：`Enemies/EnemyDotResistance.cs`（新建）、`Combat/StatusEffects/StatusEffectSystem.cs`、`Enemies/TankEnemy.cs`、`Enemies/FastEnemy.cs`、`Enemies/HealerEnemy.cs`、`Enemies/StealthEnemy.cs`
**实施**：
1. 新建 `EnemyDotResistance` 组件，支持 4 种 DOT 类型的抗性/弱点（-1.0~1.0）
2. 抗性值正值=减少DOT伤害，负值=增加DOT伤害，1.0=完全免疫
3. StatusEffectManager.Awake() 缓存 EnemyDotResistance 引用，Update() 中每 tick 应用抗性倍率
4. 4 种敌人预设：
   - TankEnemy：流血抗性+50%（高甲减物理DOT），霜冻弱点-30%（笨重易冻）
   - FastEnemy：霜冻抗性+30%（高速难冻），流血弱点-20%（薄皮易出血）
   - HealerEnemy：中毒抗性+40%（自然抗性），燃烧弱点-30%（怕火）
   - StealthEnemy：燃烧抗性+50%（暗影灭火），中毒弱点-20%
5. Boss 预设：全 DOT 抗性+20%
6. `GetPrimaryResistance()` / `GetPrimaryWeakness()` 供 EnemyHealthBar 显示标记

### ✅ #12 🟢 P3 — Mage 引爆连锁反应（多阶段引爆）【已实现】
**文件**：`Player/MagePassive.cs`
**实施**：
1. 新增连锁引爆参数：`_maxChainCount=3`（最大连锁次数）、`_chainRadius=10`（二次引爆范围）、`_chainDamageRatio=0.5`（伤害递减）
2. `Detonate()` 末尾调用 `TryChainDetonate()` 启动连锁引爆
3. `TryChainDetonate()` 遍历引爆范围内仍有 DOT 的敌人，对它们执行二次引爆（伤害 ×0.5）
4. 连锁引爆使用协程 `ChainDetonateWave()` 实现波浪扩散延迟（0.1s × 连锁等级）
5. 每轮连锁伤害递减 30%（`Mathf.Pow(0.7f, chainLevel)`），防止无限连锁
6. 连锁引爆视觉效果：紫色冲击波 + 屏幕抖动（强度随连锁等级递减）
7. 使用 `HashSet<int>` 去重防止同一敌人被重复连锁引爆
8. 已引爆的敌人位置作为下一轮连锁的扩散源点

---

## 二、项目整体性能优化（8项）

### ✅ #13 🟠 P1 — DamagePopup 对象池化【已优化】
**文件**：`UI/DamagePopup.cs`
**实施**：
1. 内部静态对象池：预热 30 个弹字实例（`POOL_INITIAL_SIZE=30`，`POOL_MAX_SIZE=50`）
2. `GetFromPool()` 懒加载初始化，搜索空闲实例复用，池满时轮转复用最早的
3. `ReturnToPool()` 替代 `Destroy()`，弹字超时后 `SetActive(false)` 回收到池
4. `Reset()` 方法重置位置/颜色/文本/透明度，`SetActive(true)` 激活
5. 对外 API 完全不变（`Create()`/`CreateDetonateTotal()`），零破坏性修改
6. Font 缓存为静态字段 `_cachedFont`，避免每弹字 `Resources.GetBuiltinResource`
7. 新增 `ResetPool()` 静态方法，场景重置时回收所有活跃弹字

### ✅ #14 🟠 P1 — Camera.main 缓存【已优化】
**文件**：`Player/MagePassive.cs`、`Core/OffScreenCuller.cs`、`Skills/TeleportSkill.cs`、`Map/MapThemeManager.cs`
**实施**：
1. 全局搜索 `Camera.main`，共找到 7 处调用（除 GameSceneBootstrap 初始化缓存外）
2. MagePassive.Detonate 中 `var cam = Camera.main` → `var cam = GameReferences.MainCamera`
3. OffScreenCuller（3 处）：Awake + Update + IsOffScreen 全部替换为 `GameReferences.MainCamera ?? Camera.main`（含 null 回退）
4. TeleportSkill.Activate 中替换为 `GameReferences.MainCamera ?? Camera.main`
5. MapThemeManager（2 处）：ApplyThemeVisuals + ResetThemes 替换为 `GameReferences.MainCamera ?? Camera.main`
6. 所有替换保留 `?? Camera.main` 作为回退，确保 GameReferences 未初始化时不会崩溃
7. 预计减少每帧 5-7 次 `FindObjectWithTag("MainCamera")` 调用

### ✅ #15 🟠 P1 — 敌人 AI 更新频率分级（Distance-based LOD）【已实现】
**文件**：`Enemies/EnemyBase.cs`、`Enemies/RangedEnemy.cs`、`Enemies/ThrowerEnemy.cs`、`Enemies/BurstEnemy.cs`、`Enemies/SummonerEnemy.cs`、`Enemies/StealthEnemy.cs`
**实施**：
1. EnemyBase.FixedUpdate() 中添加基于平方距离的 LOD 分级系统：
   - 0-15 格（distSqr ≤ 225）：每帧更新（interval=1）
   - 15-30 格（225 < distSqr ≤ 900）：每 3 帧更新（interval=3）
   - 30+ 格（distSqr > 900）：每 10 帧更新（interval=10）+ 跳过特殊能力
2. 新增 `ShouldUpdateThisFrame` 和 `SkipSpecialAbility` 属性供子类使用
3. 远距离敌人仍保持移动（方向在 AI 更新帧计算），但降低更新频率
4. 为 5 种特殊能力子类添加 SkipSpecialAbility 检查：
   - RangedEnemy：跳过射击逻辑
   - ThrowerEnemy：跳过投掷逻辑
   - BurstEnemy：跳过连射逻辑（但连射中不中断）
   - SummonerEnemy：跳过召唤逻辑
   - StealthEnemy：简化为普通移动，跳过隐身逻辑
5. 光环类敌人（Healer/Enhancer/Shielder/ChainHealer）保持每帧更新（其光环需实时计算）
6. 使用静态帧计数器 `_globalFrameCounter` 实现跨实例的分帧调度

### ✅ #16 🟡 P2 — 状态效果系统内存优化【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs`
**实施**：
1. `OnEnable()` 中已调用 `_activeEffects.Clear()` 重置所有效果（已有，确认无误）
2. `ApplyEffect()` 中新增上限检查：超过 10 个效果时移除最旧的 `_activeEffects.RemoveAt(0)`
3. 防止极端情况（大量不同 DOT 类型叠加）导致内存膨胀
4. 注意：`StatusEffect` 保持 class（非 struct），因为 `ApplyEffect` 中直接修改 `existing` 对象引用

### ✅ #17 🟡 P2 — 子弹碰撞检测优化（LayerMask 过滤）【已实现】
**文件**：`Core/PhysicsLayerSetup.cs`（新建）、`Core/GameSceneBootstrap.cs`、`Combat/DotProjectile.cs`、`Combat/Bullet.cs`、`Enemies/EnemyBase.cs`、`Player/PlayerController.cs`、`ProjectSettings/TagManager.asset`
**实施**：
1. 新建 `PhysicsLayerSetup` 静态类，定义 Layer 常量（Bullet=8, Enemy=9, Player=10, Pickup=11, Environment=12）+ 碰撞矩阵配置 + 便捷设置方法
2. `TagManager.asset` 新增 5 个自定义 Layer（Bullet/Enemy/Player/Pickup/Environment）
3. `GameSceneBootstrap.Start()` 调用 `PhysicsLayerSetup.SetupCollisionMatrix()` 配置碰撞矩阵：
   - Bullet↔Enemy = 碰撞 ✓（玩家子弹打敌人）
   - Bullet↔Environment = 碰撞 ✓（子弹撞墙）
   - Enemy↔Player = 碰撞 ✓（敌人接触伤害）
   - Pickup↔Player = 碰撞 ✓（磁吸拾取）
   - 其他自定义 Layer 组合 = 不碰撞
4. DOT 子弹创建：BleedBullet/BurnBullet/FrostBullet/PoisonBullet/PoisonPotion 创建时设置 Bullet Layer
5. 普通子弹：Bullet.CreateDefault 创建时设置 Bullet Layer
6. 敌人：EnemyBase.OnEnable() 设置 Enemy Layer
7. 玩家：PlayerController 设置 Player Layer
8. 掉落物：XPGem/Coin 模板设置 Pickup Layer
9. 敌人子弹（EnemyBullet）保持 Default Layer（需碰撞 Player，而 Player↔Bullet 矩阵不碰撞）

### ✅ #18 🟡 P2 — 诅咒传播链式特效优化【已确认无问题】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs`
**状态**：全局搜索 `LineRenderer` 关键字，代码库中不存在任何 LineRenderer 使用。诅咒传播特效要么从未实现（纯逻辑传播无视觉反馈），要么已在之前的优化中移除。当前无 LineRenderer 性能问题。

### #19 🟡 P2 — 敌人碰撞体优化（复合碰撞体）
**文件**：`Enemies/EnemyBase.cs`、`Combat/DotProjectile.cs`
**问题**：所有敌人使用单一 CircleCollider2D 作为触发器。当大量敌人聚集时（50+），碰撞检测 O(n²) 复杂度成为瓶颈。
**建议**：
- 敌人触发器半径缩小到刚好包裹精灵大小（避免过大的触发区域）
- 子弹碰撞改为使用 `Physics2D.OverlapCircleNonAlloc()` + 手动距离检测（减少物理引擎开销）
- 或使用 `ContactFilter2D` 预设过滤条件
- 考虑引入简单空间分区（Grid-based Spatial Hash）用于子弹-敌人碰撞

### #20 🟢 P3 — 渲染优化：Sprite Atlas + Material Property Block
**文件**：`Combat/DotParticleVFX.cs`、`UI/EnemyHealthBar.cs`、`Enemies/EnemyBase.cs`
**问题**：DOT 粒子特效、敌人颜色变化、血条等使用独立 SpriteRenderer/Material，导致额外 Draw Call。
**建议**：
- 创建 Sprite Atlas 将所有敌人形状、DOT 图标、UI 元素打包
- 敌人颜色变化使用 `MaterialPropertyBlock` 而非修改 `SpriteRenderer.color`（避免材质实例化）
- DOT 粒子特效共享 Material（4 种粒子共用 1 个材质）
- 减少透明材质使用（使用 URP 的 Sprite-Lit-Default 材质）

---

## 三、游戏体验优化（10项）

### ✅ #21 🟠 P1 — Boss 战专属 UI（Boss 血条 + 阶段指示器）【已优化】
**文件**：`UI/BossHealthBarUI.cs`（新建）、`Core/EventManager.cs`、`Enemies/BossEnemy.cs`、`Core/GameSceneBootstrap.cs`
**实施**：
1. EventManager 新增 4 个 Boss 事件：OnBossSpawn、OnBossPhaseChange、OnBossDeath、OnBossHPChanged + 对应 Trigger 方法 + ClearAll 清理
2. 新建 `BossHealthBarUI` 组件：OnGUI 实现，屏幕顶部 70% 宽度巨型血条 + Boss 名称 + HP 数字 + 5 个菱形阶段指示器（金色高亮/暗灰未达） + 阶段文字
3. 渐入渐出动画（alpha 过渡）+ 阶段切换时血条白色闪烁 0.5 秒 + 低血量橙色
4. BossEnemy.Start() 触发 TriggerBossSpawn、UpdatePhase 触发 TriggerBossPhaseChange、Update 中 HP 变化时触发 TriggerBossHPChanged、Boss 死亡时触发 TriggerBossDeath
5. GameSceneBootstrap.Start() 自动创建 BossHealthBarUI 实例

### ✅ #22 🟠 P1 — 敌人生成预警系统【已实现】
**文件**：`UI/SpawnWarningUI.cs`（新建）、`Enemies/SpawnManager.cs`、`Core/GameSceneBootstrap.cs`
**实施**：
1. 新建 `SpawnWarningUI` 组件（单例模式），OnGUI 实现
2. Boss 预警：屏幕中央红色闪烁 "⚠ BOSS INCOMING ⚠" + 倒计时秒数 + 警报音效（3秒持续）
3. 特殊波次提示：屏幕中央金色文字显示波次类型（如 "★ TankRush Wave 12"），3秒后淡出
4. 方向指示器：将敌人生成世界坐标转为屏幕坐标，屏幕边缘显示红色圆点指示器
5. SpawnManager.StartNextWave() 中集成：
   - Boss 波前调用 `SpawnWarningUI.ShowBossWarning()`
   - 特殊波次前调用 `SpawnWarningUI.ShowWaveHint()`
6. GameSceneBootstrap 在游戏启动时自动创建 SpawnWarningUI 实例
7. 所有纹理/样式缓存，避免每帧分配

### ✅ #23 🟠 P1 — 玩家受伤反馈增强（无敌帧 + 受伤闪红）【已优化】
**文件**：`Entities/Damageable.cs`、`UI/DamageFlashEffect.cs`（新建）
**实施**：
1. Damageable 新增无敌帧系统：`_invincibleDuration=0.5f`、`_invincibleUntil` 时间戳、`IsInvincible` 公共属性
2. TakeDamage 中添加无敌帧检查：玩家在无敌帧期间免疫所有伤害（优先于闪避检查）
3. 玩家受伤后自动触发 0.5 秒无敌帧 + SpriteRenderer 闪烁效果（每 0.05 秒切换透明/不透明）
4. 新建 `DamageFlashEffect` 组件：全屏红色闪一下（0.1 秒，平方曲线快速衰减），复用 DetonateFlashEffect 的 OnGUI 设计模式
5. 受伤音效由 SFXManager 通过 EventManager.OnPlayerDamaged 事件自动播放（已有 `OnPlayerDamaged` 回调）

### ✅ #24 🟡 P2 — XP/金币自动磁吸优化【已优化】
**文件**：`Entities/XPGem.cs`、`Entities/Coin.cs`
**实施**：
1. XPGem + Coin 均改用 `sqrMagnitude` 平方距离检测，避免每帧开方运算
2. 非磁吸状态帧跳过优化：每 3 帧检测一次距离（`FRAME_SKIP_INTERVAL=3`），减少大量掉落物时 CPU 开销
3. 磁吸速度距离递增曲线：使用 `Mathf.Lerp(_magnetSpeed, _magnetSpeedNear, speedT)` 线性插值
4. 近距离（到达玩家附近）吸引速度更快（XPGem: 25, Coin: 28），远距离保持基础速度
5. `pickupRange` 和 `magnetRange` 均使用平方值比较（`pickupRangeSqr`、`effectiveMagnetRangeSqr`）
6. `_frameSkipCounter` 在 `OnEnable()` 中通过 `_isBeingMagnetized = false` 隐式重置
7. 一旦进入磁吸状态，后续帧不再跳过（每帧更新距离和方向）

### ✅ #25 🟡 P2 — 波次间歇期（休息时间）【已实现】
**文件**：`UI/WaveIntermissionUI.cs`（新建）、`Enemies/SpawnManager.cs`、`UI/LevelUpUI.cs`、`Core/GameSceneBootstrap.cs`
**实施**：
1. 新建 `WaveIntermissionUI` 组件，屏幕中央显示 "Wave N Complete!" + 本波统计（击杀数、XP、金币）
2. 间歇期时间增加到 3.5 秒（`_restBetweenWaves=3.5f`），给玩家充分休息
3. SpawnManager 在 `RestBeforeNextWave()` 中计算下一波预告（Boss/特殊波次/普通），通知 WaveIntermissionUI
4. 淡入淡出动画（0.3秒淡入，0.5秒淡出），绿色/红色/金色配色
5. LevelUpUI 添加 `HasPendingOptions()` 公共方法，供 WaveIntermissionUI 查询升级提示
6. GameSceneBootstrap 在游戏启动时自动创建 WaveIntermissionUI 实例
7. 波次进行中追踪击杀/XP/金币统计，间歇期时展示

### ✅ #26 🟡 P2 — 游戏内小地图优化【已优化】
**文件**：`UI/MinimapUI.cs`
**实施**：
1. Boss 出现时小地图高亮闪烁 3 秒（12Hz 闪烁频率，亮红+放大标记 + 金色三角指示）
2. 小地图可缩放：`HandleZoom(float scrollDelta)` 支持鼠标滚轮缩放（范围 30-120 格）
3. Boss 标记增强：使用 6px 大洋红色点 + 金色顶部三角指示，闪烁时扩大到 8px 亮红
4. `NotifyBossSpawned()` 公共接口供 BossEnemy 触发闪烁
5. 缩放速度 `_zoomSpeed=10f`，`_minRange`/`_maxRange` 限制缩放范围
6. 原有功能保留：敌人红点、掉落物绿/黄点、地图边界框、玩家荧光青中心点

### ✅ #27 🟡 P2 — 暂停菜单增强（实时统计 + Build 回顾）【已实现】
**文件**：`UI/PauseMenuUI.cs`
**实施**：
1. 暂停菜单改为双面板布局：左侧按钮面板（28% 屏幕宽度）+ 右侧统计面板（68% 屏幕宽度）
2. 左侧保留原有 Resume / Settings / Return to Menu 按钮
3. 右侧实时统计面板（深蓝青背景 + 青色边框）：
   - 📊 GAME STATS 标题
   - WAVE INFO 区域：当前波次、存活敌人数、游戏时间、金币总数
   - PLAYER 区域：HP/MaxHP、护甲、移速、武器名称、伤害倍率
   - MAGE DOT BUILD 区域（仅 Mage 角色显示）：已解锁 DOT 子弹类型 + DPS + 等级、暴击率、暴击倍率、DOT 持续时间加成、引爆倍率
4. 使用 `GameReferences.SpawnManager` 和 `GameReferences.Player` 获取实时数据（零 FindObject 调用）
5. 样式使用 UIColorTheme 统一主题色，所有纹理缓存到字段避免每帧分配
6. 分隔线 + 边框 + 背景纹理均为懒加载缓存

### ✅ #28 🟡 P2 — 敌人死亡动画与特效【已实现】
**文件**：`Enemies/EnemyDeathEffect.cs`（新建）、`Enemies/EnemyBase.cs`
**实施**：
1. 新建 `EnemyDeathEffect` 静态工具类，提供轻量级死亡视觉反馈
2. 敌人死亡时：创建缩小代理（0.2秒缩小+淡出）+ 5-8个爆炸粒子飞散
3. 爆炸粒子基于敌人颜色随机偏移，使用圆形纹理（16x16像素缓存）
4. Boss 死亡自动检测：15个金色粒子 + 0.5秒慢动作 × 0.3时间缩放 + 屏幕震动
5. 死亡粒子组件 `DeathParticle`：快速向外飞散+减速+缩小+淡出，0.3秒后自毁
6. 缩小代理组件 `DeathShrinkProxy`：复制敌人外观，平方衰减曲线缩小+淡出
7. `ScreenShake` 组件挂载到 Camera，支持可控时长和幅度的震动
8. `BossSlowMotionCoroutine` 使用 `WaitForSecondsRealtime`（不受 Time.timeScale 影响）
9. EnemyBase.OnEnemyDeathHandler 在回收/销毁前调用 `EnemyDeathEffect.PlayDeathEffect()`
10. 自动检测 BossEnemy 组件决定是否为 Boss 死亡（isBoss 参数）
11. 所有特效零 GC 分配（纹理缓存，临时 GameObject 自动 Destroy）

### ✅ #29 🟢 P3 — 游戏内成就通知弹窗【已实现】
**文件**：`UI/AchievementUI.cs`
**实施**：
1. 成就解锁时屏幕右上角滑入通知卡片（380×80 像素），包含：🏆 图标 + 金色成就名 + 描述 + 绿色永久加成说明
2. 通知动画：0.3秒缓入滑入 + 4秒展示 + 0.5秒缓出滑出，使用 UnscaledTime 不受暂停影响
3. 多个成就同时解锁时排队显示（`Queue<NotificationEntry>`），当前通知消失后自动显示下一个
4. 金色边框 + 左侧金色条纹 + 深蓝背景，匹配游戏 UI 主题
5. 成就解锁时自动播放升级音效（`SFXManager.PlayLevelUp()`）
6. 通知卡片淡入淡出 alpha 过渡，滑入时使用缓入曲线（ease-in）

### ✅ #30 🟢 P3 — 快捷键提示 HUD【已实现】
**文件**：`UI/KeyHintHUD.cs`（新建）、`Core/GameSceneBootstrap.cs`
**实施**：
1. 新建 `KeyHintHUD` 组件（单例模式），屏幕底部显示半透明键位提示条
2. 默认提示：`WASD Move | Mouse Shoot | F Skill | Q Switch | Tab Shop | ESC Pause`
3. Mage 角色专属提示：`WASD Move | E Detonate | F Skill | Q Switch | Tab Shop | ESC Pause`
4. 首次游戏自动显示 8 秒后缓出淡出（1秒 fade）
5. 按 H 键手动切换显示/隐藏（切换后停止自动隐藏）
6. 半透明深色背景条 + 淡蓝文字 + `[H] Toggle` 小提示
7. GUIScaleHelper 缩放适配（1920×1080 参考分辨率）
8. GameSceneBootstrap 根据角色类型自动设置键位文本

> 📌 后续优化任务（#31-#43）已拆分到 **task3.md**

---

## 总结

| 优先级 | 数量 | 说明 |
|--------|------|------|
| 🔴 P0 | 0 项 | 无紧急项（task.md 已修复所有 P0） |
| 🟠 P1 | 10 项 | 核心玩法体验/性能瓶颈 |
| 🟡 P2 | 14 项 | 内容扩展/体验提升 |
| 🟢 P3 | 6 项 | 长期规划/锦上添花 |
| **合计** | **30 项** | — |

### 分类统计

| 类别 | 数量 | 说明 |
|------|------|------|
| Mage 角色深度优化 | 12 项 | #1-#12 |
| 项目整体性能优化 | 8 项 | #13-#20 |
| 游戏体验优化 | 10 项 | #21-#30 |
| 📌 架构与内容扩展 | — | 已拆分到 **task3.md**（#31-#43） |

### 建议执行顺序

**第一轮（核心体验修复）**：
1. #23 玩家受伤反馈增强（无敌帧）— 直接影响生存体验
2. #21 Boss 战专属 UI — Boss 战是核心玩法高潮
3. #13 DamagePopup 对象池化 — 大量战斗时性能保障
4. #14 Camera.main 缓存 — 简单修改，全局性能提升
5. #2 DOT 子弹穿透集成 — 已有组件未集成
6. #3 DOT 子弹反弹集成 — 已有组件未集成

**第二轮（Mage 深度优化）**：
7. #1 Mage 专属 HUD 统计面板
8. #7 Build 路线提示
9. #4 协同升级系统
10. #6 DOT 图标系统
11. #5 蓄力引爆机制
12. #12 连锁引爆

**第三轮（性能与体验）**：
13. #15 敌人 AI LOD
14. #17 碰撞 Layer 过滤
15. #16 StatusEffect 内存优化
16. #25 波次间歇期
17. #28 敌人死亡动画
18. #27 暂停菜单增强

**第四轮（其余优化）**：
19. #24 XP/金币磁吸优化
20. #26 小地图优化
21. #29 成就通知弹窗
22. #30 快捷键提示
23. 其余未完成 P2/P3 项目

**📌 后续任务请继续执行 task3.md**
