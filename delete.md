# 删除计划 — 护甲/腐蚀系统删除后的残骸清理

> 生成日期：2026-06-16
> 性质：**死代码清理**，非重构。核心子弹/DOT 体系已是干净的重构成果，无需再动。
> 已核实：每一项的引用范围（含测试、场景/预制体 GUID）均已 grep 确认，状态见下。

---

## ⚠️ 不要误删

- **`WindErosion`（风化/风蚀）= 风元素本体**，不是被删的 `Erosion`。`WindErosionEffect`、`DotEffectConfig.WindErosionKnockbackDistance`、`StatusEffectType.WindErosion` 全部是**活的**，保留。

---

## A 级 — 零引用，可直接删（低风险）

### A1. `CorrosiveEnemy`（腐蚀敌人，反 DOT）
- **文件**：`Assets/Scripts/Enemies/CorrosiveEnemy.cs` + `.cs.meta`
- **验证**：GUID `12deed24010729940984452664aa8a0f` 在**所有** `.unity`/`.prefab`/`.asset` 中**零引用**；代码中无任何 spawn/AddComponent 调用方。
- **主题**：属于已删除的"腐蚀"系，与系统删除一致。
- **动作**：直接删除两个文件。
- **连带**：无。

### A2. `WaveAffixSystem.GetBonusArmor()` + `IronSkin` 词缀
- **文件**：`Assets/Scripts/Combat/WaveAffixSystem.cs`
- **验证**：`GetBonusArmor()` 全 Assets（含测试）**零调用方**。护甲系统已删，该方法恒返回死值。
- **问题**：`IronSkin` 词缀从 `_allAffixes` 随机抽取（`WaveAffixSystem.cs:45`），但其唯一效果就是 `GetBonusArmor()`——**抽中即空效果**的坏词缀，玩家看到描述"敌人护甲+50"却无任何实际作用。
- **动作**：
  1. 删除 `GetBonusArmor()` 方法（约 79–86 行）。
  2. 删除 `AffixType` 枚举中的 `IronSkin` 成员（约第 17 行）。
  3. 删除 `IronSkin` 对应的描述/颜色 switch 分支（约 139、154 行）。
- **连带**：确认无其他文件引用 `GetBonusArmor` / `AffixType.IronSkin`（已 grep，仅本文件）。
- **备注**：若想保留"钢铁皮肤"词缀，应换成护甲无关的效果（如敌人 HP×1.5），而非删除。**这是产品决策点**。

---

## B 级 — 半成品装备系统（有测试 + reset 钩子，删除需连带处理，中风险)

> 现状：`Backpack.FindEquipment` 自己 log "no IEquipment instance… equipment uses affix-based system"——这是一套**被架空、从未真正接线**的系统，含 `EquipmentSlot.Armor`。但它被测试引用、且 `Backpack.Clear()` 挂在场景重置里，不是纯死代码。

### B 涉及文件
- `Assets/Scripts/Combat/IEquipment.cs`（含 `EquipmentSlot.Armor`、`IEquipment`、`EquipmentInstance`、`EquipmentRarity`、`EquipmentAffix`）
- `Assets/Scripts/Combat/Backpack.cs`
- `Assets/Scripts/Combat/LootDropSystem.cs`

### B 引用点（删除时必须一并处理）
- `Assets/Scripts/Core/Managers/GameStateResetter.cs:42` → `Backpack.Clear();`（删此行）
- `Assets/Scripts/ScriptableObjects/Config/GameConfig.cs:28` → `public int maxEquipmentSlots = 3;`（删此字段）
- `Assets/Tests/Editor/AutomatedPlayModeTests.cs`：
  - `P2_LootDropSystem_DropsEquipment`（约 369–379 行）
  - `P2_Backpack_AddAndRemove`（约 382–401 行）
  - 删除这两个测试方法。
- 数据资产 `Assets/Resources/Configs/GameConfig.asset:27` → `maxEquipmentSlots: 3`（序列化残留，删字段后由 Unity 自动清，或手动删）

### B 决策点
- **若短期内不做装备系统** → 整组删除（含上面所有连带），最干净。
- **若计划做装备系统** → 全部保留，但**必须在 `Ai_content.md` 登记**（目前文档完全没提，是隐形系统，已造成认知盲区）。
- 默认建议：**删除**。理由是它当前 100% 是空逻辑（`FindEquipment` 永远找不到实现）。

---

## C 级 — 序列化数据残骸（无害但陈旧误导，按需清理，低优先）

> 这些是 YAML 里的死字段，Unity 运行时忽略未知字段，**不影响功能**，但与"只有 Mage/Blue 角色、无护甲"的事实冲突，误导读者。

### C1. `armor` 字段残留
- `Assets/Resources/Configs/GameConfig.asset` → `basePlayerArmor: 0`、`dpsDummyArmor: 0`
- `Assets/Scenes/GameScene.unity:1065` → `_armor: 0`
- **动作**：手动删除这些行（或在 Unity 中重新保存以自动清理）。先确认 `GameConfig.cs` / 相关脚本已无对应字段（已确认 `Assets/Scripts` 内无 `basePlayerArmor` 活字段）。

### C2. 占位角色数据资产（产品决策，非纯清理）
- `Char_warrior / paladin / assassin / berserker / vampire / ranger / necromancer`（位于 `Assets/ScriptableObjects/Characters/` 与 `Assets/Resources/Characters/`）
- **现状**：`CharacterFactory` 把这 7 个 id 全部注册为 `MagePassive` 的别名（`CharacterFactory.cs:79–85`），即它们都是"换皮 Mage"。资产里仍写着 `armor: 2/3`、"+2 armor" 等被动描述。
- **动作**：**不在本次清理范围内自动删**。需你决定这些占位角色是否保留：
  - 保留 → 至少清掉资产里的 `armor` 字段和"armor"被动描述文案。
  - 不要 → 删除对应 `.asset` + `.meta`，并从 `CharacterFactory.EnsureInitialized()` 移除注册行。

---

## 执行顺序建议

1. **A 级**（CorrosiveEnemy + GetBonusArmor/IronSkin）— 立即做，零风险。
2. **B 级**（装备系统）— 先确认产品意向（删 or 留），再整组处理 + 删测试。
3. **C1**（序列化 armor 残骸）— 顺手清。
4. **C2**（占位角色资产）— 单独决策，不混入清理。
5. 全部完成后跑一遍测试套件（CoreSystemTests + AutomatedPlayModeTests + FiringSystemTests），并更新 `Ai_content.md` 的文件索引。

---

## 验证命令（删除前后各跑一次）

```bash
# A1: 确认 CorrosiveEnemy GUID 无场景/预制体引用（应为空）
grep -rl "12deed24010729940984452664aa8a0f" Assets --include=*.unity --include=*.prefab --include=*.asset

# A2: 确认 GetBonusArmor / IronSkin 无外部引用（应仅 WaveAffixSystem.cs 自身）
grep -rn "GetBonusArmor\|AffixType.IronSkin" Assets

# B: 确认装备系统引用点全部清理
grep -rn "IEquipment\|EquipmentInstance\|EquipmentSlot\|Backpack\|LootDropSystem\|maxEquipmentSlots" Assets
```
