# 系统设计卡：敌人AI（Shrimp）

更新时间: 2026-03-24
版本: v0.1（文档优化轮）
Owner: Design/EnemyAI
关联 DDR-ID: `DDR-2026-03-24-08`

## 1) 设计对象

- 该系统服务的玩家行为:
  - 在群战中持续感受到压迫，但始终存在可决策的生存与反打窗口。
  - 识别不同敌人行为并做优先级处理，而非被随机噪声淹没。
- 系统边界（与其他系统接口）:
  - 上游: 敌人类型参数、波次系统、关卡事件、性能预算。
  - 下游: 追击/攻击/冲锋/格挡/闪避/逃离状态机、令牌分配、LOD 更新。

## 2) 玩家核心幻想

- 玩家在该系统中“想实现什么”:
  - 敌人会协同围攻我，但不会不讲理地瞬间堆死；我能通过读局势做出正确反应并扭转战局。

## 3) 30 秒到 3 分钟核心循环

- 30 秒循环:
  - 敌群接近 -> 攻击令牌分配 -> 局部突进/压迫 -> 玩家反制 -> AI 重组站位。
- 3 分钟循环:
  - 普通波次压迫 -> 事件增压 -> 行为混合上升 -> 节奏留白 -> 新一轮高压。

## 4) 关键规则与约束

- 规则 1:
  - 攻击令牌必须体现“公平且连续”原则，避免长期空转或饥饿。
- 规则 2:
  - 不同 archetype 的主导状态占比必须可区分（非同质化）。
- 规则 3:
  - LOD 与节流只能降低计算成本，不可破坏关键行为语义。
- 关键约束（性能/输入/资源/时序）:
  - 满足 100-150 同屏压测门禁，且 GC 抖动受控。
  - 低帧率、打断、波次切换注册/注销场景下行为不能失真。

## 5) 决策点与风险回报

- 主要决策点:
  - 令牌上限与拒绝率平衡（压迫连续性 vs. 公平性）。
  - 行为占比调参（突进/格挡/闪避）避免单一化。
- 失败代价:
  - 过高拒绝率导致“全员空转”；过低约束导致围攻失衡。
- 成功收益:
  - 压迫稳定、读局势有效、战斗节奏更有张力且更可控。

## 6) 反馈与可读性策略

- 视觉反馈:
  - 冲锋、格挡、闪避、眩晕、抑制等状态切换需有清晰动作或提示。
- 音频反馈:
  - 高威胁行为（冲锋、重击）应有先行音频预警。
- UI 提示:
  - 关键危险（多向冲锋、控制链）可通过简化提示降低突发挫败。
- 失败后纠偏提示:
  - 回放/日志应能定位“被什么行为链击败”，支持针对性调整。

## 7) 学习曲线与失败学习点

- 入门教学节点:
  - 先识别基础追击与站位，再逐步引入冲锋/格挡/闪避混合行为。
- 第一次失败学习点:
  - 明白“优先处理中断源和高机动单位”比盲目清怪更有效。
- 中后期进阶点:
  - 在高密度波次里利用节奏留白窗口反打并重置局势。

## 8) 核心参数区间（最小/推荐/最大）

- `TokenUtilization`（令牌利用率）: `0.30 / 0.35-1.00 / 1.00`
- `token_reject_rate`（令牌拒绝率）: `0.10 / <=0.55 / 0.70`
- 决策间隔上限（秒）: `0.15 / 0.25 / 0.35`
- 动态活跃攻击者上限（人）: `4 / 6 / 8`
- LOD 距离分级（米）: `Full<15/Simple<40 / Full<20/Simple<50 / Full<25/Simple<60`

## 9) 验收标准（量化）

- 指标 1（公平压迫）:
  - P0 五个关键用例通过，且长时间无“全员空转”“令牌卡死”现象。
- 指标 2（行为分层）:
  - Grunt/Rusher/Tank/Elite 主导状态占比符合分层区间，单类不异常失真。
- 指标 3（性能稳定）:
  - P4 压测门禁通过，100/150 同屏关键指标满足阈值且 GC 稳定。

## 10) 证据路径（测试/报表/录像）

- 回归测试:
  - `Assets/ThirdPersonController/Tests/PlayMode/EnemyAIP0P1RegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/EnemyAIP3StressRegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/EnemyAIP4AcceptanceTests.cs`
- 门禁报告:
  - `Assets/ThirdPersonController/Reports/enemy_ai_p0_checklist_and_round1_params.md`
  - `Assets/ThirdPersonController/Reports/enemy_ai_p1_execution_runbook.md`
  - `Assets/ThirdPersonController/Reports/enemy_ai_round2_auto_tuning_table.md`
  - `Assets/ThirdPersonController/Reports/enemy_ai_p4_gate_report.md`
  - `Assets/ThirdPersonController/Reports/enemy_ai_p4_longrun_gate_report.md`
  - `Assets/ThirdPersonController/Reports/playmode_gate_matrix_summary.md`
- 数据报表:
  - `Assets/ThirdPersonController/Reports/enemy_ai_p3_stress_metrics.csv`
  - `Assets/ThirdPersonController/Reports/enemy_ai_p4_longrun_metrics.csv`

## 方法论对照区（必填）

- 00 总纲:
  - 已形成“压迫-反制-重组”循环，符合群战核心幻想。
- 01 MDA:
  - Mechanics（节流/令牌/LOD）有效塑造 Dynamics（围攻与留白），Aesthetics（紧张但公平）可达成。
- 02 FADT:
  - 玩家意图与后果链可解释，仍需持续降低极端场景的误判成本。
- 03 Lenses:
  - 关键风险为“高压下行为同质化或空转化”，已通过占比与令牌指标约束。
- 24 群战（按需）:
  - 三层压迫模型可运行，下一步侧重极端边界与长期稳定性验证。

## 评分区（100 分）

| 维度 | 分值 | 得分 |
|------|------|------|
| 体验目标清晰度 | 20 | 17 |
| 机制-行为因果完整度 | 20 | 16 |
| 反馈可读性与教学 | 15 | 11 |
| 参数与经济可调性 | 15 | 13 |
| 验收与证据完备度 | 20 | 16 |
| 扩展性与复用性 | 10 | 9 |
| 合计 | 100 | 82 |

结论:
- 当前方法论分数 82，达到“可收口并进入 P1 深化”区间。
- 下一轮文档动作: 补“极端中断/低帧抖动/大规模动态注销”专项复评模板。

