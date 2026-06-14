# 伤害公式

> 最后更新：2026-06-13

## 一、DOT 持续伤害

### 燃烧（BurnStackEffect）

```
tick间隔 = BurnBaseTickInterval (固定 0.5s，不受层数影响)
单次tick伤害 = baseDps × tick间隔 × (1 + (stacks-1) × 10%)
DPS = baseDps × (1 + (stacks-1) × 10%)
```

- `baseDps` = MageUpgradeConfig `burn.dotDps` × `dmgMult`
- 层数上限：无上限，每命中叠 1 层
- 持续时间：命中后刷新 `burn.dotDuration`
- 示例：1层 = 2 DPS，5层 = 2.8 DPS，10层 = 3.8 DPS

### 毒素（PoisonStackEffect）

```
tick间隔 = PoisonBaseTickInterval (固定 1.0s，不受层数影响)
单次tick伤害 = PoisonDamagePerTick + (stacks - 1)
DPS = (PoisonDamagePerTick + stacks - 1) / tickInterval
```

- `PoisonDamagePerTick` = DotEffectConfig 默认 2
- 层数上限：`PoisonMaxStacks` 默认 20
- 示例：1层 = 2/tick，5层 = 6/tick，10层 = 11/tick

### 霜冻（FrostEffect）

```
减速% = FrostBaseSlowPct + (stacks-1) × FrostSlowPerStack
上限减速 = FrostMaxSlow (90%)
```

- 无伤害，纯控制效果
- 叠层触发静电→冰场元素反应

### 风化（WindErosionEffect）

```
击退距离 = WindErosionKnockbackDistance × (1 + stacks × 0.1)
```

- 无伤害，纯击退效果
- 每命中 WindHitsPerStack 次叠 1 层

### 流血（BleedEffect）

```
tick伤害 = dps
DPS = dps
```

- 固定 DPS，不随层数变化

---

## 二、引爆伤害（E 键）

引爆冲击波接触敌人时触发，**上限 800**。

### 引爆冷却

```
基础冷却 = DotEffectConfig.DetonateCooldown (12s)
污染减冷 = DetonateCooldownReduction (每层 -10%，最多 6 层)
实际冷却 = 基础冷却 × max(0.1, 1 - 污染减冷)
```

| 污染层数 | 减冷% | 实际冷却 |
|----------|-------|----------|
| 0 | 0% | 12.0s |
| 1 | 10% | 10.8s |
| 2 | 20% | 9.6s |
| 3 | 30% | 8.4s |
| 4 | 40% | 7.2s |
| 5 | 50% | 6.0s |
| 6 | 60% | 4.8s |

### 引爆伤害倍率

```
辐射：每层 ×115%，最多 10 层
detonateMultiplier = 3.0 × 1.15^层数
```

| 辐射层数 | 倍率 |
|----------|------|
| 0 | 3.00 |
| 1 | 3.45 |
| 2 | 3.97 |
| 5 | 6.03 |
| 10 | 12.14 |

### 路径 1：StatusEffectManager.Detonate

```
baseDmg = eff.damagePerSecond × 5 × detonateMultiplier

毒液额外: baseDmg ×= 1 + (stacks-1) × 5%
流血额外: baseDmg ×= 1 + (stacks-1) × 3%
撕裂加成: baseDmg ×= (1 + RendDamageBonus)
痛苦加成: baseDmg ×= (1 + (1 - HP/MaxHP) × AgonyMissingHpScale)
暴击:     baseDmg ×= critMult (概率 = critChance)
```

### 路径 2：StackEffect 组件引爆（DetonateWaveEffect）

```
流血引爆 = bleed.dps × 5 × detonateMultiplier
燃烧引爆 = burn.baseDps × 5 × detonateMultiplier
毒素引爆 = 2 × 5 × detonateMultiplier (固定值)
```

- 不叠加路径 1（路径 1 读 `_activeEffects`，路径 2 读 StackEffect 组件）
- 两者独立触发，总引爆伤害 = 路径1 + 路径2

### 连锁引爆

