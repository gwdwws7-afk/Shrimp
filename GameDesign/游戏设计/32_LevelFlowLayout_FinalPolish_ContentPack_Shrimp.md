# 32 关卡流程与 Layout 终版内容包（Shrimp）
更新时间：2026-03-26  
版本：v1.0（Final Polish Blueprint）  
Owner：Design/Level + Design/Quest + Design/Economy

## 0. 目标与范围
本文件用于把当前项目的关卡策划从“可跑通”细化到“最终打磨版可执行蓝图”。覆盖：

1. 主线故事与逐关叙事节拍。  
2. 逐关流程（主线/支线/据点/波次/事件/Boss）。  
3. 逐关 Layout 目标（空间分区、战斗节点、回流与补给）。  
4. 敌群结构与事件语法（按关卡梯度递进）。  
5. 掉落、奖励、成长、经济闭环（首通与全清期望）。  
6. 与现有配置字段的落地映射（可直接回填 `LevelData/Quest/Economy`）。

设计目标时长：

1. 主线首通：6-9 小时。  
2. 主线+支线全清：10-16 小时。  
3. 周目后重复刷（成长/挑战）：16 小时以上。

---

## 1. 全局叙事主线（Level 01-10）
| 关卡 | 章节标题 | 叙事目标 | 张力阶段 | 终点状态 |
|---|---|---|---|---|
| L01 | Trench Rift | 建立“裂隙失控”与前线守备背景 | 教学开场 | 获得第一条主线情报 |
| L02 | Wrecked Station | 回收黑匣子，确认灾变来源并恢复供电 | 低压推进 | 解锁中段作战授权 |
| L03 | Thermal Vents | 稳定热井，首次进入 Boss 干扰阶段 | 首次显著抬压 | 玩家习得保护事件优先级 |
| L04 | Coral Grove | 清除感染珊瑚，验证生态武装扩散 | 持续压迫 | 建立“守点+清场”双任务意识 |
| L05 | Sunken City | 重启城市中继，打开深层交通链路 | 中段高压 | 作战半径扩大，敌群密度上升 |
| L06 | BlackTide Pipes | 关闭黑潮管线，切断敌方供能 | 高压稳定段 | 进入连续复合目标节奏 |
| L07 | Abyss Hangar | 锁死机库，阻断大规模投送 | 中后段峰值前夜 | 玩家必须管理资源与失误成本 |
| L08 | Molten Rift | 冷却裂隙并击败区域领主 | 第一终盘 | 完整“事件->Boss”闭环成型 |
| L09 | StillTide Sanctum | 破坏共振圣殿并击败镜潮术士 | 终盘前置 | 进入最终战役准备态 |
| L10 | Hive Core | 突入母巢核心，完成总攻与终结 | 最终峰值 | 主流程收束，开放后续挑战内容 |

---

## 2. 终版流程语法与 Layout 语法
### 2.1 每关标准流程骨架
每关采用同构骨架，但通过“事件组合 + 敌群配方 + 场景阻抗”制造差异：

1. `入口区（0.5-1.0 分钟）`：目标说明、首轮试探战。  
2. `据点 A（4-6 分钟）`：教学/转译关卡主题机制。  
3. `过渡区（1-2 分钟）`：短补给+路径重定向。  
4. `据点 B（5-7 分钟）`：强度主峰、复合事件。  
5. `Boss 前室（0.5-1.5 分钟）`：结算压力与资源检查。  
6. `Boss 战（3-6 分钟，L03+）`：阶段压迫与机制收束。  
7. `结算区（0.5-1.0 分钟）`：奖励投放、成长反馈、下一关动机。

### 2.2 Layout 终版打磨规则
每关都必须满足以下空间规则：

1. `三段视野`：至少存在近战遮蔽、中距转角、远距火力线。  
2. `双环路径`：主推进路径 + 一条风险收益支路（补给/掉落点）。  
3. `回流安全点`：每个据点都要有 1 处低压回整位。  
4. `事件锚点可读`：Hold/Protect 目标点必须可一眼识别。  
5. `Boss 前室净空`：避免前室被残余小怪持续打断。  
6. `失败重开快进`：重试到主要战斗点不超过 35 秒。

### 2.3 事件节拍语法（WaveEventType）
事件类型：`Reinforcement / Chase / HoldPoint / ProtectTarget`  
终版节奏原则：

1. 前半段优先 `Reinforcement + Chase` 建立杀伤与位移。  
2. 中段用 `HoldPoint` 强制站位决策。  
3. 峰值段用 `ProtectTarget` 拉高失误成本。  
4. 终盘（L08-L10）保持四类事件全覆盖，且 Boss 目标显式进入主线。

---

