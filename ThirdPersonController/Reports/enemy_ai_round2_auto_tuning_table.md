# Enemy AI Round2 自动化调参建议表

更新时间：2026-03-11

## 文件清单

- 采样输入模板：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p1_sampling_metrics_template.csv`
- 自动建议脚本：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\generate_enemy_ai_round2_suggestions.ps1`
- 本轮建议输出：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_round2_auto_suggestions.csv`

## 使用方式

```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\generate_enemy_ai_round2_suggestions.ps1' \
  -MetricsCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p1_sampling_metrics_template.csv' \
  -OutputCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_round2_auto_suggestions.csv'
```

## 指标字段说明

- `attack_ratio / charge_ratio / dodge_ratio / block_ratio / flee_ratio`：行为占比（0~1）
- `dominant_state / dominant_ratio`：主导行为及占比
- `token_reject_rate / token_utilization`：令牌拒绝率与利用率
- `avg_decision_interval / avg_hits_per_enemy`：决策频率与压迫强度参考

## Round2 规则摘要

| Rule | 触发 | 自动建议 |
|---|---|---|
| G1/G2 | `token_utilization < 0.30` | `ringStandoffDistance -0.2`，`attackCooldown -0.08` |
| G3 | `token_reject_rate > 0.55` | `attackCooldown +0.08` |
| GR1/GR2 | Grunt `attack_ratio < 0.20` | `attackCooldown -0.08`，`chaseSpeed +0.15` |
| RU1/RU2 | Rusher `charge_ratio < 0.12` | `chargeChance +0.05`，`chargeMaxDistance +0.25` |
| RU3/RU4 | Rusher `charge_ratio > 0.25` | `chargeChance -0.04`，`chargeCooldown +0.2` |
| RU5/RU6/RU7 | Rusher `dodge_ratio` 越界 | `dodgeChance` 与 `dodgeCooldown` 反向修正 |
| TA1/TA2 | Tank `block_ratio < 0.18` | `blockChance +0.05`，`blockDuration +0.08` |
| TA3/TA4 | Tank `block_ratio > 0.35` | `blockChance -0.05`，`blockCooldown +0.18` |
| EL1~EL8 | Elite `dominant_ratio > 0.55` | 按 dominant_state 压主导行为，补次级行为 |

## 本轮输出概览

- 自动生成建议条目数：`11`（已自动合并同字段重复建议）
- 覆盖 archetype：`grunt / rusher / tank / elite`
- 每条建议包含：
  - `asset_path`
  - `field`
  - `current_value`
  - `round2_value`
  - `delta`
  - `priority`
  - `rule_id`
  - `reason`

