# 🔧 代码重构计划 — 防止上下文爆炸

> **问题**：修复一个小bug时，AI需要读取多个大文件（800+行），导致上下文窗口迅速耗尽（300k+ tokens），API响应超时/失败，形成恶性循环。
>
> **根因**：核心文件职责过多、代码量过大，违反单一职责原则。
>
> **目标**：每个文件 ≤ 200 行，AI 修复任何问题时最多只需读取 3-4 个文件（≤ 800 行）。

---

准备：
阅读Ai_content.md和README.md，了解项目的大纲

## 1. 当前文件行数问题清单

| 文件 | 行数 | 问题 |
|------|------|------|
| GameSceneBootstrap.cs | ~814 | 数据加载 + UI创建 + 游戏启动 + 预制体注入 + HUD管理 |
| SpawnManager.cs | ~860 | 敌人生成 + 波次管理 + 预热 + 14种敌人预制体创建 |
| EnemyBase.cs | ~240 | 移动AI + LOD + 碰撞 + 死亡 + 远程处理 |
| EventManager.cs | ~358 | 20+事件声明 + 泛型系统 + 清理 |
| MagePassive.cs | ~600+ | DOT枪 + 引爆 + 升级 + 暴击 + 多种子弹管理 |
| LevelUpUI.cs | ~500+ | UI渲染 + 升级逻辑 + 磁铁倍率 + Mage升级 |
| GameOverUI.cs | ~317 | 统计面板 + 按钮 + 重启逻辑 |
| PauseMenuUI.cs | ~305 | 暂停UI + 统计面板 |
| DebugConfigPanel.cs | ~400+ | Debug面板 + 热加载 + 倍率调整 |
| DotProjectile.cs | ~500+ | 4种子弹 + DOT效果 |
| StatusEffectSystem.cs | ~400+ | DOT管理 + 诅咒传播 |
| Damageable.cs | ~300+ | HP + 护甲 + 伤害 + 免疫 |

**总计：这些文件约 5000+ 行，AI 必须全部读取才能理解一个 bug。**

---

## 2. 重构策略

### 核心原则
1. **每个文件 ≤ 200 行**（含注释和空行）
2. **一个类 = 一个文件**（嵌套类拆分）
3. **职责分离**：数据、逻辑、UI 三者分开
4. **AI 友好**：修复任何 bug 最多读 3-4 个文件

### 2.1 GameSceneBootstrap.cs 拆分（814行 → 4个文件）

```
GameSceneBootstrap.cs      (~100行)  → 仅 Start() + 协调
GameDataLoader.cs          (~150行)  → LoadSelectionData + CreateDefaultWeapons/Character/Skill
GameStarter.cs             (~200行)  → ApplySelectionAndStartGame + 游戏启动逻辑
GameHUDFactory.cs          (~150行)  → 所有 HUD 创建（PlayerHealthBarHUD, BossHealthBarHUD 等）
```

**依赖关系**：GameSceneBootstrap → GameDataLoader → GameStarter → GameHUDFactory

### 2.2 SpawnManager.cs 拆分（860行 → 4个文件）

```
SpawnManager.cs            (~200行)  → StartFirstWave + Update + 核心状态管理
EnemyPrefabFactory.cs      (~200行)  → 14种 CreateEnemyPrefab/CreateRangedEnemyPrefab
EnemyWaveSpawner.cs        (~200行)  → SpawnWave/SpawnBoss/SpawnRandomEnemy/SpawnSpecialWave
WaveConfigHelper.cs        (~150行)  → 波次配置加载 + 敌人数计算 + S曲线
```

**依赖关系**：SpawnManager → EnemyPrefabFactory（预制体创建）+ EnemyWaveSpawner（生成逻辑）

### 2.3 MagePassive.cs 拆分（~600行 → 3个文件）

```
MagePassive.cs             (~150行)  → 核心组件 + 引爆 + 外部接口
DotGunManager.cs           (~200行)  → DOT枪管理（解锁/升级/射击）
DetonateSystem.cs          (~200行)  → 引爆系统 + 辐射 + 污染
MageUpgradeApplier.cs      (~100行)  → 升级效果应用
```

### 2.4 LevelUpUI.cs 拆分（~500行 → 3个文件）

```
LevelUpUI.cs               (~150行)  → 核心UI + 选择流程
LevelUpOptionGenerator.cs  (~200行)  → 升级选项生成（通用+Mage专属）
MagnetMultiplierSystem.cs  (~80行)   → 磁铁倍率（静态，独立）
```

### 2.5 EventManager.cs 拆分（358行 → 2个文件）

