# 项目指令 — 每次任务执行前必读

## 上下文节约规则

1. **避免重复读取文件**：工具返回的内容已包含文件，不要重复 read
2. **精简 task_progress**：只保留关键里程碑（3-5项），不逐行跟踪
3. **用 search 定位再读**：先用 grep 定位关键代码段，再用 read 的 offset/limit 只读需要的部分
4. **分段执行大任务**：每完成 3-5 个文件操作后，评估 context 使用量
5. **edit 优先**：小改动用 edit，避免 write 重写整个文件
6. **合并工具调用**：能在一次 edit 中完成的多个替换，合并为一次调用

## 任务执行规则

当用户发送 **"开始"** 或 **"继续"** 时：

1. 读取 `Ai_content.md` — 获取项目上下文
2. 读取 `task2.md` — 获取当前优化任务清单
3. 跳过已完成项（标记 `[x]`），从未完成的最高优先级开始
4. 按推荐顺序执行（见 task2.md 末尾的"待做项"表格）
5. 每完成一项：在 task2.md 中标记 `[x] 已完成：[简要说明]`

### 任务状态标记
- `[ ]` — 未开始
- `[x] 已完成：[简要说明]` — 已完成
- `[~] 进行中` — 正在执行
- `[!] 已跳过：[原因]` — 已跳过

## Config 实时更新模式（OnValidate → OnConfigChanged）

所有 Config ScriptableObject 使用此模式：

```csharp
// Config 中：
public static System.Action OnConfigChanged;
#if UNITY_EDITOR
private void OnValidate() { OnConfigChanged?.Invoke(); }
#endif

// 消费者组件中：
private void OnEnable() { XxxConfig.OnConfigChanged += RefreshFromConfig; }
private void OnDisable() { XxxConfig.OnConfigChanged -= RefreshFromConfig; }
private void RefreshFromConfig() { /* 从 Config 重新读取字段 */ }
```

### 待完成信号刷新的组件
| 优先级 | 组件 | 文件 | Config 字段 |
|--------|------|------|-------------|
| 高 | FrostEffect | `Combat/FrostBullet.cs` | FrostMaxSlow, FrostBaseSlowPct, FrostSlowPerStack |
| 高 | StaticStackEffect | `Combat/LightningBullet.cs` | StaticBaseInterval, StaticStackReduction, StaticMinInterval, StaticStunOnHit, StaticStunOnDischarge, StaticStunOnFirstStack, StaticMaxStacks |
| 高 | WindErosionEffect | `Combat/WindBullet.cs` | WindErosionKnockbackDistance, WindMaxStacks |
| 高 | LightMarkEffect | `Combat/LightBulletController.cs` | LightMarkDamagePerStack |
| 中 | DotBulletFactory | `Combat/DotBulletFactory.cs` | 清除 `_config` 缓存 |
| 中 | DarkMarkEffect | `Combat/DarkBullet.cs` | DarkBaseRadius, DarkBaseEfficiency, DarkMarkDamageBonus |
| 低 | LightBulletController | `Combat/LightBulletController.cs` | LightChargeDuration, LightSweepAngle, LightSweepDuration |

## 关键文件索引

| 文件 | 作用 |
|------|------|
| `Assets/Scripts/ScriptableObjects/Config/DotBulletConfig.cs` | DOT子弹配置 |
| `Assets/Scripts/ScriptableObjects/Config/EnemySpawnConfig.cs` | 敌人生成配置 |
| `Assets/Scripts/ScriptableObjects/Config/MageUpgradeConfig.cs` | Mage升级配置 |
| `Assets/Scripts/ScriptableObjects/Config/EnemyWaveConfig.cs` | 波次配置 |
| `Assets/Scripts/ScriptableObjects/Config/GameConfig.cs` | 全局游戏配置 |
| `Assets/Scripts/Player/MagePassive.cs` | DOT枪管理+属性 |
| `Assets/Scripts/Player/MagePassive.Firing.cs` | 子弹发射+视觉 |
| `Assets/Scripts/Combat/DotBulletBase.cs` | DOT子弹基类 |
| `Assets/Scripts/Combat/DotBulletFactory.cs` | DOT子弹工厂 |
| `Assets/Scripts/Combat/StatusEffects/StatusEffectSystem.cs` | DOT效果管理 |
| `Assets/Scripts/Combat/StatusEffects/DotEffectRegistry.cs` | DOT效果注册表 |
| `Assets/Scripts/Player/DetonateSystem.cs` | 引爆系统 |
| `Assets/Scripts/Combat/StatusEffects/CurseSpreadSystem.cs` | 死亡传播DOT |
| `Assets/Scripts/Enemies/EnemyBase.cs` | 敌人基类 |
| `Assets/Scripts/Enemies/SpawnManager.cs` | 波次管理 |
| `Assets/Scripts/Enemies/EliteModifierSystem.cs` | 精英修饰器 |
| `Assets/Scripts/Combat/FrostLightningField.cs` | 霜电冰场 |
| `Assets/Scripts/Combat/FrostBullet.cs` | 霜冻子弹 |
| `Assets/Scripts/Combat/LightningBullet.cs` | 雷电子弹 |
| `Assets/Scripts/Combat/WindBullet.cs` | 风蚀子弹 |
| `Assets/Scripts/Combat/LightBulletController.cs` | 光明激光 |
| `Assets/Scripts/Combat/DarkBullet.cs` | 暗影子弹 |
