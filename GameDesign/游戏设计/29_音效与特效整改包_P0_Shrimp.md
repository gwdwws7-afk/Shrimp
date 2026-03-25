# 音效与特效整改包（P0，Shrimp）

更新时间: 2026-03-24  
适用模块: 音效与特效（对应系统卡 `25`）  
目标: 方法论分数从 `60` 提升到 `>=80`

## 1. 现状与问题

- 当前状态:
  - 逻辑挂点已存在，资源覆盖不足。
  - `skill_resource_gap_report.csv` 显示技能资源主要依赖 fallback。
- 主要短板:
  - 关键事件音画映射不完整。
  - 优先级体系不明确，关键反馈可能被弱反馈淹没。
  - 验收证据未形成“事件级覆盖率”视图。

## 2. P0 目标（必须达成）

1. 建立“战斗关键事件 -> SFX/VFX”一一映射（覆盖核心主链）。  
2. 建立反馈优先级规则（Critical/High/Normal）。  
3. 输出可量化验收表，接入现有报告链路。

## 3. 关键事件最小覆盖表

| 事件ID | 场景 | 优先级 | SFX | VFX | 备注 |
|---|---|---|---|---|---|
| combat.hit.light | 普攻命中 | High | 必须 | 必须 | 基础打击感主入口 |
| combat.hit.heavy | 重击命中 | Critical | 必须 | 必须 | 需明显强反馈 |
| combat.player.hurt | 玩家受击 | Critical | 必须 | 必须 | 关联风险判断 |
| combat.enemy.break | 敌人破防 | Critical | 必须 | 必须 | 破防窗口核心反馈 |
| combat.finisher | 终结触发 | Critical | 必须 | 必须 | 高光反馈 |
| combat.dodge.success | 闪避成功 | High | 必须 | 可选 | 连段容错反馈 |
| combat.block.success | 格挡成功 | High | 必须 | 可选 | 防守回报反馈 |
| skill.cast.whirlwind | 技能释放 | High | 必须 | 必须 | 6 技能均需映射 |
| skill.cast.shockwave | 技能释放 | High | 必须 | 必须 | 6 技能均需映射 |
| skill.cast.dash | 技能释放 | High | 必须 | 必须 | 6 技能均需映射 |
| skill.cast.berserk | 技能释放 | Critical | 必须 | 必须 | 状态类强反馈 |
| skill.cast.pull | 技能释放 | High | 必须 | 必须 | 控场技能可读性 |
| skill.cast.ultimate | 技能释放 | Critical | 必须 | 必须 | 全屏技能强提示 |
| boss.phase.shift | Boss阶段切换 | Critical | 必须 | 必须 | 阶段学习锚点 |
| boss.break.window | Boss破防窗口 | Critical | 必须 | 必须 | 反制窗口提示 |
| stronghold.start | 据点开启 | Normal | 必须 | 可选 | 节奏提示 |
| stronghold.complete | 据点完成 | High | 必须 | 必须 | 阶段结算反馈 |

## 4. 反馈优先级规则（P0）

- `Critical`:
  - 不可被其它反馈覆盖或静默。
  - 允许短时抢占普通反馈通道。
- `High`:
  - 可与普通反馈共存，但不能被连续弱事件淹没。
- `Normal`:
  - 可被高优先反馈降权或延后。

## 5. 验收标准（量化）

- 指标 1:
  - “关键事件最小覆盖表”中 `SFX/VFX 必须项` 覆盖率 `>=95%`（目标 100%）。
- 指标 2:
  - `Critical` 事件在 30 秒高压战斗样本中无漏播/漏显。
- 指标 3:
  - 技能 6 主链事件全部脱离 fallback（可保留非关键 fallback）。

## 6. 交付物清单

- `AudioVfx_EventCoverage_P0.csv`（事件覆盖表，含状态列）  
- `AudioVfx_PriorityPolicy_P0.md`（优先级规则）  
- `AudioVfx_P0_Acceptance.md`（验收结论与证据路径）

## 7. 证据建议路径

- `Assets/ThirdPersonController/Reports/skill_resource_gap_report.csv`
- `Assets/ThirdPersonController/Reports/playmode_gate_matrix_summary.md`
- `Assets/GameDesign/GameDesignDocument.md`（D/F/G 附录）

## 8. 执行顺序（建议）

1. 先补 `Critical` 事件音画映射。  
2. 再补 `High` 事件映射与一致性。  
3. 最后清理 `Normal` 覆盖并做一次全链路回看。  

