param(
    [string]$MetricsCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_metrics.csv",
    [string]$HistoryCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_history.csv",
    [string]$RunId = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = (Get-Date).ToString("yyyyMMdd-HHmmss")
}

if (!(Test-Path $MetricsCsv)) {
    throw "Metrics csv not found: $MetricsCsv"
}

$metricsRows = @(Import-Csv -Path $MetricsCsv)
if ($metricsRows.Count -eq 0) {
    throw "Metrics csv has no rows: $MetricsCsv"
}

$historyRows = @()
if (Test-Path $HistoryCsv) {
    $historyRows = @(Import-Csv -Path $HistoryCsv)
}

$now = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$newRows = New-Object System.Collections.Generic.List[object]

foreach ($row in $metricsRows) {
    $newRows.Add([pscustomobject]@{
        run_id = $RunId
        timestamp = $now
        step_label = [string]$row.step_label
        avg_fps = [string]$row.avg_fps
        p95_frame_ms = [string]$row.p95_frame_ms
        p99_frame_ms = [string]$row.p99_frame_ms
        avg_gc_alloc_bytes_per_frame = [string]$row.avg_gc_alloc_bytes_per_frame
        p95_gc_alloc_bytes_per_frame = [string]$row.p95_gc_alloc_bytes_per_frame
        avg_ai_decisions_per_s = [string]$row.avg_ai_decisions_per_s
        avg_active_enemies = [string]$row.avg_active_enemies
        avg_active_projectiles = [string]$row.avg_active_projectiles
        p95_active_projectiles = [string]$row.p95_active_projectiles
        avg_active_damage_texts = [string]$row.avg_active_damage_texts
        p95_active_damage_texts = [string]$row.p95_active_damage_texts
        avg_active_particles = [string]$row.avg_active_particles
        p95_active_particles = [string]$row.p95_active_particles
    }) | Out-Null
}

$combined = @($historyRows + $newRows)
$directory = Split-Path -Path $HistoryCsv -Parent
if (![string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$combined | Export-Csv -Path $HistoryCsv -NoTypeInformation -Encoding UTF8
Write-Host "Appended $($newRows.Count) row(s) to history: $HistoryCsv (run_id=$RunId)"
