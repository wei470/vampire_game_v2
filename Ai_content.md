# 🧠 AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 窗口先读此文件。最后更新：2026-06-10

## 1. 项目概述
- **引擎**：Unity 6 (URP) | **语言**：C# | **类型**：2D 俯视角射击生存
- **场景**：`MenuScene`(MenuSceneBootstrap) → `GameScene`(GameSceneBootstrap)
- **Assembly**：`Gameplay.asmdef`(运行时) / `Scripts.Editor.asmdef`(编辑器)

## 2. 核心架构

| 基类/系统 | 说明 |
|-----------|------|
| `Singleton<T>` | GameManager, ObjectPool, CombatManager, SaveManager, OffScreenCuller |
| `BaseEntity` → `Damageable` → `EnemyBase` | 实体继承链 |
| `GameReferences` | 静态引用缓存 Player/Camera/SpawnManager，**禁止 FindFirstObjectByType** |
| `EventManager` | 委托+泛型事件 `Subscribe<T>/Publish<T>`，修改必须同步 `ClearAll()` |
| `ObjectPool` + `PoolHelper` | 所有敌人/弹幕/掉落物必须走对象池，禁止 Instantiate/Destroy |
| `GameManager` | 状态机 Menu→Playing→Paused→GameOver |
| `GameInputHandler` | 统一输入（InputSystem），WASD/鼠标/E引爆/F技能/Q切换/ESC暂停/Tab商店 |

**死亡流程**：TakeDamage → HP≤0 → Die() → OnDeath → Despawn（安全网：FixedUpdate+LateUpdate）

## 3. DOT 子弹系统（Mage 专属，7种）

| 类型 | 类名 | 特点 |
|------|------|------|
| 中毒 | PoisonBullet | 无限距离，命中留毒液池 |
| 燃烧 | BurnBullet | 快速子弹，叠加燃烧层数 |
| 霜冻 | FrostBullet | 永久减速30%+叠层，每层+5%，上限90% |
| 雷电 | LightningBullet | 连锁3敌人，叠静电层，定时放电控制 |
| 黑暗 | DarkBullet | 永久标记，敌人死亡时DOT按50%传播(范围3) |
| 光明 | LightBulletController | 蓄力3秒后激光扫射，每层受伤+0.5%，无上限 |
| 风 | WindBullet | 30°扇形散射5发，叠风化层数(+伤害+击退) |

**元素反应**：燃烧×风化→燃烧扩散(r=1.5) | 霜冻×静电→霜电冰场(r=1, 2s)

**关键文件**：`DotBulletFactory`(工厂) / `DotBulletBase`(基类) / `DotBulletConfig`(配置SO) / `DotBulletHelpers`(工具)

## 4. Mage 升级系统（33种）
- **实现链**：`MageUpgradeConfig`(配置) → `MagePassive`(属性) → `MageUpgradeApplier`(应用)
- **DOT子弹(7)** + **DOT增强(4)** + **引爆增强(2)** + **子弹增强(3)** + **P0强化(3)** + **P1深度(1)** + **P2协同(7)** + **子弹扩展(2)** + **生存(2)** + **P3终极(3)**
- **联动**：StatusEffectSystem / DetonateSystem / CurseSpreadSystem / DotComboSystem

## 5. 引爆系统
- `DetonateSystem.cs` — 蓄力/连锁/余烬/碎裂，已用 SpatialGrid 空间分区加速
- 输入：E键蓄力（Keyboard API），移速惩罚50%

## 6. 敌人系统
- 14种 + 4种Boss变体，由 SpriteFactory 运行时生成
- 出怪：1-2波Basic → 3-4波+Ranged/Fast → 5-7波+Tank/Thrower → 8+全种类
- Boss每5波出现，DOT抗性：EnemyDotResistance

## 7. 关键文件索引

