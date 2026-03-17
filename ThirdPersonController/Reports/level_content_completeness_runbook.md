# Level Content Completeness Runbook

## Goal
- Validate level scene closure across LevelData/Scene/Stronghold/Player/Boss wiring.
- Export one CSV + one Markdown summary for gating and triage.

## Menu Entrypoints
- `Tools/Level/P0/Validate Level Content Completeness (CSV)`
- `Tools/Level/P0/Validate Level Content Completeness (CI Gate)`
- `Tools/Level/P0/Fix Level Content Completeness`

## Batch Method
- `ThirdPersonController.Editor.LevelContentCompletenessValidator.FixForBatch`
- `ThirdPersonController.Editor.LevelContentCompletenessValidator.ValidateForBatch`

## Batch Command (Unity)
```powershell
& 'C:\test\Shrimp\Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1' `
  -ProjectPath 'C:\test\Shrimp' `
  -NoGraphics
```

## Artifacts
- CSV: `C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_content_completeness_report.csv`
- Summary: `C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_content_completeness_summary.md`

## Gate Rule
- Any `blocking_errors > 0` fails the gate.
- Warnings are non-blocking and should be triaged in next pass.
