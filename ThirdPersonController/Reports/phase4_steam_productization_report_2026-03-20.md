# Phase4 Steam Productization Report

- Date: 2026-03-20
- Scope: Complete phase4 code work for Steam productization and acceptance gates

## Delivered

1. Unified runtime config model
- Added `SteamIntegrationConfig`:
  - Service: `enableSteam`, `logWhenUnavailable`, `appId`
  - Achievement/Stats: `enableAchievements`, `enableStats`, `statsFlushInterval`
  - Cloud: `enableCloudSaves`, `pullCloudOnStart`, `uploadCloudOnSave`, `uploadSettings`,
    `applySettingsAfterPull`, `cloudOnlyIfLocalMissing`, `cloudUploadCooldown`, `cloudPriority`
- File:
  - `Assets/ThirdPersonController/Scripts/Steam/SteamIntegrationConfig.cs`

2. Runtime config application pipeline
- Added `ApplyConfig(...)` to:
  - `SteamIntegrationService`
  - `SteamAchievementTracker`
  - `SteamStatsTracker`
  - `SteamCloudSaveBridge`
- Updated `SteamIntegrationBootstrap`:
  - resolve config from `Resources/Steam/DefaultSteamIntegrationConfig`
  - apply config to service and tracker components on startup
  - keep override hook for regression tests (`ConfigResolverOverride`)
- Files:
  - `Assets/ThirdPersonController/Scripts/Steam/SteamIntegrationService.cs`
  - `Assets/ThirdPersonController/Scripts/Steam/SteamAchievementTracker.cs`
  - `Assets/ThirdPersonController/Scripts/Steam/SteamStatsTracker.cs`
  - `Assets/ThirdPersonController/Scripts/Steam/SteamCloudSaveBridge.cs`
  - `Assets/ThirdPersonController/Scripts/Steam/SteamIntegrationBootstrap.cs`

3. Config provision tooling
- Added `SteamIntegrationConfigProvisionTool`:
  - ensures default config asset exists
  - normalizes invalid defaults (`appId`, cooldown/interval bounds)
  - emits CSV report for CI
- Files:
  - `Assets/Editor/SteamIntegrationConfigProvisionTool.cs`
  - `Assets/ThirdPersonController/Resources/Steam/DefaultSteamIntegrationConfig.asset`
  - `Assets/ThirdPersonController/Reports/steam_config_provision_report.csv`

4. Gate strengthening
- Enhanced `SteamRuntimeModeGateValidator` with:
  - default config existence and `appId` checks
  - bootstrap wiring checks
  - config-regression and bootstrap-regression test coverage checks
- File:
  - `Assets/Editor/SteamRuntimeModeGateValidator.cs`

5. Regression coverage expansion
- Added tests:
  - `SteamIntegrationConfigRegressionTests` (3)
  - `SteamIntegrationBootstrapRegressionTests` (1)
- Extended tests:
  - `SteamIntegrationStatusRegressionTests` (+1 apply-config test)
- Files:
  - `Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationConfigRegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationBootstrapRegressionTests.cs`
  - `Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationStatusRegressionTests.cs`

6. Batch runner integration
- Integrated Steam config provision step into `run_playmode_batch_tests.ps1`:
  - new args/log/report/timeout handling
  - new gate matrix row: `Steam Config Provision Gate`
- Also fixed single-row CSV strict-mode handling for both:
  - `Get-CsvStatusSummary(...)`
  - `Get-CommentLogQualitySummary(...)`
- File:
  - `Assets/ThirdPersonController/Reports/run_playmode_batch_tests.ps1`

## Verification

1. Steam-only regression run
- PlayMode: `total=12 passed=12 failed=0 skipped=0`
- Result:
  - `C:\test\Shrimp\Logs\PlayModeBatchResults_phase4_steam_round2.xml`

2. Full gate + playmode acceptance
- PlayMode: `total=111 passed=110 failed=0 skipped=1`
- Gate matrix:
  - rows: `23`
  - passed: `22`
  - failed: `0`
  - skipped: `1`
  - hard gate: passed
- Results:
  - `C:\test\Shrimp\Logs\PlayModeBatchResults_phase4_full_round3.xml`
  - `C:\test\Shrimp\Logs\PlayModeBatchRunner_phase4_full_round3.log`
  - `Assets/ThirdPersonController/Reports/playmode_gate_matrix_summary.md`

3. Steam gate details
- `Steam Runtime Mode Gate`: `total=10 ok=10 gap=0`
- `Steam Config Provision Gate`: `total=1 ok=1 gap=0`
- Reports:
  - `Assets/ThirdPersonController/Reports/steam_runtime_mode_report.csv`
  - `Assets/ThirdPersonController/Reports/steam_config_provision_report.csv`

## Current completion statement

Phase4 is complete at code and CI-gate level.
Runtime remains on stub compile branch until `STEAMWORKS_NET` is enabled in build defines and real SDK package is linked.
