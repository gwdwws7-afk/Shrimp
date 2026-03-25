param(
    [string]$BaselineCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round4_fill_backup_20260318_164913.csv",
    [string]$CurrentFillCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round4_fill.csv",
    [string]$OutputCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round6_conservative_suggestions.csv",
    [string]$SummaryMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round6_summary.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Parse-Double([object]$value, [double]$fallback = [double]::NaN) {
    if ($null -eq $value) { return $fallback }
    $text = [string]$value
    if ([string]::IsNullOrWhiteSpace($text)) { return $fallback }

    $num = 0.0
    if ([double]::TryParse($text, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$num)) {
        return $num
    }

    if ([double]::TryParse($text, [ref]$num)) {
        return $num
    }

    return $fallback
}

function Parse-Bool([object]$value, [bool]$fallback = $false) {
    if ($null -eq $value) { return $fallback }
    $text = ([string]$value).Trim().ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($text)) { return $fallback }
    switch ($text) {
        "true" { return $true }
        "false" { return $false }
        "1" { return $true }
        "0" { return $false }
        "y" { return $true }
        "n" { return $false }
        "yes" { return $true }
        "no" { return $false }
        default { return $fallback }
    }
}

function Clamp([double]$value, [double]$min, [double]$max) {
    if ($value -lt $min) { return $min }
    if ($value -gt $max) { return $max }
    return $value
}

function Round3([double]$value) {
    return [math]::Round($value, 3)
}

function To-Text([double]$value) {
    return ("{0:0.###}" -f $value)
}

