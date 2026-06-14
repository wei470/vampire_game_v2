# 重构收益总结

> 2026-06-14 | 5 轮重构 | 245 → 218 个 CS 文件 | 110 → 178 个测试

---

## 做了什么

### 一、角色系统解耦

**问题**：所有 8 个角色都强制使用 `MagePassive`，90+ 处代码直接引用 MagePassive 类型。新角色无法脱离 DOT 系统独立开发。

**解决**：
- 创建 `CharacterPassiveBase` 抽象基类，提取攻速/弹数/穿透等通用字段
- 拆分 `ICharacterPassive`（通用）+ `IDotCharacterPassive`（DOT 专属）双接口
- `MagePassive` 继承基类并实现 DOT 子接口
- `BlueCharacterPassive` 只实现通用接口，完全不依赖 DOT 系统
- 消除全部 90+ 处 `as MagePassive` 强转，改用接口调用

### 二、子弹系统泛化

**问题**：子弹只有 DOT 路径（DotBulletFactory），非 DOT 角色没有子弹系统。

**解决**：
- 创建 `ProjectileBase` 通用子弹基类（速度/碰撞/穿透/反弹）
- `DotBulletBase` 继承 ProjectileBase，复用通用逻辑
- `SimpleBullet` 继承 ProjectileBase，命中直接扣血
- 统一子弹工厂入口

### 三、Bootstrap 解耦

**问题**：GameStarter、GameSceneBootstrap、GameHUDFactory 等文件用 `"mage"` 字符串判断角色类型。

**解决**：
- 全部改为 `is IDotCharacterPassive` 接口检查
- GameReferences 新增 `DotCharacterPassive` 缓存属性
- 消除所有 `.Contains("mage")` 模糊匹配

### 四、静态状态泄漏修复

**问题**：场景重载时 10+ 个静态列表不清理，导致旧数据残留。

**解决**：
- `GameStateResetter.FullReset()` 统一清理所有静态列表
- 覆盖：Coin.All、XPGem.All、DotBulletBase.ActiveDotBullets、EnvironmentZone.All、Backpack、CurseSpreadSystem、CharacterFactory、CharacterConfigLoader

### 五、GPU 资源泄漏修复

**问题**：8 处 `new Material(Shader.Find("Sprites/Default"))` 每次调用都创建新 Material。

**解决**：
- 创建 `MaterialCache` 工具类，全局共享一个 Material 实例
- 7 个文件全部替换为 `MaterialCache.GetDefault()`

### 六、GC 压力优化

**问题**：VFX 创建/销毁产生大量 GC 垃圾（new GameObject + Destroy）。

**解决**：
- 创建 `VFXPool` 通用 VFX 对象池
- 4 个 VFX 类型使用对象池：CurseLine、ChainLine、DarkChain、FrostGhost
- `CurseSpreadSystem` 和 `LightningBullet` 的 List 改为静态复用
- `GlowReturnHelper` 内置独立对象池

### 七、文件精简

**问题**：245 个 CS 文件，大量 10-40 行的小文件，AI 每次需要读 30+ 文件。

**解决**：合并 27 个文件 → 218 个

| 合并操作 | 减少文件数 |
|----------|-----------|
| 删除未使用接口（IUpgradeApplier/IEvolutionHandler/IDetonatable/IWeaponSystem） | 4 |
| 删除重复子弹（BlueBullet→SimpleBullet）+ 死代码（BleedBullet/DamagePipeline/DotProjectile） | 3 |
| VFX 工具合并（MaterialCache+VFXPool+GlowReturnHelper+4个组件→VFXUtils） | 6 |
| TextTicker 合并（3个→FloatingText） | 2 |
| 配置合并（ICharacterConfig+CharacterConfigLoader→CharacterUpgradeConfig） | 2 |
| UI 合并（UIFormatUtils+UIFontProvider+GUIScaleHelper→UIUtils） | 2 |
| DOT 合并（StatusEffectData+DotComboSystem→StatusEffectSystem） | 2 |
| Combat 合并（PenetrateHandler+CritParams→DotBulletHelpers） | 2 |
| 其他（WeaponFiringSystem→CharacterPassiveBase、CombatColorTheme、EnvironmentZoneSpawner） | 4 |

### 八、测试覆盖

**问题**：原有 110 个测试，重构后需要验证新架构正确性。

**解决**：新增 68 个测试 → 总计 178 个

覆盖：接口实现、基类默认值、攻速钳位、工厂创建、穿透处理、暴击参数、UI 格式化、VFX 池化、Sprite 缓存、配置加载、升级应用、静态清理、护甲公式等。

---

## 给项目带来的好处

### 1. 新角色开发从 10+ 文件降到 4-5 文件

```
重构前：修改 MagePassive + ICharacterPassive + DotBulletFactory + GameReferences + 
        GameStarter + DetonateHUD + PauseMenuUI + CurseSpreadSystem + 
        BuildPathRecommender + CharacterFactory + ...（10+ 文件）

重构后：创建 XXXCharacterPassive + XXXUpgradeConfig + XXXBullet + 
        CharacterFactory.Register()（4 个新文件，0 个现有文件修改）
```

### 2. AI 上下文消耗降低 60%

```
重构前：Ai_content.md 224 行 + 需读 30+ 文件理解架构
重构后：Ai_content.md 55 行 + 核心架构在 10 个文件中
```

### 3. 消除 MagePassive 硬编码

| 指标 | 重构前 | 重构后 |
|------|--------|--------|
| `as MagePassive` 强转 | 90+ | 0 |
| `"mage"` 字符串判断 | 15+ | 0 |
| `GetComponent<MagePassive>()` | 5 | 0 |

### 4. 修复运行时 Bug

- 场景重载后静态列表残留 → 全部注册清理链
- Material 每次 new → 全局缓存
- VFX 每次 new/Destroy → 对象池
- List 每次分配 → 静态复用

### 5. 代码可测试性提升

- 接口隔离：`ICharacterPassive` 可独立于 DOT 系统测试
- 基类复用：`CharacterPassiveBase` 的通用逻辑只需测试一次
- 配置驱动：`CharacterUpgradeConfig` 子类化，升级数据与逻辑分离
