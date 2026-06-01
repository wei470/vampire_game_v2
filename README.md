# 🧛 Vampire Survivors — Unity 移植版

一个基于 Unity 6 的类 Vampire Survivors 2D 俯视角射击生存游戏。

## 🎮 游戏玩法

- **自动射击** — 武器自动朝鼠标方向开火
- **WASD 移动** — 控制角色在地图上移动
- **波次生存** — 击杀不断涌来的敌人，坚持到 Boss 波
- **升级选择** — 每次升级从 3 个选项中选择强化
- **技能系统** — 8 种主动技能 + 10 种被动技能
- **Boss 战** — 每 5 波出现 Boss，5 阶段机制

## 🛠 技术栈

- **引擎**: Unity 6 (URP)
- **语言**: C#
- **架构**: 对象池 + 事件系统 + 单例管理器
- **输入**: Unity New Input System

## 📂 项目结构

```
Assets/Scripts/
├── Core/           # 基础设施（单例、事件、对象池、输入、存档）
├── Combat/         # 武器、弹幕、伤害管理
├── Entities/       # 可伤害实体、掉落物
├── Enemies/        # 14 种敌人 + Boss + SpawnManager
├── Player/         # 玩家控制、等级、技能管理
├── Skills/         # 8 种主动技能 + 被动技能
├── UI/             # HUD、菜单、血条、伤害数字等
├── Map/            # 地图主题、边界、装饰
├── Audio/          # BGM 管理
└── Data/           # 配置加载器
```

## 🚀 快速开始

1. 使用 **Unity 6** 打开项目
2. 打开 `Assets/Scenes/GameScene.unity`
3. 点击 Play

## 📖 开发文档

详细架构说明请阅读 **[Ai_content.md](Ai_content.md)**，包含：
- 完整的模块架构和类继承关系
- 修改规范和约束
- 敌人/武器/技能系统详细说明
- Git 协作规范

## 🎯 游戏内容

### 8 种武器
Bullet, Lightning, Shockwave, Homing, Mine, Flamethrower, Frost Orb, Venom Dart

### 15 种敌人
Basic, Fast, Tank, Ranged, Thrower, Healer, ChainHealer, Enhancer, Shielder, Stealth, Burst, Splitter, Summoner, Charger + Boss

### 8 种主动技能
Wind Wave, Berserk, The World, Teleport, Death Aura, Lightning Storm, Gravity Well, Frost Nova

### 10 种被动技能
MaxHP, HP Regen, Move Speed, Attack Damage, Cooldown, Armor, Crit Chance, Crit Damage, Pickup Range, Luck

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
docs: 文档更新
```

## 📜 License

MIT License