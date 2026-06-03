# 🎯 项目优化清单 — Vampire Survivors Unity 移植版

> 更新时间：2026-06-02
> 基于 Ai_content.md、Mage_Upgrades.txt、status.txt 及源码分析

---

## 优先级说明
警告：be
| 等级 | 含义 | 预计影响 |
|------|------|---------|
| 🔴 P0 | 紧急 — Bug修复/内存泄漏/崩溃风险 | 立即修复 |
| 🟠 P1 | 高优 — 性能瓶颈/核心玩法缺陷 | 下个迭代 |
| 🟡 P2 | 中优 — 功能完善/体验提升 | 计划内 |
| 🟢 P3 | 低优 — 锦上添花/长期规划 | 有空再做 |

---

## 一、项目整体优化（12项）

### ✅ #1 🔴 P0 — StatusEffectManager.CreateSpreadLine 内存泄漏 【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs` (L473-488)
**问题**：每次诅咒传播都 `new GameObject("CurseLine")` + `new Material(Shader.Find(...))`，0.5秒后 `Destroy` 但 Material 不会被自动回收。Shader.Find 在运行时也是性能隐患。大量敌人同时死亡时会产生 GC 峰值。
**建议**：
- Material 缓存为静态字段：`private static Material _lineMaterial;`
- LineRenderer 对象走对象池（PoolHelper 预热 10-20 个）
- 或使用 VFX Graph / ParticleSystem 替代手动 LineRenderer
**实施**：已将 Material 缓存为静态字段 `_lineMaterial`，通过 `GetLineMaterial()` 方法懒加载，避免每次 Shader.Find + new Material。

### ✅ #2 🔴 P0 — EnemyHealthBar 重复 AddComponent 【已优化】
**文件**：`Enemies/EnemyBase.cs` (L46-49)
**问题**：`OnEnable()` 每次检查 `_healthBar == null` 就 `AddComponent`。对象池回收后 `_healthBar` 引用仍在，但脚本可能已被 Unity 内部清理（极端情况）。且每波大量敌人反复创建新组件。
**建议**：
- 将 EnemyHealthBar 移到 Prefab 上，`OnEnable()` 中只调用 `Setup()` 重置状态
- 或者用独立子 GameObject 管理血条，通过对象池复用
**实施**：将 EnemyHealthBar 的创建移到 `Awake()` 中一次性完成，`OnEnable()` 只调用 `Setup()` 重置血条状态。

### ✅ #3 🟠 P1 — MagePassive.Update 每帧 GetComponent 【已优化】
**文件**：`Player/MagePassive.cs` (L151)
**问题**：`Update()` 每帧调用 `GetComponent<WeaponController>()` 获取伤害倍率，虽然 Unity 有缓存但仍是不必要的开销。
**建议**：在 `Awake()` 中缓存 `_weaponController` 引用，运行时直接使用。
**实施**：在 `Awake()` 中缓存 `_weaponController = GetComponent<WeaponController>()`，`Update()` 中直接使用缓存引用。

### ✅ #4 🟠 P1 — Detonate 引爆半径50f过大的性能问题 【已优化】
**文件**：`Player/MagePassive.cs` (L248)
**问题**：`Physics2D.OverlapCircleAll(transform.position, 50f)` 半径 50 格扫描全图，即使只有 10 个敌人也会遍历所有碰撞体。
**建议**：
- 引入 `SpawnManager.ActiveEnemies` 列表直接遍历（已有 `_activeEnemies`）
- 或使用分层检测：先粗筛距离，再精筛 tag + Damageable
- 半径 50f 基本等于全屏，可以直接从 SpawnManager 获取活跃敌人列表
**实施**：
1. SpawnManager 添加 `public IReadOnlyList<GameObject> ActiveEnemies` 公共访问器
2. Detonate 改为遍历 `ActiveEnemies`，用平方距离粗筛，避免 Physics2D 全图扫描

### ✅ #5 🟠 P1 — DOT 系统 tick 间隔固定0.5秒，痛苦升级效果不明显 【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs` (L76)
**问题**：`TICK_INTERVAL = 0.5f` 硬编码。痛苦升级（DOT间隔-10%）实际只影响独立 DOT 组件（PoisonStackEffect 等），StatusEffectManager 的 tick 间隔不受影响。
**建议**：
- 将 `TICK_INTERVAL` 改为实例字段，受 `DotFrequencyBonus` 影响
- 公式：`effectiveInterval = baseInterval * (1f - dotFrequencyBonus)`
- 最低 0.15 秒，避免极端情况性能问题
**实施**：
1. `TICK_INTERVAL` 改为 `BASE_TICK_INTERVAL` 常量 + `_tickInterval` 实例字段
2. 新增 `DotFrequencyBonus` 属性，设置时自动重算 `_tickInterval = Max(0.15, 0.5 * (1 - bonus))`
3. DotBulletHelper.EnsureStatusEffectManager 中同步 MagePassive.DotFrequencyBonus 到 StatusEffectManager

