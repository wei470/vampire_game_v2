# 元素反应矩阵

> 最后更新：2026-06-15

## 反应总览

| 反应名称 | 触发条件 | 效果 | 代码位置 |
|---------|---------|------|---------|
| **融化** | 霜冻 × 燃烧 | DOT 伤害 ×2，持续 MeltDuration | `BurnBullet.TriggerMelt()` |
| **燃烧扩散** | 燃烧 × 风化 | 燃烧传播到 BurnSpreadRadius 内敌人 | `BurnBullet.TriggerBurnSpread()` |
| **霜电冰场** | 霜冻 × 雷电 | 消耗静电层，生成冰场区域 | `FrostBullet.cs` + `FrostLightningField.cs` |
| **毒爆** | 中毒 × 黑暗 | 爆炸范围 ×2，毒液池范围更大 | `PoisonBullet.LeavePuddle()` |
| **碎裂** | 引爆 × 霜冻(≥80%减速) | AoE 伤害 = 霜冻层数 × 每层伤害 | `DetonateSystem.TriggerFrostShatter()` |
| **静电扩散** | 雷电叠满层 | 击退 + 眩晕周围敌人 | `StaticStackEffect.cs` |
| **暗影传播** | 黑暗标记敌人死亡 | DOT 传播到周围敌人 | `DarkMarkEffect.cs` |

---

## 交互矩阵

横轴 = 敌人身上已有的元素，纵轴 = 新命中/叠加的元素。
交叉点 = 触发的反应。

| 新命中 ↓ \ 已有 → | 中毒 | 燃烧 | 霜冻 | 雷电 | 黑暗 | 风化 |
|-------------------|------|------|------|------|------|------|
| **中毒** | 叠层 | — | — | — | **毒爆** | — |
| **燃烧** | — | 叠层 | **融化** | — | — | **燃烧扩散** |
| **霜冻** | — | **融化** | 叠层 | **霜电冰场** | — | — |
| **雷电** | — | — | **霜电冰场** | 静电扩散 | — | — |
| **黑暗** | **毒爆** | — | — | — | 暗影传播 | — |
| **风化** | — | **燃烧扩散** | — | — | — | 叠层 |

---

## 反应详情

### 融化（霜冻 × 燃烧）

- **触发**：火场 tick 时检测敌人身上有 FrostEffect
- **效果**：激活 MeltEffect，DOT 伤害 ×2，持续 `MeltDuration` 秒
- **消耗**：消耗一层霜冻
- **配置**：`DotEffectConfig.MeltDuration`、`MeltDamageMultiplier`

### 燃烧扩散（燃烧 × 风化）

- **触发**：火场 tick 时检测敌人身上有 WindErosionEffect
- **效果**：燃烧传播到 `BurnSpreadRadius` 内所有敌人
- **消耗**：消耗一层风化
- **配置**：`DotEffectConfig.BurnSpreadRadius`

### 霜电冰场（霜冻 × 雷电）

- **触发**：霜冻子弹命中时检测敌人身上有 StaticStackEffect
- **效果**：消耗静电层，在敌人周围生成 `FrostLightningField` 冰场区域
- **消耗**：消耗一层静电
- **代码**：`FrostBullet.cs`、`FrostLightningField.cs`

### 毒爆（中毒 × 黑暗）

- **触发**：毒子弹命中时检测敌人身上有 DarkMarkEffect
- **效果**：爆炸范围 ×2，毒液池范围更大
- **配置**：`DotEffectConfig.PoisonExplosionRadius`、`PoisonPuddleRadius`

### 碎裂（引爆 × 霜冻）

- **触发**：引爆时检测敌人霜冻减速 ≥ 80%
- **效果**：AoE 伤害 = 霜冻层数 × `DetonateFrostShatterDmgPerStack`
- **范围**：`DetonateFrostShatterRadius`
- **配置**：`DotEffectConfig.DetonateFrostShatterThreshold`、`DetonateFrostShatterRadius`、`DetonateFrostShatterDmgPerStack`

### 静电扩散（雷电叠满层）

- **触发**：StaticStackEffect 达到放电阈值
- **效果**：击退 + 眩晕周围敌人

### 暗影传播（黑暗标记死亡）

- **触发**：带 DarkMarkEffect 的敌人死亡
- **效果**：DOT 传播到周围敌人
