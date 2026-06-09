# 🧠 AI 速查手册 — Vampire Survivors Unity 移植版

> 每次开新 AI 窗口先读此文件。

## 1. 项目概述
- **引擎**：Unity 6 (URP) | **语言**：C# | **类型**：2D 俯视角射击生存
- **场景**：`MenuScene`(MenuSceneBootstrap) → `GameScene`(GameSceneBootstrap)
- **Assembly**：`Gameplay.asmdef`(运行时) / `Scripts.Editor.asmdef`(编辑器)

## 2. 目录结构
```
Assets/Scripts/
├ Core/        ← 单例、事件、对象池、引用、输入、存档、Debug
│  ├── GameSceneBootstrap.cs  协调器：组件组装+生命周期
│  ├── GameDataLoader.cs      数据加载：角色/武器/技能/Mage配置
│  ├── GameStarter.cs         游戏启动：应用配置+预热池+开始游戏
│  ├── GameHUDFactory.cs      HUD工厂：创建所有游戏内HUD组件
├ Combat/      ← 武器、弹幕、伤害、DOT子弹、状态效果
│ └ StatusEffects/
├ Entities/    ← 可伤害实体、掉落物、经验/金币
├ Enemies/     ← 14种敌人子类 + Boss + 能力框架
│  ├── SpawnManager.cs        波次管理协调器
│  ├── EnemyPrefabFactory.cs  敌人预制体创建+池键映射
│  ├── WaveConfigHelper.cs    波次配置+难度倍率计算
├ Player/      ← 控制器、等级、技能管理、MagePassive
│  ├── MagePassive.cs         DOT枪+升级+协同+进化
│  ├── DetonateSystem.cs      引爆系统（蓄力/连锁/余烬/碎裂）
├ Skills/      ← 8种主动技能
├ UI/          ← 所有UI组件
├ Map/         ← 地图主题、装饰、环境区域
├ Audio/       ← 音效系统（程序化生成）
│  ├── SoundTrack.cs          ScriptableObject音效配置包（36种音效）
│  ├── ProceduralSFX.cs       程序化音效生成器（纯代码，无需音频文件）
│  ├── SFXManager.cs          音效管理器（单例+对象池+事件驱动）
│  ├── SFXPoolHelper.cs       音效池工具
│  ├── BGMManager.cs          背景音乐管理器
├ Data/        ← ConfigLoader
└ ScriptableObjects/ Config/ Characters/ Skills/
```

## 3. 核心架构

### 3.1 关键基类
- **`Singleton<T>`** → GameManager, ObjectPool, CombatManager, SaveManager, OffScreenCuller
- **`BaseEntity`** / **`Damageable`** / **`EnemyBase`** — 实体继承链

### 3.2 全局引用
- **`GameReferences`** — 静态类缓存 Player/Camera/SpawnManager 等，**禁止 FindFirstObjectByType**
- **返回菜单必须 `GameReferences.Reset()`**

### 3.3 事件系统
- **`EventManager`** — 静态委托 + 泛型事件 `Subscribe<T>/Publish<T>`
- 关键事件：OnEnemyKilled, OnXPGained, OnLevelUp, OnPlayerDeath, OnWaveStart/Complete, OnGameStateChanged
- **修改事件必须同步 `ClearAll()`**

### 3.4 对象池
- **`ObjectPool`** 单例 + **`PoolHelper`** 静态工具
- **所有敌人/弹幕/掉落物必须走对象池**，禁止 Instantiate/Destroy
- 池回收只调 `OnEnable()`，状态重置必须在 OnEnable

### 3.5 死亡流程
```
TakeDamage → HP≤0 → Die() → OnDeath事件 → EnemyBase.Despawn
安全网1: EnemyBase.FixedUpdate !IsAlive → Die()
安全网2: Damageable.LateUpdate HP≤0 → Die()
```

### 3.6 输入
- **`GameInputHandler`** — 统一输入（InputSystem），WASD移动/鼠标射击/E引爆/F技能/Q切换/ESC暂停/Tab商店

