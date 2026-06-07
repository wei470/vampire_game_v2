# 🔮 Mage 专属强化系统设计 ✅ 全部已实现

> 仅限 Mage 角色可选，与 DOT 子弹枪（7种）、DOT 增强（5种）、引爆增强（2种）、子弹增强（3种）独立。
> 
> **实现状态**: 全部28种强化已实现 ✅
> - 配置层: `MageUpgradeConfig.cs` (28条UpgradeEntry)
> - 属性层: `MagePassive.cs` (28个属性字段+访问器)
> - 应用层: `MageUpgradeApplier.cs` (28个switch分支)
> - 枚举层: `CharacterUpgradeData.cs` (28个UpgradeCategory)
> - 联动层: `StatusEffectSystem.cs` / `DetonateSystem.cs` / `CurseSpreadSystem.cs`

---

## 现有系统回顾

| 类别 | 已有 | 数量 |
|------|------|------|
| DOT 子弹枪 | bleed/poison/burn/frostbite/static/dark/light | 7 |
| DOT 增强 | corrosion/curse/agony/wither/erosion | 5 |
| 引爆增强 | radiate/contaminate | 2 |
| 子弹增强 | haste/barrage/ricochet | 3 |
| DOT 组合 | 碎冰/爆燃/脓毒/沸血/血电/冻毒/导电毒液/蒸发/等离子/超导 | 10 |
| 元素融合 | 5种 | 5 |
| 引爆系统 | 蓄力/连锁/余烬/碎裂 | 4 |

---

## 一、DOT 精通强化（深度DOT玩法）

### 1. 蔓延 (Pandemic)
- **upgradeId**: `pandemic`
- **效果**: DOT 在敌人之间传播时，传播效率 +15%（50%→65%→…），最高100%完整传播
- **叠加上限**: 无限（传播效率上限100%）
- **设计思路**: 与黑暗标记的死亡传播和诅咒传播系统联动，让 DOT 在怪群中形成连锁瘟疫效果
- **前置条件**: 需要至少1种DOT子弹

### 2. 饱和 (Saturation)
- **upgradeId**: `saturation`
- **效果**: 同一敌人身上每有1种不同DOT，所有DOT伤害 +5%
- **叠加上限**: 无限（单种加成上限15%）
- **设计思路**: 鼓励多DOT混合打法，拥有的DOT种类越多收益越高。3种DOT时 = +15%，5种时 = +25%
- **前置条件**: 需要至少2种DOT子弹

### 3. 共鸣 (Resonance)
- **upgradeId**: `resonance`
- **效果**: DOT触发时有10%几率不消耗持续时间（等于延长DOT寿命）
- **叠加上限**: 5层（上限50%）
- **设计思路**: 让DOT在敌人身上存在更久，与"侵蚀"叠加后DOT几乎不衰减
- **前置条件**: 需要至少1种DOT子弹

### 4. 剧毒天赋 (Toxicology)
- **upgradeId**: `toxicology`
- **效果**: DOT的暴击率额外 +8%（与凋零叠加）
- **叠加上限**: 5层（上限40%）
- **设计思路**: 深化DOT暴击体系，让凋零+剧毒天赋 = 极高DOT暴击率

### 5. 腐化之触 (CorruptTouch)
- **upgradeId**: `corrupt_touch`
- **效果**: DOT子弹命中时，额外施加一个2秒的弱化debuff：敌人攻击力 -10%
- **叠加上限**: 无限（攻击力降低上限50%）
- **设计思路**: DOT不仅是伤害手段，还提供控制/削弱能力

---

## 二、引爆强化（深度引爆玩法）

### 6. 连锁反应 (ChainReaction)
- **upgradeId**: `chain_reaction`
- **效果**: 引爆时如果杀死敌人，立即触发一次50%伤害的二次引爆
- **叠加上限**: 3层（触发2次二次引爆）
- **设计思路**: 引爆清场的"雪崩"效果，怪群越密集连锁越多

### 7. 余烬强化 (EmberBoost)
- **upgradeId**: `ember_boost`
- **效果**: 引爆的余烬伤害 +25%，余烬持续时间 +1秒
- **叠加上限**: 无限
- **设计思路**: 深化引爆系统中的余烬机制，让引爆后仍能持续输出

### 8. 碎裂强化 (ShatterBoost)
- **upgradeId**: `shatter_boost`
- **效果**: 引爆的碎裂碎片数量 +2，碎片伤害 +15%
- **叠加上限**: 无限
- **设计思路**: 引爆后的AOE碎片覆盖更广，清场能力更强

### 9. 蓄力精通 (ChargeMastery)
- **upgradeId**: `charge_mastery`
- **效果**: 引爆蓄力速度 +20%，满蓄力引爆伤害 +15%
- **叠加上限**: 无限（蓄力速度上限+60%）
- **设计思路**: 让引爆更频繁地使用，减少等待时间