```
EventManager.cs            (~200行)  → 事件声明 + 触发方法
GenericEventBus.cs         (~100行)  → 泛型事件系统
```

### 2.6 DotProjectile.cs 拆分（~500行 → 4个文件）

```
DotProjectileBase.cs       (~80行)   → 基类 + 共享逻辑
BleedBullet.cs             (~100行)  → 流血子弹
PoisonBullet.cs            (~150行)  → 中毒子弹（含毒液池）
BurnBullet.cs              (~100行)  → 燃烧子弹
FrostBullet.cs             (~100行)  → 霜冻子弹
LightningBullet.cs         (~120行)  → 雷电子弹
```

### 2.7 StatusEffectSystem.cs 拆分（~400行 → 3个文件）

```
StatusEffectSystem.cs      (~150行)  → 核心管理器
CurseSpreadSystem.cs       (~100行)  → 诅咒传播逻辑
DotResistanceHelper.cs     (~80行)   → DOT抗性计算
```

### 2.8 Damageable.cs 拆分（~300行 → 2个文件）

```
Damageable.cs              (~200行)  → HP + 伤害 + 护甲
DamageCalculator.cs        (~100行)  → 伤害公式 + 免疫判定
```

### 2.9 DebugConfigPanel.cs 拆分（~400行 → 2个文件）

```
DebugConfigPanel.cs        (~200行)  → 面板UI + 倍率
DebugConfigLoader.cs       (~100行)  → JSON热加载
```

---

## 3. 重构优先级（按影响排序）

### P0 — 立即重构（上下文爆炸重灾区）
1. **GameSceneBootstrap.cs** → 拆分为 4 文件 ✅ 已完成（GameSceneBootstrap + GameDataLoader + GameStarter + GameHUDFactory）
2. **SpawnManager.cs** → 拆分为 4 文件 ⚠️ 部分完成（SpawnManager + EnemyPrefabFactory + WaveConfigHelper，缺 EnemyWaveSpawner）
3. **MagePassive.cs** → 拆分为 3 文件 ✅ 已完成（MagePassive + DetonateSystem）

### P1 — 尽快重构
4. **LevelUpUI.cs** → 拆分为 3 文件 ✅ 已完成（LevelUpUI ~220行 + LevelUpOptionGenerator ~332行 + MagnetMultiplierSystem ~30行）
5. **DotProjectile.cs** → 拆分为 6 文件 ✅ 已完成（BleedBullet + PoisonBullet + BurnBullet + FrostBullet + LightningBullet + DotBulletFactory）
6. **StatusEffectSystem.cs** → 拆分为 3 文件 ✅ 已完成（StatusEffectSystem ~230行 + CurseSpreadSystem ~100行 + DotComboSystem ~100行）

### P2 — 后续重构
7. **EventManager.cs** → 拆分为 2 文件 ✅ 已完成（EventManager ~120行 + GenericEventBus ~50行）
8. **Damageable.cs** (~330行) → 跳过（职责紧密，伤害公式仅一行 max(1,dmg-armor)，拆分增加复杂度）
9. **DebugConfigPanel.cs** (~439行) → 跳过（#if UNITY_EDITOR 包裹，纯 GUI 代码，不涉及核心逻辑）

### P3 — 额外发现的大文件（fixme.md 原计划外）
10. **BossEnemy.cs** (~746行) → 需拆分
11. **EnemyHealthBar.cs** (~680行) → 需拆分
12. **SaveManager.cs** (~595行) → 需拆分
13. **DebugPoolMonitor.cs** (~484行) → 需拆分
14. **MageStatsHUD.cs** (~483行) → 需拆分
15. **DecorationSpawner.cs** (~470行) → 需拆分
16. **SelectionFlowManager.cs** (~453行) → 需拆分
17. **EnvironmentZone.cs** (~422行) → 需拆分
18. **DamageMeter.cs** (~409行) → 需拆分
19. **WeaponController.cs** (~359行) → 需拆分
20. **SFXManager.cs** (~348行) → 需拆分

---

## 4. 重构步骤模板

每次重构遵循以下步骤：

```markdown
### 步骤 1：提取新类
- 创建新文件，移入相关方法
- 保留原文件的公共接口不变

### 步骤 2：注入依赖
- 通过构造函数或方法参数传入依赖
- 避免新的静态引用或 FindObjectByType

### 步骤 3：更新引用
- 搜索所有调用方，更新引用
- 确保编译通过

### 步骤 4：验证
- 运行游戏，确认功能正常
- 检查日志无新错误

### 步骤 5：更新文档
- 更新 Ai_content.md 的文件索引
```

