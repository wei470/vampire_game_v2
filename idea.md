# 光明 & 黑暗 子弹强化构想

> 目标：给「光明」「黑暗」两种目前还**没有专属强化**的 DOT 子弹设计一批*有趣*（而非纯数值）的强化，并补上元素反应。
> 每条都标注「实现挂点」，方便直接落地。稀有度沿用：🩶Common / 🟢Uncommon / 🔵Rare / 🟣Epic。

---

## 0. 现状速览（设计基线）

| 元素 | 子弹 | 核心机制 | 主题 |
|---|---|---|---|
| **黑暗** | `DarkBullet` + `DarkMarkEffect` | 命中打**黑暗标记**（不直接伤害，**上限 1 层**）。被标记敌人**死亡时**把身上所有 DOT 按 `效率(默认50% + 层×5%)` 传播给半径内敌人（暗影传播）+ 暗紫冲击波/锁链视觉。 | 传染 · 死亡收割 · 瘟疫 |
| **光明** | `LightBulletController` + `LightMarkEffect` | 蓄力 3s → 朝鼠标射**激光** + 顺时针扫 45°，每 3 帧 1 点伤害 + 叠**光明标记**（每层 +0.5% **易伤**，无上限，持续 15s）。 | 易伤放大 · 激光 · 审判 |

已有反应：**紫电**(雷×暗)、**天照**(火×暗)、**暗影传播**(暗标记死亡)。
**光明目前 0 反应** ← 最大的发挥空间。

> 💡 现成接口：`DarkMarkEffect` 已预留 `RadiusBonus` / `EfficiencyBonus` 两个 `[NonSerialized]` 字段（注释写明"可被升级增强"），**接两条灰色强化几乎零成本**。

---

## 1. 黑暗强化（主题：传染链 · 死亡引爆 · 聚怪）

### 🩶 暗影蔓延 (Spreading Shadow)
传播半径 +20% / 层（maxStacks 3）。
**挂点**：直接写 `DarkMarkEffect.RadiusBonus`（字段已存在）。

### 🩶 深渊侵蚀 (Abyssal Mark)
死亡传播效率 +10% / 层（maxStacks 3）。
**挂点**：直接写 `DarkMarkEffect.EfficiencyBonus`（字段已存在）。

### 🟢 虚空烙印 (Void Brand)
**带黑暗标记的敌人受到的所有 DOT 伤害 +15%。**（黑暗版"瘫痪"，让标记本身有进攻价值，而不只是等死。）
**挂点**：完全复用刚做的 `DotBulletHelper.ApplyParalysis` 模式——加一个 `ApplyVoidBrand(enemy,dmg)`，条件改成 `DarkMarkEffect.IsActive`。

### 🟢 连锁噩梦 (Chain Nightmare)
被暗影传播**感染**到的敌人，自己也会获得黑暗标记（**仅可再链 1 跳**），死亡时再传播一次。把黑暗从"单次扩散"变成"可控的二段瘟疫"。
**挂点**：`DarkMarkEffect.SpreadDotOnDeath` 里，对被感染敌人 `AddComponent<DarkMarkEffect>` 并打一个 `_chainDepth` 标志防无限。

### 🔵 黑洞 (Black Hole)
黑暗标记敌人**死亡时生成短暂引力漩涡**，把附近敌人拖向死亡点（~0.5s），再触发传播。聚怪 → 喂给引爆 / 火场 / 毒池，combo 极强。
**挂点**：复用 `WindErosionVortex` 的吸引逻辑（已有 pull 实现），在 `OnDeathHandler` 里生成一个吸引体。

### 🔵 献祭 (Soul Harvest)
黑暗标记敌人死亡时，对周围被传播的敌人**立即引爆 1 秒量的已有 DOT**（迷你引爆），并回复玩家少量护盾/能量。与引爆流强联动。
**挂点**：`SpreadDotOnDeath` 末尾，对命中目标调一次类似 `DetonateWaveEffect.ApplyDetonateToEnemy` 的瞬时结算。

### 🟣 瘟疫君主 (Plague Lord)
黑暗标记上限 **1 → 5 层**；传播时**满层不再衰减**（效率直接拉到 100%，DOT 原样复制而非 ×50%）。把"撒标记"变成真正的 build 核心。
**挂点**：`DarkMarkEffect.MAX_STACK` 改为可配；`efficiency` 计算在该强化下钳到 1.0。

### 🟣 永夜 (Endless Night)
真·链式瘟疫：被传播感染的敌人**死亡也会继续传播**，每跳效率递减 50%，直到衰减为 0。后期清屏雪球。
**挂点**：`连锁噩梦` 的无限版——`_chainDepth` 不设上限，改为效率随深度 `*0.5` 自然收敛。

### 🟣 湮灭 (Annihilation)
黑暗子弹命中 **HP < 15%** 的已标记敌人 → **直接处决**（走 `BaseEntity.Die()`，绕过减伤），处决触发**满效率**暗影传播。黑暗本体版的"紫电处决"。
**挂点**：`DarkBullet.OnTriggerEnter2D` 加阈值判定 + `Die()`（参考 `LightningBullet.ExecuteEnemy`）。

---

