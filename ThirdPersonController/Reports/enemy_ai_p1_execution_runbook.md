# Enemy AI P1 执行手册（行为深度）

更新时间：2026-03-11

## 目标

- 在 P0 稳定后，验证并微调高级行为：`Dodge / Block / Charge / Flee`
- 确认四类核心怪（Grunt/Rusher/Tank/Elite）具备可区分的行为特征
- 通过运行时采样结果决定第二轮参数方向

## 新增工具

- 采样脚本：`C:\test\Shrimp\Assets\ThirdPersonController\Scripts\Enemy\EnemyAIDebugSampler.cs`
- 关键输出：
  - 各 archetype 主导状态占比
  - 决策次数 / 命中次数
  - 令牌成功/失败统计
  - 当前令牌利用率

## 场景接入步骤

1. 在测试场景挂载 `EnemyAIDebugSampler`（空物体即可）
2. `autoFindCoordinator=true`
3. 建议参数：
   - `sampleInterval=0.25`
   - `reportInterval=5`
   - `resetAfterReport=true`
   - `logToConsole=true`
4. 运行后查看 Console 的 `[EnemyAI P1 Sampler]` 报告

## P1 验收指标（首轮）

| Archetype | 期望主导状态 | 参考占比 | 备注 |
|---|---|---:|---|
| Grunt | Chase/Circle/Attack | Attack 20%-35% | 不应频繁高级动作 |
| Rusher | Charge + Dodge + Attack | Charge 12%-25%, Dodge 10%-20% | 贴身压迫 |
| Tank | Block + Attack | Block 18%-35% | 防守反打 |
| Elite | Charge/Dodge/Block/Attack 混合 | 单一状态不应 >55% | 全能压制 |

## 快速判定规则

- 若 `TokenUtilization` 长期 < 0.3：
  - 先调低 `ringStandoffDistance`
  - 再调低 `attackCooldown`
- 若 `Rusher` Charge 占比过低：
  - 提高 `chargeChance`
  - 扩大 `chargeMaxDistance`
- 若 `Tank` Block 占比过高且战斗拖沓：
  - 降低 `blockDuration`
  - 提高 `blockCooldown`
- 若 `Elite` 行为单一：
  - 降低当前高占比行为的 `Chance`
  - 提高次级行为 `Chance`

## 本轮参数来源

- 当前已写回的首轮资产表：
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_round1_fill_sheet.csv`
- 说明文档：
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p0_checklist_and_round1_params.md`