### 3.7 状态机
- **`GameManager`** — Menu→Playing→Paused→GameOver

### 3.8 存档
- **`SaveManager`** JSON序列化 / **`ConfigLoader`** 运行时加载配置

## 4. 武器系统（简化版）
- 所有角色默认获得 Bullet，Mage 不用 WeaponController
- 选择流程：角色 → 技能（无武器选择）

## 5. DOT 子弹系统（Mage 专属）

| 类型 | 类名 | 射速 | 特点 |
|------|------|------|------|
| 流血 | BleedBullet | 1/s | 命中附加流血，DPS:3/s，移动时受伤 |
| 中毒 | PoisonBullet | 0.5/s | 无限距离，命中留毒液池 |
| 燃烧 | BurnBullet | 1/s | 快速子弹，DPS:2/s，叠加燃烧层数 |
| 霜冻 | FrostBullet | 1/s | 命中不造成伤害，只施加永久减速30%+叠层，每层+5%，最高90%减速 |
| 雷电 | LightningBullet | 1/s | 命中不造成伤害，只叠静电层；连锁最多3个敌人；首次命中1秒静电，后续0.1秒静电；定时5秒放电(每层-0.2秒，最低2秒)0.5秒静电，伤害固定为0 |
| 黑暗 | DarkBullet | 0.33/s | 缓慢子弹(40%速度)，击中后消失，施加黑暗标记(永久)。命中的敌人略微变黑。敌人死亡时所有DOT按50%效果传播给3范围敌人(黑暗标记本身不传播)。不造成直接伤害 |
| 光明 | LightBulletController | 0.2/s | 蓄力3秒(玩家头上蓄力条，不减速)后，朝鼠标方向射出激光，顺时针扫45度，每3帧触发一次伤害1点。命中施加光明标记：每层受伤+0.5%，无上限，敌人身上显示xN层数 |
| 风 | WindBullet | 1.5s/发(散射5发) | 30°扇形散射5发，命中不造成伤害，每发叠1层风化(+5伤害+固定击退3f)，敌人头上显示xN层数，transform直接位移击退 |

- **中毒叠加**：基础2+每层+1，固定间隔1s（不随层数变化）
- **毒液池**：每秒叠一层中毒
- **霜冻**：永久减速30%基础，每层+5%，上限90%（不造成伤害，只减速，不再冰冻敌人）
- **静电**：雷电子弹不造成直接伤害，只叠层+连锁；首次命中触发1秒静电，后续命中触发0.1秒静电；定时基础5秒放电(每层-0.2秒，最低2秒)暂停0.5秒；静电不造成伤害，纯控制效果
- **黑暗标记**：永久标记，命中的敌人略微变黑。敌人死亡时通过BaseEntity.OnDeath事件传播所有DOT给周围敌人，同时传播StatusEffectManager效果和独立DOT组件(流血/燃烧/中毒/霜冻)。传播效率50%，范围3。DarkBullet无穿透，不造成直接伤害。死亡时从敌人到最近敌人画暗紫色锁链
- **光明标记**：每层受到伤害+0.5%，无上限(公式：1.0+stack×0.005)。LightBulletController蓄力3秒(头部蓄力条)后朝鼠标方向射出激光，顺时针扫45度，每3帧触发一次伤害1点。敌人身上用TextMesh显示"xN"层数

## 5.5 元素反应系统（V11 新增）

| 反应名 | 触发条件 | 效果 |
|--------|----------|------|
| 燃烧扩散 | 燃烧子弹命中带风化层数的敌人 | 消耗1层风化，以敌人为圆心(r=1.5)对范围内所有敌人施加1层燃烧，显示"扩散！" |
| 霜电 | 霜冻子弹命中带静电层数的敌人 | 消耗1层静电，生成冰场(r=1, 2秒)，冰场内每1.25秒施加1层霜冻(30%减速，每层+5%)，显示"霜电！" |

