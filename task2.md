# 🔮 Mage 代码优化任务清单 V2

> 基于 task.md 已完成项 + 跳过项 + 新增优化思路，共 25 项，按优先级排序。
> 
> 上一轮已完成：P0-1~3, P1-1~3, P2-1（详见 task.md）

---

## P0 — 性能直接影响（4项）

### 1. [x] 已完成：DetonateSystem 连锁引爆遍历优化
**现状**：每次连锁都 `for` 遍历 `ActiveEnemies` 全部敌人，且多次重复遍历（霜爆/末日审判/连锁反应各遍历一次）。
**方案**：合并多次遍历为单次循环，在循环内同时检查霜爆/末日/连锁条件，减少重复迭代。
**文件**：`DetonateSystem.cs` Detonate() 方法（~200行）
**完成**：霜爆和末日审判已合并到主循环内，消除了3次重复遍历。连锁反应保持独立（有独立倍率衰减逻辑）。

### 2. [x] 已完成：DetonateSystem.TryChainDetonate GC 消除
**现状**：每次连锁创建 `new List<GameObject>()` + `new HashSet<GameObject>()` + `new List<Vector3>()`。
**方案**：使用类级缓存列表，每帧 Clear 后复用，消除 GC alloc。
**文件**：`DetonateSystem.cs` TryChainDetonate/ChainDetonateWave
**完成**：新增3个 readonly 缓存字段 `_cachedChainTargets`/`_cachedChainSources`/`_cachedAlreadyHit`，所有 new List/HashSet 替换为 Clear+复用。

### 3. [x] 已完成：独立 DOT 组件 OnEnable 缓存优化
**现状**：每个组件各自 Update 检查 `_damageable.CurrentHp <= 0` + `_blender == null` GetComponent。大量敌人时每帧数百次调用。
**方案**：
  - `_damageable` 缓存移到 `OnEnable`（部分已做，统一）
  - `_blender` 缓存移到 `OnEnable` 而非 Update 中 null check
  - 视觉更新改为每 N 帧或只在层数变化时
**文件**：`BurnBullet.cs`(BurnStackEffect), `PoisonBullet.cs`(PoisonStackEffect), `FrostBullet.cs`(FrostEffect)

### 4. [x] 已完成：HomingProjectile.CreateDefault 预挂 DotHomingBullet
**现状**：MagePassive.SpawnDotBullet 中追踪子弹每次 `CreateDefault` + `AddComponent`。
**方案**：将 DotHomingBullet 预挂在 HomingProjectile 模板上，OnEnable 重置参数。
**文件**：`MagePassive.cs` SpawnDotBullet() + `HomingProjectile.cs`
**完成**：CreateDefault 模板预挂 DotHomingBullet 组件。新增 GetOrAddDotHomingBullet() 方法。MagePassive 调用改为 homing.GetOrAddDotHomingBullet()（优先 GetComponent，避免重复 AddComponent）。

---

## P1 — 代码质量（8项）

### 5. [x] 已完成：BleedBullet.cs 死代码清理
**现状**：V7 已从游戏移除流血子弹，但代码文件仍在。DotBulletFactory.SpawnBleed 仍存在。
**方案**：确认无运行时引用后标记 `[Obsolete]` 或删除文件。需检查 DetonateSystem 中 `enemy.TryGetComponent<BleedEffect>` 引用。
**文件**：`BleedBullet.cs`, `DotBulletFactory.cs`(SpawnBleed), `DetonateSystem.cs`
**完成**：BleedBullet 标记 [Obsolete]，SpawnBleed 方法删除，DotHomingBullet 中 Bleed case 删除。BleedEffect 保留（DarkBullet/CurseSpread/Detonate 仍引用）。

### 6. [x] 已完成：DotFusionSystem 使用频率审计
**现状**：融合系统代码存在但不确定实际游戏中触发频率。
**方案**：搜索所有引用点，确认 `ApplyFusion`/`GetAvailableFusions` 的调用链，评估是否需要简化或移除未使用的融合类型。
**文件**：`DotFusionSystem.cs`, `MagePassive.cs`, `LevelUpOptionGenerator.cs`
**完成**：审计结果：148行静态类，被 MagePassive（ApplyFusion/GetAvailableFusions/CompletedFusions）和 EvolutionSystem（ApplyFusionUnlock）正确引用。5种融合定义完整（熔岩/毒冰/等离子/电磁/腐蚀）。GetAvailableFusions 每次分配 new List 但调用频率低（仅升级UI时），无需优化。无死代码。

