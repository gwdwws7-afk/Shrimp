param(
    [string]$BehaviorCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p3_boss_behavior_depth_gate_report.csv",
    [string]$SceneClosureCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p3_boss_scene_closure_gate_report.csv",
    [string]$GrammarCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p3_boss_grammar_consistency_gate_report.csv",
    [string]$AggregateCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\p3_boss_depth_gate_report.csv",
    [string]$HistoryCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_p3_subset_history.csv",
    [string]$RunId = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = (Get-Date).ToString("yyyyMMdd-HHmmss")
}

function Get-SubsetRows([string]$csvPath) {
    if (!(Test-Path $csvPath)) {
        throw "Subset csv not found: $csvPath"
    }

    $rows = @(Import-Csv -Path $csvPath)
    return @($rows | Where-Object {
            $name = [string]$_.name
            -not $name.EndsWith("(no matches)", [StringComparison]::OrdinalIgnoreCase)
        })
}

function Build-SummaryRow([string]$label, [string]$csvPath, [string]$runId, [string]$timestamp) {
    $rows = @(Get-SubsetRows -csvPath $csvPath)
    $total = $rows.Count
    $passed = @($rows | Where-Object { ([string]$_.result).Equals("Passed", [StringComparison]::OrdinalIgnoreCase) }).Count
    $failed = @($rows | Where-Object { ([string]$_.result).Equals("Failed", [StringComparison]::OrdinalIgnoreCase) }).Count
    $skipped = @($rows | Where-Object { ([string]$_.result).Equals("Skipped", [StringComparison]::OrdinalIgnoreCase) }).Count
    $passRate = if ($total -gt 0) { [double]$passed / [double]$total } else { 0.0 }

    return [pscustomobject]@{
        run_id = $runId
        timestamp = $timestamp
        subset_label = $label
        total = $total
        passed = $passed
        failed = $failed
        skipped = $skipped
        pass_rate = $passRate.ToString("0.######", [System.Globalization.CultureInfo]::InvariantCulture)
        source_csv = $csvPath
    }
}

$historyRows = @()
if (Test-Path $HistoryCsv) {
    $historyRows = @(Import-Csv -Path $HistoryCsv)
}

$now = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$newRows = New-Object System.Collections.Generic.List[object]
$newRows.Add((Build-SummaryRow -label "behavior_depth" -csvPath $BehaviorCsv -runId $RunId -timestamp $now)) | Out-Null
$newRows.Add((Build-SummaryRow -label "scene_closure" -csvPath $SceneClosureCsv -runId $RunId -timestamp $now)) | Out-Null
$newRows.Add((Build-SummaryRow -label "grammar_consistency" -csvPath $GrammarCsv -runId $RunId -timestamp $now)) | Out-Null
$newRows.Add((Build-SummaryRow -label "depth_aggregate" -csvPath $AggregateCsv -runId $RunId -timestamp $now)) | Out-Null

$combined = @($historyRows + $newRows)
$directory = Split-Path -Path $HistoryCsv -Parent
if (![string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$combined | Export-Csv -Path $HistoryCsv -NoTypeInformation -Encoding UTF8
Write-Host "Appended $($newRows.Count) row(s) to history: $HistoryCsv (run_id=$RunId)"
exit 0