```
连锁伤害 = 10 × detonateMultiplier × chainDamageRatio
chainDamageRatio = 0.5^chainLevel (每次衰减50%)
最大连锁次数 = maxChainCount
```

### 霜爆

```
霜爆伤害 = 50 × detonateMultiplier (固定值，上限800)
触发条件：霜冻减速% ≥ frostShatterThreshold
```

### 末日审判

```
触发条件：3种以上DOT + HP% ≤ DoomsdayThreshold
伤害 = 敌人当前HP (直接击杀)
```

---

## 三、基础参数

| 参数 | 默认值 | 来源 |
|------|--------|------|
| detonateMultiplier | 3.0 | MagePassive.DetonateMultiplier |
| 燃烧 baseDps | 2 | MageUpgradeConfig burn.dotDps |
| 毒素 damagePerTick | 2 | DotEffectConfig PoisonDamagePerTick |
| 燃烧 tick 间隔 | 0.5s | DotEffectConfig BurnBaseTickInterval |
| 毒素 tick 间隔 | 1.0s | DotEffectConfig PoisonBaseTickInterval |
| 引爆上限 | 800 | StatusEffectSystem.Detonate |

---

## 四、示例计算

### 1 层燃烧引爆 Boss

```
路径2: burn.baseDps(2) × 5 × 3 = 30
路径1: (StatusEffectManager._activeEffects 为空，不触发)
总引爆伤害 = 30
```

### 5 层燃烧 + 5 层毒素引爆

```
路径1: DPS(2) × 5 × 3 × (1 + 4×0.05) = 30 × 1.2 = 36
路径2 燃烧: 2 × 5 × 3 = 30
路径2 毒素: 2 × 5 × 3 = 30
总引爆伤害 = 36 + 30 + 30 = 96
```

### 10 层燃烧持续伤害

```
DPS = 2 × (1 + 9×0.1) = 2 × 1.9 = 3.8
每 0.5s 造成 1.9 伤害
```

---

## 五、护甲系统

### 护甲减伤公式

```
减伤% = min(护甲 × 2%, 90%)
实际伤害 = 原始伤害 × (1 - 减伤%)
```

- 每点护甲 = 2% 减伤
- 减伤上限 90%（45 护甲即可达到）
- 最低伤害 0.01（不会完全免疫）

### 护甲值示例

| 护甲 | 减伤% | 100伤害实际 |
|------|-------|-------------|
| 0 | 0% | 100 |
| 5 | 10% | 90 |
| 10 | 20% | 80 |
| 20 | 40% | 60 |
| 30 | 60% | 40 |
| 45 | 90% (上限) | 10 |
| 50 | 90% (上限) | 10 |

### 敌人护甲成长

```
敌人护甲 = 基础护甲 + 波次 + 精英护甲
```

- 每波 +1 护甲
- 第 10 波敌人 = 10 护甲（20% 减伤）
- 第 20 波敌人 = 20 护甲（40% 减伤）
- 第 45 波敌人 = 45 护甲（90% 减伤，上限）

### 护甲腐蚀（Corrosion）+ 侵蚀（Erosion）

计算顺序：**先腐蚀，再侵蚀，最后结算伤害**

```
1. 腐蚀: 护甲 = Floor(护甲 × 90%)，最多8层
2. 侵蚀: 护甲 = 护甲 - 侵蚀层数（无视1点/层，无限叠加）
3. 护甲 = max(0, 护甲)
4. 减伤% = min(护甲 × 2%, 90%)
5. 实际伤害 = 原始伤害 × (1 - 减伤%)
```

**示例：20护甲敌人，腐蚀1层 + 侵蚀5层**

```
腐蚀: Floor(20 × 0.9) = 18
侵蚀: 18 - 5 = 13
减伤: 13 × 2% = 26%
100伤害 → 100 × 0.74 = 74
```

| 腐蚀层数 | 20护甲 → | 侵蚀5层后 |
|----------|----------|-----------|
| 0 | 20 | 15 |
| 1 | 18 | 13 |
| 2 | 16 | 11 |
| 3 | 14 | 9 |
| 5 | 12 | 7 |
| 8 | 8 | 3 |