## 3. 敌群、掉落、成长全局曲线
### 3.1 敌群角色定义（按 Archetype）
| Archetype | 设计职责 | 体感关键词 |
|---|---|---|
| Grunt | 填充战线、维持连击节奏 | 稳定、可收割 |
| Rusher | 打断与逼位 | 快压、失误惩罚 |
| Ranged/Controller | 远程牵制与地形压迫 | 站位惩罚 |
| Tank | 吸收伤害、延长战斗 | 节奏钉子 |
| Elite | 小峰值制造者 | 威胁锚点 |
| Suicide（终盘可选） | 逼迫转火 | 高风险短决策 |

### 3.2 终版敌群递进（按关卡段）
| 关卡段 | 推荐占比（Grunt/Rusher/Ranged&Controller/Tank/Elite/Suicide） |
|---|---|
| L01-L02 | 45 / 25 / 20 / 8 / 2 / 0 |
| L03-L04 | 35 / 25 / 15 / 18 / 7 / 0 |
| L05-L07 | 28 / 24 / 14 / 20 / 12 / 2 |
| L08-L09 | 20 / 22 / 16 / 22 / 16 / 4 |
| L10 | 15 / 20 / 18 / 22 / 18 / 7 |

### 3.3 掉落与稀有度曲线（PearlRarity）
| 关卡段 | Common | Uncommon | Rare | Epic | Legendary |
|---|---:|---:|---:|---:|---:|
| L01-L02 | 70% | 25% | 5% | 0% | 0% |
| L03-L04 | 55% | 35% | 10% | 0% | 0% |
| L05-L07 | 40% | 40% | 18% | 2% | 0% |
| L08-L09 | 30% | 40% | 23% | 6% | 1% |
| L10 | 20% | 35% | 30% | 12% | 3% |

附加规则：

1. 每个据点清除必定掉落 1 个珍珠拾取（已符合当前奖励链路）。  
2. Elite 与 Boss 击杀增加稀有度权重，不改“有无掉落”判定逻辑。  
3. 支线路径放置 1 个“高风险补给点”（血量或战斗资源恢复点）。

### 3.4 成长/经济全局规则
沿用现有经济倍率结构，强化“中后段感知”：

1. 敌人经验与难度倍率：保持 `EconomyConfig_Sample` 曲线。  
2. 每波与据点奖励基线：`expOnWaveComplete=25`，`expOnStrongholdClear=80`，据点清除 `+1 天赋点`。  
3. 关卡奖励以 `LevelData.baseExp/basePearls/baseCredits` 为底，主线任务奖励必须高于支线 1 个梯度。  
4. 里程碑节奏保持三路线（Offense/Control/Survival），L05 前至少激活 1 条路线二阶奖励。

---

## 4. 逐关终版内容卡（L01-L10）
以下每关均包含：故事、Layout、事件、敌群、掉落、成长、学习点。

### L01 Trench Rift（推荐战力 120）
剧情目标：建立裂隙危机与作战语法。  
Layout：入口峡谷 -> 据点 A（裂隙边）-> 狭桥过渡 -> 据点 B（信标区）-> 撤离。  
主线任务：`l01_rift_beacons`（清 rim -> 同步信标 -> 清残敌）。  
支线任务：`l01_combo_30`。  
事件编排：A 区 Reinforcement/Chase/Hold，B 区 Reinforcement/Chase/Protect。  
敌群配方：Grunt 45、Rusher 25、Ranged 20、Tank 8、Elite 2。  
掉落：Common 主导；据点清除各 1 珍珠。  
成长收益：首通主线 `EXP 470 / 珍珠 3 / 货币 110`；全清 `EXP 530 / 珍珠 4 / 货币 130`。  
学习点：先清压制源，再执行事件目标。

### L02 Wrecked Station（推荐战力 140）
剧情目标：黑匣子回收与系统恢复。  
Layout：坍塌月台 -> 据点 A（供电节点）-> 机房通道 -> 据点 B（实验区）-> 回收点。  
主线任务：`l02_blackbox_recovery`。  
支线任务：`l02_salvage_sweep`。  
事件编排：A 区 Chase/Reinforcement/Hold/Protect；B 区 Reinforcement/Chase/Hold/Protect。  
敌群配方：Grunt 38、Rusher 25、Controller 12、Ranged 13、Tank 10、Elite 2。  
掉落：Common/Uncommon 过渡，Rare 首次稳定出现。  
成长收益：首通主线 `EXP 520 / 珍珠 3 / 货币 140`；全清 `EXP 600 / 珍珠 4 / 货币 170`。  
学习点：多目标并行时的优先级切换。

