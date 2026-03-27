param(
    [string]$HistoryCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_p3_subset_history.csv",
    [string]$OutputMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_p3_subset_trend_gate_report.md",
    [int]$WindowRuns = 5,
    [double]$MaxTotalDropRatio = 0.20
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
    throw "Boss P3 subset history csv not found: $HistoryCsv"
}

$rows = @(Import-Csv -Path $HistoryCsv)
if ($rows.Count -eq 0) {
    throw "Boss P3 subset history csv has no rows: $HistoryCsv"
}

$grouped = $rows | Group-Object -Property subset_label
$resultRows = New-Object System.Collections.Generic.List[object]

foreach ($group in $grouped) {
    $label = [string]$group.Name
    $ordered = @($group.Group | Sort-Object {
        try { [DateTime]::Parse($_.timestamp) } catch { [DateTime]::MinValue }
    })

    if ($ordered.Count -lt 2) {
        $resultRows.Add([pscustomobject]@{
            subset_label = $label
            status = "PASS"
            note = "insufficient_history"
            latest_total = [double]::NaN
            baseline_total = [double]::NaN
            latest_pass_rate = [double]::NaN
            baseline_pass_rate = [double]::NaN
            latest_failed = [double]::NaN
            total_drop_ratio = [double]::NaN
            pass_rate_drop = [double]::NaN
        }) | Out-Null
        continue
    }

    $latest = $ordered[$ordered.Count - 1]
    $baselineCount = [Math]::Min([Math]::Max(1, $WindowRuns), $ordered.Count - 1)
    $baselineSlice = $ordered[($ordered.Count - 1 - $baselineCount)..($ordered.Count - 2)]

    $latestTotal = Parse-Double $latest.total
    $latestPassRate = Parse-Double $latest.pass_rate
    $latestFailed = Parse-Double $latest.failed

    $sumBaselineTotal = 0.0
    $sumBaselinePassRate = 0.0
    $baselineTotalCount = 0
    $baselinePassRateCount = 0

    foreach ($b in $baselineSlice) {
        $vTotal = Parse-Double $b.total
        if (Is-Defined $vTotal) {
            $sumBaselineTotal += $vTotal
            $baselineTotalCount++
        }

        $vPassRate = Parse-Double $b.pass_rate
        if (Is-Defined $vPassRate) {
            $sumBaselinePassRate += $vPassRate
            $baselinePassRateCount++
        }
    }

    $baselineTotal = if ($baselineTotalCount -gt 0) { $sumBaselineTotal / $baselineTotalCount } else { [double]::NaN }
    $baselinePassRate = if ($baselinePassRateCount -gt 0) { $sumBaselinePassRate / $baselinePassRateCount } else { [double]::NaN }

    $totalDropRatio = [double]::NaN
    if ((Is-Defined $baselineTotal) -and $baselineTotal -gt 0 -and (Is-Defined $latestTotal)) {
        $totalDropRatio = ($baselineTotal - $latestTotal) / $baselineTotal
    }

    $passRateDrop = [double]::NaN
    if ((Is-Defined $baselinePassRate) -and (Is-Defined $latestPassRate)) {
        $passRateDrop = $baselinePassRate - $latestPassRate
    }

    $failReasons = New-Object System.Collections.Generic.List[string]
    if ((Is-Defined $latestFailed) -and $latestFailed -gt 0) {
        $failReasons.Add("latest_failed $(Format-Num $latestFailed '0') > 0") | Out-Null
    }
    if ((Is-Defined $latestPassRate) -and $latestPassRate -lt 1.0) {
        $failReasons.Add("latest_pass_rate $(Format-Num $latestPassRate) < 1.0") | Out-Null
    }
    if ((Is-Defined $totalDropRatio) -and $totalDropRatio -gt $MaxTotalDropRatio) {
        $failReasons.Add("total_drop_ratio $(Format-Num $totalDropRatio) > max $(Format-Num $MaxTotalDropRatio)") | Out-Null
    }

    $status = if ($failReasons.Count -eq 0) { "PASS" } else { "FAIL" }
    $note = if ($failReasons.Count -eq 0) { "-" } else { [string]::Join("; ", $failReasons) }

    $resultRows.Add([pscustomobject]@{
        subset_label = $label
        status = $status
        note = $note
        latest_total = $latestTotal
        baseline_total = $baselineTotal
        latest_pass_rate = $latestPassRate
        baseline_pass_rate = $baselinePassRate
        latest_failed = $latestFailed
        total_drop_ratio = $totalDropRatio
        pass_rate_drop = $passRateDrop
    }) | Out-Null
}

$passCount = @($resultRows | Where-Object { $_.status -eq "PASS" }).Count
$failCount = @($resultRows | Where-Object { $_.status -eq "FAIL" }).Count
$total = $resultRows.Count

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Boss P3 Subset Trend Gate Report") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("- Generated: $((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))") | Out-Null
$lines.Add("- HistoryCsv: $HistoryCsv") | Out-Null
$lines.Add("- WindowRuns: $WindowRuns") | Out-Null
$lines.Add("- MaxTotalDropRatio: $MaxTotalDropRatio") | Out-Null
$lines.Add("- Total: $total, Pass: $passCount, Fail: $failCount") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| subset | status | latest_total | baseline_total | latest_pass_rate | baseline_pass_rate | latest_failed | total_drop_ratio | pass_rate_drop | note |") | Out-Null
$lines.Add("|---|---|---:|---:|---:|---:|---:|---:|---:|---|") | Out-Null

foreach ($r in $resultRows) {
    $lines.Add(
        "| $($r.subset_label) | $($r.status) | $(Format-Num $r.latest_total) | $(Format-Num $r.baseline_total) | $(Format-Num $r.latest_pass_rate) | $(Format-Num $r.baseline_pass_rate) | $(Format-Num $r.latest_failed '0') | $(Format-Num $r.total_drop_ratio) | $(Format-Num $r.pass_rate_drop) | $($r.note) |"
    ) | Out-Null
}

$outDir = Split-Path -Path $OutputMd -Parent
if (![string]::IsNullOrWhiteSpace($outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}
$lines | Set-Content -Path $OutputMd -Encoding UTF8

Write-Host "Boss P3 subset trend gate report generated: $OutputMd"
if ($failCount -gt 0) {
    Write-Warning "Boss P3 subset trend gate failed: $failCount/$total subset(s) exceeded trend budget."
    exit 2
}

Write-Host "Boss P3 subset trend gate passed."
exit 0