### 10. 元素引爆 (ElementalBurst)
- **upgradeId**: `elemental_burst`
- **效果**: 引爆时，敌人身上每种不同DOT类型额外造成 8 点固定伤害
- **叠加上限**: 无限（单DOT额外伤害上限20点）
- **设计思路**: 多DOT → 高引爆伤害的正反馈循环，鼓励叠加多种DOT后再引爆

---

## 三、子弹枪强化（DOT枪增强）

### 11. 双持 (DualWield)
- **upgradeId**: `dual_wield`
- **效果**: 随机一把已拥有的DOT枪射速 +15%
- **叠加上限**: 无限（单枪射速上限+60%）
- **设计思路**: 强化已拥有的DOT枪，让早期选择的枪在后期不落后

### 12. 溢出弹 (Overflow)
- **upgradeId**: `overflow`
- **效果**: DOT枪子弹命中已有同类型DOT的敌人时，额外叠1层DOT
- **叠加上限**: 3层（额外叠2层）
- **设计思路**: 让DOT枪在后期叠层更快，与中毒的层叠机制完美配合

### 13. 弹药精通 (AmmoMastery)
- **upgradeId**: `ammo_mastery`
- **效果**: 所有DOT枪子弹速度 +20%，射程 +15%
- **叠加上限**: 5层
- **设计思路**: 让DOT枪的弹道更可靠，减少空枪率

### 14. 元素亲和 (ElementalAffinity)
- **upgradeId**: `elemental_affinity`
- **效果**: 拥有的每种DOT枪为其他DOT枪提供 +3% 伤害加成
- **叠加上限**: 无限（单枪加成上限10%）
- **设计思路**: 多DOT枪互相增益，7种DOT枪时每把枪 +18% 伤害

### 15. 贯穿弹 (Penetrate)
- **upgradeId**: `penetrate`
- **效果**: DOT枪子弹穿透 +1 个敌人
- **叠加上限**: 3层
- **设计思路**: 让DOT枪也能穿透群怪，大幅提升群战能力

---

## 四、组合/协同强化（跨系统联动）

### 16. 元素风暴 (ElementalStorm)
- **upgradeId**: `elemental_storm`
- **效果**: 当同时有3种以上DOT在场上活跃时，每2秒对所有有DOT的敌人造成 5 点元素伤害
- **叠加上限**: 无限（伤害+3/层，间隔-0.2秒/层，最低1秒）
- **设计思路**: 全局被动伤害，DOT种类越多越强，鼓励全面铺开DOT

### 17. 暗影链接 (ShadowLink)
- **upgradeId**: `shadow_link`
- **效果**: 黑暗标记的传播范围 +1（3→4→…），传播效率 +10%
- **叠加上限**: 无限（范围上限8，效率上限100%）
- **设计思路**: 深化黑暗子弹的独特机制，让黑暗标记成为DOT传播的核心枢纽

### 18. 光明审判 (LightJudgment)
- **upgradeId**: `light_judgment`
- **效果**: 光明标记每层受伤加成 +0.5%→+0.8%（每层从+0.5%提升到+0.8%）
- **叠加上限**: 5层（每层+1.2%，即最高每层+2.7%）
- **设计思路**: 光明标记的无上限特性 + 提升单层收益 = 后期Boss杀手

### 19. 静电领域 (StaticField)
- **upgradeId**: `static_field`
- **效果**: 被静电控制的敌人周围1.5范围的其他敌人也获得1层静电
- **叠加上限**: 3层（范围+0.5）
- **设计思路**: 让雷电子弹的连锁效果更强，形成静电领域

### 20. 霜爆 (FrostExplosion)
- **upgradeId**: `frost_explosion`
- **效果**: 霜冻减速达到80%以上的敌人被引爆时，额外造成该敌人最大生命 3% 的冰霜伤害
- **叠加上限**: 无限（百分比上限10%）
- **设计思路**: 霜冻+引爆的组合技，对高血量敌人（Boss）特别有效

---

## 五、生存向 Mage 强化

### 21. 吸血法术 (VampiricSpell)
- **upgradeId**: `vampiric_spell`
- **效果**: DOT每造成一次伤害，回复 0.3 点生命
- **叠加上限**: 5层（回复+0.3/层，最高1.5/次）
- **设计思路**: Mage独有的DOT回复机制，DOT越多回得越快

### 22. 元素护盾 (ElementalShield)
- **upgradeId**: `elemental_shield`
- **效果**: 每拥有一种DOT枪，获得 +2 最大生命
- **叠加上限**: 无限
- **设计思路**: 鼓励收集多种DOT枪的被动生存收益