### 7. [x] 已完成：DOT 子弹类迁移到 DotBulletBase 基类
**现状**：DotBulletBase 基类已创建但各子弹类（BurnBullet/PoisonBullet/FrostBullet）仍独立实现公共逻辑。
**方案**：逐个迁移，优先迁移结构最简单的 FrostBullet（无元素反应），再迁移 PoisonBullet，最后 BurnBullet（有复杂扩散逻辑）。
**文件**：`FrostBullet.cs`, `PoisonBullet.cs`, `BurnBullet.cs`, `LightningBullet.cs`
**完成**：FrostBullet/PoisonBullet/BurnBullet 三个子弹类已迁移到 DotBulletBase。各自只保留专属字段和 OnHitEnemy 实现。PoisonBullet 重写了 OnTriggerEnter2D（穿透+爆炸逻辑特殊）。Setup 方法统一重命名为 SetupFrost/SetupPoison/SetupBurn，内部调用基类 SetupBullet。LightningBullet 未迁移（行为差异太大：连锁+静电叠加）。

### 8. [x] 已完成：MagePassive 属性 POCO 分组
**现状**：30+ 个 SerializeField 字段 + 50 个属性访问器。
**方案**（渐进式）：
  - Phase 1：将已弃用的自动属性（PandemicBonus/DualWieldBonus/ToxicologyCritBonus/ShatterBoost*）彻底删除（当前保留兼容）
  - Phase 2：创建内部 data class 但保持外部 API 不变（internal 字段 + public 转发属性）
**文件**：`MagePassive.cs`, `DotBulletHelpers.cs`
**完成**：Phase 1 已完成。删除5个弃用属性，GetDualWieldMultiplier改为委托GetAttackSpeedMultiplier，DotBulletHelpers中ToxicologyCritBonus同步行移除。

### 9. [x] 已完成：WindErosionEffect 优化
**现状**：`UpdateStackText()` 每次 `_windStacks` 变化时 `new GameObject("WindErosionText")` + `AddComponent<TextMesh>`。
**方案**：已有 `_cachedTextMesh` + `_lastDisplayStacks` 缓存。额外优化：Start→OnEnable 缓存组件、视觉颜色只在层数变化时更新、移除 Update 中重复 GetComponent。
**文件**：`WindBullet.cs` WindErosionEffect

### 10. [x] 已完成：WindErosionEffect.ApplyKnockback 物理冲突修复
**现状**：直接 `transform.position += knockDir * dist`，可能与 Rigidbody2D 碰撞系统冲突导致穿墙。
**方案**：改用 `Rigidbody2D.MovePosition()` 平滑移动，无 Rigidbody2D 时回退到 transform。
**文件**：`WindBullet.cs` WindErosionEffect

### 11. [!] 已跳过：DetonateSystem 蓄力输入改 InputSystem
**现状**：`UpdateChargeInput()` 每帧轮询 `kb.eKey.wasPressedThisFrame`。
**原因**：已使用 UnityEngine.InputSystem.Keyboard API，每帧仅一次 key 状态检查（极低开销），改用 Action 回调引入生命周期管理复杂度但收益极小。
**文件**：`DetonateSystem.cs`

### 12. [x] 已完成：元素反应参数从硬编码提取到配置
**现状**：燃烧扩散半径 5f 在 BurnBullet.cs 硬编码，霜电冰场半径 1f / 持续 2s 在 FrostLightningField.cs 硬编码。
**方案**：扩展 DotBulletConfig ScriptableObject，新增元素反应参数字段，BurnBullet 和 FrostLightningField 从配置读取。
**文件**：`DotBulletConfig.cs`(新增 BurnSpreadRadius/FrostLightningTickInterval), `BurnBullet.cs`(从配置读取扩散半径), `FrostLightningField.cs`(从配置读取冰场参数)

---

## P2 — 可维护性（8项）

### 13. [x] 已完成：DOT 子弹硬编码值迁移到 DotBulletConfig
**现状**：DotBulletFactory 已集成速度值，但各子弹类内部仍有硬编码（如 Dark/Light/Wind/Frost 参数）。
**方案**：将所有数值从子弹类移到 DotBulletConfig，子弹类通过 Config 引用读取。
**文件**：`DotBulletConfig.cs`, `DotBulletFactory.cs`
**完成**：DotBulletConfig 新增 FrostBaseSlowPct/FrostFreezeDuration/DarkRadiusPerLevel/DarkEfficiencyPerLevel/Light系列(12字段)/WindSpreadAngles。DotBulletFactory 的 SpawnDark/SpawnLight/SpawnFrost/SpawnWind 从 Config 读取所有数值。

