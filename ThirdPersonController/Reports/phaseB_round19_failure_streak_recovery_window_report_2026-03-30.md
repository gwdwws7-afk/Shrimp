# Phase B Round19 - Failure Streak Recovery Window Gate (2026-03-30)

## Scope
- Add Round20 regression for mid-late chapters under consecutive failure pressure.
- Validate that failed quests never leak rewards and recovery quests remain playable after streak failures.
- Integrate Round20 into `P1 Quest Economy Mid-Late` subset gate.

## Code Changes
- Added PlayMode regression suite:
  - `ThirdPersonController/Tests/PlayMode/QuestFailureEconomyRound20RegressionTests.cs`
- New Round20 cases:
  - `QuestSystem_Round20_ConsecutiveFailures_ByRuleAndChapter_OnlyRecoveryPays_AndWindowStaysPlayable`
  - `EconomyService_Round20_ConsecutiveFailureDebtCurve_RemainsRecoverableWithinRunsBudget`
- Scenario model:
  - Chapter range focused on chapter 4-5.
  - Failure streak per scenario set to 2 or 3.
  - Recovery pressure evaluated with streak-adjusted windows and required-recovery-runs budget.
- Updated subset mapping:
  - `ThirdPersonController/Reports/run_playmode_batch_tests.ps1`
  - `P1 Quest Economy Mid-Late Gate` includes Round16 ~ Round20 fixtures.

## Validation
- Focused Round20 run:
  - `run_playmode_batch_tests.ps1 -TestFilter "ThirdPersonController.Tests.QuestFailureEconomyRound20RegressionTests" -ValidateOnly -SkipWarmupCompile`
  - Result: `2/2 passed`
- P1 quest/economy aggregate run:
  - `run_playmode_batch_tests.ps1 -TestFilter "ThirdPersonController.Tests.QuestEconomyP1MidLate|ThirdPersonController.Tests.QuestFailureEconomyRound" -ValidateOnly -SkipWarmupCompile`
  - Result: `18/18 passed`
- P1 subset gate:
  - `ThirdPersonController/Reports/p1_quest_economy_midlate_gate_report.csv`
  - Result: `18 passed / 0 failed / 0 skipped`
- Gate matrix key rows:
  - `Quest Failure Learning Gate`: `Passed (40/40, blocking=0)`
  - `P1 Quest Economy Mid-Late Subset`: `Passed (18/18, blocking=0)`
  - Hard gate: `passed`

## Outcome
- Round20 objective completed:
  - Consecutive-failure (2~3 streak) reward safety and recovery-playability now have regression protection.
  - Mid-late recovery debt curve remains within runs budget in automated validation.
