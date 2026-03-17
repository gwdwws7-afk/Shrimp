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
    [string]$LevelDataSceneApplyMethod = "ThirdPersonController.Editor.LevelDataSceneValidator.FixForBatch",
    [string]$LevelDataSceneValidateMethod = "ThirdPersonController.Editor.LevelDataSceneValidator.ValidateForBatch",
    [string]$LevelDataSceneApplyLogFile = "C:\test\Shrimp\Logs\LevelDataSceneFix.log",
    [string]$LevelDataSceneValidateLogFile = "C:\test\Shrimp\Logs\LevelDataSceneValidate.log",
    [string]$LevelDataSceneReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\level_data_scene_validator_report.csv",
    [string]$InputRound3ApplyMethod = "ThirdPersonController.Editor.InputBindingRound3SceneTool.ApplySceneBindingsForBatch",
    [string]$InputRound3ValidateMethod = "ThirdPersonController.Editor.InputBindingRound3SceneTool.ValidateFullGateForBatch",
    [string]$InputRound3ApplyLogFile = "C:\test\Shrimp\Logs\InputBindingRound3Apply.log",
    [string]$InputRound3ValidateLogFile = "C:\test\Shrimp\Logs\InputBindingRound3FullGate.log",
    [string]$InputRound3SceneAuditCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_scene_audit.csv",
    [string]$InputRound3FullGateCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\input_binding_round3_full_gate_audit.csv",
    [string]$EnemyTypeSceneGateScript = "",
    [string]$EnemyTypeSceneGateLogFile = "",
    [string]$TestFilter = "",
    [string]$AssemblyFilter = "",
    [int]$RetryCount = 1,
    [int]$WaitForProjectUnlockSeconds = 30,
    [int]$ProcessTimeoutSeconds = 1800,
    [int]$LevelContentTimeoutSeconds = 1200,
    [int]$LevelDataSceneTimeoutSeconds = 1200,
    [int]$InputRound3TimeoutSeconds = 1200,
    [int]$EnemyTypeSceneGateTimeoutSeconds = 1200,
    [switch]$SkipLevelContentGate,
    [switch]$SkipLevelDataSceneGate,
    [switch]$SkipInputRound3Gate,
    [switch]$SkipWarmupCompile,
    [switch]$SkipEnemyTypeSceneGate,
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
            Error = 0
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

    return "total=$($summary.Total) ok=$($summary.OK) fixed=$($summary.Fixed) error=$($summary.Error) mismatch=$($summary.Mismatch) skipped=$($summary.Skipped) other=$($summary.Other)"
}

function Get-CsvBlockingCount([hashtable]$summary) {
    if ($null -eq $summary -or -not $summary.Exists) {
        return 1
    }

    return ($summary.Error + $summary.Mismatch + $summary.Other)
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

$projectPathResolved = (Resolve-Path $ProjectPath).Path
$unityExe = Resolve-UnityPath -projectPath $projectPathResolved -explicitUnityPath $UnityPath

if ([string]::IsNullOrWhiteSpace($EnemyTypeSceneGateScript)) {
    $EnemyTypeSceneGateScript = Join-Path $projectPathResolved "Assets\ThirdPersonController\Reports\run_enemy_type_scene_gate.ps1"
}

if ([string]::IsNullOrWhiteSpace($EnemyTypeSceneGateLogFile)) {
    $EnemyTypeSceneGateLogFile = Join-Path $projectPathResolved "Logs\EnemyTypeSceneGate.log"
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

$levelDataSceneApplyLogDir = Split-Path -Parent $LevelDataSceneApplyLogFile
if (![string]::IsNullOrWhiteSpace($levelDataSceneApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelDataSceneApplyLogDir | Out-Null
}

$levelDataSceneValidateLogDir = Split-Path -Parent $LevelDataSceneValidateLogFile
if (![string]::IsNullOrWhiteSpace($levelDataSceneValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $levelDataSceneValidateLogDir | Out-Null
}

$inputRound3ApplyLogDir = Split-Path -Parent $InputRound3ApplyLogFile
if (![string]::IsNullOrWhiteSpace($inputRound3ApplyLogDir)) {
    New-Item -ItemType Directory -Force -Path $inputRound3ApplyLogDir | Out-Null
}

$inputRound3ValidateLogDir = Split-Path -Parent $InputRound3ValidateLogFile
if (![string]::IsNullOrWhiteSpace($inputRound3ValidateLogDir)) {
    New-Item -ItemType Directory -Force -Path $inputRound3ValidateLogDir | Out-Null
}

$enemyTypeSceneGateLogDir = Split-Path -Parent $EnemyTypeSceneGateLogFile
if (![string]::IsNullOrWhiteSpace($enemyTypeSceneGateLogDir)) {
    New-Item -ItemType Directory -Force -Path $enemyTypeSceneGateLogDir | Out-Null
}

$inputRound3Timeout = if ($InputRound3TimeoutSeconds -gt 0) {
    $InputRound3TimeoutSeconds
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

$levelDataSceneTimeout = if ($LevelDataSceneTimeoutSeconds -gt 0) {
    $LevelDataSceneTimeoutSeconds
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

if (-not $SkipLevelDataSceneGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

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

if (-not $SkipLevelContentGate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

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

if (-not $SkipInputRound3Gate.IsPresent) {
    if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
        throw "Project is already open by another Unity process: $projectPathResolved"
    }

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
exit $lastExitCode
