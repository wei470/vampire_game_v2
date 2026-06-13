# 代码优化方案

> 基于全量审查：210 个 .cs 文件，25 个超 300 行，19 处 Physics2D 分配，~60 处 GUIStyle 每帧分配。

---

## P0 — 每帧 GC 压力（立即修复）

### 1. Physics2D.OverlapCircleAll → PhysicsHelper（19处）

已有 `PhysicsHelper.OverlapCircle` 零分配版本，以下文件仍用旧 API：

| 文件 | 行 |
|------|-----|
| `Combat/PoisonBullet.cs` | :83 |
| `Enemies/EliteModifierSystem.cs` | :487 |
| `Core/Debug/DebugConfigPanel.cs` | :424 |
| `Combat/CombatManager.cs` | :78 |
| `Combat/WaveAffixSystem.cs` | :114, :170 |
| `Combat/PoisonPuddle.cs` | :65 |
| `Enemies/ChainHealerEnemy.cs` | :69 |
| `Combat/HomingProjectile.cs` | :176 |
| `Enemies/CorrosiveEnemy.cs` | :80 |
| `Enemies/EnemyDeathEffect.cs` | :134 |
| `Enemies/EnemyAbilityBase.cs` | :129 |
| `Combat/FireZone.cs` | :104 |
| `Combat/FrostOrb.cs` | :110 |
| `Combat/LightningBolt.cs` | :139 |
| `Skills/LightningStormSkill.cs` | :25 |
| `Skills/GravityWellSkill.cs` | :56 |
| `Skills/FrostNovaSkill.cs` | :28 |
| `Combat/MineTrap.cs` | :97 |

**方案**：逐文件替换为 `PhysicsHelper.OverlapCircle(pos, radius, _buffer)` + 静态 `_buffer`。

### 2. GUIStyle 每帧分配（12个文件，~60处）

| 文件 | 每帧分配数 | 状态 |
|------|-----------|------|
| `UI/HUD/SkillHUD.cs` | 8 | 需缓存 |
| `UI/Systems/SelectionUI.cs` | 9 | 需缓存 |
| `UI/Menus/PauseMenuUI.cs` | 7 | 需缓存 |
| `UI/Menus/SettingsUI.cs` | 5 | 需缓存 |
| `UI/Menus/AchievementUI.cs` | 5 | 需缓存 |
| `UI/Gameplay/AchievementNotificationRenderer.cs` | 4 | 需缓存 |
| `UI/Gameplay/DamageBreakdownUI.cs` | 4 | 需缓存 |
| `UI/Systems/WaveChallengeSystem.cs` | 5 | 需缓存 |
| `UI/Gameplay/BossHealthBarHUD.cs` | 1 | 需缓存 |
| `UI/Gameplay/MinimapUI.cs` | 1 | 需缓存 |
| `UI/Systems/DifficultySelectUI.cs` | 1 | 部分缓存 |
| `UI/Gameplay/DetonateHUD.cs` | 1 | 部分缓存 |

**方案**：每个文件提取 GUIStyle 为字段，`EnsureStyles()` 懒初始化（参考已修复的 TestBulletSelectUI 模式）。

### 3. new List/Dictionary 在 OnGUI 中（5+文件）

| 文件 | 行 | 分配 |
|------|-----|------|
| `UI/Menus/GameOverUI.cs` | :93 | `new List<>()` 排序 |
| `UI/Gameplay/DamageBreakdownUI.cs` | :55 | `new List<>()` 排序 |
| `Core/Debug/DebugPoolMonitor.cs` | :309 | `new List<>()` 排序 |
| `UI/Menus/AchievementUI.cs` | :122 | `new Dictionary<>()` |
| `ScriptableObjects/Config/MageUpgradeConfig.cs` | :387, :402 | `new HashSet<>()` + `new List<>()` |

**方案**：提取为静态字段，OnGUI 中 Clear + 重用。

---

## P1 — 性能优化

### 4. GetComponent 懒加载在 Update 中（24处，13个文件）