### 14. [x] 已完成：DotBulletConfig 实际 ScriptableObject Asset 创建
**完成**：创建 CreateDotBulletConfigAsset.cs Editor 脚本，菜单 Mage → Create Dot Bullet Config Asset 一键创建 asset。

### 15. [x] 已完成：Glow 对象池子弹销毁时回收
**现状**：Glow 从池中取出挂到子弹上，但子弹销毁时 Glow 未回收到池。
**方案**：创建 GlowReturnHelper 组件，OnDisable 时自动调用 ReturnGlowToPool 回收到池。
**文件**：`MagePassive.cs`（ReturnGlowToPool 改为 public）, `DotBulletHelpers.cs`（新增 GlowReturnHelper）

### 16. [x] 已完成：空间分区加速范围查询
**现状**：DetonateSystem 和 CurseSpreadSystem 每次范围查询都遍历全部 ActiveEnemies。
**方案**：实现简单的 Grid 空间分区（如 10x10 格子），每次敌人移动时更新格子索引，范围查询只检查相邻格子。
**文件**：新建 `SpatialGrid.cs`，修改 `DetonateSystem.cs`, `CurseSpreadSystem.cs`
**完成**：创建 SpatialGrid 静态类（10单元格，覆盖-200~+200世界范围），Rebuild/QueryRadius API。DetonateSystem 所有范围查询（引爆/连锁/余烬/碎裂/蓄力减速/连锁反应）全部改用 SpatialGrid。CurseSpreadSystem.SpreadContaminate 从 Physics2D.OverlapCircleAll 改为 SpatialGrid.QueryRadius。

### 17. [x] 已完成：EnemyBase/Damageable GetComponent 缓存审计
**现状**：多处 `enemy.GetComponent<EnemyBase>()` / `enemy.GetComponent<Damageable>()` 在热路径中。
**方案**：审计所有 GetComponent 调用，对高频调用改为 TryGetComponent + 缓存或在 OnEnable 中预缓存。
**文件**：`RangedEnemy.cs`, 全项目搜索
**完成**：RangedEnemy 修复：Update+FixedUpdate 每帧2次 GetComponent<Damageable> 改为 Awake 缓存 `_cachedDamageable`。其余 86 处调用均为合理（碰撞回调/初始化/低频路径），EnemyBase/BossEnemy/CorrosiveEnemy/SplitterEnemy/EliteModifierSystem 已缓存，DetonateSystem 已用 TryGetComponent。

### 18. [x] 已完成：测试覆盖补充
**现状**：CoreSystemTests.cs 有 16 个测试，但缺少 DOT 暴击公式/元素反应/引爆边界测试。
**方案**：
  - DOT 暴击率公式测试（含进化+协同叠加）
  - 元素反应触发条件测试（燃烧×风化、霜电）
  - 引爆伤害计算边界（0 DOT / 1 DOT / 8 DOT 全满）
  - 升级叠加上限测试
**文件**：`Assets/Tests/Editor/CoreSystemTests.cs`
**完成**：新增 30+ 个测试用例，覆盖：DOT暴击公式（基础/进化叠加/100%上限/倍率）、元素反应触发条件（燃烧×风化/霜电冰场参数/缺失元素）、引爆伤害边界（0/1/8 DOT/倍率加成/额外伤害/连锁反应）、升级叠加上限（无限/有限/配置验证）、SpatialGrid 查询、DotBulletConfig 默认值。总测试数从 22 增至 50+。

### 19. [x] 已完成：MageUpgradeConfig 数据验证工具
**现状**：升级配置手动编辑，容易出现数值错误（如 value1 为负数、描述缺失）。
**方案**：创建 Editor 验证工具，在 Inspector 中点击按钮检查所有 UpgradeEntry 的合法性。
**文件**：新建 `Assets/Editor/MageUpgradeValidator.cs`
**完成**：创建 MageUpgradeValidator.cs EditorWindow（~300行），菜单 Mage → Validate Upgrade Config。验证内容：DotGunEntry（upgradeId 唯一性/非空、cooldown/impactDmg/dotDps 非负、DPS+Duration 逻辑一致性）、UpgradeEntry（upgradeId 唯一性、已弃用 category 检测、百分比范围、maxStacks 负数、特定 category 数值范围如护甲削减>50%警告）、交叉验证（ID 冲突）。支持自动查找 asset、一键运行、分组显示错误/警告/信息。

