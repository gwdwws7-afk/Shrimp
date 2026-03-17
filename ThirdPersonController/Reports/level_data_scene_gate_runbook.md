# LevelData Scene Gate Runbook

## 目标
- 在 PlayMode 回归前，自动修复并校验 `LevelData` 与场景链路一致性。
- 将 `sceneName/BuildSettings/LevelFlow/Stronghold/BossSpawnPoint` 的常见偏差前置拦截。

## 默认入口
- 脚本：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1`
- 默认执行顺序：
  1. `LevelDataSceneValidator.FixForBatch`
  2. `LevelDataSceneValidator.ValidateForBatch`
  3. Input Round3 gate
  4. EnemyType scene gate
  5. PlayMode tests

## 直接执行（推荐）
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -NoGraphics
```

## 只跑 LevelData Scene gate（调试）
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -SkipInputRound3Gate `
  -SkipEnemyTypeSceneGate `
  -TestFilter 'ThirdPersonController.Tests.EnemyAIP4AcceptanceTests.P4_RealScene_StressHarness_ExportsMetricsCsv' `
  -NoGraphics
```

## 主要产物
- 报表 CSV：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_data_scene_validator_report.csv`
- 日志：
  - `C:\test\Shrimp\Logs\LevelDataSceneFix.log`
  - `C:\test\Shrimp\Logs\LevelDataSceneValidate.log`

## 可选开关
- 跳过 LevelData Scene gate：
```powershell
-SkipLevelDataSceneGate
```

## 失败判定
- 当 `level_data_scene_validator_report.csv` 中出现 `Error`（或未知状态）时，门禁失败。
- 批处理 exit code 非 0 视为失败；`124` 表示超时。
