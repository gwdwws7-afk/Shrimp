# Phase B Round20 - Cross-Chapter Failure Compensation Gate (2026-03-30)

## Scope
- Add Round21 regression for cross-chapter failure-to-recovery pacing (chapter 4 -> 5).
- Guard reward safety under consecutive failures, and validate compensation debt window in late-game transition.
- Integrate Round21 fixture into `P1 Quest Economy Mid-Late` subset gate.

## Code Changes
- Added PlayMode regression suite:
  - `ThirdPersonController/Tests/PlayMode/QuestFailureEconomyRound21RegressionTests.cs`
- New Round21 cases:
  - `QuestSystem_Round21_CrossChapterFailureStreak_CompensationPacing_OnlyRecoveryPays`
  - `EconomyService_Round21_CrossChapterCompensationPacing_DebtWindowRemainsRecoverable`
- Round21 scenario design:
  - Failure streak coverage on chapter 4 with native fail paths.
  - Compensation + stabilization sequence on chapter 5.
  - Cross-chapter recovery ratio and debt-coverage window assertions.
- Updated subset mapping:
  - `ThirdPersonController/Reports/run_playmode_batch_tests.ps1`
  - `P1 Quest Economy Mid-Late Gate` now includes Round21 fixture.

## Validation
- Focused Round21 run:
  - `run_playmode_batch_tests.ps1 -TestFilter "ThirdPersonController.Tests.QuestFailureEconomyRound21RegressionTests" -ValidateOnly -SkipWarmupCompile -SkipEnemyTypeSceneGate`
  - Result: `2/2 passed`
- P1 quest/economy aggregate run:
  - `run_playmode_batch_tests.ps1 -TestFilter "ThirdPersonController.Tests.QuestEconomyP1MidLate|ThirdPersonController.Tests.QuestFailureEconomyRound" -ValidateOnly -SkipWarmupCompile -SkipEnemyTypeSceneGate`
  - Result: `20/20 passed`
- P1 subset gate:
  - `ThirdPersonController/Reports/p1_quest_economy_midlate_gate_report.csv`
  - Result: `20 passed / 0 failed / 0 skipped`
- Gate matrix:
  - Hard gate: `passed`

## Notes
- This round used `-SkipEnemyTypeSceneGate` due intermittent external scene-gate process exit (`exit=-1073741510`) during filtered test runs.
- Round21 test assertions and subset gating are fully validated under the same filtered validation profile.