### 23. 相位移动 (PhaseShift)
- **upgradeId**: `phase_shift`
- **效果**: 引爆后 2 秒内免疫碰撞伤害
- **叠加上限**: 3层（持续+1秒/层）
- **设计思路**: 引爆作为自保手段，鼓励积极使用引爆

### 24. 灵魂虹吸 (SoulSiphon)
- **upgradeId**: `soul_siphon`
- **效果**: 带有DOT的敌人死亡时，回复 1 点生命 + 获得 0.5 秒 30% 移速加成
- **叠加上限**: 无限（回复+1/层，移速持续+0.3秒/层）
- **设计思路**: 击杀奖励，DOT收割型打法的核心

---

## 六、高级/终极强化

### 25. 元素大师 (ElementalMaster)
- **upgradeId**: `elemental_master`
- **效果**: 拥有5种以上DOT枪时解锁：所有DOT持续时间 +25%，引爆冷却 -20%
- **叠加上限**: 1层（终极强化，不可重复）
- **前置条件**: 至少5种DOT枪
- **设计思路**: 收集型终极奖励，鼓励全面发展DOT

### 26. 末日审判 (Doomsday)
- **upgradeId**: `doomsday`
- **效果**: 引爆时，如果敌人身上有3种以上DOT，直接秒杀生命低于 15% 的敌人
- **叠加上限**: 3层（生命阈值+5%/层，最高30%）
- **设计思路**: 引爆斩杀机制，对Boss的终结技

### 27. 永恒痛苦 (EternalAgony)
- **upgradeId**: `eternal_agony`
- **效果**: DOT的持续时间不再有上限（基础持续时间 ×2），但单次伤害 -15%
- **叠加上限**: 1层
- **设计思路**: "慢但持久"的DOT哲学，配合频率增强效果更佳

### 28. 湮灭领域 (AnnihilationZone)
- **upgradeId**: `annihilation_zone`
- **效果**: 引爆后在原地留下一个持续3秒的元素领域，领域内敌人每秒受到 5 点伤害并被减速30%
- **叠加上限**: 3层（持续+1秒，伤害+3/层）
- **设计思路**: 引爆后的区域控制，适合防守和卡位

---

## 七、Build 路线分类

### 🧪 DOT 瘟疫路线（Pandemic Build）
核心：pandemic + saturation + resonance + overflow + elemental_storm
玩法：铺开多种DOT，让DOT在怪群中自行传播和增强

### 💥 引爆专家路线（Detonate Build）
核心：charge_mastery + chain_reaction + elemental_burst + ember_boost + doomsday
玩法：蓄力→引爆→连锁→余烬，一发清屏

### 🔫 DOT枪精通路线（Gun Master）
核心：dual_wield + ammo_mastery + penetrate + elemental_affinity + overflow
玩法：强化DOT枪本身，射速快、穿透强、叠层快

### 🌑 暗黑传播路线（Dark Plague）
核心：dark + shadow_link + pandemic + curse + corrosive_touch
玩法：黑暗标记 + DOT传播，敌人死亡时DOT如瘟疫般扩散

### ✨ 光明审判路线（Light Judgement）
核心：light + light_judgment + saturation + resonance + elemental_master
玩法：光明标记无限叠层，后期每一发都是致命一击

### 🛡️ 法师生存路线（Mage Survivor）
核心：vampiric_spell + elemental_shield + phase_shift + soul_siphon + tough
玩法：DOT越多越肉，引爆后无敌，击杀回血

---

## 八、实现优先级

### P0 — 核心扩展（新增DOT维度）
1. `saturation` 饱和 — 多DOT奖励，立即提升多DOT打法价值
2. `elemental_burst` 元素引爆 — 引爆与DOT数量联动
3. `vampiric_spell` 吸血法术 — Mage独有生存机制
4. `overflow` 溢出弹 — DOT枪叠层加速

### P1 — 深度玩法
5. `pandemic` 蔓延 — DOT传播增强
6. `chain_reaction` 连锁反应 — 引爆清场
7. `dual_wield` 双持 — DOT枪射速
8. `charge_mastery` 蓄力精通 — 引爆频率

### P2 — 协同/趣味
9. `resonance` 共鸣 — DOT寿命
10. `elemental_storm` 元素风暴 — 全局DOT被动
11. `shadow_link` 暗影链接 — 黑暗专精
12. `light_judgment` 光明审判 — 光明专精

### P3 — 终极/高级
13. `elemental_master` 元素大师 — 收集终极奖励
14. `doomsday` 末日审判 — 引爆斩杀
15. `annihilation_zone` 湮灭领域 — 区域控制
16. `eternal_agony` 永恒痛苦 — DOT哲学转变
</parameter>