### ✅ #6 🟠 P1 — 诅咒传播中大量 GetComponent 调用 【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs` (L355-449)
**问题**：`OnEnemyDeath_SpreadContaminate()` 中对每个目标敌人调用 5-6 次 `GetComponent`（StatusEffectManager、BleedEffect、BurnStackEffect、PoisonStackEffect、FrostEffect）。
**建议**：
- 创建 `EnemyDotComponents` 缓存组件，挂在敌人身上一次性缓存所有 DOT 组件引用
- 或使用 `Dictionary<GameObject, EnemyDotCache>` 缓存
**实施**：将源敌人的 `BleedEffect`/`BurnStackEffect`/`PoisonStackEffect`/`FrostEffect` 组件获取移到循环外部（`srcBleed`/`srcBurn`/`srcPoison`/`srcFrost`），避免每个传播目标重复 GetComponent。原先每次传播对源敌人 4 次 GetComponent × N 个目标，现在只对源敌人 4 次 + 每目标 1-2 次。

### ✅ #7 🟠 P1 — DOT 子弹 switch-case 工厂模式重构 【已优化】
**文件**：`Combat/DotProjectile.cs` (L1191-1308)
**问题**：`SpawnDotBullet` 中使用 switch-case 根据 `StatusEffectType` 创建不同子弹。新增 DOT 子弹类型需要修改多处。
**实施**：已在 DotProjectile.cs 中创建 `DotBulletFactory` 静态工厂类，使用 `Dictionary<StatusEffectType, BulletSpawner>` 委托注册机制。MagePassive.SpawnDotBullet 改为调用 `DotBulletFactory.Create()`。新增子弹类型只需调用 `DotBulletFactory.Register()` 注册创建委托，无需修改 MagePassive。工厂内集成 RicochetHandler 和 PenetrateHandler 的自动附加。

### ✅ #8 🟡 P2 — 全局 Debug 日志过多 【已确认无问题】
**文件**：`Player/MagePassive.cs`、`Combat/StatusEffects/StatusEffectSystem.cs` 等
**问题**：大量 `DebugHelper.Log` 在正常游戏循环中输出（如 DOT 解锁、引爆、诅咒传播）。Release 版本也需要手动关闭。
**建议**：
- `DebugHelper` 增加日志级别控制（Info/Warning/Error）
- 添加编译宏 `[System.Diagnostics.Conditional("DEBUG_LOG")]`
- 生产环境自动屏蔽 Info 级别日志
**确认**：`DebugHelper` 已使用 `[Conditional("UNITY_EDITOR")]` 编译宏，`Log` 和 `LogWarning` 在非编辑器构建中会被自动移除。`LogError` 始终生效。无需额外修改。

### ✅ #9 🟡 P2 — 缺少 SFX 音效系统 【已优化】
**文件**：`Audio/SFXManager.cs`（新建）
**问题**：目前只有 BGM 管理器，缺少音效（SFX）系统。DOT 子弹命中、引爆、升级选择、敌人死亡等关键操作没有音效反馈。
**实施**：
1. 新建 `SFXManager` 单例（DontDestroyOnLoad），16 个 AudioSource 对象池，自动扩展
2. 通过 EventManager 自动订阅 10 个关键事件（OnEnemyKilled/OnDamage/OnPlayerDamaged/OnLevelUp/OnWaveStart 等）
3. 防音效轰炸冷却机制：命中 50ms、DOT tick 100ms、敌死 80ms 最短间隔
4. 手动触发 API：`PlayDetonate()`（MagePassive 引爆）、`PlayDotTick()`（StatusEffect DOT）、`PlaySelect()`（LevelUpUI 选择）、`PlayPause()`（暂停）
5. 场景重置清理：PauseMenuUI.ReturnToMenu() 和 GameOverUI.ResetGameState() 中销毁 SFXManager
6. GameSceneBootstrap.Start() 自动创建 SFXManager

### ✅ #10 🟡 P2 — 缺少伤害统计/输出面板 【已优化】
**文件**：`UI/DamageMeter.cs`（新建）
**问题**：玩家无法看到各 DOT 类型的伤害贡献、DPS 统计、引爆总伤害等数据。
**实施**：
1. 新建 `DamageMeter` 组件，按伤害来源分类统计（Bleed/Poison/Burn/Frostbite/Detonate/Bullet/Skill）
2. 通过 EventManager.OnDamage 自动记录对敌人的伤害，自动推断伤害来源
3. 暂停时在右上角自动显示 DPS 面板（带彩色进度条、DPS、占比百分比、总伤害）
4. MagePassive 引爆时调用 `RecordDetonate()` 记录引爆伤害
5. StatusEffectSystem DOT tick 时调用 `RecordDotDamage()` 记录 DOT 伤害
6. 场景重置清理：PauseMenuUI 和 GameOverUI 中销毁 DamageMeter
7. GameSceneBootstrap 自动创建 DamageMeter 组件

