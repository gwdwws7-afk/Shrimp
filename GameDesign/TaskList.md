# 深渊猎手：开发任务清单

更新时间: 2026-03-25
当前阶段: 设计文档与内容生产双线收口

---

## A. 当前基线（来自最新门禁）

- [x] 全量 PlayMode 门禁通过（167 total / 166 pass / 0 fail / 1 skip）
- [x] Gate Matrix 全通过（34/34 Passed，0 Failed）
- [x] P3 Boss 深度回归通过（25/25）
- [x] 输入统一层/本地化基础/Steam 运行模式门禁通过
- [x] 关卡三层校验可持续运行（LevelData/LevelContent/CombatDensity）
- [x] 模块代码完成度复评已留痕（`31_模块代码完成度复评_2026-03-24.md`）
- [x] Phase A 代码收口完成（Steam 双路径回归 + CombatFeedback 门禁 + Boss 身份模板校验 + Pseudo-Loc 门禁）
- [x] Phase A 验收报告已留痕（`Assets/ThirdPersonController/Reports/phaseA_completion_report_2026-03-24.md`）

---

## B. P0（必须完成，影响可交付）

### B1. 关卡与流程内容化（第一优先）
- [ ] Level_02 ~ Level_10 逐关补齐“主题机制 + 战斗节奏 + 事件差异”
- [ ] 10关统一 Beat Sheet（开场、教学、峰值、结算）
- [ ] 任务链与关卡目标逐关对齐（避免纯清怪）
- [ ] 场景级首轮手工评审（可读性、节奏、失败学习）

### B2. Boss 内容化闭环（第二优先）
- [ ] 10个 Boss 身份与招式语法文档化（每个 3-4 技能 + 2 阶段）
- [ ] 每个 Boss 明确“可读招信号 + 反制窗口 + 失败学习点”
- [ ] Boss 与关卡机制联动点（地形/事件/据点）
- [ ] Boss 战后奖励和成长反馈统一

### B3. 技能/UI/音频资源化（第三优先）
- [ ] 技能图标/SFX/VFX 资源覆盖主战斗链
- [ ] HUD 信息层级与提示语统一
- [ ] 关键战斗反馈（命中、受击、破防、终结）音画同步

---

## C. P1（强体验提升）

- [ ] 战斗手感二轮调优（连击收益、耐力压力、技能收益曲线）
- [ ] 群战节奏精调（近战/远程分层、围攻公平性）
- [ ] 手柄提示与 UI 深度适配（全流程）
- [ ] 多语言文案覆盖扩展（核心系统 + 主流程）

---

## D. P2（发布侧能力）

- [ ] Steamworks 正式实接（成就/云存档/Stats）
- [ ] Store 物料文案与成就文案最终版
- [ ] 真机长时压测与异常恢复演练
- [ ] 发布前全模块回归与冻结清单

---

## E. 风险清单

- [ ] Boss 内容生产速度不足导致后期堆叠风险
- [ ] 资源化滞后导致“代码完成但体感不达标”
- [ ] 多语言与手柄适配晚接入导致返工
- [ ] Steam 正式接入的配置/审核周期风险

---

## F. 每周固定输出（必须留痕）

- [ ] 模块完成度复评（设计+代码+资源）
- [ ] 门禁矩阵快照与异常说明
- [ ] 关卡/Boss 内容差距清单更新
- [ ] 下周优先级与阻塞项同步

---

## G. 文档方法论收口（本轮）

