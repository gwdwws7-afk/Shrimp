# 版本迭代与门禁策略（Shrimp）

更新时间: 2026-03-24

## 1. 目标

把“设计优化”转成“可验证交付”，避免只停留在讨论层。

## 2. 迭代分层

### P0（必须完成）

- 直接影响主流程可玩性或交付风险
- 必须纳入全量回归与门禁矩阵
- 不通过即阻塞下一阶段

### P1（强体验提升）

- 明显改善体感与可读性
- 至少有专项回归和对照数据
- 可并行推进但不能破坏 P0 稳定性

### P2（发布增强）

- 发行侧或长尾质量项
- 在 P0/P1 稳定后推进

## 3. 门禁清单（当前项目）

运行时基础门禁:

- LevelData Scene Gate
- Level Content Gate
- Level Combat Density Gate
- Input Round3 Gate
- Localization Coverage Gate
- Steam Runtime Mode Gate
- Growth Economy Config Gate

专项运行门禁:

- P0 Boss Depth Subset
- P1 Boss Encounter Closure Subset
- P1 Skill Boundary Subset
- P2 Input/Localization/Steam Subset
- P3 Boss Depth Subset

文档方法论门禁（新增）:

- DG-01 `系统设计卡完整性`: 是否包含 05 文档定义的 10 个必填字段
- DG-02 `方法论对照完整性`: 是否完成 00/01/02/03 + 对应专项对照
- DG-03 `量化验收完整性`: 是否至少 3 条可量化验收项
- DG-04 `证据可追溯`: 是否有可访问的测试/报表/录像路径
- DG-05 `DDR 绑定完整性`: P0/P1 变更是否绑定 `DDR-ID`
- DG-06 `任务同步一致性`: 文档结论是否同步到 TaskList

## 4. 设计变更的最小验收包

每次设计变更必须附带：

- 变更描述（目的、范围、风险）
- 参数或规则对照（前后变化）
- 至少 1 条目标回归
- 1 份证据文件路径
- 1 个 `DDR-ID`
- 1 条 TaskList 对应任务/子任务

## 5. 周节奏建议

1. 周一: 确认本周 P0/P1 目标、方法论分数目标、证据标准
2. 周三: 中期门禁快照（运行门禁 + 文档门禁）与偏差修正
3. 周五: 全量回归 + 模块完成度复评 + DDR 更新
4. 周末: 文档更新、风险排队、下周目标冻结

## 6. 退出条件（阶段完成定义）

某阶段可判定“完成”需满足：

- 对应阶段的门禁行全部通过
- 关键模块无 blocker 风险
- 文档、任务、证据三者一致
- 文档方法论门禁 DG-01~DG-06 全通过