## 2. 光明强化（主题：易伤放大 · 激光手感 · 神圣审判）

### 🩶 聚焦 (Focus)
激光宽度 +25% / 层（maxStacks 3），命中更宽一条。
**挂点**：`LightBulletController._laserWidth` 读 `mage` bonus。

### 🩶 过曝 (Overexposure)
光明标记每层易伤 +0.5% → **+1.0%**（maxStacks 视情况）。让易伤堆叠更快见效。
**挂点**：`LightMarkEffect._damagePerStack` 读 bonus。

### 🟢 速能 (Quick Charge)
蓄力时间 -25% / 层。光明最大痛点是蓄力慢，这条直接改善手感。
**挂点**：`LightBulletController._chargeDuration`。

### 🟢 回旋扫射 (Wide Sweep)
扫射角度 45° → 135°（扫射时长等比延长），一次横扫小半个屏幕。
**挂点**：`LightBulletController._sweepAngle` + `_sweepDuration`。

### 🔵 棱镜 (Prism)
激光命中敌人时**分裂 2 道副激光**射向最近的其他敌人。单体激光 → 多目标网。
**挂点**：`HitEnemiesWithLaser` 命中后对最近 2 敌再做一次短 `RaycastAll` + 视觉。

### 🔵 灼光 (Searing Light)
激光命中**已带 DOT** 的敌人时，**立即结算该敌人 1 秒量的全部 DOT 伤害**（光把 DOT "点爆"一下）。光明 ↔ 其他元素的强力黏合。
**挂点**：`HitEnemiesWithLaser` 里检测 `StatusEffectManager`，调瞬时结算（同"献祭"的迷你引爆）。

### 🟣 白昼 (Daybreak)
激光不再"扫一下就消失"，而是**持续光柱跟随鼠标** N 秒（持续 DPS + 持续叠易伤）。彻底改变光明玩法（一次性 skillshot → 持续光束）。
**挂点**：`LightBulletController` 加 `Phase.Sustained`，扫射阶段替换为跟随鼠标的持续 `Update`。

### 🟣 超新星 (Supernova)
蓄力**完成瞬间**先在玩家周围炸一圈全屏闪光，对所有可见敌人**施加 X 层光明标记**，再射激光。开场即全场易伤。
**挂点**：`UpdateCharging` 蓄满分支里，遍历 `SpawnManager.ActiveEnemies` 叠 `LightMarkEffect`。

### 🟣 圣裁 (Holy Judgment)
光明标记 ≥ N 层的敌人，**引爆时**额外受到 `层数 × K` 的真实伤害；且这些敌人的易伤**翻倍**。把光明做成"引爆 + 易伤"双 build 的放大器。
**挂点**：`DetonateWaveEffect.ApplyDetonateToEnemy` 读 `LightMarkEffect.StackCount`。

---

## 3. 新元素反应（重点：给光明开光！）

> 光明与黑暗天然对立，是做反应的最佳一对。命名延续 毒爆/融化/天照/紫电 风格。

### ☀️🌑 日食 (Eclipse) — 光明 × 黑暗 ★主推
同一敌人**同时带光明标记和黑暗标记** → 触发"日食"：
- 立刻把该敌人身上的全部 DOT **按光明标记层数放大后引爆**，并以"满效率暗影传播"扩散给周围；
- 视觉：黑白光环对冲 + 一圈日冕。
对立双标记的化学反应，主题完美，且自然把两条线 build 串起来。
**挂点**：`LightMarkEffect.AddStack` / `DarkBullet` 命中时检测对方标记存在 → 触发一次合并结算。

### 🌟 虹光 (Refraction) — 光明 × 风化
风化敌人被激光命中 → 激光在该敌人处**折射**追加一道随机方向的副光束（风把光"吹散"）。机动光网。

### 💥 过载曝光 (Overload) — 光明 × 雷电
带静电层的敌人被激光命中 → 易伤标记瞬间 ×2 并触发一次小范围静电放电（光 + 电 = 过曝击穿）。

> 黑暗已有 紫电(雷)、天照(火) 两个反应，再补「日食(光)」即可让黑暗与四元素都有交互。

---

## 4. 取向小结 / 落地优先级

| 优先级 | 理由 | 推荐项 |
|---|---|---|
| **立刻能做（≈0 成本）** | 字段/Helper 已存在 | 暗影蔓延、深渊侵蚀（复用 RadiusBonus/EfficiencyBonus）；虚空烙印（复用 ApplyParalysis 模式） |
| **高趣味中成本** | 改变玩法手感 | 黑洞、献祭、棱镜、灼光、速能、回旋扫射 |
| **build 核心（紫色）** | 定义流派 | 瘟疫君主 / 永夜（黑暗瘟疫流）、白昼 / 圣裁（光明持续/引爆流） |
| **战略亮点** | 补全反应矩阵 | **日食（光×暗）** 最该先做 |

**两条线的差异化定位**：
- **黑暗 = 雪球/群体收割** —— 标记→死亡→传染→聚怪→再死，build 目标是"让第一个死亡引发连锁"。
- **光明 = 放大器/技术流** —— 蓄力 skillshot + 易伤叠层，build 目标是"把单点伤害和引爆放到最大"，奖励瞄准。