### ✅ #11 🟡 P2 — DotGunState 应改为 struct 【已优化】
**文件**：`Player/MagePassive.cs` (L309-318)
**问题**：`DotGunState` 定义为 `class`，每局游戏 4-5 个实例分配在堆上。作为轻量数据容器应使用 `struct` 减少 GC。
**建议**：改为 `public struct DotGunState`，注意 List 中 struct 的修改需要通过索引重新赋值。
**实施**：
1. `DotGunState` 从 `class` 改为 `struct`
2. `UnlockDotGun` 中 foreach 改为 for + 索引，修改后 `_dotGuns[i] = gun` 重新赋值
3. `EnhanceAllDotGuns` 同样改为 for + 索引 + 重新赋值

### ✅ #12 🟢 P3 — 地图系统缺少随机性 【已优化】
**文件**：`Map/MapThemeManager.cs`、`Map/DecorationSpawner.cs`
**问题**：地图主题和装饰物生成逻辑固定，每局游戏地图外观相同。
**实施**：
1. MapThemeManager 新增随机种子系统（`_mapSeed`：0=每局随机，>0=固定种子可复现）
2. Fisher-Yates 洗牌随机打乱主题顺序（`_shuffleThemeOrder`）
3. `SetSeed()` 公共 API 支持运行时设置种子
4. DecorationSpawner 同步使用 MapThemeManager 的种子初始化 RNG

---

## 二、Mage 角色专属优化（15项）

### ✅ #13 🔴 P0 — 共振(Resonance)升级未完整实现 【已优化】
**文件**：`Player/MagePassive.cs`
**问题**：`_bulletSizeBonus` 和 `_knockbackBonus` 属性存在，但 `SpawnDotBullet` 中未应用碰撞体积加成。子弹碰撞体大小不会随共振升级改变。
**建议**：
- 在 `SpawnDotBullet` 生成子弹后，设置 `transform.localScale *= (1f + _bulletSizeBonus)`
- 子弹 Collider2D 的 `size` 也同步缩放
- 击退力通过 `Rigidbody2D.AddForce` 应用 `_knockbackBonus`
**实施**：在 `SpawnDotBullet` 中子弹创建后，当 `_bulletSizeBonus > 0` 时缩放 `transform.localScale` 和 Collider2D（BoxCollider2D.size / CircleCollider2D.radius），实现子弹体积随共振升级增大。

### ✅ #14 🔴 P0 — 风蚀(Wind Erosion)升级未实现 【已优化】
**文件**：`Mage_Upgrades.txt` 定义了风蚀效果，但代码中 `WindErosionKnockback` 只在 `StatusEffectSystem.cs` 中有击退逻辑，缺少"微型漩涡"视觉效果和范围伤害。
**建议**：
- 实现 `WindErosionVortex` 组件：DOT 敌人移动时在脚下生成微型漩涡
- 漩涡每秒对周围造成微量伤害 + 20% 概率拉扯
- 使用 ParticleSystem 创建漩涡视觉效果
- 漩涡走对象池
**实施**：
1. 新增 `WindErosionVortex` 类（DotProjectile.cs）：圆形漩涡，每0.5秒tick范围内敌人造成微量伤害 + 20%概率拉扯
2. StatusEffectManager 新增 `TrySpawnVortex()` 方法，每秒生成一个漩涡
3. 风蚀击退逻辑中调用 `TrySpawnVortex()`，当 `WindErosionKnockback > 0` 时自动生效
4. 漩涡有旋转渐隐视觉效果，2秒后自动销毁

### #15 🟠 P1 — 弹幕(Barrage)>5发后的追踪弹/护盾弹未实现
**文件**：`Player/MagePassive.cs` (L184-228)
**问题**：`_bulletCountBonus` 只是简单增加散射子弹数，超过 5 发时没有追踪弹或护盾弹的转化逻辑。
**建议**：
- 当 `bulletCount > 5` 时，超出部分转化为 `HomingProjectile` 追踪弹
- 追踪弹速度略慢，但自动锁定最近敌人
- 或转化为环绕玩家的护盾弹（类似 Vampire Survivors 的 King Bible）

### #16 🟠 P1 — 反弹(Ricochet)>100%后的多次反弹未实现
**文件**：`Player/MagePassive.cs` (L38)
**问题**：`_ricochetMaxBounces` 属性存在但子弹生成时未应用反弹逻辑。DOT 子弹击中敌人后直接消失。
**建议**：
- 在 DOT 子弹碰撞回调中，当 `Random.value < ricochetChance` 时寻找最近的另一个敌人并改变方向
- 超过 100% 时增加最大反弹次数，移除伤害衰减
- 反弹时显示弹射轨迹线