- **反应触发文件**：`BurnBullet.cs`(燃烧扩散) / `FrostBullet.cs`(霜电)
- **反应组件文件**：`FrostLightningField.cs`(霜电冰场) / `ReactionTextTicker`(通用反应文字)
- **ConsumeStack**：`WindErosionEffect.ConsumeStack()` / `StaticStackEffect.ConsumeStack()`
- **修改的DOT组件**：`WindErosionEffect`(风化) / `StaticStackEffect`(静电) / `FrostEffect`(霜冻)

## 6. Mage 升级系统（33种）✅ 全部已实现
- DOT子弹(7)：中毒/燃烧/霜冻/雷电/黑暗/光明/风
- DOT增强(4)：腐蚀/诅咒/痛苦/凋零
- 引爆增强(2)：辐射/污染
- 子弹增强(3)：急速/弹幕/贯穿弹
- P0强化(3)：饱和/元素引爆/吸血法术
- P1深度(1)：连锁反应
- P2协同(7)：共鸣/腐化之触/元素风暴/暗影链接/光明审判/静电领域/霜爆
- 子弹扩展(2)：弹药精通/元素亲和
- 生存(2)：相位移动/灵魂虹吸
- P3终极(3)：余烬强化/元素大师/湮灭领域
- **升级数量**: 33种（移除蔓延/双持/剧毒天赋/碎裂强化 + 重复贯穿弹(penetrate) + 蓄力精通/元素护盾）
- **实现文件**: `MageUpgradeConfig`(配置) → `MagePassive`(属性) → `MageUpgradeApplier`(应用) → `CharacterUpgradeData`(枚举)
- **联动文件**: `StatusEffectSystem`(DOT回调) / `DetonateSystem`(引爆) / `CurseSpreadSystem`(传播)
- **设计文档**: 已归档（删除）
- **教学文档**: `mage.md` — Mage 全机制教学手册（DOT子弹/增强/引爆/组合/进化/强化/公式）

## 7. 状态效果系统
- **StatusEffectManager** 挂敌人身上，管理所有DOT/Debuff
- **CurseSpreadSystem** 静态类，敌人死亡时传播DOT
- **DotComboSystem** DOT组合效果：碎冰/爆燃/脓毒（共3种基础组合），提供GetBleedDamageMult()/GetPoisonDamageMult()/SuperconductMult倍率给StatusEffectSystem
- 独立组件：BleedEffect, BurnStackEffect, PoisonStackEffect, FrostEffect

## 8. 敌人系统
- 14种 + 4种Boss变体，由 SpriteFactory 运行时生成形状
- 出怪：1-2波Basic → 3-4波+Ranged/Fast → 5-7波+Tank/Thrower → 8+全种类
- Boss每5波出现，敌人数=3+(wave-1)×2
- DOT抗性系统：EnemyDotResistance

## 9. 技能系统
- 8种主动技能继承 BaseSkill，F使用/Q切换
- 被动技能：PassiveSkillData 10种类型

## 10. 选择流程
- Step1 选角色(8种) → Step2 选技能(8种)

## 11. Debug 系统
- **DebugConfigPanel** — F1键，热加载JSON + 全局倍率
- **DebugPoolMonitor** — F2键，对象池监控面板

## 12. 音效系统（V6 新增）

### 架构
- **`SgenoundTrack`** (ScriptableObject) — 统一音效配置包，36种音效条目
- **`ProceduralSFX`** — 纯代码程序化音效生成（正弦波/噪声/频率扫描），无需外部音频文件
- **`SFXManager`** (单例) — 音效管理器，事件驱动+对象池
- **`BGMManager`** (单例) — 背景音乐管理器

### 音效分类（36种）
- 战斗：hit/crit/detonate/dotTick
- 敌人：enemyDeath/bossSpawn
- 玩家：playerHurt/playerHeal/levelUp/pickup/coin
- UI：select/confirm/pause
- 环境：waveStart/waveComplete
- 技能（9种）：Cast/Teleport/FrostNova/Lightning/Gravity/DeathAura/Berserk/TheWorld/WindWave
- 元素DOT（7种）：Bleed/Poison/Burn/Frost/Lightning/Dark/Light
- 特殊（4种）：combo/shopBuy/achievement/warning