- [x] 升级 `05_项目级设计落地指南_Shrimp.md`（方法论对照轴 + 100 分打分）
- [x] 升级 `06_模块验收矩阵_Shrimp.md`（新增方法论完备度维度）
- [x] 升级 `07_版本迭代与门禁策略_Shrimp.md`（新增 DG-01~DG-06 文档门禁）
- [x] 新增 `08_系统设计卡模板_Shrimp.md`（统一系统设计卡）
- [x] 新增 `09_DDR_设计决策记录簿_Shrimp.md`（决策追溯与索引）
- [x] 新增 `10_系统设计卡_关卡与流程_Shrimp.md`（首批模块落地）
- [x] 新增 `11_系统设计卡_Boss系统_Shrimp.md`（首批模块落地）
- [x] 新增 `12_系统设计卡_技能系统_Shrimp.md`（第二批模块落地）
- [x] 新增 `13_系统设计卡_UI框架_Shrimp.md`（第二批模块落地）
- [x] 新增 `14_系统设计卡_敌人AI_Shrimp.md`（第三批模块落地）
- [x] 新增 `15_系统设计卡_敌人类型_Shrimp.md`（第三批模块落地）
- [x] 新增 `16_系统设计卡_任务系统_Shrimp.md`（第四批模块落地）
- [x] 新增 `17_系统设计卡_经济与成长_Shrimp.md`（第四批模块落地）
- [x] 新增 `18_系统设计卡_多语言_Shrimp.md`（第五批模块落地）
- [x] 新增 `19_系统设计卡_手柄支持_Shrimp.md`（第五批模块落地）
- [x] 新增 `20_系统设计卡_核心控制_Shrimp.md`（第六批模块落地）
- [x] 新增 `21_系统设计卡_战斗系统_Shrimp.md`（第六批模块落地）
- [x] 新增 `22_系统设计卡_据点与波次_Shrimp.md`（第七批模块落地）
- [x] 新增 `23_系统设计卡_Steam功能_Shrimp.md`（第七批模块落地）
- [x] 新增 `24_系统设计卡_本地存档_Shrimp.md`（第八批模块落地）
- [x] 新增 `25_系统设计卡_音效与特效_Shrimp.md`（第八批模块落地）
- [x] 新增 `26_系统设计卡_性能优化_Shrimp.md`（第九批模块落地）
- [x] 新增 `27_系统设计卡_美术资产_Shrimp.md`（第九批模块落地）
- [x] 新增 `28_模块方法论复审汇总_2026-03-24.md`（全模块复审结果）
- [x] 新增 `29_音效与特效整改包_P0_Shrimp.md`（低分模块整改包）
- [x] 新增 `30_美术资产功能优先级矩阵_P0_Shrimp.md`（低分模块整改包）

## H. 2026-03-25 Round10（Boss P3 Round6）进展

- [x] 完成 Boss P3 Round6 第一轮收口（行为深度 + 边界稳定性）。
- [x] 新增低帧率抖动回归：`BossController_InterruptRecoveryGate_LowFpsJitter_StillRespectsCounterWindow`。
- [x] 新增场景切换重绑定风暴回归：`Level08To10SceneSwitch_LowFpsJitter_RebindStorm_BossGateRemainsStable`。
- [x] 强化编排门禁：新增 post-break punish / interrupt recovery / pressure pacing 规则约束。
- [x] Boss 定向批次通过：`29/29`。
- [x] Boss 全子集通过：`63/63`。
- [x] 证据落档：`Assets/ThirdPersonController/Reports/phaseB_round10_boss_p3_round6_depth_gate_report_2026-03-25.md`。
- [ ] 下一步：执行一次“全量无过滤 gate matrix”并留最终快照。

## I. 2026-03-25 文档同步（Boss P3 Round6 正式）

- [x] 本轮完成 Boss P3 Round6 行为深度与边界稳定性收口。
- [x] 新增边界自动化：
  - `BossController_InterruptRecoveryGate_LowFpsJitter_StillRespectsCounterWindow`
  - `Level08To10SceneSwitch_LowFpsJitter_RebindStorm_BossGateRemainsStable`
- [x] 回归结果：Boss 定向 `29/29`，Boss 全子集 `63/63`，P3 Boss Depth 子门禁 `32/32`。
- [x] 正式证据：`Assets/ThirdPersonController/Reports/phaseB_round10_boss_p3_round6_depth_gate_report_2026-03-25.md`
- [ ] 后续动作：补跑一次“无过滤全量 Gate Matrix”作为全模块新基线快照。
