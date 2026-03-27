param(
    [string]$HistoryCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_history.csv",
    [string]$OutputMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_trend_gate_report.md",
    [int]$WindowRuns = 5,
    [double]$MaxFpsDropRatio = 0.20,
    [double]$MaxP95IncreaseRatio = 0.20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Parse-Double([object]$value) {
    if ($null -eq $value) { return [double]::NaN }
    $text = [string]$value
    if ([string]::IsNullOrWhiteSpace($text)) { return [double]::NaN }
    $num = 0.0
    $ok = [double]::TryParse(
        $text,
        [System.Globalization.NumberStyles]::Float,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [ref]$num
    )
    if ($ok) { return $num }
    return [double]::NaN
}

function Format-Num([double]$value, [string]$format = "0.###") {
    if ([double]::IsNaN($value)) { return "n/a" }
    return $value.ToString($format, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Is-Defined([double]$value) {
    return -not [double]::IsNaN($value)
}

if (!(Test-Path $HistoryCsv)) {
    throw "Trend history csv not found: $HistoryCsv"
}

$rows = @(Import-Csv -Path $HistoryCsv)
if ($rows.Count -eq 0) {
    throw "Trend history csv has no rows: $HistoryCsv"
}

$grouped = $rows | Group-Object -Property step_label
$resultRows = New-Object System.Collections.Generic.List[object]

foreach ($group in $grouped) {
    $step = [string]$group.Name
    $ordered = @($group.Group | Sort-Object {
        try { [DateTime]::Parse($_.timestamp) } catch { [DateTime]::MinValue }
    })

    if ($ordered.Count -lt 2) {
        $resultRows.Add([pscustomobject]@{
            step_label = $step
            status = "PASS"
            note = "insufficient_history"
            latest_avg_fps = [double]::NaN
            baseline_avg_fps = [double]::NaN
            latest_p95 = [double]::NaN
            baseline_p95 = [double]::NaN
            fps_drop_ratio = [double]::NaN
            p95_increase_ratio = [double]::NaN
        }) | Out-Null
        continue
    }

    $latest = $ordered[$ordered.Count - 1]
    $baselineCount = [Math]::Min([Math]::Max(1, $WindowRuns), $ordered.Count - 1)
    $baselineSlice = $ordered[($ordered.Count - 1 - $baselineCount)..($ordered.Count - 2)]

    $latestAvgFps = Parse-Double $latest.avg_fps
    $latestP95 = Parse-Double $latest.p95_frame_ms

    $sumAvgFps = 0.0
    $sumP95 = 0.0
    $avgCount = 0
    $p95Count = 0

    foreach ($b in $baselineSlice) {
        $vFps = Parse-Double $b.avg_fps
        if (Is-Defined $vFps) {
            $sumAvgFps += $vFps
            $avgCount++
        }

        $vP95 = Parse-Double $b.p95_frame_ms
        if (Is-Defined $vP95) {
            $sumP95 += $vP95
            $p95Count++
        }
    }

    $baselineAvgFps = if ($avgCount -gt 0) { $sumAvgFps / $avgCount } else { [double]::NaN }
    $baselineP95 = if ($p95Count -gt 0) { $sumP95 / $p95Count } else { [double]::NaN }

    $fpsDropRatio = [double]::NaN
    if ((Is-Defined $baselineAvgFps) -and $baselineAvgFps -gt 0 -and (Is-Defined $latestAvgFps)) {
        $fpsDropRatio = ($baselineAvgFps - $latestAvgFps) / $baselineAvgFps
    }

    $p95IncreaseRatio = [double]::NaN
    if ((Is-Defined $baselineP95) -and $baselineP95 -gt 0 -and (Is-Defined $latestP95)) {
        $p95IncreaseRatio = ($latestP95 - $baselineP95) / $baselineP95
    }

    $failReasons = New-Object System.Collections.Generic.List[string]
    if ((Is-Defined $fpsDropRatio) -and $fpsDropRatio -gt $MaxFpsDropRatio) {
        $failReasons.Add("fps_drop_ratio $(Format-Num $fpsDropRatio) > max $(Format-Num $MaxFpsDropRatio)") | Out-Null
    }
    if ((Is-Defined $p95IncreaseRatio) -and $p95IncreaseRatio -gt $MaxP95IncreaseRatio) {
        $failReasons.Add("p95_increase_ratio $(Format-Num $p95IncreaseRatio) > max $(Format-Num $MaxP95IncreaseRatio)") | Out-Null
    }

    $status = if ($failReasons.Count -eq 0) { "PASS" } else { "FAIL" }
    $note = if ($failReasons.Count -eq 0) { "-" } else { [string]::Join("; ", $failReasons) }

    $resultRows.Add([pscustomobject]@{
        step_label = $step
        status = $status
        note = $note
        latest_avg_fps = $latestAvgFps
        baseline_avg_fps = $baselineAvgFps
        latest_p95 = $latestP95
        baseline_p95 = $baselineP95
        fps_drop_ratio = $fpsDropRatio
        p95_increase_ratio = $p95IncreaseRatio
    }) | Out-Null
}

$passCount = @($resultRows | Where-Object { $_.status -eq "PASS" }).Count
$failCount = @($resultRows | Where-Object { $_.status -eq "FAIL" }).Count
$total = $resultRows.Count

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Enemy AI P4 Trend Gate Report") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("- Generated: $((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))") | Out-Null
$lines.Add("- HistoryCsv: $HistoryCsv") | Out-Null
$lines.Add("- WindowRuns: $WindowRuns") | Out-Null
$lines.Add("- MaxFpsDropRatio: $MaxFpsDropRatio") | Out-Null
$lines.Add("- MaxP95IncreaseRatio: $MaxP95IncreaseRatio") | Out-Null
$lines.Add("- Total: $total, Pass: $passCount, Fail: $failCount") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| step | status | latest_fps | baseline_fps | latest_p95 | baseline_p95 | fps_drop_ratio | p95_increase_ratio | note |") | Out-Null
$lines.Add("|---|---|---:|---:|---:|---:|---:|---:|---|") | Out-Null

foreach ($r in $resultRows) {
    $lines.Add(
        "| $($r.step_label) | $($r.status) | $(Format-Num $r.latest_avg_fps) | $(Format-Num $r.baseline_avg_fps) | $(Format-Num $r.latest_p95) | $(Format-Num $r.baseline_p95) | $(Format-Num $r.fps_drop_ratio) | $(Format-Num $r.p95_increase_ratio) | $($r.note) |"
    ) | Out-Null
}

$outDir = Split-Path -Path $OutputMd -Parent
if (![string]::IsNullOrWhiteSpace($outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}
$lines | Set-Content -Path $OutputMd -Encoding UTF8

Write-Host "Enemy AI P4 trend gate report generated: $OutputMd"
if ($failCount -gt 0) {
    Write-Warning "Enemy AI P4 trend gate failed: $failCount/$total step(s) exceeded regression budget."
    exit 2
}

Write-Host "Enemy AI P4 trend gate passed."
exit 0
