# 🧛 Vampire Survivors — Unity 移植版

一个基于 Unity 6 的类 Vampire Survivors 2D 俯视角射击生存游戏。

## 🎮 游戏玩法

- **自动射击** — 武器自动朝鼠标方向开火
- **WASD 移动** — 控制角色在地图上移动
- **波次生存** — 击杀不断涌来的敌人，坚持到 Boss 波
- **升级选择** — 每次升级从 3 个选项中选择强化
- **技能系统** — 8 种主动技能 + 10 种被动技能
- **Boss 战** — 每 5 波出现 Boss，5 阶段机制
- **成就系统** — 24 种成就，解锁永久加成
- **存档系统** — JSON 序列化，永久升级商店

## 🛠 技术栈

- **引擎**: Unity 6 (URP)
- **语言**: C#
- **架构**: 对象池 + 事件系统 + 单例管理器 + 状态机
- **输入**: Unity New Input System

## 📂 项目结构

```
Assets/Scripts/
├ Core/           # 基础设施（单例、事件、对象池、输入、存档、状态机）
│  ├── GameSceneBootstrap   协调器：组件组装+生命周期
│  ├── GameDataLoader       数据加载：角色/武器/技能/Mage配置
│  ├── GameStarter          游戏启动：应用配置+预热池+开始游戏
│  ├── GameHUDFactory       HUD工厂：创建游戏内HUD组件
│  ├── GameStateResetter    场景重置：完整清理所有状态
│  ├── GenericEventBus      泛型事件：类型安全的发布/订阅
├ Combat/         # 武器、弹幕、DOT子弹、状态效果、伤害管理
│  ├── DotBulletFactory     5种DOT子弹工厂
│  ├── StatusEffects/       状态效果系统（DOT/诅咒传播/组合效果）
├ Entities/       # 可伤害实体、掉落物、经验/金币
├ Enemies/        # 14 种敌人 + 4种Boss + 能力框架
│  ├── SpawnManager         波次管理协调器
│  ├── EnemyPrefabFactory   敌人预制体创建+池键映射
│  ├── WaveConfigHelper     波次配置+难度曲线
│  ├── EnemyScalingHelper   难度缩放
│  ├── BossFactory/BossAbilities  Boss系统
├ Player/         # 玩家控制、等级、技能管理
│  ├── MagePassive          DOT枪管理+升级+协同+进化
│  ├── DetonateSystem       引爆系统（蓄力/连锁/余烬/碎裂）
│  ├── MageUpgradeApplier   Mage升级应用器
├ Skills/         # 8 种主动技能
├ UI/             # HUD、菜单、血条、伤害数字等
│  ├── LevelUpUI + LevelUpOptionGenerator  升级系统
│  ├── MageStatsHUD + MageStatsHUDRenderer Mage统计面板
│  ├── DotStatusIndicator + DotStatusIconManager DOT指示器
│  ├── AchievementUI + AchievementNotificationRenderer 成就系统
│  ├── BuildPathRecommender  Build路线推荐
├ Map/            # 地图主题、装饰、环境区域
├ Audio/          # BGM + SFXManager + SFXPoolHelper
├ Data/           # 配置加载器
└ ScriptableObjects/ Config/ Characters/ Skills/
```

## 🚀 快速开始

1. 使用 **Unity 6** 打开项目
2. 打开 `Assets/Scenes/MenuScene.unity`
3. 选择角色 → 选择技能 → 开始游戏

## 📖 开发文档

详细架构说明请阅读 **[Ai_content.md](Ai_content.md)**，包含：
- 完整的模块架构和类继承关系
- 修改规范和约束
- DOT 子弹系统详细说明
- Mage 升级系统（15种）
- 敌人/技能系统详细说明
- 关键 Bug 注意事项

## 🎯 游戏内容

### 8 种角色选择
各具独特武器和被动效果

### 5 种 DOT 子弹（Mage 专属）
| 类型 | 特点 |
|------|------|
| 流血 BleedBullet | 命中附加流血，DPS:3/s，移动时受伤 |
| 中毒 PoisonBullet | 无限距离，命中留毒液池，可叠加 |
| 燃烧 BurnBullet | 快速子弹，DPS:2/s，叠加燃烧层数 |
| 霜冻 FrostBullet | 冰冻1秒 + 永久减速30% |
| 雷电 LightningBullet | 连锁最多3个敌人，施加静电层数 |

### 15 种敌人（每种独特形状 + 颜色 + 特效）

| 敌人 | 形状 | 颜色 | 特效 |
|------|------|------|------|
| BasicEnemy | █ 方形 | 🔴 红色 | - |
| RangedEnemy | ▲ 三角 | 🩷 浅红 | 圆形子弹射击 |
| FastEnemy | ▲ 三角 | 🟣 紫色 | 高速追击 |
| ChargerEnemy | ▲ 三角 | 🟤 橙棕 | 蓄力冲锋 |
| ThrowerEnemy | ◆ 菱形 | 🟠 橙色 | 抛物线投掷弹 |
| SplitterEnemy | ◆ 菱形 | 🍷 暗红 | 死亡分裂 |
| StealthEnemy | ◆ 菱形 | 🩶 暗灰 | 隐身/显形 |
| EnhancerEnemy | ⬠ 五边形 | 🟡 黄色 | 增强光环 |
| SummonerEnemy | ⬠ 五边形 | 🟣 深紫 | 召唤小兵 |
| TankEnemy | ⬡ 六边形 | 🩶 灰色 | 高血量低速 |
| ShielderEnemy | ⬡ 六边形 | 🔵 蓝色 | 护盾光环 |
| HealerEnemy | ✚ 十字 | 🟢 绿色 | 治疗光环 |
| ChainHealerEnemy | ✚ 十字 | 🩵 青绿 | 链式治疗 |
| BurstEnemy | ★ 星形 | 🟠 亮橙 | 蓄力爆发 |
| BossEnemy | █ 方形 | 动态 | 5 阶段机制 |

### 8 种主动技能
Wind Wave, Berserk, The World, Teleport, Death Aura, Lightning Storm, Gravity Well, Frost Nova

### 10 种被动技能
MaxHP, HP Regen, Move Speed, Attack Damage, Cooldown, Armor, Crit Chance, Crit Damage, Pickup Range, Luck

### 24 种成就
基础战斗成就 + DOT 专属成就，解锁永久加成（暴击率/生命/伤害/DOT伤害/引爆伤害/金币）

## ⚡ 性能优化

- **V3 性能优化**：DOT子弹 FixedUpdate GetComponent 缓存、DetonateSystem TryGetComponent + ScreenShake 缓存、CurseSpreadSystem 组件查找优化
- **V4 文件拆分**：MageStatsHUD → +Renderer、DotStatusIndicator → +IconManager、AchievementUI → +NotificationRenderer
- **对象池**：所有敌人/弹幕/掉落物走对象池，禁止 Instantiate/Destroy
- **DOT 抗性系统**：EnemyDotResistance

## 📋 协作规范

### 分支策略
- `main` — 稳定版本
- `dev` — 开发分支
- `feature/xxx` — 功能分支

### 提交信息格式
```
feat: 新增功能描述
fix: 修复问题描述
refactor: 重构描述
perf: 性能优化描述
docs: 文档更新
```

## 📄 License

MIT License