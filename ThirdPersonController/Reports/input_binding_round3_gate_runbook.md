# Input Binding Round3 Gate Runbook

## Goal
- Run a single gate command for Round3 input consistency:
  - apply scene fixes
  - validate full gate
  - run PlayMode regression
- Output one consolidated pass/fail report.

## Script
- `C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_input_round3_gate.ps1`

## Command
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_input_round3_gate.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -NoGraphics
```

## Main Artifacts
- Summary report:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_gate_summary.md`
- Scene audit csv:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_scene_audit.csv`
- Full gate csv:
  - `C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_full_gate_audit.csv`
- Logs:
  - `C:\test\Shrimp\Logs\InputBindingRound3Apply.log`
  - `C:\test\Shrimp\Logs\InputBindingRound3FullGate.log`
  - `C:\test\Shrimp\Logs\PlayModeBatchRunner.log`

## Exit Code
- `0`: gate passed.
- `2`: gate failed.

## Notes
- PlayMode step uses XML test result as source of truth.  
  If XML shows `failed=0` but process exit code is non-zero, gate records a warning and still passes.

## Integration
- `run_playmode_batch_tests.ps1` now includes Level Content gate by default
  (`LevelContentCompletenessValidator.FixForBatch` + `ValidateForBatch` before LevelData/Input gates).
- To skip Level Content gate in special runs, add:
```powershell
-SkipLevelContentGate
```
- `run_playmode_batch_tests.ps1` now includes Input Round3 gate by default
  (apply + full validate before PlayMode tests).
- `run_playmode_batch_tests.ps1` now also runs LevelData Scene gate by default
  (`LevelDataSceneValidator.FixForBatch` + `ValidateForBatch` before Input Round3).
- To skip this gate in special runs, add:
```powershell
-SkipInputRound3Gate
```
- To skip LevelData Scene gate in special runs, add:
```powershell
-SkipLevelDataSceneGate
```
