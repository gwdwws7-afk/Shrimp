# 系统设计卡：Boss系统（Shrimp）

更新时间: 2026-03-25
版本: v0.1（文档优化轮）
Owner: Design/Combat/Boss
关联 DDR-ID: `DDR-2026-03-24-05`

## 1) 设计对象

- 该系统服务的玩家行为:
  - 在每关末通过“读招 -> 反制 -> 破防 -> 爆发”完成阶段化学习与挑战收束。
  - 将前序关卡学习到的群战与资源管理能力迁移到 Boss 战。
- 系统边界（与其他系统接口）:
  - 上游: 关卡流程、Boss Gate、玩家战斗能力成长。
  - 下游: 战后奖励结算、任务判定、章节推进。

## 2) 玩家核心幻想

- 玩家在该系统中“想实现什么”:
  - 我不是靠堆伤害硬磨，而是看懂 Boss 语法并抓住破防窗口完成高光击杀。

## 3) 30 秒到 3 分钟核心循环

- 30 秒循环:
  - 识别招式前摇 -> 选择规避或打断 -> 争取破防进度。
- 3 分钟循环:
  - 阶段 1 学习招式语法 -> 阶段转换 -> 阶段 2 强化压力 -> 破防爆发收束。

## 4) 关键规则与约束

- 规则 1:
  - 每个 Boss 至少 3-4 个技能、2 个阶段，且阶段间必须引入新问题而非纯数值加压。
- 规则 2:
  - 每个 Boss 必须定义“可读招信号 + 反制窗口 + 失败学习点”三件套。
- 规则 3:
  - 破防窗口持续 3-5 秒，且触发方式可学习、可复现。
- 关键约束（性能/输入/资源/时序）:
  - 不破坏现有 Boss 深度回归门禁稳定性（P0/P1/P3 子集）。
  - 当前轮以文档与配置为主，不新增复杂核心逻辑。

## 5) 决策点与风险回报

- 主要决策点:
  - 躲避保命 vs. 强行打断；技能资源留给破防窗口还是平时控场。
- 失败代价:
  - 错读招式导致高额惩罚、节奏失控和资源耗尽。
- 成功收益:
  - 触发破防窗口获得高伤爆发期，显著缩短战斗时长并提升成就感。

## 6) 反馈与可读性策略

- 视觉反馈:
  - 前摇、危险范围、弱点暴露、破防窗口必须有显著视觉区分。
- 音频反馈:
  - 阶段切换、关键技能、破防触发要有高辨识度音频标记。
- UI 提示:
  - Boss 血条、阶段状态、破防进度可读且不遮挡主战斗信息。
- 失败后纠偏提示:
  - 失败结算提示“主要致死机制 + 建议反制动作”。

## 7) 学习曲线与失败学习点

- 入门教学节点:
  - L01-L03 Boss 强化“前摇识别和基础反制”。
- 第一次失败学习点:
  - 玩家应能在 1-2 次失败内识别至少 1 个关键破防触发条件。
- 中后期进阶点:
  - L07+ Boss 要求复合机制处理（位移、打断、资源窗口管理）。

## 8) 核心参数区间（最小/推荐/最大）

- Boss 战时长（分钟）: `3 / 4 / 5`
- 破防窗口时长（秒）: `3 / 4 / 5`
- 关键技能前摇（秒）: `0.35 / 0.5 / 0.8`
- 阶段转换血量阈值: `0.4 / 0.5 / 0.65`

## 9) 验收标准（量化）

- 指标 1（结构完整性）:
  - 10 个 Boss 均具备“身份卡 + 3-4 技能 + 2 阶段 + 破防条件”文档字段。
- 指标 2（可学习性）:
  - 首通失败后，玩家在下一次尝试中出现“针对性反制行为”的比例达到目标区间（需评审记录）。
- 指标 3（节奏稳定性）:
  - Boss 战时长稳定在 3-5 分钟，阶段切换与破防触发无明显异常断层。

## 10) 证据路径（测试/报表/录像）

- 回归测试:
  - `Assets/ThirdPersonController/Tests/PlayMode/BossCombatDepthRegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/BossLevel10GateRegressionTests.cs`
- 门禁报告:
  - `Assets/ThirdPersonController/Reports/p3_boss_depth_round4_event_storm_report_2026-03-23.md`
  - `Assets/ThirdPersonController/Reports/playmode_gate_matrix_summary.md`
- 录像/截图:
  - `Assets/ThirdPersonController/Reports/`（Boss 行为评审留痕）

## 方法论对照区（必填）

- 00 总纲:
  - Boss 已承担“高潮测验”角色，但身份差异与剧情语义仍需加强。
- 01 MDA:
  - 机制已能形成“读招与反制”动态，体验峰值存在但差异化不足。
- 02 FADT:
  - 多数关键动作可感知，需补强失败后“为什么输、如何改”的显性提示。
- 03 Lenses:
  - 核心风险为“阶段切换仅加压不加新问题”；需把阶段语法化并强制差异。
- 20 Boss（按需）:
  - 模板与门禁完备，下一步是 10 Boss 身份卡与招式语义化收口。

## 评分区（100 分）

| 维度 | 分值 | 得分 |
|------|------|------|
| 体验目标清晰度 | 20 | 15 |
| 机制-行为因果完整度 | 20 | 14 |
| 反馈可读性与教学 | 15 | 10 |
| 参数与经济可调性 | 15 | 10 |
| 验收与证据完备度 | 20 | 13 |
| 扩展性与复用性 | 10 | 8 |
| 合计 | 100 | 70 |

结论:
- 当前方法论分数 70，处于“可推进但必须限期补强”区间。
- 下一轮文档动作: 完成 10 Boss 身份卡与阶段课题表，目标提升到 >= 80。


## 2026-03-25 更新记录（Round10 / Boss P3 Round6）

本轮完成：

1. 行为深度与边界收口
- 低帧率时序抖动下，Interrupt Recovery 反制窗口保持稳定。
- 场景切换 + 强绑定重配风暴下，Boss Gate 仍保持单处理器、不重复结算。

2. 门禁规则增强
- 在编排覆盖门禁中增加以下约束：
  - post-break punish 参数范围约束
  - interrupt recovery 参数范围约束
  - time-pressure delay/ramp 的节奏约束
  - time-pressure 与 counter-window 的可读性约束

3. 配置微调（不改逻辑）
- Level_08~10：phase transition followup retry 的延迟/重试预算小幅调整。

4. 回归结果
- 定向（BossCombatDepth + BossLevel10Gate）：29/29 通过
- Boss 全子集：63/63 通过

证据路径：
- `Assets/ThirdPersonController/Reports/phaseB_round10_boss_p3_round6_depth_gate_report_2026-03-25.md`
- `Assets/ThirdPersonController/Reports/p3_boss_depth_gate_report.csv`
- `Logs/PlayMode_BossP3Round6_targeted.xml`
- `Logs/PlayMode_BossP3Round6_fullboss.xml`

## 2026-03-25 收口补记（正式）

- 设计卡对应代码验证已补齐“低帧率时序抖动”与“场景切换重绑定风暴”两类高风险边界。
- 本轮不新增核心机制逻辑，仅做边界稳定性与门禁约束强化（post-break / interrupt / pressure pacing）。
- 对应验收证据：
  - `Assets/ThirdPersonController/Reports/phaseB_round10_boss_p3_round6_depth_gate_report_2026-03-25.md`
  - `Assets/ThirdPersonController/Reports/p3_boss_depth_gate_report.csv`
