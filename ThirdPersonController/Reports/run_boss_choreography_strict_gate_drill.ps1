param(
    [string]$ProjectPath = "C:\test\Shrimp",
    [string]$UnityPath = "",
    [string]$ExecuteMethod = "ThirdPersonController.Editor.BossChoreographyCoverageValidator.ValidateForBatch",
    [string]$LevelAssetPath = "C:\test\Shrimp\Assets\GameDesign\Data\LevelData_Level08.asset",
    [string]$WhitelistCsvPath = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_strict_warning_whitelist.csv",
    [string]$CoverageCsvPath = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_coverage_report.csv",
    [string]$OutputReportPath = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_strict_gate_drill_round8_report_2026-03-19.md",
    [string]$FailureSnapshotPath = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_choreography_strict_gate_drill_round8_failure_snapshot_2026-03-19.md",
    [string]$Phase1LogFile = "C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_phase1.log",
    [string]$Phase2LogFile = "C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_phase2.log",
    [string]$RestoreLogFile = "C:\test\Shrimp\Logs\BossChoreographyStrictGateDrill_restore.log",
    [int]$WaitForProjectUnlockSeconds = 30,
    [int]$ProcessTimeoutSeconds = 1200,
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

function Ensure-ParentDirectory([string]$path) {
    $dir = Split-Path -Parent $path
    if (![string]::IsNullOrWhiteSpace($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
}

function Write-Utf8NoBom([string]$path, [string]$content) {
    Ensure-ParentDirectory -path $path
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $encoding)
}

function Set-OverrideBossSettings([string]$assetPath, [int]$value) {
    $text = [System.IO.File]::ReadAllText($assetPath)
    $updated = [System.Text.RegularExpressions.Regex]::Replace($text, 'overrideBossSettings:\s*[01]', "overrideBossSettings: $value", 1)
    if ($updated -eq $text) {
        throw "Cannot find overrideBossSettings field in level asset: $assetPath"
    }

    Write-Utf8NoBom -path $assetPath -content $updated
}

function Write-Whitelist([string]$whitelistPath, [bool]$allowLevel08) {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("enabled,level_id,note") | Out-Null
    if ($allowLevel08) {
        $lines.Add("1,LEVEL_08,round8 strict drill temporary allowlist") | Out-Null
    }

    Write-Utf8NoBom -path $whitelistPath -content ([string]::Join("`n", $lines) + "`n")
}

function Get-Level08Row([string]$coverageCsvPath) {
    if (!(Test-Path $coverageCsvPath)) {
        throw "Coverage csv not found: $coverageCsvPath"
    }

    $rows = @(Import-Csv -Path $coverageCsvPath)
    if ($rows.Count -eq 0) {
        throw "Coverage csv has no rows: $coverageCsvPath"
    }

    $row = $rows | Where-Object { ([string]$_.level_id).Trim().Equals("LEVEL_08", [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
    if ($null -eq $row) {
        throw "LEVEL_08 row missing in coverage csv: $coverageCsvPath"
    }

    return $row
}

function Invoke-Validation([string]$unityExe, [string]$projectPath, [string]$executeMethod, [string]$logFile, [switch]$noGraphics, [int]$timeoutSeconds) {
    Ensure-ParentDirectory -path $logFile
    $exitCode = Invoke-UnityExecuteMethod `
        -unityExe $unityExe `
        -projectPath $projectPath `
        -executeMethod $executeMethod `
        -logFile $logFile `
        -noGraphics:$noGraphics `
        -timeoutSeconds $timeoutSeconds

    if ($exitCode -eq 124) {
        throw "Validation timed out after $timeoutSeconds s. log=$logFile"
    }

    return $exitCode
}

function Get-RowField([object]$row, [string]$fieldName) {
    if ($null -eq $row -or [string]::IsNullOrWhiteSpace($fieldName)) {
        return "n/a"
    }

    $prop = $row.PSObject.Properties[$fieldName]
    if ($null -eq $prop) {
        return "n/a"
    }

    $text = [string]$prop.Value
    if ([string]::IsNullOrWhiteSpace($text)) {
        return "n/a"
    }

    return $text.Trim()
}

function Get-LogKeyLines([string]$logFile, [int]$tailLineCount = 40) {
    $lines = New-Object System.Collections.Generic.List[string]
    if (!(Test-Path $logFile)) {
        $lines.Add("(log missing) $logFile") | Out-Null
        return $lines
    }

    $patterns = @(
        '\[BossChoreographyCoverage\]',
        'strict-warning',
        'gate failed',
        'InvalidOperationException: \[BossChoreographyCoverage\]',
        'BossChoreographyCoverageValidator',
        'boss_choreography_coverage_report.csv'
    )

    $seen = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($pattern in $patterns) {
        $matches = Select-String -Path $logFile -Pattern $pattern -CaseSensitive:$false -ErrorAction SilentlyContinue | Select-Object -First 12
        foreach ($match in $matches) {
            $lineText = [string]$match.Line
            if ([string]::IsNullOrWhiteSpace($lineText)) {
                continue
            }

            $normalized = $lineText.Trim()
            if ($seen.Add($normalized)) {
                $lines.Add($normalized) | Out-Null
            }
        }
    }

    if ($lines.Count -gt 0) {
        return $lines
    }

    $tail = Get-Content -Path $logFile -Tail ([Math]::Max(5, $tailLineCount))
    foreach ($line in $tail) {
        $text = [string]$line
        if ([string]::IsNullOrWhiteSpace($text)) {
            continue
        }

        $lines.Add($text.Trim()) | Out-Null
    }

    if ($lines.Count -eq 0) {
        $lines.Add("(log exists but no readable lines)") | Out-Null
    }

    return $lines
}

$projectPathResolved = (Resolve-Path $ProjectPath).Path
$unityExe = Resolve-UnityPath -projectPath $projectPathResolved -explicitUnityPath $UnityPath

if (!(Test-Path $LevelAssetPath)) {
    throw "Level asset not found: $LevelAssetPath"
}

$originalLevelText = [System.IO.File]::ReadAllText($LevelAssetPath)
$whitelistExisted = Test-Path $WhitelistCsvPath
$originalWhitelistText = if ($whitelistExisted) { [System.IO.File]::ReadAllText($WhitelistCsvPath) } else { "" }

$phase1Exit = -1
$phase2Exit = -1
$restoreExit = -1
$phase1Row = $null
$phase2Row = $null
$restoreRow = $null
$phase1ExpectedFail = $false
$phase2ExpectedPass = $false
$restoreExpectedPass = $false
$capturedException = $null

try {
    try {
        if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
            throw "Project is already open by another Unity process: $projectPathResolved"
        }

        Set-OverrideBossSettings -assetPath $LevelAssetPath -value 0
        Write-Whitelist -whitelistPath $WhitelistCsvPath -allowLevel08:$false

        Write-Host "[BossStrictDrill] phase1 start (no whitelist, expect fail)."
        $phase1Exit = Invoke-Validation `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $ExecuteMethod `
            -logFile $Phase1LogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $ProcessTimeoutSeconds

        $phase1Row = Get-Level08Row -coverageCsvPath $CoverageCsvPath
        $phase1ExpectedFail = ($phase1Exit -ne 0) -and ([string]$phase1Row.status -eq "Error") -and ([int]$phase1Row.blocking_errors -gt 0)
        if (-not $phase1ExpectedFail) {
            throw "Phase1 expectation failed. exit=$phase1Exit status=$($phase1Row.status) blocking=$($phase1Row.blocking_errors)"
        }

        Write-Whitelist -whitelistPath $WhitelistCsvPath -allowLevel08:$true
        Write-Host "[BossStrictDrill] phase2 start (with whitelist, expect pass)."
        $phase2Exit = Invoke-Validation `
            -unityExe $unityExe `
            -projectPath $projectPathResolved `
            -executeMethod $ExecuteMethod `
            -logFile $Phase2LogFile `
            -noGraphics:$NoGraphics `
            -timeoutSeconds $ProcessTimeoutSeconds

        $phase2Row = Get-Level08Row -coverageCsvPath $CoverageCsvPath
        $phase2ExpectedPass = ($phase2Exit -eq 0) -and ([string]$phase2Row.status -eq "Ok") -and ([string]$phase2Row.strict_warning_whitelisted -eq "1")
        if (-not $phase2ExpectedPass) {
            throw "Phase2 expectation failed. exit=$phase2Exit status=$($phase2Row.status) whitelisted=$($phase2Row.strict_warning_whitelisted)"
        }
    }
    finally {
        Write-Utf8NoBom -path $LevelAssetPath -content $originalLevelText
        if ($whitelistExisted) {
            Write-Utf8NoBom -path $WhitelistCsvPath -content $originalWhitelistText
        }
        else {
            if (Test-Path $WhitelistCsvPath) {
                Remove-Item -Path $WhitelistCsvPath -Force
            }
        }

        if (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds) {
            Write-Host "[BossStrictDrill] restore validation start (expect pass)."
            $restoreExit = Invoke-Validation `
                -unityExe $unityExe `
                -projectPath $projectPathResolved `
                -executeMethod $ExecuteMethod `
                -logFile $RestoreLogFile `
                -noGraphics:$NoGraphics `
                -timeoutSeconds $ProcessTimeoutSeconds

            $restoreRow = Get-Level08Row -coverageCsvPath $CoverageCsvPath
            $restoreExpectedPass = ($restoreExit -eq 0) -and ([string]$restoreRow.status -eq "Ok")
        }
    }
}
catch {
    $capturedException = $_
}

$reportLines = New-Object System.Collections.Generic.List[string]
$reportLines.Add("# Boss Choreography Strict Gate Drill Report (Round8)") | Out-Null
$reportLines.Add("") | Out-Null
$reportLines.Add("- Timestamp: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))") | Out-Null
$reportLines.Add("- Project: $projectPathResolved") | Out-Null
$reportLines.Add("- ExecuteMethod: $ExecuteMethod") | Out-Null
$reportLines.Add("- Target Level Asset: $LevelAssetPath") | Out-Null
$reportLines.Add("- Whitelist CSV: $WhitelistCsvPath") | Out-Null
$reportLines.Add("- Coverage CSV: $CoverageCsvPath") | Out-Null
$reportLines.Add("- Failure Snapshot: $FailureSnapshotPath") | Out-Null
$reportLines.Add("- Captured Exception: $(if ($null -eq $capturedException) { 'none' } else { $capturedException.Exception.Message })") | Out-Null
$reportLines.Add("") | Out-Null
$reportLines.Add("## Phase Results") | Out-Null
$reportLines.Add("") | Out-Null
$reportLines.Add("| Phase | Expected | Exit | LEVEL_08 status | blocking | warnings | whitelisted | Result |") | Out-Null
$reportLines.Add("|---|---|---:|---|---:|---:|---:|---|") | Out-Null
$reportLines.Add("| Phase1 (no whitelist) | Fail | $phase1Exit | $(Get-RowField $phase1Row 'status') | $(Get-RowField $phase1Row 'blocking_errors') | $(Get-RowField $phase1Row 'warnings') | $(Get-RowField $phase1Row 'strict_warning_whitelisted') | $(if ($phase1ExpectedFail) { 'PASS' } else { 'FAIL' }) |") | Out-Null
$reportLines.Add("| Phase2 (with whitelist) | Pass | $phase2Exit | $(Get-RowField $phase2Row 'status') | $(Get-RowField $phase2Row 'blocking_errors') | $(Get-RowField $phase2Row 'warnings') | $(Get-RowField $phase2Row 'strict_warning_whitelisted') | $(if ($phase2ExpectedPass) { 'PASS' } else { 'FAIL' }) |") | Out-Null
$reportLines.Add("| Restore validation | Pass | $restoreExit | $(Get-RowField $restoreRow 'status') | $(Get-RowField $restoreRow 'blocking_errors') | $(Get-RowField $restoreRow 'warnings') | $(Get-RowField $restoreRow 'strict_warning_whitelisted') | $(if ($restoreExpectedPass) { 'PASS' } else { 'FAIL' }) |") | Out-Null
$reportLines.Add("") | Out-Null
$reportLines.Add("## Evidence") | Out-Null
$reportLines.Add("") | Out-Null
$reportLines.Add("- Phase1 log: $Phase1LogFile") | Out-Null
$reportLines.Add("- Phase2 log: $Phase2LogFile") | Out-Null
$reportLines.Add("- Restore log: $RestoreLogFile") | Out-Null
$reportLines.Add("- Failure snapshot: $FailureSnapshotPath") | Out-Null

Write-Utf8NoBom -path $OutputReportPath -content ([string]::Join("`n", $reportLines) + "`n")
Write-Host "[BossStrictDrill] report: $OutputReportPath"

$snapshotLines = New-Object System.Collections.Generic.List[string]
$snapshotLines.Add("# Boss Choreography Strict Gate Drill Snapshot (Round8)") | Out-Null
$snapshotLines.Add("") | Out-Null
$snapshotLines.Add("- Timestamp: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))") | Out-Null
$snapshotLines.Add("- Phase1 Exit: $phase1Exit") | Out-Null
$snapshotLines.Add("- Phase2 Exit: $phase2Exit") | Out-Null
$snapshotLines.Add("- Restore Exit: $restoreExit") | Out-Null
$snapshotLines.Add("- Captured Exception: $(if ($null -eq $capturedException) { 'none' } else { $capturedException.Exception.Message })") | Out-Null
$snapshotLines.Add("") | Out-Null

$phaseLogs = @(
    @{ Name = "Phase1"; Path = $Phase1LogFile },
    @{ Name = "Phase2"; Path = $Phase2LogFile },
    @{ Name = "Restore"; Path = $RestoreLogFile }
)

foreach ($item in $phaseLogs) {
    $name = [string]$item.Name
    $path = [string]$item.Path
    $snapshotLines.Add("## $name") | Out-Null
    $snapshotLines.Add("") | Out-Null
    $snapshotLines.Add("- Log: $path") | Out-Null
    $snapshotLines.Add("") | Out-Null
    $snapshotLines.Add('```text') | Out-Null
    $keyLines = Get-LogKeyLines -logFile $path -tailLineCount 40
    foreach ($line in $keyLines) {
        $snapshotLines.Add($line) | Out-Null
    }
    $snapshotLines.Add('```') | Out-Null
    $snapshotLines.Add("") | Out-Null
}

Write-Utf8NoBom -path $FailureSnapshotPath -content ([string]::Join("`n", $snapshotLines) + "`n")
Write-Host "[BossStrictDrill] snapshot: $FailureSnapshotPath"

if ($null -ne $capturedException) {
    throw "Strict gate drill aborted: $($capturedException.Exception.Message) report=$OutputReportPath snapshot=$FailureSnapshotPath"
}

if (-not $phase1ExpectedFail -or -not $phase2ExpectedPass -or -not $restoreExpectedPass) {
    throw "Strict gate drill failed. phase1=$phase1ExpectedFail phase2=$phase2ExpectedPass restore=$restoreExpectedPass report=$OutputReportPath snapshot=$FailureSnapshotPath"
}

Write-Host "[BossStrictDrill] passed."
exit 0