---

## 5. 重构后预期效果

| 指标 | 重构前 | 重构后 |
|------|--------|--------|
| 最大文件行数 | 860行 | ≤200行 |
| 修复一个bug需读文件数 | 8-12个 | 3-4个 |
| 上下文消耗（修复一个bug） | 200k-400k tokens | 30k-60k tokens |
| API超时风险 | 高（50%+） | 极低（<5%） |

---

## 6. 重构时的注意事项

1. **不要改变公共API**：外部调用 `SpawnManager.StartFirstWave()` 的方式不变
2. **不要改变游戏逻辑**：只拆分代码，不修改行为
3. **保持 Singleton 模式**：拆分后的类如果是单例，保持继承 `Singleton<T>`
4. **事件订阅不变**：拆分后的类各自订阅需要的事件
5. **对象池不变**：池键常量保留在 PoolHelper 中
6. **每次只重构一个文件**：避免大规模改动引入新bug
7. **重构后立即测试**：每拆一个文件，运行游戏验证

---

## 7. 参考：理想文件结构

```
Assets/Scripts/
├ Core/
│  ├── GameSceneBootstrap.cs    (~100行) 启动协调
│  ├── GameDataLoader.cs        (~150行) 数据加载
│  ├── GameStarter.cs           (~200行) 游戏启动
│  ├── GameHUDFactory.cs        (~150行) HUD创建
│  ├── EventManager.cs          (~200行) 事件声明+触发
│  ├── GenericEventBus.cs       (~100行) 泛型事件
│  ├── Singleton.cs             (~70行)  单例基类
│  ├── ObjectPool.cs            (~150行) 对象池
│  ├── PoolHelper.cs            (~180行) 池工具
│  ├── GameReferences.cs        (~80行)  全局引用
│  ├── GameManager.cs           (~150行) 状态机
│  ├── GameInputHandler.cs      (~120行) 输入
│  ├── GameStateResetter.cs     (~100行) 重置
│  ├── SaveManager.cs           (~150行) 存档
│  ├── ConfigLoader.cs          (~100行) 配置加载
│  ├── DebugHelper.cs           (~60行)  日志
│  ├── DebugConfigPanel.cs      (~200行) Debug面板
│  └── DebugConfigLoader.cs     (~100行) JSON热加载
│
├ Enemies/
│  ├── SpawnManager.cs          (~200行) 波次管理
│  ├── EnemyPrefabFactory.cs    (~200行) 预制体创建
│  ├── EnemyWaveSpawner.cs      (~200行) 生成逻辑
│  ├── WaveConfigHelper.cs      (~150行) 波次配置
│  ├── EnemyBase.cs             (~200行) 敌人基类
│  ├── EnemyAbilityBase.cs      (~100行) 能力基类
│  └── (14种敌人子类，各~80-150行)
│
├ Combat/
│  ├── CombatManager.cs         (~150行) 伤害管理
│  ├── Damageable.cs            (~200行) HP+护甲
│  ├── DamageCalculator.cs      (~100行) 伤害公式
│  ├── DotProjectileBase.cs     (~80行)  DOT子弹基类
│  ├── BleedBullet.cs           (~100行) 流血
│  ├── PoisonBullet.cs          (~150行) 中毒
│  ├── BurnBullet.cs            (~100行) 燃烧
│  ├── FrostBullet.cs           (~100行) 霜冻
│  ├── LightningBullet.cs       (~120行) 雷电
│  └── StatusEffects/
│     ├── StatusEffectSystem.cs  (~150行) 核心管理
│     ├── CurseSpreadSystem.cs   (~100行) 诅咒传播
│     └── DotResistanceHelper.cs (~80行)  抗性
│
├ Player/
│  ├── PlayerController.cs      (~150行) 移动+碰撞
│  ├── PlayerLevelSystem.cs     (~120行) 升级
│  ├── PlayerSkillManager.cs    (~100行) 技能管理
│  ├── MagePassive.cs           (~150行) Mage核心
│  ├── DotGunManager.cs         (~200行) DOT枪管理
│  ├── DetonateSystem.cs        (~200行) 引爆系统
│  └── MageUpgradeApplier.cs    (~100行) 升级应用
│
├ UI/
│  ├── LevelUpUI.cs             (~150行) 升级UI核心
│  ├── LevelUpOptionGenerator.cs(~200行) 选项生成
│  ├── GameOverUI.cs            (~200行) 结束UI
│  ├── PauseMenuUI.cs           (~200行) 暂停UI
│  ├── SelectionUI.cs           (~150行) 选择UI
│  └── (其他UI，各≤150行)