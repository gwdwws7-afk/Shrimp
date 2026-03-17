# Enemy AI P4 Long-Run Runbook

## 目标
- 在真实设备上执行长时压测，验证 AI 协同与性能稳定性是否持续达标。
- 覆盖 `100/150` 同屏在“长时窗口 + 重复轮次”下的表现波动。

## 产物
- 长时压测输出：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_metrics.csv`
- 长时门禁配置：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_gate_config.csv`
- 长时门禁报告：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_gate_report.md`

## 覆盖说明
- 长时场景使用 `EnemyPerformanceStressHarness.ResetLongRunSteps()` 预设。
- 默认包含：`P4_LONG_100`、`P4_LONG_150`，并按 `repeatPerStep` 自动输出 `R1/R2...` 标签。

## 真机执行建议
1. 使用目标机型构建 Development Build（开启 Autoconnect Profiler）。
2. 在 `Level_01_TrenchRift` 场景挂载 Stress Harness，并调用 `ResetLongRunSteps`。
3. 完整跑完长时步骤后，导出 `enemy_ai_p4_longrun_metrics.csv` 到 Reports 目录。
4. 执行门禁评估脚本生成最终验收报告。

## 门禁评估命令
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\evaluate_enemy_ai_p4_longrun.ps1' `
  -MetricsCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_metrics.csv' `
  -OutputMd 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_gate_report.md'
```

## 长时验收建议
- 所有 `R*` 行均 PASS 才判定整轮通过。
- 若出现单轮 FAIL，优先查看同标签下的 `p95/p99` 与 `ai/s` 波动是否偏离基线。