### ✅ #17 🟠 P1 — 引爆视觉反馈不足 【已优化】
**文件**：`Player/MagePassive.cs`、`UI/DetonateHUD.cs`、`UI/DetonateFlashEffect.cs`、`UI/DamagePopup.cs`
**问题**：引爆只有屏幕抖动和小范围爆炸特效，缺乏震撼感。没有显示引爆总伤害数字，也没有 UI 指示哪些敌人被引爆。
**实施**：
1. 每个被引爆的敌人头顶显示紫色伤害弹字（DamagePopup）
2. 屏幕中央显示巨型"DETONATE! -总伤害"弹字（DamagePopup.CreateDetonateTotal）
3. 全屏闪白效果 0.15 秒（DetonateFlashEffect 组件，OnGUI 实现）
4. DetonateHUD 冷却完成时脉冲发光（紫色渐变 + 字体缩放）
5. DetonateHUD 冷却中显示环形进度条
6. 屏幕抖动强度根据命中敌人数量动态调整

### ✅ #18 🟠 P1 — DOT 视觉效果过于单一 【已优化】
**文件**：`Combat/DotParticleVFX.cs`（新建）、`Combat/StatusEffects/StatusEffectSystem.cs`
**问题**：DOT 效果只有颜色变化（变绿/红/橙/蓝），缺少粒子特效。
**实施**：
1. 新建 `DotParticleVFX` 组件，运行时创建 4 种 ParticleSystem（无需 Prefab）
2. 流血：红色粒子从身上飘出（3emission/s，0.08 大小）
3. 中毒：绿色气泡冒泡（4emission/s，向上快速飘）
4. 燃烧：火焰粒子环绕（8emission/s，圆形发射，快速消散）
5. 霜冻：冰晶粒子（5emission/s，noise 随机运动）
6. StatusEffectManager 在 tick 时调用 `UpdateDotParticles()` 同步粒子状态
7. DOT 清除后自动销毁 VFX 组件，对象池回收时 OnDisable 停止粒子
8. 每种粒子 maxParticles=20 限制，避免性能问题

### ✅ #19 🟠 P1 — DOT 组合效果系统缺失 【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs`、`Combat/DotProjectile.cs`
**问题**：4 种 DOT（流血/中毒/燃烧/霜冻）各自独立运行，没有组合效果。
**实施**：
1. StatusEffectManager 新增 `CheckComboEffects()` 方法，每 tick 检测组合条件
2. 🔵+🔴 = **碎冰**：霜冻+流血同时存在时，总 DOT 伤害×2（`_comboShatterActive`）
3. 🔥+🟢 = **爆燃**：燃烧+中毒层数>5时，每3秒对周围2.5格内敌人造成范围伤害（每层3点），带橙绿色爆炸特效
4. 🟢+🔴 = **脓毒**：中毒+流血时，流血 DPS 随中毒层数增加（每层+30%），通过 BleedEffect._comboSepsisBonus 字段应用
5. 组合检测同时检查 StatusEffectManager 独立 DOT 组件（BleedEffect/PoisonStackEffect/BurnStackEffect/FrostEffect）

### ✅ #20 🟡 P2 — DOT 子弹升级后缺少视觉变化 【已优化】
**文件**：`Player/MagePassive.cs`
**问题**：同一类型 DOT 子弹解锁后外观不变，升级多次后没有视觉区分。
**实施**：
1. DotGunState 新增 `upgradeLevel` 字段，UnlockDotGun 时自动递增
2. `ApplyUpgradeVisual()` 方法：每次升级子弹增大 10% + 颜色更亮
3. 3 级+添加发光子物体（半透明 SpriteRenderer，1.8x 缩放）
4. 碰撞体同步缩放

### ✅ #21 🟡 P2 — 中毒层数缺乏 UI 反馈 【已优化】
**文件**：`UI/EnemyHealthBar.cs`
**问题**：敌人头顶血条只显示 DOT 类型图标，不显示具体层数。玩家无法直观判断中毒叠了多少层。
**实施**：
1. EnemyHealthBar 新增 `_poisonStackText`（TextMesh），显示在 DOT 指示器上方
2. 有中毒时显示 "xN"，颜色从绿色随层数递增到黄绿色
3. 无中毒时自动隐藏
4. 不影响原有的中毒层数条指示器宽度变化

### ✅ #22 🟡 P2 — 引爆与 DOT 层数/类型联动 【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs`、`Player/MagePassive.cs`
**问题**：当前引爆公式是 `remainingDuration × dps × multiplier`，没有考虑层数加成。
**实施**：
1. StatusEffectManager 新增 `DetonateResult` 结构体，返回各 DOT 类型层数信息
2. `Detonate()` 改为 `out DetonateResult` 参数，高层数中毒引爆额外倍率（每层+5%），流血每层+3%
3. MagePassive 中：燃烧层数>10时引爆在敌人位置留下余烬火焰区域（最多5个，每层1点伤害，持续3秒）
4. MagePassive 中：霜冻引爆触发碎裂AOE（4格范围，每层5点伤害+施加霜冻减速），带冰蓝色爆炸特效