### L03 Thermal Vents（推荐战力 160）
剧情目标：首次进入 Boss 干扰环境。  
Layout：热井外圈 -> 据点 A（散热阀）-> 喷口走廊 -> 据点 B（冷却机房）-> Boss 前室。  
主线任务：`l03_vent_stabilization`（清场 -> Protect 事件）。  
支线任务：`l03_combo_50`。  
事件编排：强调 ProtectTarget；追击事件用于打断玩家站桩。  
敌群配方：Grunt 32、Rusher 24、Ranged 14、Tank 20、Elite 10。  
掉落：Uncommon 比例提升，Rare 作为精英奖励。  
成长收益：首通主线 `EXP 580 / 珍珠 4 / 货币 160`；全清 `EXP 670 / 珍珠 5 / 货币 190`。  
学习点：保护目标时的转火纪律。

### L04 Coral Grove（推荐战力 180）
剧情目标：生态感染扩散，建立“清理+守护”双线。  
Layout：珊瑚林入口 -> 据点 A（净化池）-> 弯折礁道 -> 据点 B（生物节点）-> Boss 前室。  
主线任务：`l04_coral_purge`。  
支线任务：`l04_combo_50`。  
事件编排：HoldPoint 与 ProtectTarget交替，减少单一站点疲劳。  
敌群配方：Grunt 30、Rusher 23、Ranged 15、Tank 19、Elite 13。  
掉落：Rare 稳定化，Elite 具备更高权重。  
成长收益：首通主线 `EXP 620 / 珍珠 4 / 货币 175`；全清 `EXP 710 / 珍珠 5 / 货币 205`。  
学习点：事件站位与机动路径规划。

### L05 Sunken City（推荐战力 200）
剧情目标：重启城市中继，扩大作战半径。  
Layout：下沉主街 -> 据点 A（中继塔）-> 高架破口 -> 据点 B（交通闸门）-> Boss 前室。  
主线任务：`l05_city_relay`（清街区 -> Hold -> 完成 4 波）。  
支线任务：`l05_combo_60`。  
事件编排：加入更高频 Reinforcement，峰值段穿插 Protect。  
敌群配方：Grunt 26、Rusher 23、Ranged 14、Tank 21、Elite 14、Suicide 2。  
掉落：Rare 提升，Epic 小概率开放。  
成长收益：首通主线 `EXP 680 / 珍珠 5 / 货币 210`；全清 `EXP 790 / 珍珠 6 / 货币 250`。  
学习点：资源保留与推进速度平衡。

### L06 BlackTide Pipes（推荐战力 220）
剧情目标：切断敌方供能链路。  
Layout：管线枢纽 -> 据点 A（阀门组）-> 压力通道 -> 据点 B（排放井）-> Boss 前室。  
主线任务：`l06_pipeline_shutdown`（清 junction -> Protect -> 完成 5 波）。  
支线任务：`l06_combo_60`。  
事件编排：Protect 失败惩罚更明确，Chase 用于逼位。  
敌群配方：Grunt 24、Rusher 22、Ranged 14、Tank 22、Elite 15、Suicide 3。  
掉落：Rare 主体化，Epic 稳定出现。  
成长收益：首通主线 `EXP 730 / 珍珠 5 / 货币 230`；全清 `EXP 850 / 珍珠 6 / 货币 270`。  
学习点：高压阶段错误恢复能力。

### L07 Abyss Hangar（推荐战力 240）
剧情目标：封锁投送中心，进入终盘前夜。  
Layout：机库前坪 -> 据点 A（升降平台）-> 维修廊桥 -> 据点 B（装配线）-> Boss 前室。  
主线任务：`l07_hangar_lockdown`（清场 -> Protect -> 完成 5 波）。  
支线任务：`l07_combo_70`。  
事件编排：复合事件重叠（Reinforcement + Hold），压迫连续。  
敌群配方：Grunt 22、Rusher 21、Ranged 15、Tank 22、Elite 16、Suicide 4。  
掉落：Epic 可感知；为终盘构筑做准备。  
成长收益：首通主线 `EXP 780 / 珍珠 5 / 货币 260`；全清 `EXP 910 / 珍珠 6 / 货币 310`。  
学习点：中长战斗中的节奏切换与复盘。

### L08 Molten Rift（推荐战力 260）
剧情目标：完成第一终盘并击败区域领主。  
Layout：熔岩外环 -> 据点 A（冷却核心）-> 断裂环道 -> 据点 B（引流口）-> Boss 竞技场。  
主线任务：`l08_rift_cooling`（推进 -> Hold -> BossDefeat）。  
支线任务：`l08_kill_120`。  
事件编排：四类事件全覆盖，Boss 前必须完成冷却事件。  
敌群配方：Grunt 20、Rusher 21、Ranged/Controller 16、Tank 22、Elite 17、Suicide 4。  
Boss：`Boss_MoltenNarwhal`，以范围压迫与走位判定为主。  
掉落：Rare/Epic 明显提升，Legendary 首次可见。  
成长收益：首通主线 `EXP 830 / 珍珠 5 / 货币 280`；全清 `EXP 970 / 珍珠 6 / 货币 330`。  
学习点：事件执行与 Boss 资源管理衔接。