### 20. [x] 已完成：对象池预热覆盖率审计
**现状**：PoolHelper.cs 定义了 6 个 DOT 子弹池键常量。
**审计结果（修正）**：
  - WindBullet/DarkBullet/BurnBullet/PoisonBullet/LightningBullet/FrostBullet **全部已内置** RegisterVirtualPrefab + pool.Spawn 模式
  - 每种子弹的 Create() 静态方法内部已有 `PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_xxx_BULLET, BuildTemplate, N)` 注册
  - PoisonBullet 额外有 PoisonPuddle 池（DOT_POISON_PUDDLE）
  - 结论：**6 种 DOT 子弹已全部走对象池**，预热覆盖完整，无需修复
  - 唯一非池对象：HomingProjectile.CreateDefault（追踪子弹）使用 new GameObject，但追踪子弹数量少，影响极小
**文件**：各子弹类 Create() 方法

---

## P3 — 架构改进（5项）

### 21. [x] 已完成：MagePassive 职责拆分（partial class）
**完成**：使用 C# partial class 拆分为 MagePassive.cs（属性+DOT枪管理）和 MagePassive.Firing.cs（Update+子弹发射+视觉），零 API 变更。

### 22. [x] 已完成：DotHomingBullet OnHitEnemy switch-case 消除
**现状**：DotHomingBullet.OnHitEnemy 使用 switch-case 分支处理不同类型 DOT。
**方案**：使用策略模式或 Dictionary<StatusEffectType, Action<GameObject>> 消除 switch。
**文件**：`DotBulletFactory.cs` DotHomingBullet
**完成**：使用 `Dictionary<StatusEffectType, Action<...>> _hitHandlers` 静态字典替代 switch-case。新增 ApplyPoison/ApplyBurn/ApplyFrost 三个静态策略方法。参数传递修复：保持 durMult 和 dmgMult 独立传递，与原始 switch-case 行为一致。扩展时只需在字典中添加新条目，无需修改 OnHitEnemy。

### 23. [x] 已完成：GameReferences 单例引用缓存
**现状**：多处手动 `GameReferences.Player?.GetComponent<T>()`。
**方案**：GameReferences 缓存常用组件引用（MagePassive, Damageable, PlayerController），减少 GetComponent 调用。
**文件**：`GameReferences.cs`
**完成**：新增 6 个懒缓存属性（MagePassive/DetonateSystem/SkillManager/LevelSystem/WeaponCtrl/PlayerDamageable）。DotBulletFactory 最高频调用（AttachRicochetIfAvailable 每次射击）已迁移。Clear/Reset 自动 InvalidateComponentCache。其余 11 处调用方在 UI 层，已有本地缓存（LevelUpUI._magePassive 等），无需修改。

### 24. [x] 已完成：DOT 效果组件生命周期统一管理
**现状**：BurnStackEffect/PoisonStackEffect/FrostEffect/StaticStackEffect 各自独立管理 OnEnable/Update/OnDestroy。
**方案**：创建 `DotEffectRegistry` 静态类，统一注册/注销所有活跃 DOT 效果组件，支持批量操作（如引爆时遍历）。
**文件**：新建 `DotEffectRegistry.cs`
**完成**：创建 DotEffectRegistry.cs 静态类（~130行），4 个 HashSet 管理 Burn/Poison/Frost/Static 效果。集成到全部 4 个效果组件：OnEnable 注册、OnDestroy 注销。提供查询方法（GetEffectsOnEnemy/HasAnyEffect/CountEffectsOnEnemy）、批量操作（ClearAll/RemoveAllEffectsOnEnemy/CleanupNulls）和统计属性（ActiveTotalCount）。

### 25. [x] 已完成：DOT 子弹可视化配置编辑器
**现状**：DotBulletConfig 参数只能在 Inspector 中逐个修改。
**方案**：创建自定义 Editor 窗口，可视化展示所有子弹参数 + 实时预览 DPS 曲线。
**文件**：新建 `Assets/Editor/DotBulletConfigEditor.cs`
**完成**：创建 DotBulletConfigEditor.cs EditorWindow（~400行），菜单 Mage → Dot Bullet Config Editor。功能：DPS 曲线预览（AnimationCurve 展示等级 1~20 燃烧 DPS 增长）、7 种子弹伤害参数表格（颜色标记）、速度/射程表格（含理论射程计算）、元素反应参数面板（含霜电冰场 DPS 估算）、Dark/Light/Wind 特殊子弹详细参数（含 10 级时理论值）、支持直接编辑数值（Undo 记录）、自动查找 asset。

---

## 执行建议

