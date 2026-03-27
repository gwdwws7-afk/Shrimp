param(
    [string]$MetricsCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_metrics.csv",
    [string]$GateCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\cross_system_perf_gate_config.csv",
    [string]$OutputMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\cross_system_perf_gate_report.md",
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
    throw "Cross-system metrics csv not found: $MetricsCsv"
}

if (!(Test-Path $GateCsv)) {
    throw "Cross-system gate config csv not found: $GateCsv"
}

$metricsRows = @(Import-Csv -Path $MetricsCsv)
$gateRows = @(Import-Csv -Path $GateCsv)

if ($metricsRows.Count -eq 0) {
    throw "Cross-system metrics csv has no rows: $MetricsCsv"
}

if ($gateRows.Count -eq 0) {
    throw "Cross-system gate config csv has no rows: $GateCsv"
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
    $maxAvgProjectiles = Parse-Double $g.max_avg_active_projectiles
    $maxP95Projectiles = Parse-Double $g.max_p95_active_projectiles
    $maxAvgDamageTexts = Parse-Double $g.max_avg_active_damage_texts
    $maxP95DamageTexts = Parse-Double $g.max_p95_active_damage_texts
    $maxAvgParticles = Parse-Double $g.max_avg_active_particles
    $maxP95Particles = Parse-Double $g.max_p95_active_particles

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
            avg_active_projectiles = [double]::NaN
            p95_active_projectiles = [double]::NaN
            avg_active_damage_texts = [double]::NaN
            p95_active_damage_texts = [double]::NaN
            avg_active_particles = [double]::NaN
            p95_active_particles = [double]::NaN
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
    $avgProjectiles = Parse-Double $m.avg_active_projectiles
    $p95Projectiles = Parse-Double $m.p95_active_projectiles
    $avgDamageTexts = Parse-Double $m.avg_active_damage_texts
    $p95DamageTexts = Parse-Double $m.p95_active_damage_texts
    $avgParticles = Parse-Double $m.avg_active_particles
    $p95Particles = Parse-Double $m.p95_active_particles

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

    if ((Is-Defined $maxAvgProjectiles) -and (Is-Defined $avgProjectiles) -and $avgProjectiles -gt $maxAvgProjectiles) {
        $reasons.Add("avg_active_projectiles $(Format-Num $avgProjectiles) > max $(Format-Num $maxAvgProjectiles)") | Out-Null
    }
    if ((Is-Defined $maxP95Projectiles) -and (Is-Defined $p95Projectiles) -and $p95Projectiles -gt $maxP95Projectiles) {
        $reasons.Add("p95_active_projectiles $(Format-Num $p95Projectiles) > max $(Format-Num $maxP95Projectiles)") | Out-Null
    }
    if ((Is-Defined $maxAvgDamageTexts) -and (Is-Defined $avgDamageTexts) -and $avgDamageTexts -gt $maxAvgDamageTexts) {
        $reasons.Add("avg_active_damage_texts $(Format-Num $avgDamageTexts) > max $(Format-Num $maxAvgDamageTexts)") | Out-Null
    }
    if ((Is-Defined $maxP95DamageTexts) -and (Is-Defined $p95DamageTexts) -and $p95DamageTexts -gt $maxP95DamageTexts) {
        $reasons.Add("p95_active_damage_texts $(Format-Num $p95DamageTexts) > max $(Format-Num $maxP95DamageTexts)") | Out-Null
    }
    if ((Is-Defined $maxAvgParticles) -and (Is-Defined $avgParticles) -and $avgParticles -gt $maxAvgParticles) {
        $reasons.Add("avg_active_particles $(Format-Num $avgParticles) > max $(Format-Num $maxAvgParticles)") | Out-Null
    }
    if ((Is-Defined $maxP95Particles) -and (Is-Defined $p95Particles) -and $p95Particles -gt $maxP95Particles) {
        $reasons.Add("p95_active_particles $(Format-Num $p95Particles) > max $(Format-Num $maxP95Particles)") | Out-Null
    }

    $requiredColumns = @(
        "avg_active_projectiles",
        "p95_active_projectiles",
        "avg_active_damage_texts",
        "p95_active_damage_texts",
        "avg_active_particles",
        "p95_active_particles"
    )

    foreach ($column in $requiredColumns) {
        if (-not $m.PSObject.Properties.Match($column)) {
            $reasons.Add("missing required metric column: $column") | Out-Null
        }
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
        avg_active_projectiles = $avgProjectiles
        p95_active_projectiles = $p95Projectiles
        avg_active_damage_texts = $avgDamageTexts
        p95_active_damage_texts = $p95DamageTexts
        avg_active_particles = $avgParticles
        p95_active_particles = $p95Particles
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

$lines.Add("# Cross-System Performance Gate Report") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("- Generated: $nowText") | Out-Null
$lines.Add("- MetricsCsv: $MetricsCsv") | Out-Null
$lines.Add("- GateCsv: $GateCsv") | Out-Null
$lines.Add("- Total: $totalCount, Pass: $passCount, Fail: $failCount") | Out-Null
$lines.Add("") | Out-Null

if ($hasBaseline) {
    $lines.Add("## Summary (with baseline delta)") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("| step | status | avg_fps | p95_ms | p99_ms | gc_avg_B | ai/s | proj_avg | proj_p95 | dmgText_avg | dmgText_p95 | particles_avg | particles_p95 | d_avg_fps | d_p95_ms | d_p99_ms | reasons |") | Out-Null
    $lines.Add("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|") | Out-Null
}
else {
    $lines.Add("## Summary") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("| step | status | avg_fps | p95_ms | p99_ms | gc_avg_B | ai/s | proj_avg | proj_p95 | dmgText_avg | dmgText_p95 | particles_avg | particles_p95 | reasons |") | Out-Null
    $lines.Add("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|") | Out-Null
}

foreach ($r in $results) {
    if ($hasBaseline) {
        $lines.Add(
            "| $($r.step_label) | $($r.status) | $(Format-Num $r.avg_fps) | $(Format-Num $r.p95_frame_ms) | $(Format-Num $r.p99_frame_ms) | $(Format-Num $r.avg_gc_alloc) | $(Format-Num $r.ai_decisions) | $(Format-Num $r.avg_active_projectiles) | $(Format-Num $r.p95_active_projectiles) | $(Format-Num $r.avg_active_damage_texts) | $(Format-Num $r.p95_active_damage_texts) | $(Format-Num $r.avg_active_particles) | $(Format-Num $r.p95_active_particles) | $(Format-Num $r.delta_avg_fps) | $(Format-Num $r.delta_p95_frame_ms) | $(Format-Num $r.delta_p99_frame_ms) | $($r.reasons) |"
        ) | Out-Null
    }
    else {
        $lines.Add(
            "| $($r.step_label) | $($r.status) | $(Format-Num $r.avg_fps) | $(Format-Num $r.p95_frame_ms) | $(Format-Num $r.p99_frame_ms) | $(Format-Num $r.avg_gc_alloc) | $(Format-Num $r.ai_decisions) | $(Format-Num $r.avg_active_projectiles) | $(Format-Num $r.p95_active_projectiles) | $(Format-Num $r.avg_active_damage_texts) | $(Format-Num $r.p95_active_damage_texts) | $(Format-Num $r.avg_active_particles) | $(Format-Num $r.p95_active_particles) | $($r.reasons) |"
        ) | Out-Null
    }
}

$outDir = Split-Path -Path $OutputMd -Parent
if (![string]::IsNullOrWhiteSpace($outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$lines | Set-Content -Path $OutputMd -Encoding UTF8
Write-Host "Cross-system performance gate report generated: $OutputMd"

if ($failCount -gt 0) {
    Write-Warning "Cross-system performance gate failed: $failCount/$totalCount step(s) did not meet thresholds."
    exit 2
}

Write-Host "Cross-system performance gate passed: all $totalCount step(s) met thresholds."
exit 0