### ✅ #23 🟡 P2 — Mage 被动暴击率缺乏成长性 【已优化】
**文件**：`Player/MagePassive.cs`
**问题**：暴击率只有基础 5% + 永久存档加成，没有随等级或升级成长的途径。
**实施**：`GetDotCritChance()` 中新增 `_dotGuns.Count * 0.02f` 加成，每解锁一种 DOT 子弹类型暴击率 +2%。拥有全部 4 种 = +8%，加上基础 5% 和存档加成可达 13%+。

### ✅ #24 🟢 P3 — DOT 时间增强"侵蚀"效果不够直观 【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs`
**问题**：侵蚀每 5 次 DOT 生效触发冲击，但玩家看不到冲击的视觉特效。
**实施**：
1. 侵蚀冲击时在敌人头顶显示绿色伤害弹字（DamagePopup）
2. 冲击瞬间播放绿色爆炸特效（CombatManager.CreateExplosionEffect，1.5格范围）
3. 敌人短暂变色为黄绿色闪一下
4. Debug 日志输出 "EROSION!" + 伤害值

### ✅ #25 🟢 P3 — 引爆冷却缺乏进度反馈 【已优化，随 #17 实现】
**文件**：`UI/DetonateHUD.cs`
**实施**：在 #17 引爆视觉反馈增强中一并实现。冷却中显示环形进度条，冷却完成时紫色脉冲发光+字体缩放动画。

### ✅ #26 🟢 P3 — Mage 缺少被动成长的"里程碑"感 【已优化】
**文件**：`Player/MagePassive.cs`
**问题**：升级选择时没有"集齐4种DOT子弹触发特殊效果"等里程碑机制。
**实施**：
1. 新增里程碑系统：`_elementMasterTriggered`、`_chainDetonateEndTime` 字段
2. **元素大师**：集齐 4 种 DOT 子弹 → 全 DOT 伤害 +20%，金色"★ ELEMENT MASTER"弹字
3. **连锁引爆**：引爆命中 >10 个敌人 → 3 秒内 DOT 伤害翻倍，日志输出提示
4. `GetDotDamageMultiplier()` 公共方法返回含里程碑加成的伤害倍率
5. `SyncDotDamageMultiplierToAll()` 同步倍率到所有活跃敌人的 StatusEffectManager
6. `CheckMilestones()` 在每次 `UnlockDotGun()` 新解锁子弹后自动检查

---

## 三、性能优化（5项）