### 已知问题
- **Damageable.TakeDamage() 未调用 EventManager.TriggerDamage()**，导致子弹命中音效不播放
- 修复：在 `Damageable.cs` 的 `OnDamaged?.Invoke` 之后添加 `EventManager.TriggerDamage(gameObject, actualDamage, transform.position);`

## 13. Test 模式（V10 更新）
- **`TestBulletSelectUI`** — 双标签页：子弹选择 + 升级叠加
- 子弹标签：多选DOT子弹类型，点击整行切换选中
- 强化标签：左键+1层，右键-1层，点击整行操作
- 类别颜色自动从 `MageUpgradeConfig` 动态生成（哈希HSV），新增升级无需维护TestUI
- 确认后通过 `MageUpgradeApplier.ApplyUpgrade()` 应用
- BuildSettings中BossTest/Inc3_Test场景已禁用(enabled=0)

## 14. 关键文件索引

### 最常修改
| 文件 | 说明 |
|------|------|
| GameSceneBootstrap | 协调器（~236行）：组件组装+生命周期 |
| GameDataLoader | 数据加载（~160行）：角色/武器/技能/Mage配置 |
| GameStarter | 游戏启动（~212行）：应用配置+预热池+开始游戏 |
| GameHUDFactory | HUD工厂（~110行）：创建游戏内HUD组件 |
| SpawnManager | 波次管理（~342行）：协调波次+生成+状态 |
| EnemyPrefabFactory | 敌人工厂（~231行）：预制体创建+池键映射 |
| WaveConfigHelper | 波次配置（~120行）：难度曲线+配置加载 |
| MagePassive | DOT枪（~428行）：DOT枪管理+升级+协同+进化 |
| DetonateSystem | 引爆系统（~359行）：蓄力/连锁/余烬/碎裂 |
| LevelUpUI | 升级UI协调（~220行）：显示+选择+应用 |
| LevelUpOptionGenerator | 升级选项生成（~332行）：选项生成+Build路线+推荐 |
| StatusEffectSystem | DOT管理（~230行）：核心管理+引爆+视觉 |
| CurseSpreadSystem | 诅咒传播（~100行）：死亡时传播DOT给附近敌人 |
| DotComboSystem | DOT组合（~100行）：碎冰/爆燃/脓毒协同 |
| EventManager | 全局事件（~120行）：事件声明+触发+清理 |
| Damageable | 可伤害实体（~330行，未拆分：职责紧密） |

### 不应轻易修改
Singleton.cs, BaseEntity.cs, ObjectPool.cs, Interfaces.cs, EnemyBase.cs

## 14. 修改规范

### 必须遵守
1. 所有伤害经 `CombatManager`，生命周期走 `ObjectPool`/`PoolHelper`
2. 全局引用经 `GameReferences`，事件经 `EventManager`
3. Debug 用 `DebugHelper`，状态重置在 `OnEnable()`

### 代码风格
- 中文注释、`///` XML文档、`[Header]` 标记
- PascalCase 方法/类，_camelCase 私有字段

### 修改检查清单
- 修改 EventManager → 更新 ClearAll()
- 新增敌人 → 池键+SpawnManager+形状+特效
- 新增技能 → 继承 BaseSkill + SetSkillData()
- 新增 DOT 子弹 → 注册到 MagePassive + 更新 LevelUpUI
- 新增掉落物 → KillRewarder + GameSceneBootstrap 池预热

### 场景重置规范
返回菜单必须：EventManager.ClearAll() → MagnetMultiplierSystem.Reset() → GameReferences.Reset() → ResetCharacter() → 销毁所有单例