### L09 StillTide Sanctum（推荐战力 280）
剧情目标：击碎共振场并切断镜潮控制。  
Layout：圣殿外庭 -> 据点 A（共振柱）-> 回字形回廊 -> 据点 B（镜室）-> Boss 场。  
主线任务：`l09_sanctum_disrupt`（清 guardian -> Hold -> BossDefeat）。  
支线任务：`l09_combo_70`。  
事件编排：Hold 期间强化远程牵制，Chase 负责破阵。  
敌群配方：Grunt 18、Rusher 20、Ranged/Controller 18、Tank 22、Elite 18、Suicide 4。  
Boss：`Boss_MirrorTidemancer`，强调节奏骗招与窗口反打。  
掉落：Epic 常规化，Legendary 低概率。  
成长收益：首通主线 `EXP 880 / 珍珠 5 / 货币 300`；全清 `EXP 1030 / 珍珠 6 / 货币 350`。  
学习点：高压阶段对远程点位的先手处理。

### L10 Hive Core（推荐战力 300）
剧情目标：终局总攻，破坏母巢核心。  
Layout：巢穴外壳 -> 据点 A（破拆器平台）-> 中枢裂道 -> 据点 B（核心门）-> 最终 Boss 场。  
主线任务：`l10_hive_core`（推进 -> Protect -> 5 波 -> BossDefeat）。  
支线任务：`l10_combo_80`。  
事件编排：终局复合事件（Protect + Reinforcement）叠加，末段接 Boss。  
敌群配方：Grunt 15、Rusher 20、Ranged/Controller 18、Tank 22、Elite 18、Suicide 7。  
Boss：`Boss_HiveCore`，三阶段，后段时间压力机制生效。  
掉落：Epic 与 Legendary 权重最高。  
成长收益：首通主线 `EXP 970 / 珍珠 7 / 货币 350`；全清 `EXP 1130 / 珍珠 9 / 货币 410`。  
学习点：全系统综合考试（连击、耐力、技能、事件、Boss）。

---

## 5. 关卡实施对照表（直接映射到配置）
| 设计项 | 资产字段 | 责任模块 |
|---|---|---|
| 关卡叙事标识 | `LevelData.levelName/description` | 关卡策划 |
| 主流程顺序 | `LevelData.nextLevelId` | 关卡策划 |
| 据点链路 | `LevelData.strongholds` + Scene `StrongholdSequence` | 关卡 + 场景 |
| 波次/事件 | `LevelData.strongholdOverrides[].waves[].events[]` | 关卡策划 |
| 主线/支线任务 | `LevelData.quests[]` + `QuestDatabase.quests[]` | 任务策划 |
| Boss 关卡门控 | `overrideBossSettings` + Boss 任务阶段 | Boss 策划 |
| 关卡基础收益 | `baseExp/basePearls/baseCredits` | 经济策划 |
| 任务收益 | `QuestData.reward` | 任务/经济 |
| 掉落倍率 | `dropChanceMultiplier` + Economy config | 经济策划 |
| 成长里程碑 | `ProgressionMilestones` | 成长策划 |

---

## 6. 终版验收标准（设计侧）
### 6.1 流程与可玩性
1. 10 关全部达到“入口-据点 A-过渡-据点 B-Boss/结算”完整链路。  
2. 每关至少 1 主线 + 1 支线，且主线奖励显著高于支线。  
3. L08-L10 主线必须含显式 Boss 击败目标（已满足）。

### 6.2 Layout 与事件
1. 每关事件类型覆盖不低于 3 种，终盘 4 种全覆盖。  
2. 每个据点都有可辨识事件锚点与回流安全点。  
3. 重试回战耗时控制在 35 秒以内。

### 6.3 经济与成长
1. 首通收益梯度单调上升，不出现中段倒挂。  
2. 稀有度权重随关卡递进，L10 具备 Legendary 目标感。  
3. 里程碑路线在 L05 前至少解锁二阶，L09 前至少形成一条三阶。

---

## 7. 下一步执行包（从文档到资产）
1. 按本文件逐关回填 `LevelData` 波次事件顺序与敌群占比。  
2. 按主线/支线表回填 `QuestDatabase` 的阶段目标与奖励。  
3. 用门禁报告验证：`LevelData / LevelContent / CombatDensity / LevelQuestBeatLinkage`。  
4. 对 L03-L07 的 Boss 关主线补“BossBreak/BossDefeat”显式阶段，清理设计警告。  
5. 出一轮“可玩版封板评审”视频与逐关体验打分表。

