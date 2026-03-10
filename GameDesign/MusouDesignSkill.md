# 无双类动作游戏设计 Skill（爽感优先，项目落地版）

## 1. 目标与原则

**目标**：在高密度敌群场景中维持持续爽感，保证战斗、关卡、经济、成长与 UI 形成稳定闭环。

**核心原则（10 条）**
- 高反馈密度：单位时间内“命中反馈”不断档
- 连段不断档：断连必须可控且可解释
- 破防 + 击飞循环：小怪击飞，精英破防
- 决策频率适中：8–12 秒一次明显决策点
- 资源回收快于消耗：爽感阶段资源回转更快
- 留白短且明确：喘息 5–8 秒为上限
- 位移是爽感的一部分：穿群与聚怪是核心体验
- 容错高于惩罚：群战不适合高惩罚反馈
- 事件不打断：事件用于加压或转场
- Boss 是节奏巅峰：破防窗口与爆发阶段必须明显

## 2. 完整系统与流程

**系统总览**
- 战斗系统：连段/击飞/破防/技能回转
- 敌群与 AI：配比、节流、攻击令牌
- 关卡节奏：据点 → 事件 → 精英 → Boss Gate
- 经济循环：掉落/消耗品/深渊币/商店
- 成长系统：等级/天赋/珍珠/技能
- 任务系统：主线/支线/挑战目标与奖励
- UI 交互：HUD 信息密度与可读性

**流程模板**
1) 进入关卡：读取章节/难度/奖励倍率
2) 据点序列：Stronghold_01 → Stronghold_02
3) 事件插入：Reinforcement/Chase/Hold/Protect
4) Boss Gate：据点清完后刷 Boss
5) 结算回路：关卡奖励 + 任务奖励 + 掉落回收

## 3. 关卡与事件设计

**关卡结构**
- 每关 2 个据点：
  - Stronghold_01 建立节奏
  - Stronghold_02 拉升强度
- 每据点 3–5 波次：
  - Wave1 稳态
  - Wave2–3 引入变体
  - Wave4+ 高压

**事件节奏与作用**
- Reinforcement：密度提升，节奏更紧凑
- Chase：短促高压，拉高 KPM
- HoldPoint：留白与聚怪空间
- ProtectTarget：目标防守，风险聚焦

## 4. 敌群配比与节奏（项目默认）

**统一规则入口**
- `Assets/ThirdPersonController/Scripts/Core/IntensityWaveDirector.cs`
- `WaveArchetypeProfile`（波次配比）
- `WaveEventTuning`（事件节奏）

**波次配比（默认）**
| WaveIndex | Grunt | Rusher | Tank | Elite | Ranged | Controller | Suicide |
|-----------|-------|--------|------|-------|--------|------------|---------|
| 0 | 1.15 | 0.95 | 0.85 | 0.9 | 0 | 0 | 0 |
| 1 | 1 | 1 | 0.95 | 1 | 0.7 | 0.4 | 0 |
| 2 | 0.95 | 0.95 | 1 | 1.05 | 0.8 | 0.7 | 0.5 |
| 3+ | 0.9 | 0.9 | 1.05 | 1.1 | 0.85 | 0.85 | 0.75 |

**事件节奏（默认）**
| EventType | countMultiplier | intervalMultiplier | 说明 |
|----------|-----------------|--------------------|------|
| Reinforcement | 0.95 | 0.9 | 密度略高、节奏更紧凑 |
| Chase | 0.85 | 0.8 | 高压短促 |
| HoldPoint | 0.9 | 1.1 | 留白与位移空间 |
| ProtectTarget | 1 | 1 | 标准节奏 |

**强度控制（默认）**
| 参数 | 默认值 | 说明 |
|------|--------|------|
| targetKillsPerMinute | 120 | 强度目标 KPM |
| intensityWindowSeconds | 20 | 统计窗口 |
| minCountMultiplier | 0.85 | 低强度倍率 |
| maxCountMultiplier | 2.1 | 高强度倍率 |
| minIntervalMultiplier | 0.45 | 高强度间隔倍率 |
| maxIntervalMultiplier | 1.2 | 低强度间隔倍率 |
| waveRampPerWave | 0.12 | 波次递增 |
| maxTotalCountMultiplier | 2.4 | 总量上限 |

## 5. 战斗与 AI 关键点

**战斗指标（目标区间）**
- KPM：90–150
- 连段平均时长：20–35s
- 破防窗口频率：15–25s
- 击飞频率：2–4s

**AI 节流与攻击令牌**
- 近敌数触发决策节流（默认 12→40 达到 2x）
- 攻击令牌动态上限：`maxActiveAttackers + nearby/6`（上限 8）
- 冷却中不占用令牌

## 6. 经济循环与成长

**掉落与回路**
- 珍珠/消耗品/深渊币随关卡倍率与难度叠乘
- 关卡间隔与据点间喘息提供补给与策略调整

**成长要点**
- 等级/天赋：提高技能回转、连段容错、破防效率
- 珍珠：绑定元素与技能倾向，形成中期 build 差异

## 7. 任务与奖励权重

**奖励叠乘顺序**
- QuestType → RewardTier → Chapter → Stronghold → 难度倍率 → 关卡倍率

**入口配置**
- `EconomyConfig`：`questTypeMultipliers / questTierMultipliers / questChapterMultipliers / questStrongholdMultipliers`
- `QuestData.rewardTier`：主线/支线/挑战

## 8. UI 交互设计

**HUD 重点**
- 连段/资源/破防窗口/弱点图标必须高可读
- 任务目标仅显示关键节点，避免遮挡战斗
- 奖励反馈突出“爽感提升项”（技能/珍珠/消耗品）

## 9. 验证与调优流程

**快速验证**
1) 选 2 关（早期/中期）跑 KPM 与连段时长
2) 根据指标调 `IntensityWaveDirector` 与 `WaveEventTuning`
3) 扩展到全 10 关

**常见问题修复**
- 爽感不足：提高基础怪比例 / 提升击中反馈密度
- 连段易断：降低远程/控制比例 / 增加连段容错
- 事件压迫过强：降低事件密度 / 增加喘息
- Boss 疲劳：增加破防窗口与爆发阶段

## 10. 落地清单

- 强度与节奏规则启用（SpawnDirector + IntensityWaveDirector）
- 敌人 prefab 绑定 ArchetypeConfigurator
- 事件刷怪走 Event 调优入口
- 任务奖励倍率链完整配置
- UI 反馈信息完整可读