### 关键 Bug 注意
- PoisonBullet/FrostBullet 必须有 _lifetime 超时销毁
- FrostEffect.OnEnable() 必须 RestoreSpeed + 重置状态
- DOT子弹枪去重：LevelUpUI 检查已拥有则跳过
- 攻速公式：`1f - bonus`，最低0.2
- DOT子弹命中必须调 DotBulletHelper.EnsureStatusEffectManager()
- InputSystem：使用 `Keyboard.current.xxxKey.wasPressedThisFrame`，禁止 `Input.GetKeyDown`
- **SpawnManager.StartFirstWave() 必须重置状态**：调用前必须 `StopAllCoroutines()` + 重置 `_currentWave=0, _enemiesAlive=0, _isSpawning=false, _waveInProgress=false` + 清除 `_activeEnemies`
- **所有重启路径必须调用 `GameStateResetter.FullReset()`**：R键、GameOverUI重启、PauseMenuUI返回菜单等。仅 `EventManager.ClearAll()` + `LoadScene()` 不够！
- **ObjectPool 是 DontDestroyOnLoad 单例**：LoadScene 重建场景时池中旧敌人会残留，必须通过 `GameStateResetter.FullReset()` 销毁。
- **重启后对象池必须重新预热**：`FullReset()` 销毁 ObjectPool 后，`SpawnManager.StartFirstWave()` 必须在 `EnsureEnemyPrefabs()` 之后调用 `EnsureEnemyPoolsWarmedUp()` 确保池中有可激活的敌人实例。
- **（V7新增）退出到菜单可能引发 Unity 卡死**：原因包括 `WaitForSeconds` 在 `Time.timeScale=0` 情况下不完成、Destroy 阶段回调级联。解决方案：在 `GameStateResetter.FullReset()` 里先冻结时间、停止场景中所有 MonoBehaviour 的协程、禁用 MonoBehaviour、使用 `DestroyImmediate`、重置静态状态（DamagePopup/Combo/传播）并恢复时间缩放。
- **（V7新增）Boss 被纳入波次计数**：原先 Boss 未计入 `_activeEnemies` 导致 Boss 未被击杀就进入下一波。修复：`SpawnManager.SpawnBoss()` 现在将 Boss 加入 `_activeEnemies` 并更新 `_enemiesAlive`。
- **（V7新增）Boss 血条 UI 不消失**：原先 `BossEnemy.Update()` 在 Boss 死亡后未必触发死亡事件。修复：在 `BossEnemy.OnDisable()` 中触发 `TriggerBossDeath`，确保 `BossHealthBarUI` 隐藏。
- **（V7新增）流血子弹移除**：`MageUpgradeConfig.dotGunEntries` 不再包含 bleed，`DotBulletFactory` 不再注册流血子弹。流血仍在代码文件中存在但不参与游戏。
- **（V7新增）反弹强化移除**：原先"反弹"升级项改为"贯穿"行为；新的属性 `PiercingBonus` 控制穿透数。config `value1=1f`。
- **（V7新增）穿透系统修复**：原先 `PenetrateHandler` 的触发条件过严（依赖 BulletSpeedBonus）且子弹逻辑先销毁再尝试穿透。修改：将穿透数来源改为 `PiercingBonus`，并在子弹命中逻辑中先检测穿透再决定销毁。
- **（V7新增）DOT 子弹增强**：痛苦(DotFrequencyBonus) bug 修复：`StatusEffectSystem.DotFrequencyBonus` setter 现在正确调用 `RecalcTickInterval()`。凋零暴击显示放大伤害。侵蚀冲击每5次触发，灰色特效+灰色字体+10%DOT总伤。DOT 每 tick 按元素颜色弹伤害数字。10种组合触发时在敌人头上显示组合名称小字。
- **（V7新增）贯穿弹修复**：`MageUpgradeApplier` 中 `Penetrate` 类别改为增加 `PiercingBonus`，与 `Ricochet` 统一，确保贯穿强化在正常和test模式都生效。
- **（V7新增）DOT子弹命中移除直接伤害**：BleedBullet 和 BurnBullet 命中时不再调用 `TakeDamage()`，只施加DOT效果。所有DOT子弹命中0伤害，DOT tick伤害正常显示。
- **（V7新增）子弹强化需解锁对应子弹**：`LevelUpOptionGenerator` 中新增 `GetRequiredDotGunType()` 方法，子弹强化（shadow_link→Dark、light_judgment→Light、static_field→Static、frost_explosion→Frostbite）需要对应DOT枪已解锁。DOT增强和引爆增强需要至少1种DOT枪。
- **（V7新增）MageStatsHUD显示子弹攻速**：简化版HUD图标旁显示 `Lv1 0.6/s` 格式（等级+每秒攻击次数）。
- **（V7新增）蓄力移速惩罚50%**：DetonateSystem 蓄力时移速惩罚从30%改为50%。
- **（V8新增）Boss测试模式**：主菜单按B键或点击"Boss Test"按钮进入Boss-only模式，每波只生成Boss。
- **（V8新增）中毒层数上限**：PoisonStackEffect最大20层，修复Boss高频毒伤bug。
- **（V8新增）中毒伤害频率修复**：移除PoisonStackEffect中的TICK_DECAY加速机制，毒伤害改为固定1秒间隔，不再随层数加快。
- **（V8新增）PoisonPuddle去重**：移除OnTriggerStay2D，仅保留Update中的ApplyPoisonToNearby。
- **（V8新增）升级移除**：蓄力精通、碎裂强化、末日审判、元素护盾、侵蚀、溢出弹已从config移除（枚举保留兼容）。
- **（V8新增）升级描述简化**：所有升级描述改为2-4字+数值格式，如"护甲-10%"、"攻速+15% 速度+10%"。
- **（V10新增）敌人速度集中管理**：EnemyBase新增 `FrostSlowMultiplier`（霜冻减速乘数）和 `IsStaticStunned`（静电硬直标志），FixedUpdate统一计算实际速度 `effectiveSpeed = IsStaticStunned ? 0f : BaseMoveSpeed * FrostSlowMultiplier`。FrostEffect和StaticStackEffect不再直接修改MoveSpeed，只设置标志
- **（V10新增）霜冻变蓝视觉**：FrostEffect在LateUpdate中根据霜冻层数逐渐变蓝（1层≈3%蓝，5层≈15%蓝，20层≈60%蓝，34层以上完全蓝色），通过`_frostStacks/34f`计算强度
- **（V10新增）静电命中特效**：StaticStackEffect.AddStack()每次命中播放青色爆炸特效（首次1.2f，后续0.6f）
- **（V10新增）角色速度×2**：所有8个角色初始速度×2（Warrior 5.6, Mage 7.0, Necromancer 6.4, Berserker 7.6, Ranger 9.0, Paladin 5.0, Vampire 7.0, Assassin 10.0）
- **（V10新增）废弃API清理**：GameStateResetter移除FindObjectsSortMode参数
- **修改文件**：EnemyBase/FrostBullet/LightningBullet/DotColorBlender/GameStateResetter/StatusEffectSystem/TemporaryBuffSystem/CharacterData/GameDataLoader + 8个角色.asset

