# Phase B Round7 Report (BossAttackCsv Idempotence Fix)

Timestamp: 2026-03-25 (+08:00)

## Problem

- `BossAttackCsvTuningTool` apply mode always appended a CSV normalization row as `Fixed`, even when `fill.csv` content did not change.
- Result: repeated runs showed persistent `boss-attack-csv apply summary: fixed=1`, causing noisy gate telemetry.

## Code Changes

Updated `Assets/Editor/BossAttackCsvTuningTool.cs`:

1. Added `WriteAttackCsvIfChanged(...)` to write `fill.csv` only when content differs.
2. Added `BuildAttackCsvContent(...)` and reused it in both write paths.
3. Updated apply-mode report behavior:
   - changed content -> `status=Fixed`, `fixed_count=1`
   - unchanged content -> `status=Ok`, `fixed_count=0`, note=`fill-csv-already-normalized`
4. Cleaned one garbled export dialog string in `ExportTemplate()` to readable text.

## Verification

### A. Single gate idempotence run

`run_playmode_batch_tests.ps1 -TestFilter "ThirdPersonController.Tests.BossAttackCsv" -Skip... (only boss-attack-csv gate active)`

Result after fix:

- boss-attack-csv apply summary: `total=9 ok=9 fixed=0 gap=0 error=0`
- boss-attack-csv validate summary: `total=8 ok=8 fixed=0 gap=0 error=0`
- Hard gate: `passed`

### B. Boss-chain regression rerun

`run_playmode_batch_tests.ps1 -TestFilter "ThirdPersonController.Tests.Boss" -Skip... (boss chain gates active)`

Result after fix:

- Boss chain gate summaries remain green.
- boss-attack-csv apply summary remains `fixed=0`.
- PlayMode summary: `total=57 passed=57 failed=0 skipped=0`
- Hard gate: `passed`

## Evidence

- `C:\test\Shrimp\Logs\PlayMode_PhaseB_round_boss_attack_csv_idempotence_after_fix.xml`
- `C:\test\Shrimp\Logs\PlayModeBatchRunner_phaseB_round_boss_attack_csv_idempotence_after_fix.log`
- `C:\test\Shrimp\Logs\PlayMode_PhaseB_round_boss_chain_focus_after_csv_fix.xml`
- `C:\test\Shrimp\Logs\PlayModeBatchRunner_phaseB_round_boss_chain_focus_after_csv_fix.log`
- `C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round4_import_report.csv`
- `C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_report.csv`
- `C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_summary.md`