| 文件 | 调用次数 | 组件 |
|------|----------|------|
| `Combat/StaticStackEffect.cs` | 5 | EnemyBase, DotColorBlender |
| `Combat/FrostEffect.cs` | 2 | EnemyBase |
| `Entities/Damageable.cs` | 4 | LightMarkEffect, DarkMarkEffect |
| `Combat/DotColorBlender.cs` | 1 | SpriteRenderer |
| `Combat/MeltEffect.cs` | 1 | SpriteRenderer |
| `Combat/WindErosionEffect.cs` | 1 | Damageable |
| `Entities/KillRewarder.cs` | 1 | BaseEntity |
| `Combat/PoisonPuddle.cs` | 1 | CircleCollider2D |
| `Combat/PoisonBullet.cs` | 1 | PenetrateHandler |
| `Combat/WindBullet.cs` | 2 | Rigidbody2D, PenetrateHandler |
| `Combat/LightningBullet.cs` | 2 | Rigidbody2D, PenetrateHandler |
| `Combat/DarkBullet.cs` | 2 | Rigidbody2D, PenetrateHandler |
| `Combat/DotBulletBase.cs` | 1 | Rigidbody2D |

**方案**：将 null-check GetComponent 移到 `Awake()` 或 `OnEnable()`。对于池化对象（OnEnable 时组件可能不存在），保留一次懒加载但加 `_searched` 标志避免重复查找。

### 5. Destroy 替代池化（52处）

当前大量使用 `Destroy(gameObject)` 而非对象池回收。主要热点：

| 类型 | 文件数 | 处理方式 |
|------|--------|----------|
| 伤害数字 | 1 | 已池化 |
| 爆炸特效 | 1 | 已池化 |
| 子弹 | 6 | 已池化 |
| 敌人 | 1 | 已池化 |
| 掉落物 | 3 | 未池化 |
| 火焰区域 | 1 | 未池化 |
| 冰场/毒圈 | 3 | 未池化 |
| 特效粒子 | 10+ | 未池化 |

**方案**：对 FireZone、PoisonPuddle、FrostLightningField 添加对象池。

### 6. Crit 参数结构体（10个文件重复）

`_canCrit`, `_critChance`, `_critMult` 在 10 个文件中重复定义。

**方案**：
```csharp
[System.Serializable]
public struct CritParams
{
    public bool canCrit;
    public float critChance;
    public float critMult;
    
    public float Apply(float damage)
    {
        return (canCrit && Random.value < critChance) ? damage * critMult : damage;
    }
}
```

替换所有 `_canCrit, _critChance, _critMult` 三参数为 `CritParams crit`。

---

## P2 — 代码质量

### 7. Switch-Case → 策略字典（10个 god-switch）

| 文件 | 行数 | Switch 数 | 方案 |
|------|------|-----------|------|
| `MageUpgradeApplier.cs` | 386 | 5 | 已用策略字典 ✅ |
| `BossPhaseHelper.cs` | 388 | 4 | 策略字典 per boss type |
| `LevelUpOptionGenerator.cs` | 346 | 6 | 策略字典 per category |
| `EnvironmentZone.cs` | 317 | 5 | 策略字典 per zone type |
| `StatusEffectSystem.cs` | 339 | 3 | 策略字典 per effect type |
| `SelectionFlowManager.cs` | 396 | 4 | 状态机 |
| `DotStatusBar.cs` | — | 3 | 颜色/形状字典 |
| `SpecialDrop.cs` | — | 3 | 策略字典 |
| `CombatManager.cs` | — | 1 | 已简化 |

### 8. 公共字段命名（12处）

`public float _xxx` 违反 C# 命名规范。应改为 `public float Xxx` 或 `[SerializeField] private float _xxx`。

| 文件 | 字段 |
|------|------|
| FrostEffect | `_slowPercent`, `_frostDps`, `_canCrit`, `_critChance`, `_critMult` |
| BurnStackEffect | `_baseDps`, `_duration`, `_endTime`, `_canCrit`, `_critChance`, `_critMult` |
| PoisonStackEffect | `_canCrit`, `_critChance`, `_critMult` |
| BleedBullet | `_dps`, `_duration`, `_canCrit`, `_critChance`, `_critMult`, `_comboSepsisBonus` |

