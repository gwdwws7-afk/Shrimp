# Boss Choreography Strict Gate Drill Snapshot (Round8)

- Timestamp: 2026-03-30 18:10:00 +08:00
- Phase1 Exit: 1
- Phase2 Exit: 0
- Restore Exit: 0
- Captured Exception: none

## Phase1

- Log: C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_phase1.log

```text
[BossChoreographyCoverage] strict-warning whitelist loaded | entries=0 csv=Assets/ThirdPersonController/Reports/boss_choreography_strict_warning_whitelist.csv
[BossChoreographyCoverage] strict-warning gate applied | targets=3 escalatedRows=1 escalatedWarnings=1 whitelistedRows=0 whitelistEntries=0
[BossChoreographyCoverage] complete | targets=3 errorRows=1 blocking=1 warnings=1 strictGate=1 strictWhitelistRows=0 csv=Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv summary=Assets/ThirdPersonController/Reports/boss_choreography_coverage_summary.md
InvalidOperationException: [BossChoreographyCoverage] gate failed. blocking=1 csv=Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv
ThirdPersonController.Editor.BossChoreographyCoverageValidator.ValidateForBatch
ThirdPersonController.Editor.BossChoreographyCoverageValidator:LoadStrictWarningWhitelist () (at Assets/Editor/BossChoreographyCoverageValidator.cs:337)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:207)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:ValidateForBatch () (at Assets/Editor/BossChoreographyCoverageValidator.cs:160)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 337)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:ApplyStrictWarningGate (System.Collections.Generic.List`1<ThirdPersonController.Editor.BossChoreographyCoverageValidator/ValidationRow>,System.Collections.Generic.HashSet`1<string>) (at Assets/Editor/BossChoreographyCoverageValidator.cs:378)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:208)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 378)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:248)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 248)
Start importing Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv using Guid(a58b510bf9a279e469734ea5cd7aceb0) Importer(-1,00000000000000000000000000000000) [PhysX] Initialized MultithreadedTaskDispatcher with 20 workers.
```

## Phase2

- Log: C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_phase2.log

```text
[BossChoreographyCoverage] strict-warning whitelist loaded | entries=1 csv=Assets/ThirdPersonController/Reports/boss_choreography_strict_warning_whitelist.csv
[BossChoreographyCoverage] strict-warning gate applied | targets=3 escalatedRows=0 escalatedWarnings=0 whitelistedRows=1 whitelistEntries=1
[BossChoreographyCoverage] complete | targets=3 errorRows=0 blocking=0 warnings=1 strictGate=1 strictWhitelistRows=1 csv=Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv summary=Assets/ThirdPersonController/Reports/boss_choreography_coverage_summary.md
ThirdPersonController.Editor.BossChoreographyCoverageValidator.ValidateForBatch
ThirdPersonController.Editor.BossChoreographyCoverageValidator:LoadStrictWarningWhitelist () (at Assets/Editor/BossChoreographyCoverageValidator.cs:337)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:207)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:ValidateForBatch () (at Assets/Editor/BossChoreographyCoverageValidator.cs:160)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 337)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:ApplyStrictWarningGate (System.Collections.Generic.List`1<ThirdPersonController.Editor.BossChoreographyCoverageValidator/ValidationRow>,System.Collections.Generic.HashSet`1<string>) (at Assets/Editor/BossChoreographyCoverageValidator.cs:378)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:208)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 378)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:248)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 248)
Start importing Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv using Guid(a58b510bf9a279e469734ea5cd7aceb0) Importer(-1,00000000000000000000000000000000) [PhysX] Initialized MultithreadedTaskDispatcher with 20 workers.
```

## Restore

- Log: C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_restore.log

```text
[BossChoreographyCoverage] strict-warning whitelist loaded | entries=0 csv=Assets/ThirdPersonController/Reports/boss_choreography_strict_warning_whitelist.csv
[BossChoreographyCoverage] strict-warning gate applied | targets=3 escalatedRows=0 escalatedWarnings=0 whitelistedRows=0 whitelistEntries=0
[BossChoreographyCoverage] complete | targets=3 errorRows=0 blocking=0 warnings=0 strictGate=1 strictWhitelistRows=0 csv=Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv summary=Assets/ThirdPersonController/Reports/boss_choreography_coverage_summary.md
ThirdPersonController.Editor.BossChoreographyCoverageValidator.ValidateForBatch
ThirdPersonController.Editor.BossChoreographyCoverageValidator:LoadStrictWarningWhitelist () (at Assets/Editor/BossChoreographyCoverageValidator.cs:337)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:207)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:ValidateForBatch () (at Assets/Editor/BossChoreographyCoverageValidator.cs:160)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 337)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:ApplyStrictWarningGate (System.Collections.Generic.List`1<ThirdPersonController.Editor.BossChoreographyCoverageValidator/ValidationRow>,System.Collections.Generic.HashSet`1<string>) (at Assets/Editor/BossChoreographyCoverageValidator.cs:378)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:208)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 378)
ThirdPersonController.Editor.BossChoreographyCoverageValidator:Run (bool,bool,bool) (at Assets/Editor/BossChoreographyCoverageValidator.cs:248)
(Filename: Assets/Editor/BossChoreographyCoverageValidator.cs Line: 248)
Start importing Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv using Guid(a58b510bf9a279e469734ea5cd7aceb0) Importer(-1,00000000000000000000000000000000) [PhysX] Initialized MultithreadedTaskDispatcher with 20 workers.
```

