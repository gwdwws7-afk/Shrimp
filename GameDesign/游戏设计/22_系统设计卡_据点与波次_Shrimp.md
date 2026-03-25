# 系统设计卡：据点与波次（Shrimp）

更新时间: 2026-03-24
版本: v0.1（文档优化轮）
Owner: Design/StrongholdWave
关联 DDR-ID: `DDR-2026-03-24-16`

## 1) 设计对象

- 该系统服务的玩家行为:
  - 在据点序列中处理事件波次，形成节奏峰谷并推进至 Boss Gate。
- 系统边界（与其他系统接口）:
  - 上游: 关卡结构、敌人配比、事件池。
  - 下游: 结算触发、任务推进、Boss 出场门控。

## 2) 玩家核心幻想

- 玩家在该系统中“想实现什么”:
  - 我在战场上通过一段段据点战推进前线，而不是无目标刷怪。

## 3) 30 秒到 3 分钟核心循环

- 30 秒循环:
  - 清理当前波次 -> 处理事件条件 -> 获取阶段反馈。
- 3 分钟循环:
  - Stronghold_01 建立节奏 -> Stronghold_02 拉升强度 -> 进入 Boss Gate。

## 4) 关键规则与约束

- 规则 1: 每关据点序列完整，事件类型与强度有递进。
- 规则 2: 波次配比遵循统一规则并可按关卡局部覆盖。
- 规则 3: 事件不能破坏主战斗节奏，应起“加压或转场”作用。
- 关键约束:
  - LevelData/LevelContent/CombatDensity 三层校验必须持续通过。
  - 同一关卡内事件重复感需受控。

## 5) 决策点与风险回报

- 主要决策点: 事件处理优先级与资源节奏管理。
- 失败代价: 据点流程卡顿、事件疲劳、节奏塌陷。
- 成功收益: 关卡推进稳定、压迫连续、Boss 前状态可控。

## 6) 反馈与可读性策略

- 视觉反馈: 据点状态、事件切换、波次进度、Boss Gate 明确显示。
- 音频反馈: 据点开启/完成、事件触发、Boss 出场分层提示。
- UI 提示: 波次信息简洁可读，不遮挡战斗核心信息。
- 失败纠偏: 显示失败事件类型与主要压力来源。

## 7) 学习曲线与失败学习点

- 入门: 前期关卡教学据点语法和事件类型识别。
- 首次失败: 学会“先处理目标事件，再清场”的优先级。
- 进阶: 复合事件中保持资源与站位控制。

## 8) 核心参数区间（最小/推荐/最大）

- 每关据点数: `2 / 2 / 3`
- 每据点波次数: `3 / 4-5 / 6`
- 每关事件数: `2 / 3 / 4`
- 波次总刷怪基数（示例）: `140 / 180+ / 230`

## 9) 验收标准（量化）

- 指标 1: Level Content Gate 10 关全部通过且无阻塞项。
- 指标 2: Combat Density Gate 无缺口，覆盖率达到目标区间。
- 指标 3: 10 关据点与波次链路完整，Boss Gate 正常触发。

## 10) 证据路径（测试/报表/录像）

- 回归测试:
  - `Assets/ThirdPersonController/Tests/PlayMode/LevelSceneGateTests.cs`
- 门禁报告:
  - `Assets/ThirdPersonController/Reports/level_data_scene_validator_report.csv`
  - `Assets/ThirdPersonController/Reports/level_content_completeness_report.csv`
  - `Assets/ThirdPersonController/Reports/level_content_completeness_summary.md`
  - `Assets/ThirdPersonController/Reports/level_combat_density_gap_report.csv`
  - `Assets/ThirdPersonController/Reports/level_combat_density_gap_summary.md`

## 方法论对照区（必填）

- 00 总纲: 据点->事件->Boss Gate 闭环明确。
- 01 MDA: 规则驱动行为有效，体验层稳定。
- 02 FADT: 玩家可理解推进结果，需继续优化事件多样性。
- 03 Lenses: 风险在中后期事件重复感。
- 24 群战: 结构已能承载爽压平衡。

## 评分区（100 分）

| 维度 | 分值 | 得分 |
|------|------|------|
| 体验目标清晰度 | 20 | 17 |
| 机制-行为因果完整度 | 20 | 16 |
| 反馈可读性与教学 | 15 | 12 |
| 参数与经济可调性 | 15 | 12 |
| 验收与证据完备度 | 20 | 16 |
| 扩展性与复用性 | 10 | 9 |
| 合计 | 100 | 82 |

结论:
- 当前方法论分数 82，达到可收口区间。