## 15. 重构进度 — 全部完成 ✅

> **V1+V2 重构已全部完成**，V3 性能优化已全部完成，V4 文件拆分已完成，V5 玩法扩展已完成，V9 性能+代码质量优化已完成，fixme.md 已删除。

### V9 性能+代码质量优化总结
**对象池化**：DOT弹幕(5种)+毒液池 → RegisterVirtualPrefab + Spawn/DespawnOrDestroy，OnEnable重置状态
**Update优化**：StatusEffectManager HashSet缓存、PoisonStackEffect视觉降频(0.15s)、FrostEffect只在层数变化时更新、EnemyDotResistance GetComponent缓存
**物理优化**：PoisonPuddle/WindErosionVortex 0.5s检测间隔、CurseSpreadSystem单次OverlapCircleAll
**渲染优化**：BurnStackEffect视觉降频、EnemyHealthBar距离优化(远距离每5帧)、MinimapUI用ActiveEnemies+MAX_DOTS=40
**DOT系统优化**：LightMarkEffect TextMesh缓存+只在层数变化时更新、DOT伤害字号加大20%
**代码清理**：侵蚀/溢出弹死代码移除、SpawnBleed死代码移除、DrawEliteDots合并到DrawEnemyDots
**测试覆盖**：新增16个单元测试（DOT伤害公式/引爆边界/升级叠加/暴击计算/Boss循环）
**修改文件**：PoolHelper/PoisonBullet/BurnBullet/FrostBullet/LightningBullet/DarkBullet/LightBulletController/MinimapUI/DamagePopup/CoreSystemTests