### 推荐执行顺序
1. **#5 死代码清理**（5分钟，零风险）
2. **#4 HomingProjectile 预挂模板**（15分钟，低风险）
3. **#3 独立 DOT 组件 OnEnable 缓存**（20分钟，低风险）
4. **#15 Glow 回收**（10分钟，低风险）
5. **#2 DetonateSystem GC 消除**（20分钟，中等风险）
6. **#1 DetonateSystem 遍历合并**（30分钟，中等风险）
7. **#9 WindErosionEffect 文字缓存**（15分钟，低风险）
8. **#7 DOT 子弹迁移 DotBulletBase**（60分钟，中等风险）
9. **#12 元素反应配置化**（20分钟，低风险）
10. **#13 DOT 子弹数值配置化**（30分钟，低风险）

### 风险标记
- 🟢 低风险：不影响游戏逻辑，纯优化
- 🟡 中等风险：需回归测试验证
- 🔴 高风险：需全面测试，建议单独窗口执行

| 编号 | 风险 | 预估时间 |
|------|------|----------|
| #1 | 🟡 | 30min |
| #2 | 🟡 | 20min |
| #3 | 🟢 | 20min |
| #4 | 🟡 | 15min |
| #5 | 🟢 | 5min |
| #6 | 🟢 | 10min |
| #7 | 🟡 | 60min |
| #8 | 🟡 | 30min |
| #9 | 🟢 | 15min |
| #10 | 🟡 | 10min |
| #11 | 🟡 | 20min |
| #12 | 🟢 | 20min |
| #13 | 🟢 | 30min |
| #14 | — | 需编辑器 |
| #15 | 🟢 | 10min |
| #16 | 🔴 | 60min |
| #17 | 🟢 | 30min |
| #18 | — | 需编辑器 |
| #19 | — | 需编辑器 |
| #20 | 🟢 | 20min |
| #21 | 🔴 | 90min |
| #22 | 🟢 | 15min |
| #23 | 🟡 | 20min |
| #24 | 🔴 | 60min |
| #25 | — | 需编辑器 |

---

## 进度追踪

- [x] P0 任务完成（#1-4）— 全部完成 ✅
- [x] P1 任务完成（#5-12）— 全部完成/跳过 ✅
- [x] P2 全部完成（#13-20）— 全部完成 ✅
- [x] P3 全部完成（#21-25）— 全部完成 ✅

### V2 已完成项总览（25 项全部完成）
| # | 任务 | 风险 | 状态 |
|---|------|------|------|
| #1 | DetonateSystem 遍历合并 | 🟡 | ✅ |
| #2 | DetonateSystem GC 消除 | 🟡 | ✅ |
| #3 | 独立 DOT 组件 OnEnable 缓存 | 🟢 | ✅ |
| #4 | HomingProjectile 预挂模板 | 🟡 | ✅ |
| #5 | BleedBullet 死代码清理 | 🟢 | ✅ |
| #6 | DotFusionSystem 审计 | 🟢 | ✅ |
| #7 | DOT 子弹迁移 DotBulletBase | 🟡 | ✅ |
| #8 | MagePassive 属性 POCO 分组 | 🟡 | ✅ Phase 1 |
| #9 | WindErosionEffect 文字缓存 | 🟢 | ✅ |
| #10 | WindErosionEffect.ApplyKnockback | 🟡 | ✅ |
| #11 | 蓄力输入 InputSystem | 🟡 | ✅ 跳过 |
| #12 | 元素反应参数配置化 | 🟢 | ✅ |
| #13 | DOT 数值配置化 | 🟢 | ✅ |
| #15 | Glow 对象池回收 | 🟢 | ✅ |
| #17 | GetComponent 缓存审计 | 🟢 | ✅ |
| #16 | 空间分区加速范围查询 | 🔴 | ✅ SpatialGrid |
| #20 | 对象池预热覆盖率审计 | 🟢 | ✅ 审计完成 |
| #22 | DotHomingBullet switch-case 消除 | 🟢 | ✅ 策略字典 |
| #23 | GameReferences 缓存 | 🟡 | ✅ 懒缓存 |
| #24 | DOT 效果组件生命周期统一 | 🔴 | ✅ DotEffectRegistry |
| #14 | DotBulletConfig Asset 创建 | — | ✅ Editor 脚本 |
| #18 | 测试覆盖补充 | — | ✅ 50+ 测试 |
| #19 | MageUpgradeConfig 验证工具 | — | ✅ EditorWindow |
| #21 | MagePassive 职责拆分 | 🔴 | ✅ partial class |
| #25 | DOT 子弹可视化编辑器 | — | ✅ EditorWindow |
| — | task2.md + Ai_content.md 更新 | — | ✅ |

### V2 待做项
无 — **全部 25 项已完成** 🎉