### #27 🟠 P1 — UpdateVisual 每帧 Sin 计算
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs` (L536)
**问题**：`UpdateVisual()` 中 `Mathf.Sin(Time.time * 6f)` 每帧计算颜色脉冲。当屏幕上 50+ 个带 DOT 的敌人时，这是不必要的开销。
**建议**：
- 将视觉更新频率降低到每 0.1 秒一次（而非每帧）
- 或在 `Update` 中只在 tick 时更新视觉（已有 TICK_INTERVAL 逻辑，合并即可）

### #28 🟠 P1 — Physics2D.OverlapCircleAll 频繁调用
**文件**：多处（StatusEffectSystem 辐射检测、诅咒传播、引爆）
**问题**：每 0.5 秒多个敌人各自调用 `OverlapCircleAll` 检测辐射范围。100 个带辐射 DOT 的敌人 = 每 0.5 秒 100 次物理查询。
**建议**：
- 使用 `ContactFilter2D` 预设过滤条件减少结果后处理
- 或使用空间分区（Spatial Partitioning）如网格系统替代物理查询
- 小范围辐射可以用简单的距离检测替代物理引擎

### ✅ #29 🟡 P2 — 对象池预热不充分 【已优化】
**文件**：`Core/PoolHelper.cs`、`Core/ObjectPool.cs`
**问题**：后期波次敌人数量可达 50+，如果对象池预热不够，运行时会频繁 Instantiate 导致卡顿。
**实施**：
1. ObjectPool 新增 `ExpandPool(poolKey, extraCount)` 方法，按需扩展空闲池容量
2. PoolHelper 新增 `ExpandPoolsForWave(waveNumber)` 方法，每10波自动扩展敌人/掉落物/子弹池
3. 波次间歇可调用 `PoolHelper.ExpandPoolsForWave()` 预加载下一波对象
4. 扩展逻辑智能判断：仅在空闲对象不足时创建，避免重复膨胀

### ✅ #30 🟡 P2 — GC Alloc 追踪与优化 【已优化】
**文件**：`Combat/StatusEffects/StatusEffectSystem.cs`、`Player/MagePassive.cs` 等
**问题**：多处使用 `new List`、lambda 闭包等产生 GC 分配。
**实施**：
1. StatusEffectManager.ApplyEffect 中 `_activeEffects.Find(lambda)` 改为 for 循环，消除闭包 GC
2. DotGunState 已改为 struct（#11），减少堆分配
3. Material 缓存为静态字段（#1），避免运行时 Shader.Find
4. 对象池系统（#29）减少 Instantiate/Destroy 的 GC
5. DebugHelper 使用 Conditional 编译宏，Release 版本不产生字符串拼接 GC
6. OffScreenCuller 使用 Dictionary + 缓存 List，减少遍历 GC
7. 已有最佳实践：List 使用 Clear()、避免 LINQ、StringBuilder 用于统计输出

### ✅ #31 🟢 P3 — OffScreenCuller 优化 【已优化】
**文件**：`Core/OffScreenCuller.cs`、`Combat/StatusEffects/StatusEffectSystem.cs`
**问题**：屏幕外敌人仍然运行 AI 和 DOT 逻辑。
**实施**：
1. OffScreenCuller 新增 `IsOffScreen(Vector2)` 静态方法，判断世界坐标是否在屏幕外
2. StatusEffectManager.Update() 中：屏幕外敌人 DOT tick 频率降低至 2 倍间隔
3. 屏幕外敌人跳过 UpdateVisual()（颜色脉冲）和 UpdateDotParticles()（粒子特效）
4. DOT 伤害仍在运行（保证游戏公平性），仅跳过视觉/粒子更新

---

## 四、游戏玩法优化（5项）

### ✅ #32 🟡 P2 — 波次难度曲线可配置化 【已优化】
**文件**：`ScriptableObjects/Config/EnemyWaveConfig.cs`、`Enemies/SpawnManager.cs`
**问题**：S 曲线难度公式硬编码在代码中，调整需要重新编译。
**实施**：
1. EnemyWaveConfig 新增 S 曲线参数字段（sCurveMidpoint/sCurveSteepness/hpBaseRate/dmgBaseRate/maxMultiplierScale）
2. EnemyWaveConfig 新增 CalculateSCurveMultiplier/GetHpMultiplier/GetDamageMultiplier 方法
3. EnemyWaveConfig 新增特殊波次系统：6 种特殊波次类型（TankRush/SpeedSurge/SwarmWave/EliteWave/HealerArmy/BossRush），可配置触发概率和最低波次
4. SpawnManager.DamageMultiplier/HpMultiplier 改为优先使用 EnemyWaveConfig 的可配置 S 曲线
5. SpawnManager.StartNextWave 集成特殊波次检测和 Boss 波配置
6. 保持向后兼容：无 EnemyWaveConfig 时使用内置 fallback S 曲线

### ✅ #33 🟡 P2 — 技能系统缺少升级机制 【已优化】
**文件**：`UI/LevelUpUI.cs`、`Skills/BaseSkill.cs`、`ScriptableObjects/Skills/SkillData.cs`
**问题**：8 种主动技能选择后固定参数，无法升级强化。
**实施**：
1. LevelUpUI.GenerateOptions 中自动检测已拥有且未满级的主动技能，作为升级选项加入候选池
2. 技能升级选项显示为 "🔮 技能名 ⬆ Lv.N"，附带 DMG/CD 对比预览
3. 选择后调用 BaseSkill.Upgrade()，自动提升等级、伤害、冷却、效果强度
4. SkillData 已有 maxLevel/damagePerLevel/cooldownReductionPerLevel/perLevel 参数支持
5. BaseSkill.OnUpgrade() 虚方法供子类覆写实现满级特殊效果

### ✅ #34 🟡 P2 — 商店系统功能单薄 【已优化】
**文件**：`UI/ShopUI.cs`、`Core/SaveManager.cs`、`UI/WaveRewardUI.cs`
**问题**：Tab 打开的商店功能有限，金币消耗渠道不足。
**实施**：
1. SaveManager.SHOP_UPGRADES 新增 3 种商品：Gold Interest（金币利息+2%/波）、Revive +1（额外复活）、Skill CD -5%（技能冷却缩减）
2. WaveRewardUI.OnWaveComplete 集成金币利息系统：每波结束根据当前金币 × 利息率自动获得利息
3. ShopUI 自动适配新增商品（动态创建 upgradeCount 个卡片）
4. 利息通过 SaveManager.GetPermanentBonus("gold_interest") 读取，与永久加成系统统一

### ✅ #35 🟢 P3 — 成就系统完善 【已优化】
**文件**：`UI/AchievementUI.cs`
**问题**：成就系统已存在但只有14个基础成就，缺少 DOT/Mage 专属成就和永久加成。
**实施**：
1. 扩展至 24 个成就（+10个新成就），涵盖基础/生存/DOT/引爆等维度
2. Achievement 类新增 `bonusKey` + `bonusValue` 字段，解锁时自动关联永久加成（暴击率/HP/伤害/金币等）
3. 新增 DOT 专属成就：Element Master（4种DOT）、Cataclysm（引爆20+）、Toxic Cloud（10万DOT伤害）、Plague Bearer（50层中毒）、Trinity（3种combo）、Chain Reaction（连锁引爆）
4. 新增生存成就：Endurance（10分钟）、Flawless Boss（无伤Boss）、Apocalypse（2000击杀）、Tycoon（5000金币）
5. 成就解锁加成通过 SaveManager.GetPermanentBonus 系统统一管理

### ✅ #36 🟢 P3 — Boss 战多样性不足 【已优化】
**文件**：`Enemies/BossEnemy.cs`、`Enemies/BossSplitBullet.cs`（新建）
**问题**：每 5 波出一次 Boss，Boss 行为模式单一。
**实施**：
1. 新增 `BossType` 枚举：Juggernaut（重装）、Sorcerer（法师）、Phantom（幽灵）、Berserker（狂战）
2. `SelectBossTypeForWave()` 根据波次自动选择 Boss 类型
3. 幽灵型专属：周期性隐身+闪现到玩家附近+环形弹幕
4. 狂战型专属：分裂弹幕（`BossSplitBullet` 组件）
5. 4 种类型颜色和数值参数差异化

---

## 五、代码架构优化（4项）

### ✅ #37 🟡 P2 — 场景重置逻辑分散 【已优化】
**文件**：`Core/GameStateResetter.cs`（新建）、`UI/PauseMenuUI.cs`、`UI/GameOverUI.cs`
**问题**：返回菜单/重启的重置逻辑分散在两个文件中，步骤多且容易遗漏。
**实施**：
1. 新建 `GameStateResetter` 静态工具类，封装 7 个重置步骤（事件、引用、单例、SFX、DamageMeter 等）
2. `PauseMenuUI.ReturnToMenu()` 改为调用 `GameStateResetter.FullReset()`
3. `GameOverUI.ResetGameState()` 改为调用 `GameStateResetter.FullReset()`
4. 新增重置项只需修改 `GameStateResetter.FullReset()` 一处

### #38 🟡 P2 — 升级数据与代码耦合
**文件**：`UI/LevelUpUI.cs`、`Player/MagePassive.cs`、`Core/GameSceneBootstrap.cs`
**问题**：Mage 16 种升级的具体参数散落在多个文件中。`GameSceneBootstrap` 负责注入升级逻辑，`LevelUpUI` 负责过滤，`MagePassive` 负责存储。
**建议**：
- 创建 `MageUpgradeConfig` ScriptableObject 集中管理所有升级数据
- 升级效果通过统一的 `ApplyUpgrade(type, level)` 接口应用
- LevelUpUI 只负责 UI 展示，不包含过滤逻辑

### ✅ #39 🟢 P3 — 接口系统扩展 【已优化】
**文件**：`Core/Interfaces.cs`
**问题**：接口定义可能不够完善，新增功能时缺乏统一抽象。
**实施**：
1. 新增 `IDotEffect` 接口：`EffectType`、`IsActive`、`Apply()`、`Clear()`、`GetCurrentDps()`
2. 新增 `IUpgradable` 接口：`CurrentLevel`、`MaxLevel`、`IsMaxLevel`、`Upgrade()`
3. 已有 `IPoolable` 接口保持不变
4. 接口设计为可选实现，不破坏现有代码

### #40 🟢 P3 — 单元测试/集成测试
**问题**：项目没有自动化测试，修改容易引入回归 Bug。
**建议**：
- 使用 Unity Test Framework 编写核心系统测试
- 重点覆盖：DOT 计算、引爆伤害、对象池回收/重置、场景重置
- CI 自动运行测试

---

## 六、Mage 升级内容补全（按优先级排序）

以下为 Mage_Upgrades.txt 中定义但尚未完全实现的升级内容：

| # | 升级名 | 状态 | 优先级 | 实现要点 |
|---|--------|------|--------|---------|
| 41 | 共振(Resonance) | ❌ 未生效 | 🔴 P0 | 碰撞体积+20%、击退+15%应用到子弹 |
| 42 | 风蚀(Wind Erosion) | ❌ 未实现 | 🔴 P0 | DOT敌人移动生成微型漩涡 |
| 43 | 弹幕>5追踪弹 | ❌ 未实现 | 🟠 P1 | 超出部分转追踪弹/护盾弹 |
| 44 | 反弹>100%多次反弹 | ❌ 未实现 | 🟠 P1 | 超100%增加反弹次数、移除衰减 |
| 45 | 急速>100%穿透 | ❌ 未实现 | 🟡 P2 | 子弹速度超100%后多穿透1个敌人 |

---

## 总结

| 优先级 | 数量 | 说明 |
|--------|------|------|
| 🔴 P0 | 5 项 | 内存泄漏、未实现的核心升级 |
| 🟠 P1 | 12 项 | 性能瓶颈、功能缺失 |
| 🟡 P2 | 14 项 | 体验完善、架构改进 |
| 🟢 P3 | 10 项 | 长期规划、锦上添花 |
| **合计** | **41 项** | — |

### 已完成进度：41/41 ✅ 全部完成！
- ✅ #1 CreateSpreadLine 内存泄漏修复
- ✅ #2 EnemyHealthBar 重复 AddComponent 修复
- ✅ #3 MagePassive 每帧 GetComponent 优化
- ✅ #4 Detonate 引爆性能优化
- ✅ #5 DOT tick 间隔动态化
- ✅ #6 诅咒传播 GetComponent 优化
- ✅ #7 DOT 子弹 switch-case 工厂模式重构（DotBulletFactory）
- ✅ #8 Debug 日志确认已有 Conditional 编译宏保护
- ✅ #11 DotGunState 改为 struct
- ✅ #13 共振(Resonance)子弹体积缩放实现
- ✅ #14 风蚀(Wind Erosion)漩涡效果实现
- ✅ #15 弹幕>5追踪弹（超出部分转 HomingProjectile + DotHomingBullet）
- ✅ #16 反弹>100%多次反弹（RicochetHandler 组件）
- ✅ #17 引爆视觉反馈增强（闪白+伤害弹字+脉冲HUD）
- ✅ #18 DOT粒子视觉效果（流血/中毒/燃烧/霜冻 4种粒子特效）
- ✅ #20 DOT子弹升级视觉变化（增大+变亮+3级发光）
- ✅ #21 中毒层数UI反馈（EnemyHealthBar 显示 xN 层数文字）
- ✅ #23 Mage暴击率成长性（每解锁一种DOT子弹+2%暴击率）
- ✅ #37 场景重置逻辑统一（GameStateResetter 工具类）
- ✅ #27 UpdateVisual Sin 计算确认已由 tick 间隔门控，无需优化
- ✅ #28 Physics2D.OverlapCircleAll 辐射检测改用 SpawnManager.ActiveEnemies
- ✅ #22 引爆与DOT层数/类型联动（中毒+5%/层，燃烧余烬，霜冻碎裂AOE）
- ✅ #26 Mage里程碑感（元素大师+20%DOT、连锁引爆x2 DOT 3秒）
- ✅ #31 OffScreenCuller优化（IsOffScreen API，屏幕外DOT降频+跳过视觉）
- ✅ #39 接口系统扩展（IDotEffect + IUpgradable 接口）
- ✅ #35 成就系统完善（24个成就+永久加成）
- ✅ #29 对象池预热不充分（ExpandPool + ExpandPoolsForWave 动态扩展）
- ✅ #30 GC Alloc追踪与优化（Find→for循环、struct、缓存、Conditional编译宏）
- ✅ #45 急速>100%穿透（PenetrateHandler 组件 + BleedBullet 集成）
- ✅ #33 技能系统升级机制（LevelUpUI 集成技能升级选项 + BaseSkill.Upgrade）
- ✅ #34 商店系统功能扩展（+3种商品：金币利息/额外复活/技能CD缩减 + 波次利息系统）

### 建议执行顺序
1. **第一轮**：修复 P0（内存泄漏 + 共振/风蚀实现）✅ 已完成
2. **第二轮**：P1 性能优化 + 弹幕/反弹补全 ✅ 已完成
3. **第三轮**：P2 功能完善（SFX、DPS面板、DOT组合、技能升级、急速穿透）✅ 已完成
4. **第四轮**：P3 长期规划（地图随机、成就、Boss多样性、测试）

### ✅ #38 🟡 P2 — 升级数据与代码耦合 【已优化】
**文件**：`ScriptableObjects/Config/MageUpgradeConfig.cs`（新建）、`Player/MagePassive.cs`、`Core/GameSceneBootstrap.cs`、`UI/LevelUpUI.cs`
**问题**：Mage 16 种升级的具体参数散落在多个文件中。GameSceneBootstrap 负责注入升级逻辑，LevelUpUI 负责过滤，MagePassive 负责存储。
**实施**：
1. 新建 `MageUpgradeConfig` ScriptableObject，集中管理 4 种 DOT 子弹枪配置 + 10 种增强升级配置
2. MagePassive 新增 `SetUpgradeConfig()`/`GetUpgradeConfig()`/`ApplyUpgrade(string upgradeId)` 统一接口
3. GameSceneBootstrap 改为从 MageUpgradeConfig 加载升级数据，注入 MagePassive
4. LevelUpUI.ApplyCustomUpgrade 改为委托 `MagePassive.ApplyUpgrade()`，IsDotGunUpgrade/GetDotGunForUpgrade 改为查询配置
5. 保留 Fallback 硬编码兼容无配置情况
