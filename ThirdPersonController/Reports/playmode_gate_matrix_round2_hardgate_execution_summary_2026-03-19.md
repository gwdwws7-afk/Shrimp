# PlayMode Gate Matrix Round2 Hard-Gate Summary (2026-03-19)

## This Round
- Upgraded gate matrix from "report only" to hard-gate enforcement.
- Added CI failure summary artifact for quick diagnosis.
- Kept filtered smoke runs usable by treating subset no-match as `Skipped` when `-TestFilter`/`-AssemblyFilter` is set.

## Script Changes
- File: `Assets/ThirdPersonController/Reports/run_playmode_batch_tests.ps1`
- Added parameters:
  - `GateMatrixCiFailureSummaryMd`
  - `DisableGateMatrixHardFail`
- Added functions:
  - `Write-GateMatrixCiFailureSummary`
  - Extended `New-GateMatrixRowFromTestSubset(..., allowNoMatchesAsSkipped)`
- Added hard-gate evaluation at end of batch:
  - Blocks on `Failed` / `Missing` / `Unknown` matrix rows.
  - Emits non-zero exit (`2`) when PlayMode run is otherwise green but matrix has blockers.

## Verification
1. Filtered smoke run
- Result XML: `C:\test\Shrimp\Logs\PlayModeBatchResults_gate_hardgate_smoke.xml`
- Summary: `total=1 passed=1 failed=0 skipped=0`
- Hard gate: passed.
- Subset no-match rows (P0 subsets) were marked `Skipped` as designed.

2. Full run (all gates enabled)
- Result XML: `C:\test\Shrimp\Logs\PlayModeBatchResults_gate_hardgate_full.xml`
- Summary: `total=93 passed=92 failed=0 skipped=1`
- Matrix summary: 17 passed, 0 failed, 0 missing.
- Hard gate: passed.

## Artifacts
- Gate matrix CSV:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_report.csv`
- Gate matrix summary:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_summary.md`
- CI failure summary:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_ci_failure_summary.md`
- Full run log:
  - `C:\test\Shrimp\Logs\PlayModeBatchRunner_gate_hardgate_full.log`
