# 系统设计卡：Steam功能（Shrimp）

更新时间: 2026-03-24
版本: v0.1（文档优化轮）
Owner: Design/Release
关联 DDR-ID: `DDR-2026-03-24-17`

## 1) 设计对象

- 该系统服务的玩家行为:
  - 通过成就、云存档、统计等发布能力获得跨会话连续体验。
- 系统边界（与其他系统接口）:
  - 上游: 存档系统、成就触发、统计上报。
  - 下游: Steam 运行模式、配置加载、云冲突解决。

## 2) 玩家核心幻想

- 玩家在该系统中“想实现什么”:
  - 我的进度和成就在平台上可持续、可同步、可验证。

## 3) 30 秒到 3 分钟核心循环

- 30 秒循环:
  - 触发行为 -> 更新成就/统计状态 -> 反馈可见。
- 3 分钟循环:
  - 存档写入 -> 云同步策略执行 -> 下次启动可恢复。

## 4) 关键规则与约束

- 规则 1: 运行时需区分 stub 合规模式与 real-backend-ready 模式。
- 规则 2: 默认配置资产必须存在且字段合法。
- 规则 3: 云冲突策略（Newest/CloudPreferred/LocalPreferred）可回归验证。
- 关键约束:
  - 当前保持 stub 稳定路径，待 `STEAMWORKS_NET` 正式启用再切实接。
  - `steam_appid.txt` 与配置 appId 一致。

## 5) 决策点与风险回报

- 主要决策点: 何时从 stub 切换 real backend；云冲突优先级策略。
- 失败代价: 数据不一致、同步错误、发布审核阻塞。
- 成功收益: 平台能力闭环，发布准备度提升。

## 6) 反馈与可读性策略

- 视觉反馈: 成就解锁和云同步状态应有明确 UI 表达。
- 音频反馈: 成就解锁可有轻量提示。
- UI 提示: 设置中提供运行模式和同步状态可读信息。
- 失败纠偏: 同步冲突提供可解释分支处理结果。

## 7) 学习曲线与失败学习点

- 入门: 平台功能默认无侵入，不影响主流程。
- 首次失败: 云冲突或配置异常时可读诊断。
- 进阶: 发布前完成 real-backend-ready 清单核对。

## 8) 核心参数区间（最小/推荐/最大）

- Steam runtime gate 通过率（%）: `95 / 100 / 100`
- Config provision gate 通过率（%）: `95 / 100 / 100`
- Steam 子集回归通过率（%）: `95 / 100 / 100`
- 云冲突分支覆盖（条）: `2 / 3 / 3`

## 9) 验收标准（量化）

- 指标 1: Steam Runtime Mode Gate 通过且无 gap。
- 指标 2: Steam Config Provision Gate 通过。
- 指标 3: Steam 专项回归（配置/引导/状态/云桥）全部通过。

## 10) 证据路径（测试/报表/录像）

- 回归测试:
  - `Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationConfigRegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationBootstrapRegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationStatusRegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/SteamCloudSaveBridgeRegressionTests.cs`
- 门禁与报告:
  - `Assets/ThirdPersonController/Reports/steam_runtime_mode_report.csv`
  - `Assets/ThirdPersonController/Reports/steam_config_provision_report.csv`
  - `Assets/ThirdPersonController/Reports/p2_steam_runtime_regression_gate_report.csv`
  - `Assets/ThirdPersonController/Reports/phase4_steam_productization_report_2026-03-20.md`
  - `Assets/ThirdPersonController/Reports/phase6_steam_backend_readiness_report_2026-03-20.md`

## 方法论对照区（必填）

- 00 总纲: 发布侧闭环定义明确。
- 01 MDA: 平台机制与行为链路可验证。
- 02 FADT: 配置与运行后果可解释。
- 03 Lenses: 风险在 real backend 切换与审核链路。

## 评分区（100 分）

| 维度 | 分值 | 得分 |
|------|------|------|
| 体验目标清晰度 | 20 | 14 |
| 机制-行为因果完整度 | 20 | 14 |
| 反馈可读性与教学 | 15 | 10 |
| 参数与经济可调性 | 15 | 11 |
| 验收与证据完备度 | 20 | 16 |
| 扩展性与复用性 | 10 | 9 |
| 合计 | 100 | 74 |

结论:
- 当前方法论分数 74，处于可推进但需补“real backend 切换清单”的区间。

