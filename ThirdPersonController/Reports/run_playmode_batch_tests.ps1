param(
    [string]$ProjectPath = "C:\test\Shrimp",
    [string]$UnityPath = "",
    [string]$ResultsXml = "C:\test\Shrimp\Logs\PlayModeBatchResults.xml",
    [string]$LogFile = "C:\test\Shrimp\Logs\PlayModeBatchRunner.log",
    [string]$WarmupLogFile = "C:\test\Shrimp\Logs\PlayModeBatchWarmup.log",
    [string]$LevelContentApplyMethod = "ThirdPersonController.Editor.LevelContentCompletenessValidator.FixForBatch",
    [string]$LevelContentValidateMethod = "ThirdPersonController.Editor.LevelContentCompletenessValidator.ValidateForBatch",
    [string]$LevelContentApplyLogFile = "C:\test\Shrimp\Logs\LevelContentCompletenessFix.log",
    [string]$LevelContentLogFile = "C:\test\Shrimp\Logs\LevelContentCompleteness.log",
    [string]$LevelContentReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_content_completeness_report.csv",
    [string]$LevelCombatDensityApplyMethod = "ThirdPersonController.Editor.LevelCombatDensityValidator.FixForBatch",
    [string]$LevelCombatDensityValidateMethod = "ThirdPersonController.Editor.LevelCombatDensityValidator.ValidateForBatch",
    [string]$LevelCombatDensityApplyLogFile = "C:\test\Shrimp\Logs\LevelCombatDensityFix.log",
    [string]$LevelCombatDensityValidateLogFile = "C:\test\Shrimp\Logs\LevelCombatDensityValidate.log",
    [string]$LevelCombatDensityReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_combat_density_gap_report.csv",
    [string]$LevelDataSceneApplyMethod = "ThirdPersonController.Editor.LevelDataSceneValidator.FixForBatch",
    [string]$LevelDataSceneValidateMethod = "ThirdPersonController.Editor.LevelDataSceneValidator.ValidateForBatch",
    [string]$LevelDataSceneApplyLogFile = "C:\test\Shrimp\Logs\LevelDataSceneFix.log",
    [string]$LevelDataSceneValidateLogFile = "C:\test\Shrimp\Logs\LevelDataSceneValidate.log",
    [string]$LevelDataSceneReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_data_scene_validator_report.csv",
    [string]$LevelBeatProgressionApplyMethod = "ThirdPersonController.Editor.LevelBeatProgressionTuningTool.ApplyForBatch",
    [string]$LevelBeatProgressionValidateMethod = "ThirdPersonController.Editor.LevelBeatProgressionTuningTool.ValidateForBatch",
    [string]$LevelBeatProgressionApplyLogFile = "C:\test\Shrimp\Logs\LevelBeatProgressionApply.log",
    [string]$LevelBeatProgressionValidateLogFile = "C:\test\Shrimp\Logs\LevelBeatProgressionValidate.log",
    [string]$LevelBeatProgressionReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_beat_progression_tuning_report.csv",
    [string]$LevelBeatSheetValidateMethod = "ThirdPersonController.Editor.LevelBeatSheetConsistencyValidator.ValidateForBatch",
    [string]$LevelBeatSheetLogFile = "C:\test\Shrimp\Logs\LevelBeatSheetConsistencyValidate.log",
    [string]$LevelBeatSheetReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_beat_sheet_consistency_report.csv",
    [string]$LevelProgressionCurveValidateMethod = "ThirdPersonController.Editor.LevelProgressionCurveConsistencyValidator.ValidateForBatch",
    [string]$LevelProgressionCurveLogFile = "C:\test\Shrimp\Logs\LevelProgressionCurveConsistencyValidate.log",
    [string]$LevelProgressionCurveReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_progression_curve_consistency_report.csv",
    [string]$BossFlowCouplingValidateMethod = "ThirdPersonController.Editor.BossFlowCouplingValidator.ValidateForBatch",
    [string]$BossFlowCouplingLogFile = "C:\test\Shrimp\Logs\BossFlowCouplingValidate.log",
    [string]$BossFlowCouplingReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_level_flow_coupling_report.csv",
    [string]$BossEncounterRound3ApplyMethod = "ThirdPersonController.Editor.BossEncounterRound3TuningTool.ApplyForBatch",
    [string]$BossEncounterRound3ValidateMethod = "ThirdPersonController.Editor.BossEncounterRound3TuningTool.ValidateForBatch",
    [string]$BossEncounterRound3ApplyLogFile = "C:\test\Shrimp\Logs\BossEncounterRound3Apply.log",
    [string]$BossEncounterRound3ValidateLogFile = "C:\test\Shrimp\Logs\BossEncounterRound3Validate.log",
    [string]$BossEncounterRound3ReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_encounter_round3_tuning_report.csv",
    [string]$BossPhaseAttackApplyMethod = "ThirdPersonController.Editor.BossPhaseAttackConsistencyValidator.ApplyForBatch",
    [string]$BossPhaseAttackValidateMethod = "ThirdPersonController.Editor.BossPhaseAttackConsistencyValidator.ValidateForBatch",
    [string]$BossPhaseAttackApplyLogFile = "C:\test\Shrimp\Logs\BossPhaseAttackConsistencyApply.log",
    [string]$BossPhaseAttackValidateLogFile = "C:\test\Shrimp\Logs\BossPhaseAttackConsistencyValidate.log",
    [string]$BossPhaseAttackReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_phase_attack_consistency_report.csv",
    [string]$BossChoreographyValidateMethod = "ThirdPersonController.Editor.BossChoreographyCoverageValidator.ValidateForBatch",
    [string]$BossChoreographyLogFile = "C:\test\Shrimp\Logs\BossChoreographyCoverageValidate.log",
    [string]$BossChoreographyReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_coverage_report.csv",
    [string]$BossEncounterProfileCoverageApplyMethod = "ThirdPersonController.Editor.BossEncounterProfileCoverageValidator.FixForBatch",
    [string]$BossEncounterProfileCoverageValidateMethod = "ThirdPersonController.Editor.BossEncounterProfileCoverageValidator.ValidateForBatch",
    [string]$BossEncounterProfileCoverageApplyLogFile = "C:\test\Shrimp\Logs\BossEncounterProfileCoverageApply.log",
    [string]$BossEncounterProfileCoverageLogFile = "C:\test\Shrimp\Logs\BossEncounterProfileCoverageValidate.log",
    [string]$BossEncounterProfileCoverageReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_encounter_profile_coverage_report.csv",
    [string]$BossAttackCsvApplyMethod = "ThirdPersonController.Editor.BossAttackCsvTuningTool.ApplyForBatch",
    [string]$BossAttackCsvValidateMethod = "ThirdPersonController.Editor.BossAttackCsvTuningTool.ValidateForBatch",
    [string]$BossAttackCsvApplyLogFile = "C:\test\Shrimp\Logs\BossAttackCsvApply.log",
    [string]$BossAttackCsvValidateLogFile = "C:\test\Shrimp\Logs\BossAttackCsvValidate.log",
    [string]$BossAttackCsvReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round4_import_report.csv",
    [string]$InputRound3ApplyMethod = "ThirdPersonController.Editor.InputBindingRound3SceneTool.ApplySceneBindingsForBatch",
    [string]$InputRound3ValidateMethod = "ThirdPersonController.Editor.InputBindingRound3SceneTool.ValidateFullGateForBatch",
    [string]$InputRound3ApplyLogFile = "C:\test\Shrimp\Logs\InputBindingRound3Apply.log",
    [string]$InputRound3ValidateLogFile = "C:\test\Shrimp\Logs\InputBindingRound3FullGate.log",
    [string]$InputRound3SceneAuditCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_scene_audit.csv",
    [string]$InputRound3FullGateCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_full_gate_audit.csv",
    [string]$InputMirrorValidateMethod = "ThirdPersonController.Editor.InputActionsMirrorAuditTool.ValidateForBatch",
    [string]$InputMirrorValidateLogFile = "C:\test\Shrimp\Logs\InputActionsMirrorAudit.log",
    [string]$InputMirrorReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_actions_mirror_audit.csv",
    [string]$UICrossDeviceReadabilityValidateMethod = "ThirdPersonController.Editor.UICrossDeviceReadabilityValidator.ValidateForBatch",
    [string]$UICrossDeviceReadabilityLogFile = "C:\test\Shrimp\Logs\UICrossDeviceReadabilityValidate.log",
    [string]$UICrossDeviceReadabilityReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\ui_cross_device_readability_report.csv",
    [string]$CommentLogQualityValidateMethod = "ThirdPersonController.Editor.CommentLogQualityGate.ValidateForBatch",
    [string]$CommentLogQualityLogFile = "C:\test\Shrimp\Logs\CommentLogQualityGate.log",
    [string]$CommentLogQualityReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\comment_log_quality_gate_report.csv",
    [string]$CombatFeedbackCoverageValidateMethod = "ThirdPersonController.Editor.CombatFeedbackCoverageGateValidator.ValidateForBatch",
    [string]$CombatFeedbackCoverageLogFile = "C:\test\Shrimp\Logs\CombatFeedbackCoverageGate.log",
    [string]$CombatFeedbackCoverageReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\combat_feedback_coverage_gate_report.csv",
    [string]$SkillResourceGapApplyMethod = "ThirdPersonController.Editor.SkillResourceGapValidator.ApplyForBatch",
    [string]$SkillResourceGapValidateMethod = "ThirdPersonController.Editor.SkillResourceGapValidator.ValidateForBatch",
    [string]$SkillResourceGapApplyLogFile = "C:\test\Shrimp\Logs\SkillResourceGapApply.log",
    [string]$SkillResourceGapValidateLogFile = "C:\test\Shrimp\Logs\SkillResourceGapValidate.log",
    [string]$SkillResourceGapReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\skill_resource_gap_report.csv",
    [string]$LocalizationCoverageApplyMethod = "ThirdPersonController.Editor.LocalizationEncodingRepairTool.ApplyForBatch",
    [string]$LocalizationCoverageApplyLogFile = "C:\test\Shrimp\Logs\LocalizationCoverageApply.log",
    [string]$LocalizationCoverageValidateMethod = "ThirdPersonController.Editor.LocalizationCoverageGateValidator.ValidateForBatch",
    [string]$LocalizationCoverageLogFile = "C:\test\Shrimp\Logs\LocalizationCoverageGate.log",
    [string]$LocalizationCoverageReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\localization_coverage_gate_report.csv",
    [string]$LocalizationPseudoLocValidateMethod = "ThirdPersonController.Editor.LocalizationPseudoLocGateValidator.ValidateForBatch",
    [string]$LocalizationPseudoLocLogFile = "C:\test\Shrimp\Logs\LocalizationPseudoLocGate.log",
    [string]$LocalizationPseudoLocReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\localization_pseudoloc_gate_report.csv",
    [string]$GrowthEconomyConfigApplyMethod = "ThirdPersonController.Editor.GrowthEconomyConfigGateValidator.ApplyForBatch",
    [string]$GrowthEconomyConfigValidateMethod = "ThirdPersonController.Editor.GrowthEconomyConfigGateValidator.ValidateForBatch",
    [string]$GrowthEconomyConfigApplyLogFile = "C:\test\Shrimp\Logs\GrowthEconomyConfigApply.log",
    [string]$GrowthEconomyConfigValidateLogFile = "C:\test\Shrimp\Logs\GrowthEconomyConfigValidate.log",
    [string]$GrowthEconomyConfigReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\growth_economy_config_gate_report.csv",
    [string]$SteamConfigEnsureMethod = "ThirdPersonController.Editor.SteamIntegrationConfigProvisionTool.EnsureForBatch",
    [string]$SteamConfigEnsureLogFile = "C:\test\Shrimp\Logs\SteamConfigProvision.log",
    [string]$SteamConfigEnsureReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\steam_config_provision_report.csv",
    [string]$SteamRuntimeModeValidateMethod = "ThirdPersonController.Editor.SteamRuntimeModeGateValidator.ValidateForBatch",
    [string]$SteamRuntimeModeLogFile = "C:\test\Shrimp\Logs\SteamRuntimeModeGate.log",
    [string]$SteamRuntimeModeReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\steam_runtime_mode_report.csv",
    [string]$P0BossGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p0_boss_depth_gate_report.csv",
    [string]$P0QuestEconomyGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p0_quest_economy_gate_report.csv",
    [string]$P0InputHintGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p0_input_hint_consistency_gate_report.csv",
    [string]$P1BossQuestCouplingGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p1_boss_quest_coupling_gate_report.csv",
    [string]$P1BossClosureGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p1_boss_encounter_closure_gate_report.csv",
    [string]$P1SkillBoundaryGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p1_skill_boundary_gate_report.csv",
    [string]$P1QuestEconomyMidLateGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p1_quest_economy_midlate_gate_report.csv",
    [string]$P2InputProductizationGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p2_input_productization_gate_report.csv",
    [string]$P2UIReadabilityGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p2_ui_readability_gate_report.csv",
    [string]$P2LocalizationGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p2_localization_regression_gate_report.csv",
    [string]$P2SteamRuntimeGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p2_steam_runtime_regression_gate_report.csv",
    [string]$P3BossDepthGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p3_boss_depth_gate_report.csv",
    [string]$P5GrowthEconomyGateReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p5_growth_economy_gate_report.csv",
    [string]$GateMatrixReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_report.csv",
    [string]$GateMatrixSummaryMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_summary.md",
    [string]$GateMatrixCiFailureSummaryMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\playmode_gate_matrix_ci_failure_summary.md",
    [string]$EnemyTypeSceneGateScript = "",
    [string]$EnemyTypeSceneGateLogFile = "",
    [string]$BossStrictDrillGateScript = "",
    [string]$BossStrictDrillGateReport = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_strict_gate_drill_round8_report_2026-03-19.md",
    [string]$TestFilter = "",
    [string]$AssemblyFilter = "",
    [int]$RetryCount = 1,
    [int]$WaitForProjectUnlockSeconds = 30,
    [int]$ProcessTimeoutSeconds = 1800,
    [int]$LevelContentTimeoutSeconds = 1200,
    [int]$LevelCombatDensityTimeoutSeconds = 1200,
    [int]$LevelDataSceneTimeoutSeconds = 1200,
    [int]$LevelBeatProgressionTimeoutSeconds = 1200,
    [int]$LevelBeatSheetTimeoutSeconds = 1200,
    [int]$LevelProgressionCurveTimeoutSeconds = 1200,
    [int]$BossFlowCouplingTimeoutSeconds = 1200,
    [int]$BossEncounterRound3TimeoutSeconds = 1200,
    [int]$BossPhaseAttackTimeoutSeconds = 1200,
    [int]$BossChoreographyTimeoutSeconds = 1200,
    [int]$BossEncounterProfileCoverageTimeoutSeconds = 1200,
    [int]$BossAttackCsvTimeoutSeconds = 1200,
    [int]$InputRound3TimeoutSeconds = 1200,
    [int]$InputMirrorTimeoutSeconds = 1200,
    [int]$UICrossDeviceReadabilityTimeoutSeconds = 1200,
    [int]$CommentLogQualityTimeoutSeconds = 1200,
    [int]$CombatFeedbackCoverageTimeoutSeconds = 1200,
    [int]$SkillResourceGapTimeoutSeconds = 1200,
    [int]$LocalizationCoverageTimeoutSeconds = 1200,
    [int]$LocalizationPseudoLocTimeoutSeconds = 1200,
    [int]$GrowthEconomyConfigTimeoutSeconds = 1200,
    [int]$SteamConfigEnsureTimeoutSeconds = 1200,
    [int]$SteamRuntimeModeTimeoutSeconds = 1200,
    [int]$EnemyTypeSceneGateTimeoutSeconds = 1200,
    [int]$BossStrictDrillGateTimeoutSeconds = 1200,
    [int]$CommentLogQualityWarningBudget = 3,
    [switch]$SkipLevelContentGate,
    [switch]$SkipLevelCombatDensityGate,
    [switch]$SkipLevelDataSceneGate,
    [switch]$SkipLevelBeatProgressionGate,
    [switch]$SkipLevelBeatSheetGate,
    [switch]$SkipLevelProgressionCurveGate,
    [switch]$SkipBossFlowCouplingGate,
    [switch]$SkipBossEncounterRound3Gate,
    [switch]$SkipBossPhaseAttackGate,
    [switch]$SkipBossChoreographyGate,
    [switch]$SkipBossEncounterProfileCoverageGate,
    [switch]$SkipBossAttackCsvGate,
    [switch]$SkipInputRound3Gate,
    [switch]$SkipInputMirrorGate,
    [switch]$SkipUICrossDeviceReadabilityGate,
    [switch]$SkipCommentLogQualityGate,
    [switch]$SkipCombatFeedbackCoverageGate,
    [switch]$SkipSkillResourceGapGate,
    [switch]$SkipLocalizationCoverageGate,
    [switch]$SkipLocalizationPseudoLocGate,
    [switch]$SkipGrowthEconomyConfigGate,
    [switch]$SkipSteamConfigEnsure,
    [switch]$SkipSteamRuntimeModeGate,
    [switch]$SkipWarmupCompile,
    [switch]$SkipEnemyTypeSceneGate,
    [switch]$SkipBossStrictDrillGate,
    [switch]$RunBossStrictDrillGate,
    [switch]$DisableGateMatrixHardFail,
    [switch]$ValidateOnly,
    [switch]$NoGraphics
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-UnityPath([string]$projectPath, [string]$explicitUnityPath) {
    if (![string]::IsNullOrWhiteSpace($explicitUnityPath)) {
        if (!(Test-Path $explicitUnityPath)) {
            throw "Unity executable not found: $explicitUnityPath"
        }

        return $explicitUnityPath
    }

    $projectVersionFile = Join-Path $projectPath "ProjectSettings\ProjectVersion.txt"
    if (!(Test-Path $projectVersionFile)) {
        throw "ProjectVersion.txt not found: $projectVersionFile"
    }

    $versionLine = Get-Content $projectVersionFile | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionLine)) {
        throw "Cannot parse m_EditorVersion from $projectVersionFile"
    }

    $version = ($versionLine.Split(':')[1]).Trim()
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "C:\PROGRA~1\Unity\Hub\Editor\$version\Editor\Unity.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Unity executable not found for version $version. Checked: $($candidates -join '; ')"
}

