param(
    [string]$ProjectPath = "C:\test\Shrimp",
    [string]$UnityPath = "",
    [string]$ApplyMethod = "ThirdPersonController.Editor.InputBindingRound3SceneTool.ApplySceneBindingsForBatch",
    [string]$ValidateMethod = "ThirdPersonController.Editor.InputBindingRound3SceneTool.ValidateFullGateForBatch",
    [string]$ApplyLogFile = "C:\test\Shrimp\Logs\InputBindingRound3Apply.log",
    [string]$ValidateLogFile = "C:\test\Shrimp\Logs\InputBindingRound3FullGate.log",
    [string]$SceneAuditCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_scene_audit.csv",
    [string]$FullGateCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_full_gate_audit.csv",
    [string]$PlayModeScript = "",
    [string]$PlayModeResultsXml = "C:\test\Shrimp\Logs\PlayModeBatchResults.xml",
    [string]$PlayModeLogFile = "C:\test\Shrimp\Logs\PlayModeBatchRunner.log",
    [string]$PlayModeWarmupLogFile = "C:\test\Shrimp\Logs\PlayModeBatchWarmup.log",
    [string]$SummaryReportMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_gate_summary.md",
    [int]$WaitForProjectUnlockSeconds = 30,
    [int]$UnityStepTimeoutSeconds = 1200,
    [int]$PlayModeTimeoutSeconds = 1800,
    [int]$PlayModeRetryCount = 1,
    [switch]$NoGraphics,
    [switch]$SkipPlayModeWarmup
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

    $process = Start-Process -FilePath $unityExe -ArgumentList $args -PassThru
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

function Get-CsvStatusSummary([string]$csvPath) {
    if (!(Test-Path $csvPath)) {
        return [ordered]@{
            Exists = $false
            Total = 0
            OK = 0
            Fixed = 0
            Mismatch = 0
            Skipped = 0
            Other = 0
        }
    }

    $rows = Import-Csv $csvPath
    $summary = [ordered]@{
        Exists = $true
        Total = $rows.Count
        OK = 0
        Fixed = 0
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

function Get-PlayModeSummary([string]$resultsXmlPath) {
    if (!(Test-Path $resultsXmlPath)) {
        return [ordered]@{
            Exists = $false
            Total = 0
            Passed = 0
            Failed = 0
            Skipped = 0
            Inconclusive = 0
        }
    }

    [xml]$doc = Get-Content $resultsXmlPath
    $run = $doc.SelectSingleNode("//test-run")
    if ($null -eq $run) {
        return [ordered]@{
            Exists = $false
            Total = 0
            Passed = 0
            Failed = 0
            Skipped = 0
            Inconclusive = 0
        }
    }

    return [ordered]@{
        Exists = $true
        Total = [int]$run.total
        Passed = [int]$run.passed
        Failed = [int]$run.failed
        Skipped = [int]$run.skipped
        Inconclusive = [int]$run.inconclusive
    }
}

function Format-CsvSummary([hashtable]$summary) {
    if ($null -eq $summary -or -not $summary.Exists) {
        return "missing"
    }

    return "total=$($summary.Total) ok=$($summary.OK) fixed=$($summary.Fixed) mismatch=$($summary.Mismatch) skipped=$($summary.Skipped) other=$($summary.Other)"
}

function Format-PlayModeSummary([hashtable]$summary) {
    if ($null -eq $summary -or -not $summary.Exists) {
        return "missing"
    }

    return "total=$($summary.Total) passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) inconclusive=$($summary.Inconclusive)"
}

function Write-GateSummaryReport(
    [string]$outputPath,
    [string]$status,
    [string]$failedStage,
    [string]$failureMessage,
    [string[]]$warnings,
    [string]$projectPath,
    [string]$unityExe,
    [int]$applyExitCode,
    [int]$validateExitCode,
    [int]$playModeExitCode,
    [hashtable]$sceneSummary,
    [hashtable]$fullGateSummary,
    [hashtable]$playModeSummary,
    [string]$applyLogFile,
    [string]$validateLogFile,
    [string]$playModeLogFile,
    [string]$sceneCsv,
    [string]$fullCsv,
    [string]$playModeXml
) {
    $dir = Split-Path -Parent $outputPath
    if (![string]::IsNullOrWhiteSpace($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Input Round3 Gate Summary")
    $lines.Add("")
    $lines.Add("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $lines.Add("- Status: $status")
    $lines.Add("- ProjectPath: $projectPath")
    $lines.Add("- Unity: $unityExe")
    if (![string]::IsNullOrWhiteSpace($failedStage)) {
        $lines.Add("- FailedStage: $failedStage")
    }
    if (![string]::IsNullOrWhiteSpace($failureMessage)) {
        $lines.Add("- Error: $failureMessage")
    }
    if ($null -ne $warnings -and $warnings.Count -gt 0) {
        $lines.Add("- Warnings: $($warnings.Count)")
    }

    $lines.Add("")
    $lines.Add("## Step Results")
    $lines.Add("| Step | ExitCode | Summary | Artifact |")
    $lines.Add("|---|---:|---|---|")
    $lines.Add("| ApplySceneBindingsForBatch | $applyExitCode | $(Format-CsvSummary -summary $sceneSummary) | $sceneCsv |")
    $lines.Add("| ValidateFullGateForBatch | $validateExitCode | $(Format-CsvSummary -summary $fullGateSummary) | $fullCsv |")
    $lines.Add("| PlayModeBatch | $playModeExitCode | $(Format-PlayModeSummary -summary $playModeSummary) | $playModeXml |")

    $lines.Add("")
    $lines.Add("## Logs")
    $lines.Add("- Apply Log: $applyLogFile")
    $lines.Add("- Validate Log: $validateLogFile")
    $lines.Add("- PlayMode Log: $playModeLogFile")

    if ($null -ne $warnings -and $warnings.Count -gt 0) {
        $lines.Add("")
        $lines.Add("## Warnings")
        foreach ($warning in $warnings) {
            if (![string]::IsNullOrWhiteSpace($warning)) {
                $lines.Add("- $warning")
            }
        }
    }

    Set-Content -Path $outputPath -Value $lines -Encoding UTF8
}

$projectPathResolved = (Resolve-Path $ProjectPath).Path
$unityExe = Resolve-UnityPath -projectPath $projectPathResolved -explicitUnityPath $UnityPath
if ([string]::IsNullOrWhiteSpace($PlayModeScript)) {
    $PlayModeScript = Join-Path $projectPathResolved "Assets\ThirdPersonController\Reports\run_playmode_batch_tests.ps1"
}

$dirs = @(
    (Split-Path -Parent $ApplyLogFile),
    (Split-Path -Parent $ValidateLogFile),
    (Split-Path -Parent $PlayModeLogFile),
    (Split-Path -Parent $PlayModeWarmupLogFile),
    (Split-Path -Parent $PlayModeResultsXml),
    (Split-Path -Parent $SummaryReportMd)
)

foreach ($dir in $dirs) {
    if (![string]::IsNullOrWhiteSpace($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
}

$status = "PASS"
$failedStage = ""
$failureMessage = ""
$currentStage = "Init"
$applyExitCode = -1
$validateExitCode = -1
$playModeExitCode = -1
$sceneSummary = [ordered]@{}
$fullGateSummary = [ordered]@{}
$playModeSummary = [ordered]@{}
$warnings = New-Object System.Collections.Generic.List[string]

try {
    if (!(Test-Path $PlayModeScript)) {
        throw "PlayMode batch script not found: $PlayModeScript"
    }

    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

    $currentStage = "Apply"
    Write-Host "[InputRound3Gate] step=apply method=$ApplyMethod unity=`"$unityExe`""
    $applyExitCode = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $ApplyMethod `
        -logFile $ApplyLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $UnityStepTimeoutSeconds

    if ($applyExitCode -eq 124) {
        throw "Apply step timed out after $UnityStepTimeoutSeconds s. Log: $ApplyLogFile"
    }
    if ($applyExitCode -ne 0) {
        throw "Apply step failed (exit=$applyExitCode). Log: $ApplyLogFile"
    }

    $sceneSummary = Get-CsvStatusSummary -csvPath $SceneAuditCsv
    Write-Host "[InputRound3Gate] apply summary: $(Format-CsvSummary -summary $sceneSummary)"
    if (-not $sceneSummary.Exists) {
        throw "Scene audit csv missing: $SceneAuditCsv"
    }
    if ($sceneSummary.Mismatch -gt 0) {
        throw "Scene audit still has mismatches after apply. csv=$SceneAuditCsv"
    }

    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is still locked before validate step: $projectPathResolved"
    }

    $currentStage = "Validate"
    Write-Host "[InputRound3Gate] step=validate method=$ValidateMethod unity=`"$unityExe`""
    $validateExitCode = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPathResolved `
        -executeMethod $ValidateMethod `
        -logFile $ValidateLogFile `
        -noGraphics:$NoGraphics `
        -timeoutSeconds $UnityStepTimeoutSeconds

    if ($validateExitCode -eq 124) {
        throw "Validate step timed out after $UnityStepTimeoutSeconds s. Log: $ValidateLogFile"
    }
    if ($validateExitCode -ne 0) {
        throw "Validate step failed (exit=$validateExitCode). Log: $ValidateLogFile"
    }

    $fullGateSummary = Get-CsvStatusSummary -csvPath $FullGateCsv
    Write-Host "[InputRound3Gate] full gate summary: $(Format-CsvSummary -summary $fullGateSummary)"
    if (-not $fullGateSummary.Exists) {
        throw "Full gate csv missing: $FullGateCsv"
    }
    if ($fullGateSummary.Mismatch -gt 0) {
        throw "Full gate has mismatches. csv=$FullGateCsv"
    }

    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is still locked before playmode step: $projectPathResolved"
    }

    $currentStage = "PlayMode"
    $powershellExe = Join-Path $env:WINDIR "System32\WindowsPowerShell\v1.0\powershell.exe"
    if (!(Test-Path $powershellExe)) {
        $powershellExe = "powershell.exe"
    }

    $playModeArgs = New-Object System.Collections.Generic.List[string]
    $playModeArgs.Add("-ExecutionPolicy")
    $playModeArgs.Add("Bypass")
    $playModeArgs.Add("-File")
    $playModeArgs.Add($PlayModeScript)
    $playModeArgs.Add("-ProjectPath")
    $playModeArgs.Add($projectPathResolved)
    $playModeArgs.Add("-UnityPath")
    $playModeArgs.Add($unityExe)
    $playModeArgs.Add("-ResultsXml")
    $playModeArgs.Add($PlayModeResultsXml)
    $playModeArgs.Add("-LogFile")
    $playModeArgs.Add($PlayModeLogFile)
    $playModeArgs.Add("-WarmupLogFile")
    $playModeArgs.Add($PlayModeWarmupLogFile)
    $playModeArgs.Add("-RetryCount")
    $playModeArgs.Add("$PlayModeRetryCount")
    $playModeArgs.Add("-WaitForProjectUnlockSeconds")
    $playModeArgs.Add("$WaitForProjectUnlockSeconds")
    $playModeArgs.Add("-ProcessTimeoutSeconds")
    $playModeArgs.Add("$PlayModeTimeoutSeconds")
    $playModeArgs.Add("-SkipEnemyTypeSceneGate")
    if ($NoGraphics.IsPresent) {
        $playModeArgs.Add("-NoGraphics")
    }
    if ($SkipPlayModeWarmup.IsPresent) {
        $playModeArgs.Add("-SkipWarmupCompile")
    }

    Write-Host "[InputRound3Gate] step=playmode script=`"$PlayModeScript`""
    $playModeExitCode = Invoke-ScriptProcess `
        -scriptHostExe $powershellExe `
        -arguments $playModeArgs `
        -timeoutSeconds $PlayModeTimeoutSeconds

    if ($playModeExitCode -eq 124) {
        throw "PlayMode step timed out after $PlayModeTimeoutSeconds s. Log: $PlayModeLogFile"
    }

    $playModeSummary = Get-PlayModeSummary -resultsXmlPath $PlayModeResultsXml
    Write-Host "[InputRound3Gate] playmode summary: $(Format-PlayModeSummary -summary $playModeSummary)"
    if (-not $playModeSummary.Exists) {
        throw "PlayMode result xml missing: $PlayModeResultsXml"
    }
    if ($playModeSummary.Failed -gt 0) {
        throw "PlayMode has failed tests: $($playModeSummary.Failed)"
    }

    if ($playModeExitCode -ne 0) {
        $warning = "PlayMode step returned exit=$playModeExitCode but result xml has failed=0. Treated as pass."
        $warnings.Add($warning)
        Write-Warning "[InputRound3Gate] $warning"
    }
}
catch {
    $status = "FAIL"
    $failedStage = $currentStage
    $failureMessage = $_.Exception.Message
}
finally {
    if ($sceneSummary.Count -eq 0) {
        $sceneSummary = Get-CsvStatusSummary -csvPath $SceneAuditCsv
    }
    if ($fullGateSummary.Count -eq 0) {
        $fullGateSummary = Get-CsvStatusSummary -csvPath $FullGateCsv
    }
    if ($playModeSummary.Count -eq 0) {
        $playModeSummary = Get-PlayModeSummary -resultsXmlPath $PlayModeResultsXml
    }

    Write-GateSummaryReport `
        -outputPath $SummaryReportMd `
        -status $status `
        -failedStage $failedStage `
        -failureMessage $failureMessage `
        -warnings $warnings `
        -projectPath $projectPathResolved `
        -unityExe $unityExe `
        -applyExitCode $applyExitCode `
        -validateExitCode $validateExitCode `
        -playModeExitCode $playModeExitCode `
        -sceneSummary $sceneSummary `
        -fullGateSummary $fullGateSummary `
        -playModeSummary $playModeSummary `
        -applyLogFile $ApplyLogFile `
        -validateLogFile $ValidateLogFile `
        -playModeLogFile $PlayModeLogFile `
        -sceneCsv $SceneAuditCsv `
        -fullCsv $FullGateCsv `
        -playModeXml $PlayModeResultsXml

    Write-Host "[InputRound3Gate] summary report: $SummaryReportMd"
}

if ($status -ne "PASS") {
    Write-Error "[InputRound3Gate] failed stage=$failedStage error=$failureMessage"
    exit 2
}

Write-Host "[InputRound3Gate] PASS"
exit 0
