param(
    [string]$MetricsCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p3_stress_metrics.csv",
    [string]$GateCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_config.csv",
    [string]$OutputMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_gate_report.md",
    [string]$BaselineCsv = ""
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

if (!(Test-Path $MetricsCsv)) {
    throw "Metrics csv not found: $MetricsCsv"
}

if (!(Test-Path $GateCsv)) {
    throw "Gate config csv not found: $GateCsv"
}

$metricsRows = @(Import-Csv -Path $MetricsCsv)
$gateRows = @(Import-Csv -Path $GateCsv)

if ($metricsRows.Count -eq 0) {
    throw "Metrics csv has no rows: $MetricsCsv"
}

if ($gateRows.Count -eq 0) {
    throw "Gate config csv has no rows: $GateCsv"
}

$metricsByStep = @{}
foreach ($m in $metricsRows) {
    $step = ([string]$m.step_label).Trim()
    if ([string]::IsNullOrWhiteSpace($step)) { continue }
    $metricsByStep[$step] = $m
}

$baselineByStep = @{}
if (![string]::IsNullOrWhiteSpace($BaselineCsv) -and (Test-Path $BaselineCsv)) {
    $baselineRows = @(Import-Csv -Path $BaselineCsv)
    foreach ($b in $baselineRows) {
        $step = ([string]$b.step_label).Trim()
        if ([string]::IsNullOrWhiteSpace($step)) { continue }
        $baselineByStep[$step] = $b
    }
}

$results = [System.Collections.Generic.List[object]]::new()

foreach ($g in $gateRows) {
    $step = ([string]$g.step_label).Trim()
    if ([string]::IsNullOrWhiteSpace($step)) { continue }

    $minActive = Parse-Double $g.min_active_enemies
    $minAvgFps = Parse-Double $g.min_avg_fps
    $maxP95 = Parse-Double $g.max_p95_frame_ms
    $maxP99 = Parse-Double $g.max_p99_frame_ms
    $maxGcAvg = Parse-Double $g.max_avg_gc_alloc_bytes_per_frame
    $maxGcP95 = Parse-Double $g.max_p95_gc_alloc_bytes_per_frame
    $minAi = Parse-Double $g.min_ai_decisions_per_s
    $maxAi = Parse-Double $g.max_ai_decisions_per_s

    if (-not $metricsByStep.ContainsKey($step)) {
        $results.Add([pscustomobject]@{
            step_label = $step
            status = "FAIL"
            reasons = "metrics row missing"
            avg_active_enemies = [double]::NaN
            avg_fps = [double]::NaN
            p95_frame_ms = [double]::NaN
            p99_frame_ms = [double]::NaN
            avg_gc_alloc = [double]::NaN
            p95_gc_alloc = [double]::NaN
            ai_decisions = [double]::NaN
            delta_avg_fps = [double]::NaN
            delta_p95_frame_ms = [double]::NaN
            delta_p99_frame_ms = [double]::NaN
        }) | Out-Null
        continue
    }

    $m = $metricsByStep[$step]
    $active = Parse-Double $m.avg_active_enemies
    $avgFps = Parse-Double $m.avg_fps
    $p95 = Parse-Double $m.p95_frame_ms
    $p99 = Parse-Double $m.p99_frame_ms
    $gcAvg = Parse-Double $m.avg_gc_alloc_bytes_per_frame
    $gcP95 = Parse-Double $m.p95_gc_alloc_bytes_per_frame
    $ai = Parse-Double $m.avg_ai_decisions_per_s

    $reasons = [System.Collections.Generic.List[string]]::new()

    if ((Is-Defined $minActive) -and (Is-Defined $active) -and $active -lt $minActive) {
        $reasons.Add("avg_active_enemies $(Format-Num $active) < min $(Format-Num $minActive)") | Out-Null
    }
    if ((Is-Defined $minAvgFps) -and (Is-Defined $avgFps) -and $avgFps -lt $minAvgFps) {
        $reasons.Add("avg_fps $(Format-Num $avgFps) < min $(Format-Num $minAvgFps)") | Out-Null
    }
    if ((Is-Defined $maxP95) -and (Is-Defined $p95) -and $p95 -gt $maxP95) {
        $reasons.Add("p95_frame_ms $(Format-Num $p95) > max $(Format-Num $maxP95)") | Out-Null
    }
    if ((Is-Defined $maxP99) -and (Is-Defined $p99) -and $p99 -gt $maxP99) {
        $reasons.Add("p99_frame_ms $(Format-Num $p99) > max $(Format-Num $maxP99)") | Out-Null
    }
    if ((Is-Defined $maxGcAvg) -and (Is-Defined $gcAvg) -and $gcAvg -gt $maxGcAvg) {
        $reasons.Add("avg_gc_alloc_bytes_per_frame $(Format-Num $gcAvg) > max $(Format-Num $maxGcAvg)") | Out-Null
    }
    if ((Is-Defined $maxGcP95) -and (Is-Defined $gcP95) -and $gcP95 -gt $maxGcP95) {
        $reasons.Add("p95_gc_alloc_bytes_per_frame $(Format-Num $gcP95) > max $(Format-Num $maxGcP95)") | Out-Null
    }
    if ((Is-Defined $minAi) -and (Is-Defined $ai) -and $ai -lt $minAi) {
        $reasons.Add("avg_ai_decisions_per_s $(Format-Num $ai) < min $(Format-Num $minAi)") | Out-Null
    }
    if ((Is-Defined $maxAi) -and (Is-Defined $ai) -and $ai -gt $maxAi) {
        $reasons.Add("avg_ai_decisions_per_s $(Format-Num $ai) > max $(Format-Num $maxAi)") | Out-Null
    }

    $deltaAvgFps = [double]::NaN
    $deltaP95 = [double]::NaN
    $deltaP99 = [double]::NaN
    if ($baselineByStep.ContainsKey($step)) {
        $b = $baselineByStep[$step]
        $bAvgFps = Parse-Double $b.avg_fps
        $bP95 = Parse-Double $b.p95_frame_ms
        $bP99 = Parse-Double $b.p99_frame_ms

        if ((Is-Defined $avgFps) -and (Is-Defined $bAvgFps)) { $deltaAvgFps = $avgFps - $bAvgFps }
        if ((Is-Defined $p95) -and (Is-Defined $bP95)) { $deltaP95 = $p95 - $bP95 }
        if ((Is-Defined $p99) -and (Is-Defined $bP99)) { $deltaP99 = $p99 - $bP99 }
    }

    $status = if ($reasons.Count -eq 0) { "PASS" } else { "FAIL" }
    $reasonText = if ($reasons.Count -eq 0) { "-" } else { [string]::Join("; ", $reasons) }

    $results.Add([pscustomobject]@{
        step_label = $step
        status = $status
        reasons = $reasonText
        avg_active_enemies = $active
        avg_fps = $avgFps
        p95_frame_ms = $p95
        p99_frame_ms = $p99
        avg_gc_alloc = $gcAvg
        p95_gc_alloc = $gcP95
        ai_decisions = $ai
        delta_avg_fps = $deltaAvgFps
        delta_p95_frame_ms = $deltaP95
        delta_p99_frame_ms = $deltaP99
    }) | Out-Null
}

$passCount = @($results | Where-Object { $_.status -eq "PASS" }).Count
$failCount = @($results | Where-Object { $_.status -eq "FAIL" }).Count
$totalCount = $results.Count

$hasBaseline = $baselineByStep.Count -gt 0
$lines = [System.Collections.Generic.List[string]]::new()
$nowText = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")

$lines.Add("# Enemy AI P4 Gate Report") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("- Generated: $nowText") | Out-Null
$lines.Add("- MetricsCsv: $MetricsCsv") | Out-Null
$lines.Add("- GateCsv: $GateCsv") | Out-Null
$lines.Add("- Total: $totalCount, Pass: $passCount, Fail: $failCount") | Out-Null
$lines.Add("") | Out-Null

if ($hasBaseline) {
    $lines.Add("## Summary (with baseline delta)") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("| step | status | avg_active | avg_fps | p95_ms | p99_ms | gc_avg_B | gc_p95_B | ai/s | d_avg_fps | d_p95_ms | d_p99_ms | reasons |") | Out-Null
    $lines.Add("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|") | Out-Null
}
else {
    $lines.Add("## Summary") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("| step | status | avg_active | avg_fps | p95_ms | p99_ms | gc_avg_B | gc_p95_B | ai/s | reasons |") | Out-Null
    $lines.Add("|---|---|---:|---:|---:|---:|---:|---:|---:|---|") | Out-Null
}

foreach ($r in $results) {
    if ($hasBaseline) {
        $lines.Add(
            "| $($r.step_label) | $($r.status) | $(Format-Num $r.avg_active_enemies) | $(Format-Num $r.avg_fps) | $(Format-Num $r.p95_frame_ms) | $(Format-Num $r.p99_frame_ms) | $(Format-Num $r.avg_gc_alloc) | $(Format-Num $r.p95_gc_alloc) | $(Format-Num $r.ai_decisions) | $(Format-Num $r.delta_avg_fps) | $(Format-Num $r.delta_p95_frame_ms) | $(Format-Num $r.delta_p99_frame_ms) | $($r.reasons) |"
        ) | Out-Null
    }
    else {
        $lines.Add(
            "| $($r.step_label) | $($r.status) | $(Format-Num $r.avg_active_enemies) | $(Format-Num $r.avg_fps) | $(Format-Num $r.p95_frame_ms) | $(Format-Num $r.p99_frame_ms) | $(Format-Num $r.avg_gc_alloc) | $(Format-Num $r.p95_gc_alloc) | $(Format-Num $r.ai_decisions) | $($r.reasons) |"
        ) | Out-Null
    }
}

$outDir = Split-Path -Path $OutputMd -Parent
if (![string]::IsNullOrWhiteSpace($outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$lines | Set-Content -Path $OutputMd -Encoding UTF8
Write-Host "P4 gate report generated: $OutputMd"

if ($failCount -gt 0) {
    Write-Warning "P4 gate failed: $failCount/$totalCount step(s) did not meet thresholds."
    exit 2
}

Write-Host "P4 gate passed: all $totalCount step(s) met thresholds."
exit 0