### V5 玩法扩展总结
**Phase 1（内容扩展）**：黑暗子弹+光明子弹+7种DOT组合+临时道具+击杀连击+波次挑战+伤害数字颜色
**Phase 2（核心新玩法）**：精英词缀12种
**Phase 3（深度玩法）**：元素融合5种+角色进化系统+Boss Rush模式
**Phase 4（长期留存）**：小地图增强+Boss战增强

### V3 性能优化总结
- **1.1 DOT子弹 FixedUpdate GetComponent 缓存**：BleedBullet/BurnBullet/FrostBullet/LightningBullet 在 OnEnable 中缓存 Rigidbody2D
- **1.2 DetonateSystem GetComponent 优化**：所有 `GetComponent` → `TryGetComponent`，缓存 ScreenShake 引用
- **1.3 CurseSpreadSystem GetComponent 优化**：源DOT组件单次获取（检查+缓存合并），目标组件改用 TryGetComponent

### V4 文件拆分总结
- **MageStatsHUD(509)** → MageStatsHUD(~170) + MageStatsHUDRenderer(~280) 静态绘制类
- **DotStatusIndicator(493)** → DotStatusIndicator(~310) + DotStatusIconManager(~180) 图标管理器
- **AchievementUI(387)** → AchievementUI(~190) + AchievementNotificationRenderer(~190) 通知渲染器

### 重构总结

**V1 阶段**（7次拆分）：
- GameSceneBootstrap → Bootstrap + GameDataLoader + GameStarter + GameHUDFactory
- SpawnManager → SpawnManager + EnemyPrefabFactory + WaveConfigHelper
- MagePassive → MagePassive + DetonateSystem
- LevelUpUI → LevelUpUI + LevelUpOptionGenerator + MagnetMultiplierSystem
- DotProjectile → BleedBullet + PoisonBullet + BurnBullet + FrostBullet + LightningBullet + DotBulletFactory
- StatusEffectSystem → StatusEffectSystem + CurseSpreadSystem + DotComboSystem
- EventManager → EventManager + GenericEventBus

**V2 阶段**（P4-P6，14次拆分）：
- BossEnemy → BossEnemy + BossAbilities + BossFactory
- EnemyHealthBar → EnemyHealthBar + HealthBarSpriteHelper + DotStatusIndicator
- SaveManager → SaveManager + SaveData + PermanentUpgradeStore
- MagePassive → MagePassive + MageUpgradeApplier
- StatusEffectSystem → StatusEffectSystem + StatusEffectData + DotVisualEffectManager
- SpawnManager → SpawnManager + EnemyScalingHelper
- LevelUpOptionGenerator → LevelUpOptionGenerator + BuildPathRecommender
- MageStatsHUD → MageStatsHUD + MageStatsDataCollector
- DecorationSpawner → DecorationSpawner + DecorationSpriteHelper
- SelectionFlowManager → SelectionFlowManager + SelectionDataLoader
- EnvironmentZone → EnvironmentZone + EnvironmentZoneEffect
- DamageMeter → DamageMeter + DamageBreakdownUI
- WeaponController → WeaponController + WeaponProjectileFactory
- SFXManager → SFXManager + SFXPoolHelper

**跳过的文件**：ObjectPool(核心单例)、DebugPoolMonitor(编辑器代码)、P7文件(250-330行，结构清晰无需拆分)