function Get-UnityProjectProcesses([string]$projectPath) {
    $normalized = $projectPath.Replace('/', '\')
    $allUnity = Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" -ErrorAction SilentlyContinue
    if ($null -eq $allUnity) {
        return @()
    }

    return @($allUnity | Where-Object {
            $cmd = [string]$_.CommandLine
            if ([string]::IsNullOrWhiteSpace($cmd)) {
                return $false
            }

            $cmd.Replace('/', '\').IndexOf($normalized, [StringComparison]::OrdinalIgnoreCase) -ge 0
        })
}

function Wait-ForProjectUnlock([string]$projectPath, [int]$timeoutSeconds) {
    $waitSeconds = [Math]::Max(0, $timeoutSeconds)
    $deadline = (Get-Date).AddSeconds($waitSeconds)

    while ($true) {
        $running = @(Get-UnityProjectProcesses -projectPath $projectPath)
        if ($running.Count -eq 0) {
            return $true
        }

        if ((Get-Date) -ge $deadline) {
            return $false
        }

        Start-Sleep -Seconds 2
    }
}

function Invoke-UnityProcess(
    [string]$unityExe,
    [System.Collections.Generic.List[string]]$arguments,
    [int]$timeoutSeconds
) {
    $process = Start-Process -FilePath $unityExe -ArgumentList $arguments -PassThru
    $timeoutMs = [Math]::Max(1, $timeoutSeconds) * 1000
    $completed = $process.WaitForExit($timeoutMs)
    if (-not $completed) {
        try {
            Stop-Process -Id $process.Id -Force
        }
        catch {
        }

        return 124
    }

    return $process.ExitCode
}

function Invoke-ScriptProcess(
    [string]$scriptHostExe,
    [System.Collections.Generic.List[string]]$arguments,
    [int]$timeoutSeconds
) {
    $process = Start-Process -FilePath $scriptHostExe -ArgumentList $arguments -PassThru
    $timeoutMs = [Math]::Max(1, $timeoutSeconds) * 1000
    $completed = $process.WaitForExit($timeoutMs)
    if (-not $completed) {
        try {
            Stop-Process -Id $process.Id -Force
        }
        catch {
        }

        return 124
    }

    return $process.ExitCode
}

function Invoke-UnityExecuteMethod(
    [string]$unityExe,
    [string]$projectPath,
    [string]$executeMethod,
    [string]$logFile,
    [switch]$noGraphics,
    [int]$timeoutSeconds
) {
    $args = New-Object System.Collections.Generic.List[string]
    $args.Add("-batchmode")
    if ($noGraphics.IsPresent) {
        $args.Add("-nographics")
    }

    $args.Add("-quit")
    $args.Add("-projectPath")
    $args.Add($projectPath)
    $args.Add("-executeMethod")
    $args.Add($executeMethod)
    $args.Add("-logFile")
    $args.Add($logFile)

    return Invoke-UnityProcess -unityExe $unityExe -arguments $args -timeoutSeconds $timeoutSeconds
}

function Get-CsvStatusSummary([string]$csvPath) {
    if (!(Test-Path $csvPath)) {
        return [ordered]@{
            Exists = $false
            Total = 0
            OK = 0
            Fixed = 0
            Gap = 0
            Partial = 0
            Error = 0
            Mismatch = 0
            Skipped = 0
            Other = 0
        }
    }

    $rows = @(Import-Csv $csvPath)
    $summary = [ordered]@{
        Exists = $true
        Total = $rows.Count
        OK = 0
        Fixed = 0
        Gap = 0
        Partial = 0
        Error = 0
        Mismatch = 0
        Skipped = 0
        Other = 0
    }

    foreach ($row in $rows) {
        $status = [string]$row.status
        switch -Regex ($status.ToLowerInvariant()) {
            "^ok$" {
                $summary.OK++
                continue
            }
            "^fixed$" {
                $summary.Fixed++
                continue
            }
            "^gap$" {
                $summary.Gap++
                continue
            }
            "^partial$" {
                $summary.Partial++
                continue
            }
            "^error$" {
                $summary.Error++
                continue
            }
            "^mismatch$" {
                $summary.Mismatch++
                continue
            }
            "^skipped$" {
                $summary.Skipped++
                continue
            }
            default {
                $summary.Other++
                continue
            }
        }
    }

    return $summary
}

function Format-CsvStatusSummary([hashtable]$summary) {
    if ($null -eq $summary -or -not $summary.Exists) {
        return "missing"
    }

    return "total=$($summary.Total) ok=$($summary.OK) fixed=$($summary.Fixed) gap=$($summary.Gap) partial=$($summary.Partial) error=$($summary.Error) mismatch=$($summary.Mismatch) skipped=$($summary.Skipped) other=$($summary.Other)"
}

function Get-CsvBlockingCount([hashtable]$summary) {
    if ($null -eq $summary -or -not $summary.Exists) {
        return 1
    }

    return ($summary.Gap + $summary.Partial + $summary.Error + $summary.Mismatch + $summary.Other)
}

function Invoke-WarmupCompile(
    [string]$unityExe,
    [string]$projectPath,
    [string]$warmupLogFile,
    [switch]$noGraphics,
    [int]$timeoutSeconds
) {
    $args = New-Object System.Collections.Generic.List[string]
    $args.Add("-batchmode")
    if ($noGraphics.IsPresent) {
        $args.Add("-nographics")
    }

    $args.Add("-quit")
    $args.Add("-projectPath")
    $args.Add($projectPath)
    $args.Add("-logFile")
    $args.Add($warmupLogFile)
    return Invoke-UnityProcess -unityExe $unityExe -arguments $args -timeoutSeconds $timeoutSeconds
}

function Invoke-PlayModeTests(
    [string]$unityExe,
    [string]$projectPath,
    [string]$resultsXml,
    [string]$logFile,
    [string]$testFilter,
    [string]$assemblyFilter,
    [switch]$noGraphics,
    [int]$timeoutSeconds
) {
    $args = New-Object System.Collections.Generic.List[string]
    $args.Add("-batchmode")
    if ($noGraphics.IsPresent) {
        $args.Add("-nographics")
    }

    $args.Add("-projectPath")
    $args.Add($projectPath)
    $args.Add("-runTests")
    $args.Add("-testPlatform")
    $args.Add("PlayMode")
    $args.Add("-testResults")
    $args.Add($resultsXml)

    if (![string]::IsNullOrWhiteSpace($testFilter)) {
        $args.Add("-testFilter")
        $args.Add($testFilter)
    }

    if (![string]::IsNullOrWhiteSpace($assemblyFilter)) {
        $args.Add("-assemblyNames")
        $args.Add($assemblyFilter)
    }

    $args.Add("-logFile")
    $args.Add($logFile)

    return Invoke-UnityProcess -unityExe $unityExe -arguments $args -timeoutSeconds $timeoutSeconds
}

function Invoke-EnemyTypeSceneGate(
    [string]$gateScriptPath,
    [string]$gateLogFile,
    [string]$projectPath,
    [int]$waitForProjectUnlockSeconds,
    [int]$gateTimeoutSeconds,
    [switch]$noGraphics
) {
    if (!(Test-Path $gateScriptPath)) {
        throw "Enemy type scene gate script not found: $gateScriptPath"
    }

    $powershellExe = Join-Path $env:WINDIR "System32\WindowsPowerShell\v1.0\powershell.exe"
    if (!(Test-Path $powershellExe)) {
        $powershellExe = "powershell.exe"
    }

    $args = New-Object System.Collections.Generic.List[string]
    $args.Add("-ExecutionPolicy")
    $args.Add("Bypass")
    $args.Add("-File")
    $args.Add($gateScriptPath)
    $args.Add("-ProjectPath")
    $args.Add($projectPath)
    $args.Add("-LogFile")
    $args.Add($gateLogFile)
    $args.Add("-WaitForProjectUnlockSeconds")
    $args.Add("$waitForProjectUnlockSeconds")
    $args.Add("-ProcessTimeoutSeconds")
    $args.Add("$gateTimeoutSeconds")
    if ($noGraphics.IsPresent) {
        $args.Add("-NoGraphics")
    }

    return Invoke-ScriptProcess -scriptHostExe $powershellExe -arguments $args -timeoutSeconds $gateTimeoutSeconds
}

function Invoke-BossStrictDrillGate(
    [string]$gateScriptPath,
    [string]$outputReportPath,
    [string]$projectPath,
    [int]$waitForProjectUnlockSeconds,
    [int]$gateTimeoutSeconds,
    [switch]$noGraphics
) {
    if (!(Test-Path $gateScriptPath)) {
        throw "Boss strict drill gate script not found: $gateScriptPath"
    }

    $powershellExe = Join-Path $env:WINDIR "System32\WindowsPowerShell\v1.0\powershell.exe"
    if (!(Test-Path $powershellExe)) {
        $powershellExe = "powershell.exe"
    }

    $args = New-Object System.Collections.Generic.List[string]
    $args.Add("-ExecutionPolicy")
    $args.Add("Bypass")
    $args.Add("-File")
    $args.Add($gateScriptPath)
    $args.Add("-ProjectPath")
    $args.Add($projectPath)
    $args.Add("-OutputReportPath")
    $args.Add($outputReportPath)
    $args.Add("-WaitForProjectUnlockSeconds")
    $args.Add("$waitForProjectUnlockSeconds")
    $args.Add("-ProcessTimeoutSeconds")
    $args.Add("$gateTimeoutSeconds")
    if ($noGraphics.IsPresent) {
        $args.Add("-NoGraphics")
    }

    return Invoke-ScriptProcess -scriptHostExe $powershellExe -arguments $args -timeoutSeconds $gateTimeoutSeconds
}

function Is-CompilationOnlyLog([string]$logFilePath) {
    if (!(Test-Path $logFilePath)) {
        return $false
    }

    $logText = Get-Content $logFilePath -Raw
    if ([string]::IsNullOrWhiteSpace($logText)) {
        return $false
    }

    $hasCompiling = $logText.IndexOf("Compiling Scripts", [StringComparison]::OrdinalIgnoreCase) -ge 0
    $hasRunTestsArg = $logText.IndexOf("-runTests", [StringComparison]::OrdinalIgnoreCase) -ge 0
    $hasBatchQuit = $logText.IndexOf("Batchmode quit successfully invoked", [StringComparison]::OrdinalIgnoreCase) -ge 0
    $hasResultTag = $logText.IndexOf("<test-run", [StringComparison]::OrdinalIgnoreCase) -ge 0
    return $hasCompiling -and $hasRunTestsArg -and $hasBatchQuit -and (-not $hasResultTag)
}

function Get-ResultSummary([string]$resultsXmlPath) {
    if (!(Test-Path $resultsXmlPath)) {
        return "missing result file"
    }

    try {
        [xml]$doc = Get-Content $resultsXmlPath
        $run = $doc.SelectSingleNode("//test-run")
        if ($null -eq $run) {
            return "result xml missing test-run node"
        }

        $total = $run.total
        $passed = $run.passed
        $failed = $run.failed
        $skipped = $run.skipped
        return "total=$total passed=$passed failed=$failed skipped=$skipped"
    }
    catch {
        return "result xml parse failed: $($_.Exception.Message)"
    }
}

function Get-CommentLogQualitySummary([string]$csvPath) {
    if (!(Test-Path $csvPath)) {
        return [ordered]@{
            Exists = $false
            Total = 0
            Warnings = 0
            Errors = 0
            Ok = 0
        }
    }

    $rows = @(Import-Csv $csvPath)
    $summary = [ordered]@{
        Exists = $true
        Total = $rows.Count
        Warnings = 0
        Errors = 0
        Ok = 0
    }

    foreach ($row in $rows) {
        $severity = [string]$row.severity
        $status = [string]$row.status
        if ($severity.Equals("Error", [StringComparison]::OrdinalIgnoreCase) -or
            $status.Equals("Error", [StringComparison]::OrdinalIgnoreCase)) {
            $summary.Errors++
            continue
        }

        if ($severity.Equals("Warning", [StringComparison]::OrdinalIgnoreCase) -or
            $status.Equals("Warning", [StringComparison]::OrdinalIgnoreCase)) {
            $summary.Warnings++
            continue
        }

        if ($status.Equals("Ok", [StringComparison]::OrdinalIgnoreCase)) {
            $summary.Ok++
            continue
        }
    }

    return $summary
}

function Format-CommentLogQualitySummary([hashtable]$summary) {
    if ($null -eq $summary -or -not $summary.Exists) {
        return "missing"
    }

    return "total=$($summary.Total) warnings=$($summary.Warnings) errors=$($summary.Errors) ok=$($summary.Ok)"
}

function Export-TestSubsetReport(
    [string]$resultsXmlPath,
    [string]$reportCsvPath,
    [string]$reportName,
    [string[]]$matchTokens
) {
    if (!(Test-Path $resultsXmlPath)) {
        Write-Warning "[PlayModeBatch] cannot export $reportName report; results xml missing: $resultsXmlPath"
        return
    }

    [xml]$doc = Get-Content $resultsXmlPath
    $allCases = $doc.SelectNodes("//test-case")
    if ($null -eq $allCases) {
        Write-Warning "[PlayModeBatch] cannot export $reportName report; no test-case nodes."
        return
    }

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($case in $allCases) {
        $name = [string]$case.name
        $fullName = [string]$case.fullname
        $match = $false
        foreach ($token in $matchTokens) {
            if ([string]::IsNullOrWhiteSpace($token)) {
                continue
            }

            if ($name.IndexOf($token, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $fullName.IndexOf($token, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $match = $true
                break
            }
        }

        if (-not $match) {
            continue
        }

        $rows.Add([pscustomobject]@{
                name = $name
                fullname = $fullName
                result = [string]$case.result
                duration = [string]$case.duration
            })
    }

    $dir = Split-Path -Parent $reportCsvPath
    if (![string]::IsNullOrWhiteSpace($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    if ($rows.Count -eq 0) {
        @([pscustomobject]@{
                name = "$reportName (no matches)"
                fullname = ""
                result = "Skipped"
                duration = "0"
            }) | Export-Csv -Path $reportCsvPath -NoTypeInformation -Encoding UTF8
        Write-Host "[PlayModeBatch] $reportName report: no matches -> $reportCsvPath"
        return
    }

    $rows | Export-Csv -Path $reportCsvPath -NoTypeInformation -Encoding UTF8

    $passed = @($rows | Where-Object { $_.result -eq "Passed" }).Count
    $failed = @($rows | Where-Object { $_.result -eq "Failed" }).Count
    $skipped = @($rows | Where-Object { $_.result -eq "Skipped" }).Count
    Write-Host "[PlayModeBatch] $reportName report: total=$($rows.Count) passed=$passed failed=$failed skipped=$skipped csv=$reportCsvPath"
}

function Get-OptionalVariableValue([string]$name) {
    if ([string]::IsNullOrWhiteSpace($name)) {
        return $null
    }

    $var = Get-Variable -Name $name -Scope Script -ErrorAction SilentlyContinue
    if ($null -eq $var) {
        return $null
    }

    return $var.Value
}

function New-GateMatrixRowFromCsvSummary(
    [string]$gateName,
    [bool]$isSkipped,
    [string]$summaryVariableName,
    [string]$csvPath
) {
    if ($isSkipped) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Skipped"
            total = 0
            blocking = 0
            warnings = 0
            errors = 0
            csv = $csvPath
            note = "Skipped by switch"
        }
    }

    $summary = Get-OptionalVariableValue -name $summaryVariableName
    if ($null -eq $summary) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Unknown"
            total = 0
            blocking = 0
            warnings = 0
            errors = 0
            csv = $csvPath
            note = "summary variable missing"
        }
    }

    $blocking = Get-CsvBlockingCount -summary $summary
    $status = if (-not $summary.Exists) {
        "Missing"
    }
    elseif ($blocking -gt 0) {
        "Failed"
    }
    else {
        "Passed"
    }

    $warnCount = 0
    if ($summary.Exists) {
        $warnCount = [int]$summary.Gap + [int]$summary.Partial + [int]$summary.Mismatch + [int]$summary.Other
    }

    return [pscustomobject]@{
        gate = $gateName
        status = $status
        total = if ($summary.Exists) { [int]$summary.Total } else { 0 }
        blocking = [int]$blocking
        warnings = $warnCount
        errors = if ($summary.Exists) { [int]$summary.Error } else { 0 }
        csv = $csvPath
        note = if ($summary.Exists) { Format-CsvStatusSummary -summary $summary } else { "report missing" }
    }
}

function New-GateMatrixRowFromCommentSummary(
    [string]$gateName,
    [bool]$isSkipped,
    [string]$summaryVariableName,
    [string]$csvPath,
    [int]$warningBudget
) {
    if ($isSkipped) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Skipped"
            total = 0
            blocking = 0
            warnings = 0
            errors = 0
            csv = $csvPath
            note = "Skipped by switch"
        }
    }

    $summary = Get-OptionalVariableValue -name $summaryVariableName
    if ($null -eq $summary) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Unknown"
            total = 0
            blocking = 0
            warnings = 0
            errors = 0
            csv = $csvPath
            note = "summary variable missing"
        }
    }

    $warnings = if ($summary.Exists) { [int]$summary.Warnings } else { 0 }
    $errors = if ($summary.Exists) { [int]$summary.Errors } else { 0 }
    $excessWarnings = 0
    if ($summary.Exists -and $warningBudget -ge 0 -and $warnings -gt $warningBudget) {
        $excessWarnings = $warnings - $warningBudget
    }

    $status = if (-not $summary.Exists) {
        "Missing"
    }
    elseif ($errors -gt 0) {
        "Failed"
    }
    elseif ($excessWarnings -gt 0) {
        "Failed"
    }
    else {
        "Passed"
    }

    $blocking = $errors + $excessWarnings
    $note = if ($summary.Exists) {
        $budgetText = if ($warningBudget -ge 0) { "warning-budget=$warningBudget" } else { "warning-budget=disabled" }
        "$((Format-CommentLogQualitySummary -summary $summary)); $budgetText"
    }
    else {
        "report missing"
    }

    return [pscustomobject]@{
        gate = $gateName
        status = $status
        total = if ($summary.Exists) { [int]$summary.Total } else { 0 }
        blocking = $blocking
        warnings = $warnings
        errors = $errors
        csv = $csvPath
        note = $note
    }
}

