# Phase B Round3 Report (Validate-Only Runner)

Timestamp: 2026-03-24 (+08:00)

## Goal

Reduce asset re-serialization noise during batch regression by supporting a validate-only execution path in `run_playmode_batch_tests.ps1`.

## Changes

- Updated script: `Assets/ThirdPersonController/Reports/run_playmode_batch_tests.ps1`
- New switch parameter:
  - `-ValidateOnly`
- Behavior:
  - In validate-only mode, mutating `ApplyForBatch/FixForBatch` stages are skipped.
  - Corresponding `ValidateForBatch` stages still run.
  - Existing default behavior remains unchanged when `-ValidateOnly` is not provided.
- Gate matrix alignment:
  - `Input Round3 Scene Gate` is treated as `Skipped` in validate-only mode to avoid false `Unknown` status.

## Verification

Executed:

`run_playmode_batch_tests.ps1 -ValidateOnly -NoGraphics -TestFilter "ThirdPersonController.Tests.BossLevel10GateRegressionTests"`

Result highlights:

- Validate-only skip logs appeared for all mutating apply stages.
- PlayMode summary: `total=5 passed=5 failed=0 skipped=0`
- Hard gate: `passed`
- P3 boss depth subset: `5/5 passed`

Evidence:

- `Assets/ThirdPersonController/Reports/PlayModeBatchResults_phaseB_round3_validateonly_fix.xml`
- `Assets/ThirdPersonController/Reports/PlayModeBatchRunner_phaseB_round3_validateonly_fix.log`
- `Assets/ThirdPersonController/Reports/playmode_gate_matrix_report.csv`
- `Assets/ThirdPersonController/Reports/playmode_gate_matrix_summary.md`