function Make-Key([string]$prefabPath, [string]$attackId) {
    $p = if ($null -eq $prefabPath) { "" } else { $prefabPath.Trim().Replace('\', '/') }
    $a = if ($null -eq $attackId) { "" } else { $attackId.Trim() }
    return ($p + "|" + $a).ToLowerInvariant()
}

if (!(Test-Path $CurrentFillCsv)) {
    throw "Current fill csv not found: $CurrentFillCsv"
}

if (!(Test-Path $BaselineCsv)) {
    $fallback = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round4_template.csv"
    if (Test-Path $fallback) {
        $BaselineCsv = $fallback
    }
    else {
        throw "Baseline csv not found: $BaselineCsv"
    }
}

$baselineRows = Import-Csv -Path $BaselineCsv
$currentRows = Import-Csv -Path $CurrentFillCsv

$baselineMap = @{}
foreach ($row in $baselineRows) {
    $key = Make-Key ([string]$row.prefab_path) ([string]$row.attack_id)
    if ([string]::IsNullOrWhiteSpace($key)) { continue }
    if (-not $baselineMap.ContainsKey($key)) {
        $baselineMap[$key] = $row
    }
}

$suggestions = [System.Collections.Generic.List[object]]::new()
$changedRows = 0
$changedFieldsTotal = 0

foreach ($row in $currentRows) {
    $prefabPath = ([string]$row.prefab_path).Trim()
    $attackId = ([string]$row.attack_id).Trim()
    $attackName = ([string]$row.attack_name).Trim()
    if ([string]::IsNullOrWhiteSpace($prefabPath) -or [string]::IsNullOrWhiteSpace($attackId)) {
        continue
    }

    $key = Make-Key $prefabPath $attackId
    $base = $null
    if ($baselineMap.ContainsKey($key)) {
        $base = $baselineMap[$key]
    }
    else {
        $base = $row
    }

    $currentDamage = Parse-Double $row.damage 0
    $currentCooldown = Parse-Double $row.cooldown 0
    $currentWeight = Parse-Double $row.selection_weight 1
    $currentWindup = Parse-Double $row.windup_time 0
    $currentActive = Parse-Double $row.active_time 0
    $currentRecovery = Parse-Double $row.recovery_time 0
    $currentRange = Parse-Double $row.range 0
    $currentKnockback = Parse-Double $row.knockback_force 0
    $currentAoeRadius = Parse-Double $row.aoe_radius 0

    $baseDamage = Parse-Double $base.damage $currentDamage
    $baseCooldown = Parse-Double $base.cooldown $currentCooldown
    $baseWeight = Parse-Double $base.selection_weight $currentWeight
    $baseWindup = Parse-Double $base.windup_time $currentWindup
    $baseActive = Parse-Double $base.active_time $currentActive
    $baseRecovery = Parse-Double $base.recovery_time $currentRecovery
    $baseRange = Parse-Double $base.range $currentRange
    $baseKnockback = Parse-Double $base.knockback_force $currentKnockback
    $baseAoeRadius = Parse-Double $base.aoe_radius $currentAoeRadius

    $isSpecial = Parse-Bool $row.is_special $false
    $requiresPhase2 = Parse-Bool $row.requires_phase2 $false
    $requiresPhase3 = Parse-Bool $row.requires_phase3 $false
    $aoe = Parse-Bool $row.aoe $false
    $targetPlayer = Parse-Bool $row.target_player $true

    $damageCapMul = 1.06
    $cooldownFloorMul = 0.92
    $weightCapMul = 1.05
    if ($isSpecial) {
        $damageCapMul = 1.09
        $cooldownFloorMul = 0.90
        $weightCapMul = 1.06
    }

    if ($requiresPhase2) {
        $damageCapMul = 1.10
        $cooldownFloorMul = 0.89
        $weightCapMul = 1.07
    }

    if ($requiresPhase3) {
        $damageCapMul = 1.12
        $cooldownFloorMul = 0.87
        $weightCapMul = 1.08
    }

    $aoeCapMul = if ($aoe) { 1.06 } else { 1.04 }
    $rangeCapMul = if ($targetPlayer) { 1.06 } else { 1.05 }

    $targetDamage = Round3 (Clamp ([math]::Min($currentDamage, $baseDamage * $damageCapMul)) 20 280)
    $targetCooldown = Round3 (Clamp ([math]::Max($currentCooldown, $baseCooldown * $cooldownFloorMul)) 2.2 14)
    $targetWeight = Round3 (Clamp ([math]::Min($currentWeight, $baseWeight * $weightCapMul)) 0.35 1.6)
    $targetWindup = Round3 (Clamp ([math]::Min($currentWindup, $baseWindup * 1.08)) 0.2 1.2)
    $targetActive = Round3 (Clamp ([math]::Min($currentActive, $baseActive * 1.06)) 0.15 0.6)
    $targetRecovery = Round3 (Clamp ([math]::Max($currentRecovery, $baseRecovery * 0.92)) 0.35 1.2)
    $targetRange = Round3 (Clamp ([math]::Min($currentRange, $baseRange * $rangeCapMul)) 3.5 12)
    $targetKnockback = Round3 (Clamp ([math]::Min($currentKnockback, $baseKnockback * 1.08)) 4 16)
    $targetAoeRadius = Round3 (Clamp ([math]::Min($currentAoeRadius, $baseAoeRadius * $aoeCapMul)) 0 10)

    $changedFields = [System.Collections.Generic.List[string]]::new()
    if ([math]::Abs($targetDamage - $currentDamage) -gt 0.0001) { $changedFields.Add("damage") | Out-Null }
    if ([math]::Abs($targetCooldown - $currentCooldown) -gt 0.0001) { $changedFields.Add("cooldown") | Out-Null }
    if ([math]::Abs($targetWeight - $currentWeight) -gt 0.0001) { $changedFields.Add("selection_weight") | Out-Null }
    if ([math]::Abs($targetWindup - $currentWindup) -gt 0.0001) { $changedFields.Add("windup_time") | Out-Null }
    if ([math]::Abs($targetActive - $currentActive) -gt 0.0001) { $changedFields.Add("active_time") | Out-Null }
    if ([math]::Abs($targetRecovery - $currentRecovery) -gt 0.0001) { $changedFields.Add("recovery_time") | Out-Null }
    if ([math]::Abs($targetRange - $currentRange) -gt 0.0001) { $changedFields.Add("range") | Out-Null }
    if ([math]::Abs($targetKnockback - $currentKnockback) -gt 0.0001) { $changedFields.Add("knockback_force") | Out-Null }
    if ([math]::Abs($targetAoeRadius - $currentAoeRadius) -gt 0.0001) { $changedFields.Add("aoe_radius") | Out-Null }

    $changedCount = @($changedFields).Count
    if ($changedCount -gt 0) {
        $changedRows++
        $changedFieldsTotal += $changedCount
    }

    $priority = "P3"
    if ($changedCount -gt 0 -and ($requiresPhase3 -or $requiresPhase2 -or $isSpecial)) {
        $priority = "P1"
    }
    elseif ($changedCount -gt 0) {
        $priority = "P2"
    }

    $suggestions.Add([pscustomobject]@{
        prefab_path                     = $prefabPath
        attack_id                       = $attackId
        attack_name                     = $attackName
        baseline_damage                 = To-Text $baseDamage
        current_damage                  = To-Text $currentDamage
        round6_damage                   = To-Text $targetDamage
        baseline_cooldown               = To-Text $baseCooldown
        current_cooldown                = To-Text $currentCooldown
        round6_cooldown                 = To-Text $targetCooldown
        baseline_selection_weight       = To-Text $baseWeight
        current_selection_weight        = To-Text $currentWeight
        round6_selection_weight         = To-Text $targetWeight
        baseline_windup_time            = To-Text $baseWindup
        current_windup_time             = To-Text $currentWindup
        round6_windup_time              = To-Text $targetWindup
        baseline_active_time            = To-Text $baseActive
        current_active_time             = To-Text $currentActive
        round6_active_time              = To-Text $targetActive
        baseline_recovery_time          = To-Text $baseRecovery
        current_recovery_time           = To-Text $currentRecovery
        round6_recovery_time            = To-Text $targetRecovery
        baseline_range                  = To-Text $baseRange
        current_range                   = To-Text $currentRange
        round6_range                    = To-Text $targetRange
        baseline_knockback_force        = To-Text $baseKnockback
        current_knockback_force         = To-Text $currentKnockback
        round6_knockback_force          = To-Text $targetKnockback
        baseline_aoe_radius             = To-Text $baseAoeRadius
        current_aoe_radius              = To-Text $currentAoeRadius
        round6_aoe_radius               = To-Text $targetAoeRadius
        is_special                      = if ($isSpecial) { "true" } else { "false" }
        requires_phase2                 = if ($requiresPhase2) { "true" } else { "false" }
        requires_phase3                 = if ($requiresPhase3) { "true" } else { "false" }
        target_player                   = if ($targetPlayer) { "true" } else { "false" }
        aoe                             = if ($aoe) { "true" } else { "false" }
        changed_fields                  = $changedCount
        changed_field_list              = if ($changedCount -gt 0) { ($changedFields -join "|") } else { "" }
        priority                        = $priority
        rule_id                         = "R6C"
        reason                          = if ($changedCount -gt 0) { "Conservative cap relative to round4 baseline" } else { "Within conservative cap" }
    }) | Out-Null
}

$outDir = Split-Path -Parent $OutputCsv
if (!(Test-Path $outDir)) {
    New-Item -Path $outDir -ItemType Directory | Out-Null
}

$sorted = $suggestions | Sort-Object prefab_path, attack_id
$sorted | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

$total = @($sorted).Count
$p1 = @($sorted | Where-Object { $_.priority -eq "P1" }).Count
$p2 = @($sorted | Where-Object { $_.priority -eq "P2" }).Count
$p3 = @($sorted | Where-Object { $_.priority -eq "P3" }).Count

$summaryLines = @(
    "# Boss Attack Round6 Conservative Suggestions",
    "",
    "- Baseline: $BaselineCsv",
    "- Current: $CurrentFillCsv",
    "- Output: $OutputCsv",
    "- Rows: $total",
    "- Changed rows: $changedRows",
    "- Changed fields total: $changedFieldsTotal",
    "- Priority: P1=$p1, P2=$p2, P3=$p3",
    "",
    "Note: Round6 only pulls back values that exceed conservative caps versus round4 baseline."
)

$summaryDir = Split-Path -Parent $SummaryMd
if (!(Test-Path $summaryDir)) {
    New-Item -Path $summaryDir -ItemType Directory | Out-Null
}

$summaryLines | Set-Content -Path $SummaryMd -Encoding UTF8

Write-Output "Baseline: $BaselineCsv"
Write-Output "Current: $CurrentFillCsv"
Write-Output "Suggestions: $OutputCsv"
Write-Output "Summary: $SummaryMd"
Write-Output ("Rows: {0}, ChangedRows: {1}, ChangedFields: {2}" -f $total, $changedRows, $changedFieldsTotal)