function Parse-ResultSummaryObject([string]$summaryText) {
    $result = [ordered]@{
        total = 0
        passed = 0
        failed = 0
        skipped = 0
    }

    if ([string]::IsNullOrWhiteSpace($summaryText)) {
        return $result
    }

    $match = [regex]::Match($summaryText, "total=(\d+)\s+passed=(\d+)\s+failed=(\d+)\s+skipped=(\d+)")
    if (-not $match.Success) {
        return $result
    }

    $result.total = [int]$match.Groups[1].Value
    $result.passed = [int]$match.Groups[2].Value
    $result.failed = [int]$match.Groups[3].Value
    $result.skipped = [int]$match.Groups[4].Value
    return $result
}

function Get-TestSubsetSummary([string]$csvPath) {
    if (!(Test-Path $csvPath)) {
        return [ordered]@{
            Exists = $false
            Total = 0
            Passed = 0
            Failed = 0
            Skipped = 0
            NoMatches = $false
        }
    }

    $rows = @(Import-Csv $csvPath)
    $effectiveRows = @($rows | Where-Object {
            $name = [string]$_.name
            -not $name.EndsWith("(no matches)", [StringComparison]::OrdinalIgnoreCase)
        })

    $summary = [ordered]@{
        Exists = $true
        Total = $effectiveRows.Count
        Passed = @($effectiveRows | Where-Object { ([string]$_.result).Equals("Passed", [StringComparison]::OrdinalIgnoreCase) }).Count
        Failed = @($effectiveRows | Where-Object { ([string]$_.result).Equals("Failed", [StringComparison]::OrdinalIgnoreCase) }).Count
        Skipped = @($effectiveRows | Where-Object { ([string]$_.result).Equals("Skipped", [StringComparison]::OrdinalIgnoreCase) }).Count
        NoMatches = ($effectiveRows.Count -eq 0)
    }

    return $summary
}

function Format-TestSubsetSummary([hashtable]$summary) {
    if ($null -eq $summary -or -not $summary.Exists) {
        return "missing"
    }

    if ($summary.NoMatches) {
        return "no matches"
    }

    return "total=$($summary.Total) passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped)"
}

function New-GateMatrixRowFromTestSubset(
    [string]$gateName,
    [string]$csvPath,
    [bool]$allowNoMatchesAsSkipped
) {
    $summary = Get-TestSubsetSummary -csvPath $csvPath
    if (-not $summary.Exists) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Missing"
            total = 0
            blocking = 1
            warnings = 0
            errors = 1
            csv = $csvPath
            note = "subset report missing"
        }
    }

    if ($summary.NoMatches) {
        $status = if ($allowNoMatchesAsSkipped) { "Skipped" } else { "Missing" }
        $blocking = if ($allowNoMatchesAsSkipped) { 0 } else { 1 }
        $note = if ($allowNoMatchesAsSkipped) {
            "subset has no matching test cases under current filter"
        }
        else {
            "subset has no matching test cases"
        }

        return [pscustomobject]@{
            gate = $gateName
            status = $status
            total = 0
            blocking = $blocking
            warnings = 1
            errors = $blocking
            csv = $csvPath
            note = $note
        }
    }

    $status = if ([int]$summary.Failed -gt 0) {
        "Failed"
    }
    else {
        "Passed"
    }

    return [pscustomobject]@{
        gate = $gateName
        status = $status
        total = [int]$summary.Total
        blocking = [int]$summary.Failed
        warnings = [int]$summary.Skipped
        errors = [int]$summary.Failed
        csv = $csvPath
        note = Format-TestSubsetSummary -summary $summary
    }
}

function New-GateMatrixRowFromExitCode(
    [string]$gateName,
    [bool]$isSkipped,
    [string]$exitCodeVariableName,
    [string]$evidencePath
) {
    if ($isSkipped) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Skipped"
            total = 0
            blocking = 0
            warnings = 0
            errors = 0
            csv = $evidencePath
            note = "Skipped by switch"
        }
    }

    $exitCodeValue = Get-OptionalVariableValue -name $exitCodeVariableName
    if ($null -eq $exitCodeValue) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Unknown"
            total = 0
            blocking = 0
            warnings = 0
            errors = 0
            csv = $evidencePath
            note = "exit code variable missing"
        }
    }

    $exitCode = [int]$exitCodeValue
    $status = if ($exitCode -eq 0) { "Passed" } else { "Failed" }
    $blocking = if ($exitCode -eq 0) { 0 } else { 1 }

    return [pscustomobject]@{
        gate = $gateName
        status = $status
        total = 1
        blocking = $blocking
        warnings = 0
        errors = $blocking
        csv = $evidencePath
        note = "exit=$exitCode"
    }
}

function New-GateMatrixRowFromPlayModeSummary(
    [string]$gateName,
    [string]$summaryText,
    [string]$resultsXmlPath
) {
    if (!(Test-Path $resultsXmlPath)) {
        return [pscustomobject]@{
            gate = $gateName
            status = "Missing"
            total = 0
            blocking = 1
            warnings = 0
            errors = 1
            csv = $resultsXmlPath
            note = "results xml missing"
        }
    }

    $play = Parse-ResultSummaryObject -summaryText $summaryText
    $status = if ($play.failed -gt 0) { "Failed" } else { "Passed" }

    return [pscustomobject]@{
        gate = $gateName
        status = $status
        total = [int]$play.total
        blocking = [int]$play.failed
        warnings = [int]$play.skipped
        errors = [int]$play.failed
        csv = $resultsXmlPath
        note = "total=$($play.total) passed=$($play.passed) failed=$($play.failed) skipped=$($play.skipped)"
    }
}