| 文件 | 说明 |
|------|------|
| GameSceneBootstrap | 协调器：组件组装+生命周期 |
| GameDataLoader | 数据加载：角色/武器/技能/Mage配置 |
| GameStarter | 游戏启动：应用配置+预热池+开始游戏 |
| SpawnManager | 波次管理：协调波次+生成+状态 |
| EnemyPrefabFactory | 敌人工厂：预制体创建+池键映射 |
| **MagePassive.cs** | DOT枪管理+属性（~370行，partial class） |
| **MagePassive.Firing.cs** | Update+子弹发射+视觉（~150行，partial class） |
| **DetonateSystem** | 引爆系统（~359行） |
| **DotBulletConfig** | DOT子弹数据配置（ScriptableObject） |
| **SpatialGrid** | 空间分区加速范围查询 |
| **DotEffectRegistry** | DOT效果组件生命周期统一管理 |
| LevelUpUI + LevelUpOptionGenerator | 升级UI+选项生成 |
| StatusEffectSystem | DOT管理+引爆+视觉 |
| CurseSpreadSystem | 死亡时传播DOT |
| GameReferences | 全局引用缓存（6个懒缓存属性） |

## 8. Editor 工具

| 菜单 | 文件 | 功能 |
|------|------|------|
| Mage → Validate Upgrade Config | `MageUpgradeValidator.cs` | 升级配置验证 |
| Mage → Dot Bullet Config Editor | `DotBulletConfigEditor.cs` | 子弹参数可视化编辑 |
| Mage → Create Dot Bullet Config Asset | `CreateDotBulletConfigAsset.cs` | 创建 DotBulletConfig.asset |

## 9. 修改规范

**必须遵守**：
1. 所有伤害经 `CombatManager`，生命周期走 `ObjectPool`/`PoolHelper`
2. 全局引用经 `GameReferences`，事件经 `EventManager`
3. Debug 用 `DebugHelper`，状态重置在 `OnEnable()`
4. InputSystem 用 `Keyboard.current.xxxKey.wasPressedThisFrame`，禁止 `Input.GetKeyDown`

**检查清单**：
- 修改 EventManager → 更新 ClearAll()
- 新增敌人 → 池键+SpawnManager+形状+特效
- 新增技能 → 继承 BaseSkill + SetSkillData()
- 新增 DOT 子弹 → 注册到 MagePassive + 更新 LevelUpUI
- 返回菜单 → EventManager.ClearAll() + GameReferences.Reset() + GameStateResetter.FullReset()

**关键 Bug 注意**：
- PoisonBullet/FrostBullet 必须有 _lifetime 超时销毁
- FrostEffect.OnEnable() 必须 RestoreSpeed + 重置状态
- DOT子弹命中必须调 DotBulletHelper.EnsureStatusEffectManager()
- ObjectPool 是 DontDestroyOnLoad，重启必须 FullReset() 销毁
- 所有重启路径必须调 GameStateResetter.FullReset()（R键/GameOver/PauseMenu）
- **DamagePopup.FullCleanup()** 必须在 FullReset 中调用（非 ResetPool），否则 DontDestroyOnLoad 池父级永久残留
- **SpawnManager.ForceDestroyAllEnemies** 必须用 DestroyImmediate（非 SetActive(false)），否则 zombie 敌人占池
- **PoisonBurstTextTicker.OnDisable** 禁止 Destroy(gameObject)，否则 FullReset 禁用阶段级联崩溃
- **DotEffectRegistry.ClearAll()** 必须在 FullReset 中调用，否则残留 PoisonStackEffect 引用

## 10. Bug 修复记录

### 2026-06-10: 毒子弹 DontDestroyOnLoad 泄漏 + 敌人消失 + 重启不生成
**症状**：DamagePopup 永久残留 DontDestroyOnLoad；敌人偶尔永久消失；重启后敌人不生成
**根因**：
1. DamagePopup 对象池使用 DontDestroyOnLoad 但 ResetPool() 从不销毁池父级
2. SpawnManager.ForceDestroyAllEnemies() 用 SetActive(false) 隐藏敌人而非 DestroyImmediate
3. DotEffectRegistry 静态集合在 FullReset 时未清空
4. PoisonBurstTextTicker.OnDisable 中 Destroy(gameObject) 触发级联销毁
**修复文件**：DamagePopup.cs / GameStateResetter.cs / SpawnManager.cs / PoisonBullet.cs
**关键变更**：
- DamagePopup 新增 FullCleanup() 销毁池父级+所有池化对象
- GameStateResetter 调用 FullCleanup() 替代 ResetPool()，新增 DotEffectRegistry.ClearAll()
- SpawnManager.ForceDestroyAllEnemies 改用 Object.DestroyImmediate
- PoisonBurstTextTicker.OnDisable 移除 Destroy 调用
- CleanupTempEffects 额外清理 "PoisonBurstText" 对象

