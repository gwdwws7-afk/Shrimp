# Enemy AI P4 Gate Runbook

## 目标
- 将 P3 压测结果转为可复用的门禁判定（PASS/FAIL）。
- 固化 `100/150` 同屏关键指标阈值，避免后续迭代性能回退。

## 文件
- 门禁阈值配置：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_config.csv`
- 门禁评估脚本：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\evaluate_enemy_ai_p4_gate.ps1`
- 默认压测结果：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p3_stress_metrics.csv`
- 评估输出报告：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_report.md`

## 标准流程
1. 先按 P3 Runbook 跑出新一轮压测 CSV。
2. 执行 P4 门禁脚本，生成 Markdown 报告。
3. 若有 FAIL，先按报告定位超阈值项，再进入下一轮调参。

## 执行命令
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\evaluate_enemy_ai_p4_gate.ps1' `
  -MetricsCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p3_stress_metrics.csv' `
  -GateCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_config.csv' `
  -OutputMd 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_report.md'
```

## 可选：基线差异
可传入历史基线 CSV 做差异对比（`d_avg_fps / d_p95_ms / d_p99_ms`）。
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\evaluate_enemy_ai_p4_gate.ps1' `
  -MetricsCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p3_stress_metrics.csv' `
  -GateCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_config.csv' `
  -OutputMd 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_report.md' `
  -BaselineCsv 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p3_stress_metrics_baseline.csv'
```

## 退出码
- `0`：全部步骤达标（PASS）
- `2`：存在至少一步不达标（FAIL）

## 责任建议
- Design/Combat：维护阈值配置（按目标机型分档）
- Engineering：确保脚本纳入回归流程
- QA：按报告回填失败项与复测结论

## Batch TestRunner 稳定执行（修复早退）
若命令行 `-runTests` 偶发“编译后直接退出且无 XML”，改用以下稳定入口：
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -ResultsXml 'C:\test\Shrimp\Logs\PlayModeBatchResults.xml' `
  -LogFile 'C:\test\Shrimp\Logs\PlayModeBatchRunner.log' `
  -NoGraphics `
  -RetryCount 1
```
可选：只跑某个用例
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -TestFilter 'ThirdPersonController.Tests.EnemyAIP4AcceptanceTests.P4_RealScene_StressHarness_ExportsMetricsCsv' `
  -ResultsXml 'C:\test\Shrimp\Logs\P4AcceptanceBatchResults.xml' `
  -LogFile 'C:\test\Shrimp\Logs\P4AcceptanceBatchRunner.log' `
  -NoGraphics `
  -RetryCount 1
```