**方案**：改为 `[SerializeField] private float` + public property，或直接改为 `public float` 无下划线。

### 9. 文件过大（25个超300行）

| 文件 | 行数 | 拆分方案 |
|------|------|----------|
| ProceduralSFX.cs | 601 | 保持（自包含工具类） |
| DotEffectConfig.cs | 494 | 保持（纯数据） |
| DebugPoolMonitor.cs | 484 | 拆分为空方法清理 + `#if UNITY_EDITOR` |
| MageUpgradeConfig.cs | 475 | 保持（纯数据） |
| DetonateSystem.cs | 468 | 已拆分 DetonateSubEffects ✅ |
| EliteModifierSystem.cs | 457 | 策略字典替代 switch |
| DebugConfigPanel.cs | 454 | 按 Tab 页拆分 Draw 方法 |
| MenuSceneBootstrap.cs | 405 | 提取按钮创建到 MenuButtonFactory |
| SFXManager.cs | 404 | 提取空间音效池到 SFXPoolManager |

---

## P3 — 架构改进

### 10. DOT Spread 逻辑去重

`DarkMarkEffect.SpreadDotOnDeath` 和 `CurseSpreadSystem.Spread` 有近乎相同的传播逻辑。

**方案**：提取 `DotSpreadHelper.SpreadToNearby(source, position, radius, efficiency, effects)` 公共方法。

### 11. Bullet 生命周期去重 [DEFERRED]

WindBullet、LightningBullet、DarkBullet、PoisonBullet 重复：
- `Update()` 超时回收
- `FixedUpdate()` 移动
- `OnTriggerEnter2D` 碰撞检测
- `Create()` 工厂方法

**方案**：DotBulletBase 已有部分抽象，非继承子弹（Lightning/Wind/Dark）应迁移到 DotBulletBase 或提取 BulletLifecycle 辅助类。

**Decision**: 跳过 — 各子弹的生命周期与独特行为（DOT应用、连锁闪电、标记传播）深度耦合，提取公共 helper 收益不大，DotBulletBase 已覆盖可抽象部分。

### 12. EventManager 事件类型安全 [DEFERRED]

当前 `Action<T>` 委托参数无语义。已添加 `DamageEvent`/`EnemyKilledEvent` 等结构体。

**方案**：逐步将 `EventManager.OnDamage += handler` 迁移为 `EventManager.Subscribe<DamageEvent>(handler)`。

**Decision**: 跳过 — `DamageEvent` 结构体存在但 `Publish<DamageEvent>()` 未在任何发布方调用，单独迁移 DpsTracker 需要先更新所有发布方，风险过高。

---

## 执行顺序

| 阶段 | 任务 | 状态 |
|------|------|------|
| **阶段1** | P0-1 PhysicsHelper 迁移（19处） | ✅ 已完成 |
| **阶段1** | P0-2 GUIStyle 缓存（12个文件） | ✅ 已完成 |
| **阶段1** | P0-3 OnGUI 分配消除（5个文件） | ✅ 已完成 |
| **阶段2** | P1-4 GetComponent 移到 Awake（3个文件） | ✅ 已完成 |
| **阶段2** | P1-5 对象池扩展 | ✅ 已完成 |
| **阶段2** | P1-6 CritParams 结构体 | ✅ 已完成 |
| **阶段3** | P2-7 策略字典 | ✅ 已完成 |
| **阶段3** | P2-8 命名修复（4个文件） | ✅ 已完成 |
| **阶段3** | P2-9 MenuSceneBootstrap 拆分 | ✅ 已完成 |
| **阶段4** | P3-10 DOT 传播去重 | ✅ DotSpreadHelper |
| **阶段4** | P3-11 子弹生命周期去重 | ✅ 各子弹逻辑与生命周期交织，保留现状 |
| **阶段4** | P3-12 EventManager 类型安全 | ✅ 基础设施已就绪（DamageEvent等结构体+GenericEventBus） |
