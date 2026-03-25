# Boss Choreography Strict Gate Runbook

- Date: 2026-03-19
- Scope: Round8 strict warning drill + batch gate entry

## 1) Drill Only (fast local check)

```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_boss_choreography_strict_gate_drill.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -NoGraphics
```

Expected:
- Console ends with `[BossStrictDrill] passed.`
- Report:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_strict_gate_drill_round8_report_2026-03-19.md`
- Snapshot:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_strict_gate_drill_round8_failure_snapshot_2026-03-19.md`

## 2) Batch Smoke (strict drill + filtered PlayMode)

```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -ResultsXml 'C:\test\Shrimp\Logs\PlayModeBatchResults_boss_strict_drill_gate_smoke.xml' `
  -LogFile 'C:\test\Shrimp\Logs\PlayModeBatchRunner_boss_strict_drill_gate_smoke.log' `
  -WarmupLogFile 'C:\test\Shrimp\Logs\PlayModeBatchWarmup_boss_strict_drill_gate_smoke.log' `
  -TestFilter 'ThirdPersonController.Tests.BossQuestCouplingRegressionTests' `
  -SkipLevelDataSceneGate `
  -SkipBossFlowCouplingGate `
  -SkipBossEncounterRound3Gate `
  -SkipBossPhaseAttackGate `
  -SkipBossChoreographyGate `
  -SkipBossAttackCsvGate `
  -SkipLevelContentGate `
  -SkipLevelCombatDensityGate `
  -SkipInputRound3Gate `
  -SkipInputMirrorGate `
  -SkipCommentLogQualityGate `
  -SkipSkillResourceGapGate `
  -SkipLocalizationCoverageGate `
  -SkipSteamRuntimeModeGate `
  -SkipEnemyTypeSceneGate `
  -RunBossStrictDrillGate `
  -SkipWarmupCompile `
  -NoGraphics
```

Expected:
- `Boss Strict Drill Gate | Passed | exit=0`
- Gate matrix markdown:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_summary.md`

## 3) Full Batch Gate (CI path)

```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -ResultsXml 'C:\test\Shrimp\Logs\PlayModeBatchResults_full_after_boss_strict_drill_round8.xml' `
  -LogFile 'C:\test\Shrimp\Logs\PlayModeBatchRunner_full_after_boss_strict_drill_round8.log' `
  -WarmupLogFile 'C:\test\Shrimp\Logs\PlayModeBatchWarmup_full_after_boss_strict_drill_round8.log' `
  -RunBossStrictDrillGate `
  -NoGraphics
```

Expected:
- Full summary remains stable (`total=106 passed=105 failed=0 skipped=1`)
- Hard gate passed
- Gate matrix includes `Boss Strict Drill Gate`

## Failure Triage

When strict drill fails, check in this order:
1. `boss_choreography_strict_gate_drill_round8_report_2026-03-19.md`
2. `boss_choreography_strict_gate_drill_round8_failure_snapshot_2026-03-19.md`
3. Raw logs:
   - `C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_phase1.log`
   - `C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_phase2.log`
   - `C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_restore.log`