function Write-GateMatrixReports(
    [System.Collections.Generic.List[object]]$rows,
    [string]$reportCsvPath,
    [string]$summaryMdPath,
    [string]$playModeSummary,
    [string]$resultsXmlPath
) {
    if ($null -eq $rows) {
        return
    }

    $csvDir = Split-Path -Parent $reportCsvPath
    if (![string]::IsNullOrWhiteSpace($csvDir)) {
        New-Item -ItemType Directory -Force -Path $csvDir | Out-Null
    }

    $rows | Export-Csv -Path $reportCsvPath -NoTypeInformation -Encoding UTF8

    $mdDir = Split-Path -Parent $summaryMdPath
    if (![string]::IsNullOrWhiteSpace($mdDir)) {
        New-Item -ItemType Directory -Force -Path $mdDir | Out-Null
    }

    $play = Parse-ResultSummaryObject -summaryText $playModeSummary
    $passedGates = @($rows | Where-Object { $_.status -eq "Passed" }).Count
    $failedGates = @($rows | Where-Object { $_.status -eq "Failed" }).Count
    $skippedGates = @($rows | Where-Object { $_.status -eq "Skipped" }).Count
    $unknownGates = @($rows | Where-Object { $_.status -eq "Unknown" -or $_.status -eq "Missing" }).Count

    $md = New-Object System.Text.StringBuilder
    $null = $md.AppendLine("# PlayMode Gate Matrix Summary")
    $null = $md.AppendLine()
    $null = $md.AppendLine("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $null = $md.AppendLine("- Gate Rows: $($rows.Count)")
    $null = $md.AppendLine("- Gate Passed: $passedGates")
    $null = $md.AppendLine("- Gate Failed: $failedGates")
    $null = $md.AppendLine("- Gate Skipped: $skippedGates")
    $null = $md.AppendLine("- Gate Unknown/Missing: $unknownGates")
    $null = $md.AppendLine("- PlayMode Tests: total=$($play.total) passed=$($play.passed) failed=$($play.failed) skipped=$($play.skipped)")
    $null = $md.AppendLine("- Result XML: $resultsXmlPath")
    $null = $md.AppendLine("- CSV: $reportCsvPath")
    $null = $md.AppendLine()
    $null = $md.AppendLine("| Gate | Status | Blocking | Warnings | Errors | Note |")
    $null = $md.AppendLine("|---|---|---:|---:|---:|---|")

    foreach ($row in $rows) {
        $gateCell = ([string]$row.gate).Replace("|", "\|")
        $statusCell = ([string]$row.status).Replace("|", "\|")
        $noteCell = ([string]$row.note).Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
        $null = $md.Append("| ").Append($gateCell).Append(" | ")
        $null = $md.Append($statusCell).Append(" | ")
        $null = $md.Append($row.blocking).Append(" | ")
        $null = $md.Append($row.warnings).Append(" | ")
        $null = $md.Append($row.errors).Append(" | ")
        $null = $md.Append($noteCell).AppendLine(" |")
    }

    [System.IO.File]::WriteAllText($summaryMdPath, $md.ToString(), [System.Text.UTF8Encoding]::new($false))
    Write-Host "[PlayModeBatch] gate-matrix csv: $reportCsvPath"
    Write-Host "[PlayModeBatch] gate-matrix summary: $summaryMdPath"
}

function Write-GateMatrixCiFailureSummary(
    [System.Collections.Generic.List[object]]$blockingRows,
    [string]$summaryMdPath,
    [string]$gateMatrixCsvPath,
    [string]$gateMatrixSummaryPath,
    [string]$playModeSummary,
    [string]$resultsXmlPath
) {
    $mdDir = Split-Path -Parent $summaryMdPath
    if (![string]::IsNullOrWhiteSpace($mdDir)) {
        New-Item -ItemType Directory -Force -Path $mdDir | Out-Null
    }

    $md = New-Object System.Text.StringBuilder
    $null = $md.AppendLine("# PlayMode Gate Matrix CI Failure Summary")
    $null = $md.AppendLine()
    $null = $md.AppendLine("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $null = $md.AppendLine("- PlayMode Summary: $playModeSummary")
    $null = $md.AppendLine("- Result XML: $resultsXmlPath")
    $null = $md.AppendLine("- Gate Matrix CSV: $gateMatrixCsvPath")
    $null = $md.AppendLine("- Gate Matrix Markdown: $gateMatrixSummaryPath")

    if ($null -eq $blockingRows -or $blockingRows.Count -eq 0) {
        $null = $md.AppendLine("- Blocking Rows: 0")
        $null = $md.AppendLine()
        $null = $md.AppendLine("All hard-gate rows passed.")
    }
    else {
        $null = $md.AppendLine("- Blocking Rows: $($blockingRows.Count)")
        $null = $md.AppendLine()
        $null = $md.AppendLine("| Gate | Status | Blocking | Errors | Note |")
        $null = $md.AppendLine("|---|---|---:|---:|---|")
        foreach ($row in $blockingRows) {
            $gateCell = ([string]$row.gate).Replace("|", "\|")
            $statusCell = ([string]$row.status).Replace("|", "\|")
            $noteCell = ([string]$row.note).Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
            $null = $md.Append("| ").Append($gateCell).Append(" | ")
            $null = $md.Append($statusCell).Append(" | ")
            $null = $md.Append($row.blocking).Append(" | ")
            $null = $md.Append($row.errors).Append(" | ")
            $null = $md.Append($noteCell).AppendLine(" |")
        }
    }

    [System.IO.File]::WriteAllText($summaryMdPath, $md.ToString(), [System.Text.UTF8Encoding]::new($false))
    Write-Host "[PlayModeBatch] gate-matrix CI summary: $summaryMdPath"
}

$projectPathResolved = (Resolve-Path $ProjectPath).Path
$unityExe = Resolve-UnityPath -projectPath $projectPathResolved -explicitUnityPath $UnityPath

if ([string]::IsNullOrWhiteSpace($EnemyTypeSceneGateScript)) {
    $EnemyTypeSceneGateScript = Join-Path $projectPathResolved "Assets\ThirdPersonController\Reports\run_enemy_type_scene_gate.ps1"
}

if ([string]::IsNullOrWhiteSpace($EnemyTypeSceneGateLogFile)) {
    $EnemyTypeSceneGateLogFile = Join-Path $projectPathResolved "Logs\EnemyTypeSceneGate.log"
}

if ([string]::IsNullOrWhiteSpace($BossStrictDrillGateScript)) {
    $BossStrictDrillGateScript = Join-Path $projectPathResolved "Assets\ThirdPersonController\Reports\run_boss_choreography_strict_gate_drill.ps1"
}

if ([string]::IsNullOrWhiteSpace($BossStrictDrillGateReport)) {
    $BossStrictDrillGateReport = Join-Path $projectPathResolved "Assets\ThirdPersonController\Reports\boss_choreography_strict_gate_drill_round8_report_2026-03-19.md"
}

$isFilteredTestRun = (-not [string]::IsNullOrWhiteSpace($TestFilter)) -or (-not [string]::IsNullOrWhiteSpace($AssemblyFilter))
$skipMutatingApplyPasses = $ValidateOnly.IsPresent
if ($skipMutatingApplyPasses) {
    Write-Host "[PlayModeBatch] validate-only mode enabled: skip mutating apply/fix passes."
}

$shouldRunBossStrictDrillGate = (-not $SkipBossStrictDrillGate.IsPresent) -and (-not $isFilteredTestRun)
if ($RunBossStrictDrillGate.IsPresent) {
    $shouldRunBossStrictDrillGate = $true
}

$resultsDir = Split-Path -Parent $ResultsXml
if (![string]::IsNullOrWhiteSpace($resultsDir)) {
    New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
}

$logDir = Split-Path -Parent $LogFile
if (![string]::IsNullOrWhiteSpace($logDir)) {
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
}

$warmupLogDir = Split-Path -Parent $WarmupLogFile
if (![string]::IsNullOrWhiteSpace($warmupLogDir)) {
    New-Item -ItemType Directory -Force -Path $warmupLogDir | Out-Null
}

$levelContentApplyLogDir = Split-Path -Parent $LevelContentApplyLogFile
if (![string]::IsNullOrWhiteSpace($levelContentApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelContentApplyLogDir | Out-Null
}

$levelContentLogDir = Split-Path -Parent $LevelContentLogFile
if (![string]::IsNullOrWhiteSpace($levelContentLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelContentLogDir | Out-Null
}

$levelCombatDensityApplyLogDir = Split-Path -Parent $LevelCombatDensityApplyLogFile
if (![string]::IsNullOrWhiteSpace($levelCombatDensityApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelCombatDensityApplyLogDir | Out-Null
}

$levelCombatDensityValidateLogDir = Split-Path -Parent $LevelCombatDensityValidateLogFile
if (![string]::IsNullOrWhiteSpace($levelCombatDensityValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelCombatDensityValidateLogDir | Out-Null
}

$levelDataSceneApplyLogDir = Split-Path -Parent $LevelDataSceneApplyLogFile
if (![string]::IsNullOrWhiteSpace($levelDataSceneApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelDataSceneApplyLogDir | Out-Null
}

$levelDataSceneValidateLogDir = Split-Path -Parent $LevelDataSceneValidateLogFile
if (![string]::IsNullOrWhiteSpace($levelDataSceneValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelDataSceneValidateLogDir | Out-Null
}

$levelBeatProgressionApplyLogDir = Split-Path -Parent $LevelBeatProgressionApplyLogFile
if (![string]::IsNullOrWhiteSpace($levelBeatProgressionApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelBeatProgressionApplyLogDir | Out-Null
}

$levelBeatProgressionValidateLogDir = Split-Path -Parent $LevelBeatProgressionValidateLogFile
if (![string]::IsNullOrWhiteSpace($levelBeatProgressionValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelBeatProgressionValidateLogDir | Out-Null
}

$levelBeatSheetLogDir = Split-Path -Parent $LevelBeatSheetLogFile
if (![string]::IsNullOrWhiteSpace($levelBeatSheetLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelBeatSheetLogDir | Out-Null
}

$levelProgressionCurveLogDir = Split-Path -Parent $LevelProgressionCurveLogFile
if (![string]::IsNullOrWhiteSpace($levelProgressionCurveLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelProgressionCurveLogDir | Out-Null
}

$bossFlowCouplingLogDir = Split-Path -Parent $BossFlowCouplingLogFile
if (![string]::IsNullOrWhiteSpace($bossFlowCouplingLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossFlowCouplingLogDir | Out-Null
}

$bossEncounterRound3ApplyLogDir = Split-Path -Parent $BossEncounterRound3ApplyLogFile
if (![string]::IsNullOrWhiteSpace($bossEncounterRound3ApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossEncounterRound3ApplyLogDir | Out-Null
}

$bossEncounterRound3ValidateLogDir = Split-Path -Parent $BossEncounterRound3ValidateLogFile
if (![string]::IsNullOrWhiteSpace($bossEncounterRound3ValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossEncounterRound3ValidateLogDir | Out-Null
}

$bossPhaseAttackApplyLogDir = Split-Path -Parent $BossPhaseAttackApplyLogFile
if (![string]::IsNullOrWhiteSpace($bossPhaseAttackApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossPhaseAttackApplyLogDir | Out-Null
}

$bossPhaseAttackValidateLogDir = Split-Path -Parent $BossPhaseAttackValidateLogFile
if (![string]::IsNullOrWhiteSpace($bossPhaseAttackValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossPhaseAttackValidateLogDir | Out-Null
}

$bossChoreographyLogDir = Split-Path -Parent $BossChoreographyLogFile
if (![string]::IsNullOrWhiteSpace($bossChoreographyLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossChoreographyLogDir | Out-Null
}

$bossEncounterProfileCoverageApplyLogDir = Split-Path -Parent $BossEncounterProfileCoverageApplyLogFile
if (![string]::IsNullOrWhiteSpace($bossEncounterProfileCoverageApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossEncounterProfileCoverageApplyLogDir | Out-Null
}

$bossEncounterProfileCoverageValidateLogDir = Split-Path -Parent $BossEncounterProfileCoverageLogFile
if (![string]::IsNullOrWhiteSpace($bossEncounterProfileCoverageValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossEncounterProfileCoverageValidateLogDir | Out-Null
}

$bossAttackCsvApplyLogDir = Split-Path -Parent $BossAttackCsvApplyLogFile
if (![string]::IsNullOrWhiteSpace($bossAttackCsvApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossAttackCsvApplyLogDir | Out-Null
}

$bossAttackCsvValidateLogDir = Split-Path -Parent $BossAttackCsvValidateLogFile
if (![string]::IsNullOrWhiteSpace($bossAttackCsvValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $bossAttackCsvValidateLogDir | Out-Null
}

$inputRound3ApplyLogDir = Split-Path -Parent $InputRound3ApplyLogFile
if (![string]::IsNullOrWhiteSpace($inputRound3ApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $inputRound3ApplyLogDir | Out-Null
}

$inputRound3ValidateLogDir = Split-Path -Parent $InputRound3ValidateLogFile
if (![string]::IsNullOrWhiteSpace($inputRound3ValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $inputRound3ValidateLogDir | Out-Null
}

$inputMirrorValidateLogDir = Split-Path -Parent $InputMirrorValidateLogFile
if (![string]::IsNullOrWhiteSpace($inputMirrorValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $inputMirrorValidateLogDir | Out-Null
}

$uiCrossDeviceReadabilityLogDir = Split-Path -Parent $UICrossDeviceReadabilityLogFile
if (![string]::IsNullOrWhiteSpace($uiCrossDeviceReadabilityLogDir)) {
    New-Item -ItemType Directory -Force -Path $uiCrossDeviceReadabilityLogDir | Out-Null
}

$commentLogQualityLogDir = Split-Path -Parent $CommentLogQualityLogFile
if (![string]::IsNullOrWhiteSpace($commentLogQualityLogDir)) {
    New-Item -ItemType Directory -Force -Path $commentLogQualityLogDir | Out-Null
}

$combatFeedbackCoverageLogDir = Split-Path -Parent $CombatFeedbackCoverageLogFile
if (![string]::IsNullOrWhiteSpace($combatFeedbackCoverageLogDir)) {
    New-Item -ItemType Directory -Force -Path $combatFeedbackCoverageLogDir | Out-Null
}

$skillResourceGapApplyLogDir = Split-Path -Parent $SkillResourceGapApplyLogFile
if (![string]::IsNullOrWhiteSpace($skillResourceGapApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $skillResourceGapApplyLogDir | Out-Null
}

$skillResourceGapValidateLogDir = Split-Path -Parent $SkillResourceGapValidateLogFile
if (![string]::IsNullOrWhiteSpace($skillResourceGapValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $skillResourceGapValidateLogDir | Out-Null
}

$localizationCoverageApplyLogDir = Split-Path -Parent $LocalizationCoverageApplyLogFile
if (![string]::IsNullOrWhiteSpace($localizationCoverageApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $localizationCoverageApplyLogDir | Out-Null
}

$localizationCoverageLogDir = Split-Path -Parent $LocalizationCoverageLogFile
if (![string]::IsNullOrWhiteSpace($localizationCoverageLogDir)) {
    New-Item -ItemType Directory -Force -Path $localizationCoverageLogDir | Out-Null
}

$localizationPseudoLocLogDir = Split-Path -Parent $LocalizationPseudoLocLogFile
if (![string]::IsNullOrWhiteSpace($localizationPseudoLocLogDir)) {
    New-Item -ItemType Directory -Force -Path $localizationPseudoLocLogDir | Out-Null
}

$growthEconomyConfigApplyLogDir = Split-Path -Parent $GrowthEconomyConfigApplyLogFile
if (![string]::IsNullOrWhiteSpace($growthEconomyConfigApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $growthEconomyConfigApplyLogDir | Out-Null
}

$growthEconomyConfigValidateLogDir = Split-Path -Parent $GrowthEconomyConfigValidateLogFile
if (![string]::IsNullOrWhiteSpace($growthEconomyConfigValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $growthEconomyConfigValidateLogDir | Out-Null
}

$steamConfigEnsureLogDir = Split-Path -Parent $SteamConfigEnsureLogFile
if (![string]::IsNullOrWhiteSpace($steamConfigEnsureLogDir)) {
    New-Item -ItemType Directory -Force -Path $steamConfigEnsureLogDir | Out-Null
}

$steamRuntimeModeLogDir = Split-Path -Parent $SteamRuntimeModeLogFile
if (![string]::IsNullOrWhiteSpace($steamRuntimeModeLogDir)) {
    New-Item -ItemType Directory -Force -Path $steamRuntimeModeLogDir | Out-Null
}

$enemyTypeSceneGateLogDir = Split-Path -Parent $EnemyTypeSceneGateLogFile
if (![string]::IsNullOrWhiteSpace($enemyTypeSceneGateLogDir)) {
    New-Item -ItemType Directory -Force -Path $enemyTypeSceneGateLogDir | Out-Null
}

$bossStrictDrillGateReportDir = Split-Path -Parent $BossStrictDrillGateReport
if (![string]::IsNullOrWhiteSpace($bossStrictDrillGateReportDir)) {
    New-Item -ItemType Directory -Force -Path $bossStrictDrillGateReportDir | Out-Null
}

$inputRound3Timeout = if ($InputRound3TimeoutSeconds -gt 0) {
    $InputRound3TimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$inputMirrorTimeout = if ($InputMirrorTimeoutSeconds -gt 0) {
    $InputMirrorTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$uiCrossDeviceReadabilityTimeout = if ($UICrossDeviceReadabilityTimeoutSeconds -gt 0) {
    $UICrossDeviceReadabilityTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$commentLogQualityTimeout = if ($CommentLogQualityTimeoutSeconds -gt 0) {
    $CommentLogQualityTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$combatFeedbackCoverageTimeout = if ($CombatFeedbackCoverageTimeoutSeconds -gt 0) {
    $CombatFeedbackCoverageTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$skillResourceGapTimeout = if ($SkillResourceGapTimeoutSeconds -gt 0) {
    $SkillResourceGapTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$localizationCoverageTimeout = if ($LocalizationCoverageTimeoutSeconds -gt 0) {
    $LocalizationCoverageTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$localizationPseudoLocTimeout = if ($LocalizationPseudoLocTimeoutSeconds -gt 0) {
    $LocalizationPseudoLocTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$growthEconomyConfigTimeout = if ($GrowthEconomyConfigTimeoutSeconds -gt 0) {
    $GrowthEconomyConfigTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$steamConfigEnsureTimeout = if ($SteamConfigEnsureTimeoutSeconds -gt 0) {
    $SteamConfigEnsureTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$steamRuntimeModeTimeout = if ($SteamRuntimeModeTimeoutSeconds -gt 0) {
    $SteamRuntimeModeTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$levelContentTimeout = if ($LevelContentTimeoutSeconds -gt 0) {
    $LevelContentTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$levelCombatDensityTimeout = if ($LevelCombatDensityTimeoutSeconds -gt 0) {
    $LevelCombatDensityTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$levelDataSceneTimeout = if ($LevelDataSceneTimeoutSeconds -gt 0) {
    $LevelDataSceneTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$levelBeatProgressionTimeout = if ($LevelBeatProgressionTimeoutSeconds -gt 0) {
    $LevelBeatProgressionTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$levelBeatSheetTimeout = if ($LevelBeatSheetTimeoutSeconds -gt 0) {
    $LevelBeatSheetTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$levelProgressionCurveTimeout = if ($LevelProgressionCurveTimeoutSeconds -gt 0) {
    $LevelProgressionCurveTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$bossFlowCouplingTimeout = if ($BossFlowCouplingTimeoutSeconds -gt 0) {
    $BossFlowCouplingTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$bossEncounterRound3Timeout = if ($BossEncounterRound3TimeoutSeconds -gt 0) {
    $BossEncounterRound3TimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$bossPhaseAttackTimeout = if ($BossPhaseAttackTimeoutSeconds -gt 0) {
    $BossPhaseAttackTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$bossChoreographyTimeout = if ($BossChoreographyTimeoutSeconds -gt 0) {
    $BossChoreographyTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$bossEncounterProfileCoverageTimeout = if ($BossEncounterProfileCoverageTimeoutSeconds -gt 0) {
    $BossEncounterProfileCoverageTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$bossAttackCsvTimeout = if ($BossAttackCsvTimeoutSeconds -gt 0) {
    $BossAttackCsvTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$enemyTypeGateTimeout = if ($EnemyTypeSceneGateTimeoutSeconds -gt 0) {
    $EnemyTypeSceneGateTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

$bossStrictDrillGateTimeout = if ($BossStrictDrillGateTimeoutSeconds -gt 0) {
    $BossStrictDrillGateTimeoutSeconds
}
else {
    $ProcessTimeoutSeconds
}

if (-not $SkipLevelDataSceneGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] level-data apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] level-data apply method=`"$LevelDataSceneApplyMethod`" unity=`"$unityExe`""
        $levelApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $LevelDataSceneApplyMethod `
            -logFile $LevelDataSceneApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $levelDataSceneTimeout

        if ($levelApplyExit -eq 124) {
            throw "LevelData scene apply timed out after $levelDataSceneTimeout s. See log: $LevelDataSceneApplyLogFile"
        }

        if ($levelApplyExit -ne 0) {
            throw "LevelData scene apply failed (exit=$levelApplyExit). See log: $LevelDataSceneApplyLogFile"
        }

        $levelApplySummary = Get-CsvStatusSummary -csvPath $LevelDataSceneReportCsv
        Write-Host "[PlayModeBatch] level-data apply summary: $(Format-CsvStatusSummary -summary $levelApplySummary)"
        if (-not $levelApplySummary.Exists) {
            throw "LevelData scene report missing after apply: $LevelDataSceneReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $levelApplySummary) -gt 0) {
            throw "LevelData scene apply has blocking statuses. csv=$LevelDataSceneReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after level-data apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] level-data validate method=`"$LevelDataSceneValidateMethod`" unity=`"$unityExe`""
    $levelValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LevelDataSceneValidateMethod `
        -logFile $LevelDataSceneValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $levelDataSceneTimeout

    if ($levelValidateExit -eq 124) {
        throw "LevelData scene validate timed out after $levelDataSceneTimeout s. See log: $LevelDataSceneValidateLogFile"
    }

    if ($levelValidateExit -ne 0) {
        throw "LevelData scene validate failed (exit=$levelValidateExit). See log: $LevelDataSceneValidateLogFile"
    }

    $levelValidateSummary = Get-CsvStatusSummary -csvPath $LevelDataSceneReportCsv
    Write-Host "[PlayModeBatch] level-data validate summary: $(Format-CsvStatusSummary -summary $levelValidateSummary)"
    if (-not $levelValidateSummary.Exists) {
        throw "LevelData scene report missing after validate: $LevelDataSceneReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $levelValidateSummary) -gt 0) {
        throw "LevelData scene validate has blocking statuses. csv=$LevelDataSceneReportCsv"
    }
}

if (-not $SkipBossFlowCouplingGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] boss-flow-coupling validate method=`"$BossFlowCouplingValidateMethod`" unity=`"$unityExe`""
    $bossFlowCouplingExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $BossFlowCouplingValidateMethod `
        -logFile $BossFlowCouplingLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $bossFlowCouplingTimeout

    if ($bossFlowCouplingExit -eq 124) {
        throw "Boss flow coupling validate timed out after $bossFlowCouplingTimeout s. See log: $BossFlowCouplingLogFile"
    }

    if ($bossFlowCouplingExit -ne 0) {
        throw "Boss flow coupling validate failed (exit=$bossFlowCouplingExit). See log: $BossFlowCouplingLogFile"
    }

    $bossFlowCouplingSummary = Get-CsvStatusSummary -csvPath $BossFlowCouplingReportCsv
    Write-Host "[PlayModeBatch] boss-flow-coupling summary: $(Format-CsvStatusSummary -summary $bossFlowCouplingSummary)"
    if (-not $bossFlowCouplingSummary.Exists) {
        throw "Boss flow coupling report missing: $BossFlowCouplingReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $bossFlowCouplingSummary) -gt 0) {
        throw "Boss flow coupling gate has blocking statuses. csv=$BossFlowCouplingReportCsv"
    }
}

if (-not $SkipBossEncounterRound3Gate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] boss-round3 apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] boss-round3 apply method=`"$BossEncounterRound3ApplyMethod`" unity=`"$unityExe`""
        $bossRound3ApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $BossEncounterRound3ApplyMethod `
            -logFile $BossEncounterRound3ApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $bossEncounterRound3Timeout

        if ($bossRound3ApplyExit -eq 124) {
            throw "Boss round3 apply timed out after $bossEncounterRound3Timeout s. See log: $BossEncounterRound3ApplyLogFile"
        }

        if ($bossRound3ApplyExit -ne 0) {
            throw "Boss round3 apply failed (exit=$bossRound3ApplyExit). See log: $BossEncounterRound3ApplyLogFile"
        }

        $bossRound3ApplySummary = Get-CsvStatusSummary -csvPath $BossEncounterRound3ReportCsv
        Write-Host "[PlayModeBatch] boss-round3 apply summary: $(Format-CsvStatusSummary -summary $bossRound3ApplySummary)"
        if (-not $bossRound3ApplySummary.Exists) {
            throw "Boss round3 report missing after apply: $BossEncounterRound3ReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $bossRound3ApplySummary) -gt 0) {
            throw "Boss round3 apply has blocking statuses. csv=$BossEncounterRound3ReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after boss round3 apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] boss-round3 validate method=`"$BossEncounterRound3ValidateMethod`" unity=`"$unityExe`""
    $bossRound3ValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $BossEncounterRound3ValidateMethod `
        -logFile $BossEncounterRound3ValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $bossEncounterRound3Timeout

    if ($bossRound3ValidateExit -eq 124) {
        throw "Boss round3 validate timed out after $bossEncounterRound3Timeout s. See log: $BossEncounterRound3ValidateLogFile"
    }

    if ($bossRound3ValidateExit -ne 0) {
        throw "Boss round3 validate failed (exit=$bossRound3ValidateExit). See log: $BossEncounterRound3ValidateLogFile"
    }

    $bossRound3ValidateSummary = Get-CsvStatusSummary -csvPath $BossEncounterRound3ReportCsv
    Write-Host "[PlayModeBatch] boss-round3 summary: $(Format-CsvStatusSummary -summary $bossRound3ValidateSummary)"
    if (-not $bossRound3ValidateSummary.Exists) {
        throw "Boss round3 report missing after validate: $BossEncounterRound3ReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $bossRound3ValidateSummary) -gt 0) {
        throw "Boss round3 gate has blocking statuses. csv=$BossEncounterRound3ReportCsv"
    }
}

if (-not $SkipBossPhaseAttackGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] boss-phase-attack apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] boss-phase-attack apply method=`"$BossPhaseAttackApplyMethod`" unity=`"$unityExe`""
        $bossPhaseApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $BossPhaseAttackApplyMethod `
            -logFile $BossPhaseAttackApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $bossPhaseAttackTimeout

        if ($bossPhaseApplyExit -eq 124) {
            throw "Boss phase attack apply timed out after $bossPhaseAttackTimeout s. See log: $BossPhaseAttackApplyLogFile"
        }

        if ($bossPhaseApplyExit -ne 0) {
            throw "Boss phase attack apply failed (exit=$bossPhaseApplyExit). See log: $BossPhaseAttackApplyLogFile"
        }

        $bossPhaseApplySummary = Get-CsvStatusSummary -csvPath $BossPhaseAttackReportCsv
        Write-Host "[PlayModeBatch] boss-phase-attack apply summary: $(Format-CsvStatusSummary -summary $bossPhaseApplySummary)"
        if (-not $bossPhaseApplySummary.Exists) {
            throw "Boss phase attack report missing after apply: $BossPhaseAttackReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $bossPhaseApplySummary) -gt 0) {
            throw "Boss phase attack apply has blocking statuses. csv=$BossPhaseAttackReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after boss phase attack apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] boss-phase-attack validate method=`"$BossPhaseAttackValidateMethod`" unity=`"$unityExe`""
    $bossPhaseValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $BossPhaseAttackValidateMethod `
        -logFile $BossPhaseAttackValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $bossPhaseAttackTimeout

    if ($bossPhaseValidateExit -eq 124) {
        throw "Boss phase attack validate timed out after $bossPhaseAttackTimeout s. See log: $BossPhaseAttackValidateLogFile"
    }

    if ($bossPhaseValidateExit -ne 0) {
        throw "Boss phase attack validate failed (exit=$bossPhaseValidateExit). See log: $BossPhaseAttackValidateLogFile"
    }

    $bossPhaseValidateSummary = Get-CsvStatusSummary -csvPath $BossPhaseAttackReportCsv
    Write-Host "[PlayModeBatch] boss-phase-attack summary: $(Format-CsvStatusSummary -summary $bossPhaseValidateSummary)"
    if (-not $bossPhaseValidateSummary.Exists) {
        throw "Boss phase attack report missing after validate: $BossPhaseAttackReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $bossPhaseValidateSummary) -gt 0) {
        throw "Boss phase attack gate has blocking statuses. csv=$BossPhaseAttackReportCsv"
    }
}

if (-not $SkipBossChoreographyGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] boss-choreography validate method=`"$BossChoreographyValidateMethod`" unity=`"$unityExe`""
    $bossChoreographyExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $BossChoreographyValidateMethod `
        -logFile $BossChoreographyLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $bossChoreographyTimeout

    if ($bossChoreographyExit -eq 124) {
        throw "Boss choreography validate timed out after $bossChoreographyTimeout s. See log: $BossChoreographyLogFile"
    }

    if ($bossChoreographyExit -ne 0) {
        throw "Boss choreography validate failed (exit=$bossChoreographyExit). See log: $BossChoreographyLogFile"
    }

    $bossChoreographySummary = Get-CsvStatusSummary -csvPath $BossChoreographyReportCsv
    Write-Host "[PlayModeBatch] boss-choreography summary: $(Format-CsvStatusSummary -summary $bossChoreographySummary)"
    if (-not $bossChoreographySummary.Exists) {
        throw "Boss choreography report missing: $BossChoreographyReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $bossChoreographySummary) -gt 0) {
        throw "Boss choreography gate has blocking statuses. csv=$BossChoreographyReportCsv"
    }
}

if (-not $SkipBossEncounterProfileCoverageGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] boss-encounter-profile-coverage apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] boss-encounter-profile-coverage apply method=`"$BossEncounterProfileCoverageApplyMethod`" unity=`"$unityExe`""
        $bossEncounterProfileCoverageApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $BossEncounterProfileCoverageApplyMethod `
            -logFile $BossEncounterProfileCoverageApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $bossEncounterProfileCoverageTimeout

        if ($bossEncounterProfileCoverageApplyExit -eq 124) {
            throw "Boss encounter profile coverage apply timed out after $bossEncounterProfileCoverageTimeout s. See log: $BossEncounterProfileCoverageApplyLogFile"
        }

        if ($bossEncounterProfileCoverageApplyExit -ne 0) {
            throw "Boss encounter profile coverage apply failed (exit=$bossEncounterProfileCoverageApplyExit). See log: $BossEncounterProfileCoverageApplyLogFile"
        }

        $bossEncounterProfileCoverageApplySummary = Get-CsvStatusSummary -csvPath $BossEncounterProfileCoverageReportCsv
        Write-Host "[PlayModeBatch] boss-encounter-profile-coverage apply summary: $(Format-CsvStatusSummary -summary $bossEncounterProfileCoverageApplySummary)"
        if (-not $bossEncounterProfileCoverageApplySummary.Exists) {
            throw "Boss encounter profile coverage report missing after apply: $BossEncounterProfileCoverageReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $bossEncounterProfileCoverageApplySummary) -gt 0) {
            throw "Boss encounter profile coverage apply has blocking statuses. csv=$BossEncounterProfileCoverageReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after boss encounter profile coverage apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] boss-encounter-profile-coverage validate method=`"$BossEncounterProfileCoverageValidateMethod`" unity=`"$unityExe`""
    $bossEncounterProfileCoverageExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $BossEncounterProfileCoverageValidateMethod `
        -logFile $BossEncounterProfileCoverageLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $bossEncounterProfileCoverageTimeout

    if ($bossEncounterProfileCoverageExit -eq 124) {
        throw "Boss encounter profile coverage validate timed out after $bossEncounterProfileCoverageTimeout s. See log: $BossEncounterProfileCoverageLogFile"
    }

    if ($bossEncounterProfileCoverageExit -ne 0) {
        throw "Boss encounter profile coverage validate failed (exit=$bossEncounterProfileCoverageExit). See log: $BossEncounterProfileCoverageLogFile"
    }

    $bossEncounterProfileCoverageValidateSummary = Get-CsvStatusSummary -csvPath $BossEncounterProfileCoverageReportCsv
    Write-Host "[PlayModeBatch] boss-encounter-profile-coverage summary: $(Format-CsvStatusSummary -summary $bossEncounterProfileCoverageValidateSummary)"
    if (-not $bossEncounterProfileCoverageValidateSummary.Exists) {
        throw "Boss encounter profile coverage report missing: $BossEncounterProfileCoverageReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $bossEncounterProfileCoverageValidateSummary) -gt 0) {
        throw "Boss encounter profile coverage gate has blocking statuses. csv=$BossEncounterProfileCoverageReportCsv"
    }
}

if (-not $SkipBossAttackCsvGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] boss-attack-csv apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] boss-attack-csv apply method=`"$BossAttackCsvApplyMethod`" unity=`"$unityExe`""
        $bossAttackCsvApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $BossAttackCsvApplyMethod `
            -logFile $BossAttackCsvApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $bossAttackCsvTimeout

        if ($bossAttackCsvApplyExit -eq 124) {
            throw "Boss attack csv apply timed out after $bossAttackCsvTimeout s. See log: $BossAttackCsvApplyLogFile"
        }

        if ($bossAttackCsvApplyExit -ne 0) {
            throw "Boss attack csv apply failed (exit=$bossAttackCsvApplyExit). See log: $BossAttackCsvApplyLogFile"
        }

        $bossAttackCsvApplySummary = Get-CsvStatusSummary -csvPath $BossAttackCsvReportCsv
        Write-Host "[PlayModeBatch] boss-attack-csv apply summary: $(Format-CsvStatusSummary -summary $bossAttackCsvApplySummary)"
        if (-not $bossAttackCsvApplySummary.Exists) {
            throw "Boss attack csv report missing after apply: $BossAttackCsvReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $bossAttackCsvApplySummary) -gt 0) {
            throw "Boss attack csv apply has blocking statuses. csv=$BossAttackCsvReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after boss attack csv apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] boss-attack-csv validate method=`"$BossAttackCsvValidateMethod`" unity=`"$unityExe`""
    $bossAttackCsvValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $BossAttackCsvValidateMethod `
        -logFile $BossAttackCsvValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $bossAttackCsvTimeout

    if ($bossAttackCsvValidateExit -eq 124) {
        throw "Boss attack csv validate timed out after $bossAttackCsvTimeout s. See log: $BossAttackCsvValidateLogFile"
    }

    if ($bossAttackCsvValidateExit -ne 0) {
        throw "Boss attack csv validate failed (exit=$bossAttackCsvValidateExit). See log: $BossAttackCsvValidateLogFile"
    }

    $bossAttackCsvValidateSummary = Get-CsvStatusSummary -csvPath $BossAttackCsvReportCsv
    Write-Host "[PlayModeBatch] boss-attack-csv summary: $(Format-CsvStatusSummary -summary $bossAttackCsvValidateSummary)"
    if (-not $bossAttackCsvValidateSummary.Exists) {
        throw "Boss attack csv report missing after validate: $BossAttackCsvReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $bossAttackCsvValidateSummary) -gt 0) {
        throw "Boss attack csv gate has blocking statuses. csv=$BossAttackCsvReportCsv"
    }
}

if (-not $SkipLevelContentGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] level-content apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] level-content apply method=`"$LevelContentApplyMethod`" unity=`"$unityExe`""
        $levelContentApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $LevelContentApplyMethod `
            -logFile $LevelContentApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $levelContentTimeout

        if ($levelContentApplyExit -eq 124) {
            throw "Level content apply timed out after $levelContentTimeout s. See log: $LevelContentApplyLogFile"
        }

        if ($levelContentApplyExit -ne 0) {
            throw "Level content apply failed (exit=$levelContentApplyExit). See log: $LevelContentApplyLogFile"
        }

        $levelContentApplySummary = Get-CsvStatusSummary -csvPath $LevelContentReportCsv
        Write-Host "[PlayModeBatch] level-content apply summary: $(Format-CsvStatusSummary -summary $levelContentApplySummary)"
        if (-not $levelContentApplySummary.Exists) {
            throw "Level content report missing after apply: $LevelContentReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $levelContentApplySummary) -gt 0) {
            throw "Level content apply has blocking statuses. csv=$LevelContentReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after level-content apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] level-content validate method=`"$LevelContentValidateMethod`" unity=`"$unityExe`""
    $levelContentExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LevelContentValidateMethod `
        -logFile $LevelContentLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $levelContentTimeout

    if ($levelContentExit -eq 124) {
        throw "Level content validate timed out after $levelContentTimeout s. See log: $LevelContentLogFile"
    }

    if ($levelContentExit -ne 0) {
        throw "Level content validate failed (exit=$levelContentExit). See log: $LevelContentLogFile"
    }

    $levelContentSummary = Get-CsvStatusSummary -csvPath $LevelContentReportCsv
    Write-Host "[PlayModeBatch] level-content summary: $(Format-CsvStatusSummary -summary $levelContentSummary)"
    if (-not $levelContentSummary.Exists) {
        throw "Level content report missing: $LevelContentReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $levelContentSummary) -gt 0) {
        throw "Level content gate has blocking statuses. csv=$LevelContentReportCsv"
    }
}

if (-not $SkipLevelCombatDensityGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] level-combat-density apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] level-combat-density apply method=`"$LevelCombatDensityApplyMethod`" unity=`"$unityExe`""
        $densityApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $LevelCombatDensityApplyMethod `
            -logFile $LevelCombatDensityApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $levelCombatDensityTimeout

        if ($densityApplyExit -eq 124) {
            throw "Level combat density apply timed out after $levelCombatDensityTimeout s. See log: $LevelCombatDensityApplyLogFile"
        }

        if ($densityApplyExit -ne 0) {
            throw "Level combat density apply failed (exit=$densityApplyExit). See log: $LevelCombatDensityApplyLogFile"
        }

        $densityApplySummary = Get-CsvStatusSummary -csvPath $LevelCombatDensityReportCsv
        Write-Host "[PlayModeBatch] level-combat-density apply summary: $(Format-CsvStatusSummary -summary $densityApplySummary)"
        if (-not $densityApplySummary.Exists) {
            throw "Level combat density report missing after apply: $LevelCombatDensityReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $densityApplySummary) -gt 0) {
            throw "Level combat density apply has blocking statuses. csv=$LevelCombatDensityReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after level-combat-density apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] level-combat-density validate method=`"$LevelCombatDensityValidateMethod`" unity=`"$unityExe`""
    $densityValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LevelCombatDensityValidateMethod `
        -logFile $LevelCombatDensityValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $levelCombatDensityTimeout

    if ($densityValidateExit -eq 124) {
        throw "Level combat density validate timed out after $levelCombatDensityTimeout s. See log: $LevelCombatDensityValidateLogFile"
    }

    if ($densityValidateExit -ne 0) {
        throw "Level combat density validate failed (exit=$densityValidateExit). See log: $LevelCombatDensityValidateLogFile"
    }

    $densityValidateSummary = Get-CsvStatusSummary -csvPath $LevelCombatDensityReportCsv
    Write-Host "[PlayModeBatch] level-combat-density summary: $(Format-CsvStatusSummary -summary $densityValidateSummary)"
    if (-not $densityValidateSummary.Exists) {
        throw "Level combat density report missing: $LevelCombatDensityReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $densityValidateSummary) -gt 0) {
        throw "Level combat density gate has blocking statuses. csv=$LevelCombatDensityReportCsv"
    }
}

if (-not $SkipLevelBeatProgressionGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] level-beat-progression apply skipped (validate-only mode)."
        $levelBeatProgressionApplySummary = Get-CsvStatusSummary -csvPath $LevelBeatProgressionReportCsv
        if ($levelBeatProgressionApplySummary.Exists) {
            Write-Host "[PlayModeBatch] level-beat-progression apply summary (from existing csv): $(Format-CsvStatusSummary -summary $levelBeatProgressionApplySummary)"
        }
    }
    else {
        Write-Host "[PlayModeBatch] level-beat-progression apply method=`"$LevelBeatProgressionApplyMethod`" unity=`"$unityExe`""
        $levelBeatProgressionApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $LevelBeatProgressionApplyMethod `
            -logFile $LevelBeatProgressionApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $levelBeatProgressionTimeout

        if ($levelBeatProgressionApplyExit -eq 124) {
            throw "Level beat progression apply timed out after $levelBeatProgressionTimeout s. See log: $LevelBeatProgressionApplyLogFile"
        }

        if ($levelBeatProgressionApplyExit -ne 0) {
            throw "Level beat progression apply failed (exit=$levelBeatProgressionApplyExit). See log: $LevelBeatProgressionApplyLogFile"
        }

        $levelBeatProgressionApplySummary = Get-CsvStatusSummary -csvPath $LevelBeatProgressionReportCsv
        Write-Host "[PlayModeBatch] level-beat-progression apply summary: $(Format-CsvStatusSummary -summary $levelBeatProgressionApplySummary)"
        if (-not $levelBeatProgressionApplySummary.Exists) {
            throw "Level beat progression report missing after apply: $LevelBeatProgressionReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $levelBeatProgressionApplySummary) -gt 0) {
            throw "Level beat progression apply has blocking statuses. csv=$LevelBeatProgressionReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after level-beat-progression apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] level-beat-progression validate method=`"$LevelBeatProgressionValidateMethod`" unity=`"$unityExe`""
    $levelBeatProgressionValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LevelBeatProgressionValidateMethod `
        -logFile $LevelBeatProgressionValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $levelBeatProgressionTimeout

    if ($levelBeatProgressionValidateExit -eq 124) {
        throw "Level beat progression validate timed out after $levelBeatProgressionTimeout s. See log: $LevelBeatProgressionValidateLogFile"
    }

    if ($levelBeatProgressionValidateExit -ne 0) {
        throw "Level beat progression validate failed (exit=$levelBeatProgressionValidateExit). See log: $LevelBeatProgressionValidateLogFile"
    }

    $levelBeatProgressionSummary = Get-CsvStatusSummary -csvPath $LevelBeatProgressionReportCsv
    Write-Host "[PlayModeBatch] level-beat-progression summary: $(Format-CsvStatusSummary -summary $levelBeatProgressionSummary)"
    if (-not $levelBeatProgressionSummary.Exists) {
        throw "Level beat progression report missing: $LevelBeatProgressionReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $levelBeatProgressionSummary) -gt 0) {
        throw "Level beat progression gate has blocking statuses. csv=$LevelBeatProgressionReportCsv"
    }
}

if (-not $SkipLevelBeatSheetGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] level-beat-sheet validate method=`"$LevelBeatSheetValidateMethod`" unity=`"$unityExe`""
    $levelBeatSheetExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LevelBeatSheetValidateMethod `
        -logFile $LevelBeatSheetLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $levelBeatSheetTimeout

    if ($levelBeatSheetExit -eq 124) {
        throw "Level beat sheet validate timed out after $levelBeatSheetTimeout s. See log: $LevelBeatSheetLogFile"
    }

    if ($levelBeatSheetExit -ne 0) {
        throw "Level beat sheet validate failed (exit=$levelBeatSheetExit). See log: $LevelBeatSheetLogFile"
    }

    $levelBeatSheetSummary = Get-CsvStatusSummary -csvPath $LevelBeatSheetReportCsv
    Write-Host "[PlayModeBatch] level-beat-sheet summary: $(Format-CsvStatusSummary -summary $levelBeatSheetSummary)"
    if (-not $levelBeatSheetSummary.Exists) {
        throw "Level beat sheet report missing: $LevelBeatSheetReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $levelBeatSheetSummary) -gt 0) {
        throw "Level beat sheet gate has blocking statuses. csv=$LevelBeatSheetReportCsv"
    }
}

if (-not $SkipLevelProgressionCurveGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] level-progression-curve validate method=`"$LevelProgressionCurveValidateMethod`" unity=`"$unityExe`""
    $levelProgressionCurveExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LevelProgressionCurveValidateMethod `
        -logFile $LevelProgressionCurveLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $levelProgressionCurveTimeout

    if ($levelProgressionCurveExit -eq 124) {
        throw "Level progression curve validate timed out after $levelProgressionCurveTimeout s. See log: $LevelProgressionCurveLogFile"
    }

    if ($levelProgressionCurveExit -ne 0) {
        throw "Level progression curve validate failed (exit=$levelProgressionCurveExit). See log: $LevelProgressionCurveLogFile"
    }

    $levelProgressionCurveSummary = Get-CsvStatusSummary -csvPath $LevelProgressionCurveReportCsv
    Write-Host "[PlayModeBatch] level-progression-curve summary: $(Format-CsvStatusSummary -summary $levelProgressionCurveSummary)"
    if (-not $levelProgressionCurveSummary.Exists) {
        throw "Level progression curve report missing: $LevelProgressionCurveReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $levelProgressionCurveSummary) -gt 0) {
        throw "Level progression curve gate has blocking statuses. csv=$LevelProgressionCurveReportCsv"
    }
}

if (-not $SkipInputRound3Gate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] input-round3 apply skipped (validate-only mode)."
        $sceneSummary = Get-CsvStatusSummary -csvPath $InputRound3SceneAuditCsv
        if ($sceneSummary.Exists) {
            Write-Host "[PlayModeBatch] input-round3 scene summary (from existing csv): $(Format-CsvStatusSummary -summary $sceneSummary)"
        }
    }
    else {
        Write-Host "[PlayModeBatch] input-round3 apply method=`"$InputRound3ApplyMethod`" unity=`"$unityExe`""
        $inputApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $InputRound3ApplyMethod `
            -logFile $InputRound3ApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $inputRound3Timeout

        if ($inputApplyExit -eq 124) {
            throw "Input round3 apply timed out after $inputRound3Timeout s. See log: $InputRound3ApplyLogFile"
        }

        if ($inputApplyExit -ne 0) {
            throw "Input round3 apply failed (exit=$inputApplyExit). See log: $InputRound3ApplyLogFile"
        }

        $sceneSummary = Get-CsvStatusSummary -csvPath $InputRound3SceneAuditCsv
        Write-Host "[PlayModeBatch] input-round3 scene summary: $(Format-CsvStatusSummary -summary $sceneSummary)"
        if (-not $sceneSummary.Exists) {
            throw "Input round3 scene audit missing: $InputRound3SceneAuditCsv"
        }

        if ((Get-CsvBlockingCount -summary $sceneSummary) -gt 0) {
            throw "Input round3 scene audit has blocking statuses after apply. csv=$InputRound3SceneAuditCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after input round3 apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] input-round3 validate method=`"$InputRound3ValidateMethod`" unity=`"$unityExe`""
    $inputValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $InputRound3ValidateMethod `
        -logFile $InputRound3ValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $inputRound3Timeout

    if ($inputValidateExit -eq 124) {
        throw "Input round3 validate timed out after $inputRound3Timeout s. See log: $InputRound3ValidateLogFile"
    }

    if ($inputValidateExit -ne 0) {
        throw "Input round3 validate failed (exit=$inputValidateExit). See log: $InputRound3ValidateLogFile"
    }

    $fullGateSummary = Get-CsvStatusSummary -csvPath $InputRound3FullGateCsv
    Write-Host "[PlayModeBatch] input-round3 full summary: $(Format-CsvStatusSummary -summary $fullGateSummary)"
    if (-not $fullGateSummary.Exists) {
        throw "Input round3 full gate audit missing: $InputRound3FullGateCsv"
    }

    if ((Get-CsvBlockingCount -summary $fullGateSummary) -gt 0) {
        throw "Input round3 full gate has blocking statuses. csv=$InputRound3FullGateCsv"
    }
}

if (-not $SkipInputMirrorGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] input-mirror validate method=`"$InputMirrorValidateMethod`" unity=`"$unityExe`""
    $inputMirrorExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $InputMirrorValidateMethod `
        -logFile $InputMirrorValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $inputMirrorTimeout

    if ($inputMirrorExit -eq 124) {
        throw "Input mirror validate timed out after $inputMirrorTimeout s. See log: $InputMirrorValidateLogFile"
    }

    if ($inputMirrorExit -ne 0) {
        throw "Input mirror validate failed (exit=$inputMirrorExit). See log: $InputMirrorValidateLogFile"
    }

    $inputMirrorSummary = Get-CsvStatusSummary -csvPath $InputMirrorReportCsv
    Write-Host "[PlayModeBatch] input-mirror summary: $(Format-CsvStatusSummary -summary $inputMirrorSummary)"
    if (-not $inputMirrorSummary.Exists) {
        throw "Input mirror report missing: $InputMirrorReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $inputMirrorSummary) -gt 0) {
        throw "Input mirror audit has blocking statuses. csv=$InputMirrorReportCsv"
    }
}

if (-not $SkipUICrossDeviceReadabilityGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] ui-cross-device-readability validate method=`"$UICrossDeviceReadabilityValidateMethod`" unity=`"$unityExe`""
    $uiCrossDeviceReadabilityExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $UICrossDeviceReadabilityValidateMethod `
        -logFile $UICrossDeviceReadabilityLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $uiCrossDeviceReadabilityTimeout

    if ($uiCrossDeviceReadabilityExit -eq 124) {
        throw "UI cross-device readability validate timed out after $uiCrossDeviceReadabilityTimeout s. See log: $UICrossDeviceReadabilityLogFile"
    }

    if ($uiCrossDeviceReadabilityExit -ne 0) {
        throw "UI cross-device readability validate failed (exit=$uiCrossDeviceReadabilityExit). See log: $UICrossDeviceReadabilityLogFile"
    }

    $uiCrossDeviceReadabilitySummary = Get-CsvStatusSummary -csvPath $UICrossDeviceReadabilityReportCsv
    Write-Host "[PlayModeBatch] ui-cross-device-readability summary: $(Format-CsvStatusSummary -summary $uiCrossDeviceReadabilitySummary)"
    if (-not $uiCrossDeviceReadabilitySummary.Exists) {
        throw "UI cross-device readability report missing: $UICrossDeviceReadabilityReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $uiCrossDeviceReadabilitySummary) -gt 0) {
        throw "UI cross-device readability gate has blocking statuses. csv=$UICrossDeviceReadabilityReportCsv"
    }
}

if (-not $SkipCommentLogQualityGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] comment-log-quality validate method=`"$CommentLogQualityValidateMethod`" unity=`"$unityExe`""
    $commentGateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $CommentLogQualityValidateMethod `
        -logFile $CommentLogQualityLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $commentLogQualityTimeout

    if ($commentGateExit -eq 124) {
        throw "Comment log quality gate timed out after $commentLogQualityTimeout s. See log: $CommentLogQualityLogFile"
    }

    if ($commentGateExit -ne 0) {
        throw "Comment log quality gate failed (exit=$commentGateExit). See log: $CommentLogQualityLogFile"
    }

    $commentSummary = Get-CommentLogQualitySummary -csvPath $CommentLogQualityReportCsv
    Write-Host "[PlayModeBatch] comment-log-quality summary: $(Format-CommentLogQualitySummary -summary $commentSummary)"
    if (-not $commentSummary.Exists) {
        throw "Comment log quality report missing: $CommentLogQualityReportCsv"
    }

    if ($commentSummary.Errors -gt 0) {
        throw "Comment log quality gate has blocking errors=$($commentSummary.Errors). csv=$CommentLogQualityReportCsv"
    }

    if ($CommentLogQualityWarningBudget -ge 0) {
        Write-Host "[PlayModeBatch] comment-log-quality warning-budget=$CommentLogQualityWarningBudget"
        if ($commentSummary.Warnings -gt $CommentLogQualityWarningBudget) {
            Write-Warning "[PlayModeBatch] comment-log-quality warnings exceed budget: warnings=$($commentSummary.Warnings) budget=$CommentLogQualityWarningBudget (will be hard-gated in matrix)."
        }
    }
    else {
        Write-Host "[PlayModeBatch] comment-log-quality warning-budget=disabled"
    }
}

if (-not $SkipCombatFeedbackCoverageGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] combat-feedback-coverage validate method=`"$CombatFeedbackCoverageValidateMethod`" unity=`"$unityExe`""
    $combatFeedbackCoverageExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $CombatFeedbackCoverageValidateMethod `
        -logFile $CombatFeedbackCoverageLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $combatFeedbackCoverageTimeout

    if ($combatFeedbackCoverageExit -eq 124) {
        throw "Combat feedback coverage gate timed out after $combatFeedbackCoverageTimeout s. See log: $CombatFeedbackCoverageLogFile"
    }

    if ($combatFeedbackCoverageExit -ne 0) {
        throw "Combat feedback coverage gate failed (exit=$combatFeedbackCoverageExit). See log: $CombatFeedbackCoverageLogFile"
    }

    $combatFeedbackCoverageSummary = Get-CsvStatusSummary -csvPath $CombatFeedbackCoverageReportCsv
    Write-Host "[PlayModeBatch] combat-feedback-coverage summary: $(Format-CsvStatusSummary -summary $combatFeedbackCoverageSummary)"
    if (-not $combatFeedbackCoverageSummary.Exists) {
        throw "Combat feedback coverage report missing: $CombatFeedbackCoverageReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $combatFeedbackCoverageSummary) -gt 0) {
        throw "Combat feedback coverage gate has blocking statuses. csv=$CombatFeedbackCoverageReportCsv"
    }
}

if (-not $SkipSkillResourceGapGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] skill-resource-gap apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] skill-resource-gap apply method=`"$SkillResourceGapApplyMethod`" unity=`"$unityExe`""
        $skillResourceApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $SkillResourceGapApplyMethod `
            -logFile $SkillResourceGapApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $skillResourceGapTimeout

        if ($skillResourceApplyExit -eq 124) {
            throw "Skill resource gap apply timed out after $skillResourceGapTimeout s. See log: $SkillResourceGapApplyLogFile"
        }

        if ($skillResourceApplyExit -ne 0) {
            throw "Skill resource gap apply failed (exit=$skillResourceApplyExit). See log: $SkillResourceGapApplyLogFile"
        }

        $skillResourceApplySummary = Get-CsvStatusSummary -csvPath $SkillResourceGapReportCsv
        Write-Host "[PlayModeBatch] skill-resource-gap apply summary: $(Format-CsvStatusSummary -summary $skillResourceApplySummary)"
        if (-not $skillResourceApplySummary.Exists) {
            throw "Skill resource gap report missing after apply: $SkillResourceGapReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $skillResourceApplySummary) -gt 0) {
            throw "Skill resource gap apply has blocking statuses. csv=$SkillResourceGapReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after skill resource gap apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] skill-resource-gap validate method=`"$SkillResourceGapValidateMethod`" unity=`"$unityExe`""
    $skillResourceValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $SkillResourceGapValidateMethod `
        -logFile $SkillResourceGapValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $skillResourceGapTimeout

    if ($skillResourceValidateExit -eq 124) {
        throw "Skill resource gap validate timed out after $skillResourceGapTimeout s. See log: $SkillResourceGapValidateLogFile"
    }

    if ($skillResourceValidateExit -ne 0) {
        throw "Skill resource gap validate failed (exit=$skillResourceValidateExit). See log: $SkillResourceGapValidateLogFile"
    }

    $skillResourceValidateSummary = Get-CsvStatusSummary -csvPath $SkillResourceGapReportCsv
    Write-Host "[PlayModeBatch] skill-resource-gap summary: $(Format-CsvStatusSummary -summary $skillResourceValidateSummary)"
    if (-not $skillResourceValidateSummary.Exists) {
        throw "Skill resource gap report missing after validate: $SkillResourceGapReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $skillResourceValidateSummary) -gt 0) {
        throw "Skill resource gap gate has blocking statuses. csv=$SkillResourceGapReportCsv"
    }
}

if (-not $SkipLocalizationCoverageGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] localization-coverage apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] localization-coverage apply method=`"$LocalizationCoverageApplyMethod`" unity=`"$unityExe`""
        $localizationCoverageApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $LocalizationCoverageApplyMethod `
            -logFile $LocalizationCoverageApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $localizationCoverageTimeout

        if ($localizationCoverageApplyExit -eq 124) {
            throw "Localization coverage apply timed out after $localizationCoverageTimeout s. See log: $LocalizationCoverageApplyLogFile"
        }

        if ($localizationCoverageApplyExit -ne 0) {
            throw "Localization coverage apply failed (exit=$localizationCoverageApplyExit). See log: $LocalizationCoverageApplyLogFile"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after localization coverage apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] localization-coverage validate method=`"$LocalizationCoverageValidateMethod`" unity=`"$unityExe`""
    $localizationCoverageExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LocalizationCoverageValidateMethod `
        -logFile $LocalizationCoverageLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $localizationCoverageTimeout

    if ($localizationCoverageExit -eq 124) {
        throw "Localization coverage validate timed out after $localizationCoverageTimeout s. See log: $LocalizationCoverageLogFile"
    }

    if ($localizationCoverageExit -ne 0) {
        throw "Localization coverage validate failed (exit=$localizationCoverageExit). See log: $LocalizationCoverageLogFile"
    }

    $localizationCoverageSummary = Get-CsvStatusSummary -csvPath $LocalizationCoverageReportCsv
    Write-Host "[PlayModeBatch] localization-coverage summary: $(Format-CsvStatusSummary -summary $localizationCoverageSummary)"
    if (-not $localizationCoverageSummary.Exists) {
        throw "Localization coverage report missing: $LocalizationCoverageReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $localizationCoverageSummary) -gt 0) {
        throw "Localization coverage gate has blocking statuses. csv=$LocalizationCoverageReportCsv"
    }
}

if (-not $SkipLocalizationPseudoLocGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] localization-pseudoloc validate method=`"$LocalizationPseudoLocValidateMethod`" unity=`"$unityExe`""
    $localizationPseudoLocExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $LocalizationPseudoLocValidateMethod `
        -logFile $LocalizationPseudoLocLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $localizationPseudoLocTimeout

    if ($localizationPseudoLocExit -eq 124) {
        throw "Localization pseudo-loc validate timed out after $localizationPseudoLocTimeout s. See log: $LocalizationPseudoLocLogFile"
    }

    if ($localizationPseudoLocExit -ne 0) {
        throw "Localization pseudo-loc validate failed (exit=$localizationPseudoLocExit). See log: $LocalizationPseudoLocLogFile"
    }

    $localizationPseudoLocSummary = Get-CsvStatusSummary -csvPath $LocalizationPseudoLocReportCsv
    Write-Host "[PlayModeBatch] localization-pseudoloc summary: $(Format-CsvStatusSummary -summary $localizationPseudoLocSummary)"
    if (-not $localizationPseudoLocSummary.Exists) {
        throw "Localization pseudo-loc report missing: $LocalizationPseudoLocReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $localizationPseudoLocSummary) -gt 0) {
        throw "Localization pseudo-loc gate has blocking statuses. csv=$LocalizationPseudoLocReportCsv"
    }
}

if (-not $SkipGrowthEconomyConfigGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if ($skipMutatingApplyPasses) {
        Write-Host "[PlayModeBatch] growth-economy-config apply skipped (validate-only mode)."
    }
    else {
        Write-Host "[PlayModeBatch] growth-economy-config apply method=`"$GrowthEconomyConfigApplyMethod`" unity=`"$unityExe`""
        $growthEconomyConfigApplyExit = Invoke-UnityExecuteMethod `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $GrowthEconomyConfigApplyMethod `
            -logFile $GrowthEconomyConfigApplyLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $growthEconomyConfigTimeout

        if ($growthEconomyConfigApplyExit -eq 124) {
            throw "Growth economy config apply timed out after $growthEconomyConfigTimeout s. See log: $GrowthEconomyConfigApplyLogFile"
        }

        if ($growthEconomyConfigApplyExit -ne 0) {
            throw "Growth economy config apply failed (exit=$growthEconomyConfigApplyExit). See log: $GrowthEconomyConfigApplyLogFile"
        }

        $growthEconomyConfigApplySummary = Get-CsvStatusSummary -csvPath $GrowthEconomyConfigReportCsv
        Write-Host "[PlayModeBatch] growth-economy-config apply summary: $(Format-CsvStatusSummary -summary $growthEconomyConfigApplySummary)"
        if (-not $growthEconomyConfigApplySummary.Exists) {
            throw "Growth economy config report missing after apply: $GrowthEconomyConfigReportCsv"
        }

        if ((Get-CsvBlockingCount -summary $growthEconomyConfigApplySummary) -gt 0) {
            throw "Growth economy config apply has blocking statuses. csv=$GrowthEconomyConfigReportCsv"
        }

        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is still locked after growth economy config apply: $projectPathResolved"
        }
    }

    Write-Host "[PlayModeBatch] growth-economy-config validate method=`"$GrowthEconomyConfigValidateMethod`" unity=`"$unityExe`""
    $growthEconomyConfigValidateExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $GrowthEconomyConfigValidateMethod `
        -logFile $GrowthEconomyConfigValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $growthEconomyConfigTimeout

    if ($growthEconomyConfigValidateExit -eq 124) {
        throw "Growth economy config validate timed out after $growthEconomyConfigTimeout s. See log: $GrowthEconomyConfigValidateLogFile"
    }

    if ($growthEconomyConfigValidateExit -ne 0) {
        throw "Growth economy config validate failed (exit=$growthEconomyConfigValidateExit). See log: $GrowthEconomyConfigValidateLogFile"
    }

    $growthEconomyConfigSummary = Get-CsvStatusSummary -csvPath $GrowthEconomyConfigReportCsv
    Write-Host "[PlayModeBatch] growth-economy-config summary: $(Format-CsvStatusSummary -summary $growthEconomyConfigSummary)"
    if (-not $growthEconomyConfigSummary.Exists) {
        throw "Growth economy config report missing after validate: $GrowthEconomyConfigReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $growthEconomyConfigSummary) -gt 0) {
        throw "Growth economy config gate has blocking statuses. csv=$GrowthEconomyConfigReportCsv"
    }
}

if ((-not $SkipSteamRuntimeModeGate.IsPresent) -and (-not $SkipSteamConfigEnsure.IsPresent) -and (-not $skipMutatingApplyPasses)) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] steam-config ensure method=`"$SteamConfigEnsureMethod`" unity=`"$unityExe`""
    $steamConfigEnsureExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $SteamConfigEnsureMethod `
        -logFile $SteamConfigEnsureLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $steamConfigEnsureTimeout

    if ($steamConfigEnsureExit -eq 124) {
        throw "Steam config ensure timed out after $steamConfigEnsureTimeout s. See log: $SteamConfigEnsureLogFile"
    }

    if ($steamConfigEnsureExit -ne 0) {
        throw "Steam config ensure failed (exit=$steamConfigEnsureExit). See log: $SteamConfigEnsureLogFile"
    }

    $steamConfigEnsureSummary = Get-CsvStatusSummary -csvPath $SteamConfigEnsureReportCsv
    Write-Host "[PlayModeBatch] steam-config ensure summary: $(Format-CsvStatusSummary -summary $steamConfigEnsureSummary)"
    if (-not $steamConfigEnsureSummary.Exists) {
        throw "Steam config ensure report missing: $SteamConfigEnsureReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $steamConfigEnsureSummary) -gt 0) {
        throw "Steam config ensure has blocking statuses. csv=$SteamConfigEnsureReportCsv"
    }
}
elseif ((-not $SkipSteamRuntimeModeGate.IsPresent) -and (-not $SkipSteamConfigEnsure.IsPresent) -and $skipMutatingApplyPasses) {
    Write-Host "[PlayModeBatch] steam-config ensure skipped (validate-only mode)."
}

if (-not $SkipSteamRuntimeModeGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] steam-runtime-mode validate method=`"$SteamRuntimeModeValidateMethod`" unity=`"$unityExe`""
    $steamRuntimeModeExit = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $SteamRuntimeModeValidateMethod `
        -logFile $SteamRuntimeModeLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $steamRuntimeModeTimeout

    if ($steamRuntimeModeExit -eq 124) {
        throw "Steam runtime mode validate timed out after $steamRuntimeModeTimeout s. See log: $SteamRuntimeModeLogFile"
    }

    if ($steamRuntimeModeExit -ne 0) {
        throw "Steam runtime mode validate failed (exit=$steamRuntimeModeExit). See log: $SteamRuntimeModeLogFile"
    }

    $steamRuntimeModeSummary = Get-CsvStatusSummary -csvPath $SteamRuntimeModeReportCsv
    Write-Host "[PlayModeBatch] steam-runtime-mode summary: $(Format-CsvStatusSummary -summary $steamRuntimeModeSummary)"
    if (-not $steamRuntimeModeSummary.Exists) {
        throw "Steam runtime mode report missing: $SteamRuntimeModeReportCsv"
    }

    if ((Get-CsvBlockingCount -summary $steamRuntimeModeSummary) -gt 0) {
        throw "Steam runtime mode gate has blocking statuses. csv=$SteamRuntimeModeReportCsv"
    }
}

if (-not $SkipEnemyTypeSceneGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] enemy-type gate unity=`"$unityExe`""
    $gateExit = Invoke-EnemyTypeSceneGate `
        -gateScriptPath $EnemyTypeSceneGateScript `
        -gateLogFile $EnemyTypeSceneGateLogFile `
        -projectPath $projectPathResolved `
        -waitForProjectUnlockSeconds $WaitForProjectUnlockSeconds `
        -gateTimeoutSeconds $enemyTypeGateTimeout `
        -noGraphics:$NoGraphics

    if ($gateExit -eq 124) {
        throw "Enemy type scene gate timed out after $enemyTypeGateTimeout s. See log: $EnemyTypeSceneGateLogFile"
    }

    if ($gateExit -ne 0) {
        throw "Enemy type scene gate failed (exit=$gateExit). See log: $EnemyTypeSceneGateLogFile"
    }
}

if ($shouldRunBossStrictDrillGate) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    Write-Host "[PlayModeBatch] boss-strict-drill gate script=`"$BossStrictDrillGateScript`""
    $bossStrictDrillExit = Invoke-BossStrictDrillGate `
        -gateScriptPath $BossStrictDrillGateScript `
        -outputReportPath $BossStrictDrillGateReport `
        -projectPath $projectPathResolved `
        -waitForProjectUnlockSeconds $WaitForProjectUnlockSeconds `
        -gateTimeoutSeconds $bossStrictDrillGateTimeout `
        -noGraphics:$NoGraphics

    if ($bossStrictDrillExit -eq 124) {
        throw "Boss strict drill gate timed out after $bossStrictDrillGateTimeout s. See report: $BossStrictDrillGateReport"
    }

    if ($bossStrictDrillExit -ne 0) {
        throw "Boss strict drill gate failed (exit=$bossStrictDrillExit). See report: $BossStrictDrillGateReport"
    }

    if (!(Test-Path $BossStrictDrillGateReport)) {
        throw "Boss strict drill gate report missing: $BossStrictDrillGateReport"
    }

    Write-Host "[PlayModeBatch] boss-strict-drill report: $BossStrictDrillGateReport"
}

$attemptMax = [Math]::Max(0, $RetryCount) + 1
$lastExitCode = 3
$success = $false

for ($attempt = 1; $attempt -le $attemptMax; $attempt++) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    if (-not $SkipWarmupCompile.IsPresent) {
        Write-Host "[PlayModeBatch] warmup attempt=$attempt"
        $warmupExit = Invoke-WarmupCompile `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -warmupLogFile $WarmupLogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $ProcessTimeoutSeconds

        if ($warmupExit -eq 124) {
            throw "Warmup compile timed out after $ProcessTimeoutSeconds s. See log: $WarmupLogFile"
        }

        if ($warmupExit -ne 0) {
            throw "Warmup compile failed (exit=$warmupExit). See log: $WarmupLogFile"
        }
    }

    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is still locked after warmup: $projectPathResolved"
    }

    if (Test-Path $ResultsXml) {
        Remove-Item $ResultsXml -Force
    }

    Write-Host "[PlayModeBatch] run attempt=$attempt unity=`"$unityExe`""
    $lastExitCode = Invoke-PlayModeTests `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -resultsXml $ResultsXml `
        -logFile $LogFile `
        -testFilter $TestFilter `
        -assemblyFilter $AssemblyFilter `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $ProcessTimeoutSeconds

    if (Test-Path $ResultsXml) {
        $success = $true
        break
    }

    if ($lastExitCode -eq 124) {
        Write-Warning "[PlayModeBatch] run timed out after $ProcessTimeoutSeconds s (attempt=$attempt)."
    }
    elseif (Is-CompilationOnlyLog -logFilePath $LogFile) {
        Write-Warning "[PlayModeBatch] compile-only pass detected (attempt=$attempt), retrying."
    }
    else {
        Write-Warning "[PlayModeBatch] result xml missing after attempt=$attempt (exit=$lastExitCode)."
    }
}

if (-not $success) {
    throw "PlayMode batch run did not produce result xml: $ResultsXml (lastExit=$lastExitCode, log=$LogFile)"
}

$summary = Get-ResultSummary -resultsXmlPath $ResultsXml
Write-Host "[PlayModeBatch] result xml: $ResultsXml"
Write-Host "[PlayModeBatch] summary: $summary"
Write-Host "[PlayModeBatch] log file: $LogFile"

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P0BossGateReportCsv `
    -reportName "P0 Boss Depth Gate" `
    -matchTokens @("ThirdPersonController.Tests.BossP0CompositeGateTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P0QuestEconomyGateReportCsv `
    -reportName "P0 Quest Economy Gate" `
    -matchTokens @("ThirdPersonController.Tests.QuestEconomyP0RegressionTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P0InputHintGateReportCsv `
    -reportName "P0 Input Hint Gate" `
    -matchTokens @("ThirdPersonController.Tests.InputHintConsistencyP0RegressionTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P1BossQuestCouplingGateReportCsv `
    -reportName "P1 Boss Quest Coupling Gate" `
    -matchTokens @("ThirdPersonController.Tests.BossQuestCouplingRegressionTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P1BossClosureGateReportCsv `
    -reportName "P1 Boss Encounter Closure Gate" `
    -matchTokens @(
        "ThirdPersonController.Tests.BossEncounterClosureRegressionTests",
        "ThirdPersonController.Tests.BossResultFlowClosureRegressionTests"
    )

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P1SkillBoundaryGateReportCsv `
    -reportName "P1 Skill Boundary Gate" `
    -matchTokens @("ThirdPersonController.Tests.SkillBoundaryP1RegressionTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P1QuestEconomyMidLateGateReportCsv `
    -reportName "P1 Quest Economy Mid-Late Gate" `
    -matchTokens @("ThirdPersonController.Tests.QuestEconomyP1MidLateSimulationRegressionTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P2InputProductizationGateReportCsv `
    -reportName "P2 Input Productization Gate" `
    -matchTokens @("ThirdPersonController.Tests.InputProductizationRegressionTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P2UIReadabilityGateReportCsv `
    -reportName "P2 UI Readability Gate" `
    -matchTokens @("ThirdPersonController.Tests.UICrossDeviceReadabilityRegressionTests")

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P2LocalizationGateReportCsv `
    -reportName "P2 Localization Regression Gate" `
    -matchTokens @(
        "ThirdPersonController.Tests.LocalizationQualityGateTests",
        "ThirdPersonController.Tests.LocalizationServiceRegressionTests"
    )

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P2SteamRuntimeGateReportCsv `
    -reportName "P2 Steam Runtime Regression Gate" `
    -matchTokens @(
        "ThirdPersonController.Tests.SteamIntegrationStatusRegressionTests",
        "ThirdPersonController.Tests.SteamIntegrationBootstrapRegressionTests",
        "ThirdPersonController.Tests.SteamIntegrationConfigRegressionTests",
        "ThirdPersonController.Tests.SteamCloudSaveBridgeRegressionTests"
    )

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P3BossDepthGateReportCsv `
    -reportName "P3 Boss Depth Gate" `
    -matchTokens @(
        "ThirdPersonController.Tests.BossCombatDepthRegressionTests",
        "ThirdPersonController.Tests.BossLevel10GateRegressionTests",
        "ThirdPersonController.Tests.BossPhaseGrammarRegressionTests"
    )

Export-TestSubsetReport `
    -resultsXmlPath $ResultsXml `
    -reportCsvPath $P5GrowthEconomyGateReportCsv `
    -reportName "P5 Growth Economy Gate" `
    -matchTokens @("ThirdPersonController.Tests.GrowthEconomyP5RegressionTests")

$gateMatrixRows = New-Object System.Collections.Generic.List[object]
$gateMatrixRows.Add((New-GateMatrixRowFromPlayModeSummary -gateName "PlayMode Tests" -summaryText $summary -resultsXmlPath $ResultsXml))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "LevelData Scene Gate" -isSkipped $SkipLevelDataSceneGate.IsPresent -summaryVariableName "levelValidateSummary" -csvPath $LevelDataSceneReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Level Content Gate" -isSkipped $SkipLevelContentGate.IsPresent -summaryVariableName "levelContentSummary" -csvPath $LevelContentReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Level Combat Density Gate" -isSkipped $SkipLevelCombatDensityGate.IsPresent -summaryVariableName "densityValidateSummary" -csvPath $LevelCombatDensityReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Level Beat Progression Tuning Gate" -isSkipped $SkipLevelBeatProgressionGate.IsPresent -summaryVariableName "levelBeatProgressionSummary" -csvPath $LevelBeatProgressionReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Level Beat Sheet Gate" -isSkipped $SkipLevelBeatSheetGate.IsPresent -summaryVariableName "levelBeatSheetSummary" -csvPath $LevelBeatSheetReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Level Progression Curve Gate" -isSkipped $SkipLevelProgressionCurveGate.IsPresent -summaryVariableName "levelProgressionCurveSummary" -csvPath $LevelProgressionCurveReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Boss Flow Coupling Gate" -isSkipped $SkipBossFlowCouplingGate.IsPresent -summaryVariableName "bossFlowCouplingSummary" -csvPath $BossFlowCouplingReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Boss Encounter Round3 Gate" -isSkipped $SkipBossEncounterRound3Gate.IsPresent -summaryVariableName "bossRound3ValidateSummary" -csvPath $BossEncounterRound3ReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Boss Phase Attack Gate" -isSkipped $SkipBossPhaseAttackGate.IsPresent -summaryVariableName "bossPhaseValidateSummary" -csvPath $BossPhaseAttackReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Boss Choreography Coverage Gate" -isSkipped $SkipBossChoreographyGate.IsPresent -summaryVariableName "bossChoreographySummary" -csvPath $BossChoreographyReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Boss Encounter Profile Coverage Gate" -isSkipped $SkipBossEncounterProfileCoverageGate.IsPresent -summaryVariableName "bossEncounterProfileCoverageValidateSummary" -csvPath $BossEncounterProfileCoverageReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Boss Attack CSV Gate" -isSkipped $SkipBossAttackCsvGate.IsPresent -summaryVariableName "bossAttackCsvValidateSummary" -csvPath $BossAttackCsvReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Input Round3 Scene Gate" -isSkipped ($SkipInputRound3Gate.IsPresent -or $skipMutatingApplyPasses) -summaryVariableName "sceneSummary" -csvPath $InputRound3SceneAuditCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Input Round3 Full Gate" -isSkipped $SkipInputRound3Gate.IsPresent -summaryVariableName "fullGateSummary" -csvPath $InputRound3FullGateCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Input Mirror Gate" -isSkipped $SkipInputMirrorGate.IsPresent -summaryVariableName "inputMirrorSummary" -csvPath $InputMirrorReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "UI Cross-Device Readability Gate" -isSkipped $SkipUICrossDeviceReadabilityGate.IsPresent -summaryVariableName "uiCrossDeviceReadabilitySummary" -csvPath $UICrossDeviceReadabilityReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCommentSummary -gateName "Comment/Log Quality Gate" -isSkipped $SkipCommentLogQualityGate.IsPresent -summaryVariableName "commentSummary" -csvPath $CommentLogQualityReportCsv -warningBudget $CommentLogQualityWarningBudget))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Combat Feedback Coverage Gate" -isSkipped $SkipCombatFeedbackCoverageGate.IsPresent -summaryVariableName "combatFeedbackCoverageSummary" -csvPath $CombatFeedbackCoverageReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Skill Resource Gap Gate" -isSkipped $SkipSkillResourceGapGate.IsPresent -summaryVariableName "skillResourceValidateSummary" -csvPath $SkillResourceGapReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Localization Coverage Gate" -isSkipped $SkipLocalizationCoverageGate.IsPresent -summaryVariableName "localizationCoverageSummary" -csvPath $LocalizationCoverageReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Localization Pseudo-Loc Gate" -isSkipped $SkipLocalizationPseudoLocGate.IsPresent -summaryVariableName "localizationPseudoLocSummary" -csvPath $LocalizationPseudoLocReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Growth Economy Config Gate" -isSkipped $SkipGrowthEconomyConfigGate.IsPresent -summaryVariableName "growthEconomyConfigSummary" -csvPath $GrowthEconomyConfigReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Steam Config Provision Gate" -isSkipped ($SkipSteamRuntimeModeGate.IsPresent -or $SkipSteamConfigEnsure.IsPresent -or $skipMutatingApplyPasses) -summaryVariableName "steamConfigEnsureSummary" -csvPath $SteamConfigEnsureReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromCsvSummary -gateName "Steam Runtime Mode Gate" -isSkipped $SkipSteamRuntimeModeGate.IsPresent -summaryVariableName "steamRuntimeModeSummary" -csvPath $SteamRuntimeModeReportCsv))
$gateMatrixRows.Add((New-GateMatrixRowFromExitCode -gateName "Enemy Type Scene Gate" -isSkipped $SkipEnemyTypeSceneGate.IsPresent -exitCodeVariableName "gateExit" -evidencePath $EnemyTypeSceneGateLogFile))
$gateMatrixRows.Add((New-GateMatrixRowFromExitCode -gateName "Boss Strict Drill Gate" -isSkipped (-not $shouldRunBossStrictDrillGate) -exitCodeVariableName "bossStrictDrillExit" -evidencePath $BossStrictDrillGateReport))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P0 Boss Depth Subset" -csvPath $P0BossGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P0 Quest Economy Subset" -csvPath $P0QuestEconomyGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P0 Input Hint Subset" -csvPath $P0InputHintGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P1 Boss Quest Coupling Subset" -csvPath $P1BossQuestCouplingGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P1 Boss Encounter Closure Subset" -csvPath $P1BossClosureGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P1 Skill Boundary Subset" -csvPath $P1SkillBoundaryGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P1 Quest Economy Mid-Late Subset" -csvPath $P1QuestEconomyMidLateGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P2 Input Productization Subset" -csvPath $P2InputProductizationGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P2 UI Readability Subset" -csvPath $P2UIReadabilityGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P2 Localization Subset" -csvPath $P2LocalizationGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P2 Steam Runtime Subset" -csvPath $P2SteamRuntimeGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P3 Boss Depth Subset" -csvPath $P3BossDepthGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))
$gateMatrixRows.Add((New-GateMatrixRowFromTestSubset -gateName "P5 Growth Economy Subset" -csvPath $P5GrowthEconomyGateReportCsv -allowNoMatchesAsSkipped $isFilteredTestRun))

Write-GateMatrixReports `
    -rows $gateMatrixRows `
    -reportCsvPath $GateMatrixReportCsv `
    -summaryMdPath $GateMatrixSummaryMd `
    -playModeSummary $summary `
    -resultsXmlPath $ResultsXml

$hardGateRows = @($gateMatrixRows | Where-Object { $_.status -eq "Failed" -or $_.status -eq "Missing" -or $_.status -eq "Unknown" })
$hardGateList = New-Object System.Collections.Generic.List[object]
foreach ($row in $hardGateRows) {
    $hardGateList.Add($row)
}

Write-GateMatrixCiFailureSummary `
    -blockingRows $hardGateList `
    -summaryMdPath $GateMatrixCiFailureSummaryMd `
    -gateMatrixCsvPath $GateMatrixReportCsv `
    -gateMatrixSummaryPath $GateMatrixSummaryMd `
    -playModeSummary $summary `
    -resultsXmlPath $ResultsXml

if (-not $DisableGateMatrixHardFail.IsPresent -and $hardGateRows.Count -gt 0) {
    Write-Host "[PlayModeBatch] HARD-GATE failed: $($hardGateRows.Count) row(s) are Failed/Missing/Unknown."
    foreach ($row in $hardGateRows) {
        Write-Warning "[PlayModeBatch] HARD-GATE row: gate=$($row.gate) status=$($row.status) blocking=$($row.blocking) errors=$($row.errors) note=$($row.note)"
    }

    if ($lastExitCode -eq 0) {
        $lastExitCode = 2
    }
}
elseif ($DisableGateMatrixHardFail.IsPresent -and $hardGateRows.Count -gt 0) {
    Write-Warning "[PlayModeBatch] HARD-GATE detected failures but is disabled by switch: -DisableGateMatrixHardFail"
}
else {
    Write-Host "[PlayModeBatch] HARD-GATE passed (no Failed/Missing/Unknown rows)."
}

exit $lastExitCode
