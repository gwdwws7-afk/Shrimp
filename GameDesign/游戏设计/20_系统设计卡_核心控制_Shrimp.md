# 系统设计卡：核心控制（Shrimp）

更新时间: 2026-03-24
版本: v0.1（文档优化轮）
Owner: Design/InputControl
关联 DDR-ID: `DDR-2026-03-24-14`

## 1) 设计对象

- 该系统服务的玩家行为:
  - 让玩家稳定完成移动、攻击、技能、交互、菜单切换等核心操作。
- 系统边界（与其他系统接口）:
  - 上游: 输入设备事件（键鼠/手柄）。
  - 下游: 战斗系统、UI 提示、菜单系统、调试/工具动作。

## 2) 玩家核心幻想

- 玩家在该系统中“想实现什么”:
  - 我的输入会被准确执行，且提示始终和当前绑定一致。

## 3) 30 秒到 3 分钟核心循环

- 30 秒循环:
  - 输入动作 -> 角色执行 -> 获得即时反馈。
- 3 分钟循环:
  - 在战斗与菜单多场景切换中保持输入一致性和可预期性。

## 4) 关键规则与约束

- 规则 1: 核心动作必须统一在 Input Actions 层维护。
- 规则 2: 所有提示文案必须通过 action label 输出，不直写硬编码按键。
- 规则 3: 场景对象的快捷键配置需通过自动审计门禁。
- 关键约束:
  - 不回退 `Input.GetKeyDown` 主路径。
  - 支持键鼠与手柄并存。

## 5) 决策点与风险回报

- 主要决策点: 动作优先级与冲突白名单策略。
- 失败代价: 误触、提示错配、关键动作失效。
- 成功收益: 可玩性稳定、学习成本低、跨设备一致。

## 6) 反馈与可读性策略

- 视觉反馈: 提示层和实际绑定保持同源。
- 音频反馈: 菜单确认/取消有统一响应。
- UI 提示: 主菜单、HUD、技能栏使用一致标签。
- 失败纠偏: 显示冲突来源或缺失绑定。

## 7) 学习曲线与失败学习点

- 入门: 基础移动/攻击/技能/交互。
- 首次失败: 通过统一提示快速纠偏。
- 进阶: 高频战斗中稳定执行复合输入。

## 8) 核心参数区间（最小/推荐/最大）

- 核心动作绑定覆盖率（%）: `95 / 100 / 100`
- 提示一致性审计通过率（%）: `95 / 100 / 100`
- 场景绑定 mismatch（条）: `0 / 0 / 0`

## 9) 验收标准（量化）

- 指标 1: Input Round3 Scene/Full Gate 全通过。
- 指标 2: Input Hint 一致性专项回归通过。
- 指标 3: 输入产品化专项回归通过。

## 10) 证据路径（测试/报表/录像）

- 回归测试:
  - `Assets/ThirdPersonController/Tests/PlayMode/InputHintConsistencyP0RegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/InputProductizationRegressionTests.cs`
- 门禁报告:
  - `Assets/ThirdPersonController/Reports/input_binding_round3_full_gate_audit.csv`
  - `Assets/ThirdPersonController/Reports/input_binding_round3_scene_audit.csv`
  - `Assets/ThirdPersonController/Reports/p0_input_hint_consistency_gate_report.csv`
  - `Assets/ThirdPersonController/Reports/p2_input_productization_gate_report.csv`

## 方法论对照区（必填）

- 00 总纲: 核心控制链路稳定。
- 01 MDA: 输入机制与行为响应一致性良好。
- 02 FADT: 意图到后果链条可感知。
- 03 Lenses: 风险集中在提示细节与设备切换。

## 评分区（100 分）

| 维度 | 分值 | 得分 |
|------|------|------|
| 体验目标清晰度 | 20 | 18 |
| 机制-行为因果完整度 | 20 | 17 |
| 反馈可读性与教学 | 15 | 12 |
| 参数与经济可调性 | 15 | 11 |
| 验收与证据完备度 | 20 | 16 |
| 扩展性与复用性 | 10 | 9 |
| 合计 | 100 | 83 |

结论:
- 当前方法论分数 83，达到可收口区间。

