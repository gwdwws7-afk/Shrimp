# 系统设计卡：敌人类型（Shrimp）

更新时间: 2026-03-24
版本: v0.1（文档优化轮）
Owner: Design/EnemyType
关联 DDR-ID: `DDR-2026-03-24-09`

## 1) 设计对象

- 该系统服务的玩家行为:
  - 在群战中快速识别不同敌种威胁，做优先级击杀与针对性应对。
  - 随关卡推进体验到“敌种组合升级”，而非同质怪堆量。
- 系统边界（与其他系统接口）:
  - 上游: 关卡波次配置、事件强度、敌人 AI 行为参数。
  - 下游: 具体 prefab/Archetype 绑定、掉落与奖励节奏、战斗可读性。

## 2) 玩家核心幻想

- 玩家在该系统中“想实现什么”:
  - 我能看一眼敌群结构就知道先处理谁、怎么处理，并在处理优先级中获得掌控感。

## 3) 30 秒到 3 分钟核心循环

- 30 秒循环:
  - 识别“草料层/干扰层/锚点层” -> 快速清场或点杀高威胁 -> 调整站位。
- 3 分钟循环:
  - 波次引入新敌种 -> 组合复杂度上升 -> 玩家形成反制策略并迁移到下一波。

## 4) 关键规则与约束

- 规则 1:
  - 每个 archetype 必须有独立战术身份与可描述反制策略。
- 规则 2:
  - 波次配比应呈现“基础 -> 混合 -> 高压”的递进，不允许无意义随机堆叠。
- 规则 3:
  - 场景级 prefab 引用完整性必须通过门禁，避免运行时缺失或替代失真。
- 关键约束（性能/输入/资源/时序）:
  - 不增加复杂运行逻辑，本轮以文档与配置治理为主。
  - 与敌人 AI 的行为参数保持一致，避免“类型身份与行为相互打架”。

## 5) 决策点与风险回报

- 主要决策点:
  - 各敌种在波次中的比例与出场顺序。
  - 哪些敌种承担“教学角色”，哪些承担“压测角色”。
- 失败代价:
  - 身份重叠导致玩家无法读局、失去优先级判断，体验变成纯数值消耗。
- 成功收益:
  - 敌群结构可读，玩家每波都有明确应对策略与成长反馈。

## 6) 反馈与可读性策略

- 视觉反馈:
  - 各敌种在轮廓、动作节奏、攻击前摇上需可区分。
- 音频反馈:
  - 高威胁敌种（突进/控制/自爆）应有差异化预警音。
- UI 提示:
  - 关键敌种首次出现可给轻提示，避免强教学打断节奏。
- 失败后纠偏提示:
  - 失败复盘应指出“最致命敌种/事件组合”。

## 7) 学习曲线与失败学习点

- 入门教学节点:
  - 先教会玩家处理 Grunt/Rusher，再引入 Tank/Elite 与远程/控制/自爆。
- 第一次失败学习点:
  - 玩家理解“先处理干扰层再清草料层”的优先级原则。
- 中后期进阶点:
  - 在复合敌群中完成“控场、破防、收割”节奏切换。

## 8) 核心参数区间（最小/推荐/最大）

- 每波基础敌数量（Grunt）: `25 / 30-50 / 60`
- 突进敌占比（Rusher）: `10% / 20%-30% / 40%`
- 重装敌占比（Tank）: `5% / 8%-15% / 20%`
- 精英占比（Elite）: `3% / 5%-10% / 15%`
- 变体占比（Ranged/Controller/Suicide）: `0% / 10%-20% / 30%`

## 9) 验收标准（量化）

- 指标 1（身份明确性）:
  - 所有 archetype 均有“战术身份 + 反制说明 + 典型出场波次”三项定义。
- 指标 2（场景完整性）:
  - 场景级缺失 prefab 清单为 0（或全部可追溯自动补齐）。
- 指标 3（组合递进性）:
  - Wave 0/1/2/3+ 配比规则在 10 关中可验证且与关卡节奏一致。

## 10) 证据路径（测试/报表/录像）

- 回归测试:
  - `Assets/ThirdPersonController/Tests/PlayMode/EnemyTypeP0RegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/EnemyTypeP1RegressionTests.cs`
- 门禁报告:
  - `Assets/ThirdPersonController/Reports/enemy_type_scene_missing_prefab_checklist.csv`
  - `Assets/ThirdPersonController/Reports/run_enemy_type_scene_gate.ps1`
  - `Assets/ThirdPersonController/Reports/playmode_gate_matrix_summary.md`
- 配置资产:
  - `Assets/GameDesign/Data/EnemyArchetype_Grunt.asset`
  - `Assets/GameDesign/Data/EnemyArchetype_Rusher.asset`
  - `Assets/GameDesign/Data/EnemyArchetype_Tank.asset`
  - `Assets/GameDesign/Data/EnemyArchetype_Elite.asset`
  - `Assets/GameDesign/Data/EnemyArchetype_Ranged.asset`
  - `Assets/GameDesign/Data/EnemyArchetype_Controller.asset`
  - `Assets/GameDesign/Data/EnemyArchetype_Suicide.asset`

## 方法论对照区（必填）

- 00 总纲:
  - 敌种体系可支撑“可读压迫 + 可学习反制”核心体验。
- 01 MDA:
  - 机制到行为链路清晰，但中后期高阶差异还可继续拉开。
- 02 FADT:
  - 意图与后果可解释，需持续强化“第一次见到就能看懂”的感知设计。
- 03 Lenses:
  - 核心风险为“敌种身份重叠”；通过克制矩阵与波次规则控制。
- 24 群战（按需）:
  - 三层敌群模型已经落地，下一步聚焦组合深度与反制表达。

## 评分区（100 分）

| 维度 | 分值 | 得分 |
|------|------|------|
| 体验目标清晰度 | 20 | 17 |
| 机制-行为因果完整度 | 20 | 15 |
| 反馈可读性与教学 | 15 | 11 |
| 参数与经济可调性 | 15 | 12 |
| 验收与证据完备度 | 20 | 16 |
| 扩展性与复用性 | 10 | 9 |
| 合计 | 100 | 80 |

结论:
- 当前方法论分数 80，达到“可收口并进入 P1 深化”区间。
- 下一轮文档动作: 补齐“敌种克制矩阵 + 波次反制教学图”。

