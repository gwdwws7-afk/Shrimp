param(
    [string]$InputCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round4_fill.csv",
    [string]$OutputCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round5_auto_suggestions.csv",
    [string]$SummaryMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round5_summary.md"
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

function Boss-Type([string]$prefabPath) {
    if ([string]::IsNullOrWhiteSpace($prefabPath)) { return "unknown" }
    $text = $prefabPath.ToLowerInvariant()
    if ($text.Contains("guardian")) { return "guardian" }
    if ($text.Contains("eel")) { return "eel" }
    return "unknown"
}

if (!(Test-Path $InputCsv)) {
    throw "Input csv not found: $InputCsv"
}

$rows = Import-Csv -Path $InputCsv
$suggestions = [System.Collections.Generic.List[object]]::new()

foreach ($row in $rows) {
    $prefabPath = ([string]$row.prefab_path).Trim()
    $attackId = ([string]$row.attack_id).Trim()
    $attackName = ([string]$row.attack_name).Trim()

    if ([string]::IsNullOrWhiteSpace($prefabPath) -or [string]::IsNullOrWhiteSpace($attackId)) {
        continue
    }

    $damage = Parse-Double $row.damage 0
    $cooldown = Parse-Double $row.cooldown 0
    $selectionWeight = Parse-Double $row.selection_weight 1
    $windup = Parse-Double $row.windup_time 0
    $active = Parse-Double $row.active_time 0
    $recovery = Parse-Double $row.recovery_time 0
    $range = Parse-Double $row.range 0
    $knockback = Parse-Double $row.knockback_force 0
    $aoeRadius = Parse-Double $row.aoe_radius 0

    $isSpecial = Parse-Bool $row.is_special $false
    $requiresPhase2 = Parse-Bool $row.requires_phase2 $false
    $requiresPhase3 = Parse-Bool $row.requires_phase3 $false
    $targetPlayer = Parse-Bool $row.target_player $true
    $aoe = Parse-Bool $row.aoe $false

    $ruleIds = [System.Collections.Generic.List[string]]::new()
    $reasons = [System.Collections.Generic.List[string]]::new()
    $priority = "P2"

    $damageMul = 1.0
    $cooldownMul = 1.0
    $weightMul = 1.0
    $windupMul = 1.0
    $activeMul = 1.0
    $recoveryMul = 1.0
    $rangeMul = 1.0
    $knockbackMul = 1.0
    $aoeRadiusMul = 1.0

    if ($requiresPhase3) {
        $damageMul *= 1.08
        $cooldownMul *= 0.92
        $weightMul *= 1.05
        $ruleIds.Add("PH3")
        $reasons.Add("Phase3 attack boosts burst and shortens cooldown")
        $priority = "P1"
    }
    elseif ($requiresPhase2) {
        $damageMul *= 1.05
        $cooldownMul *= 0.95
        $weightMul *= 1.04
        $ruleIds.Add("PH2")
        $reasons.Add("Phase2 attack gets moderate value and cadence uplift")
        if ($priority -ne "P1") { $priority = "P1" }
    }
    else {
        $damageMul *= 1.02
        $cooldownMul *= 0.97
        $weightMul *= 1.03
        $ruleIds.Add("PH1")
        $reasons.Add("Baseline attack gets a light uplift for pacing continuity")
    }

    if ($isSpecial) {
        $damageMul *= 1.04
        $cooldownMul *= 0.95
        $windupMul *= 1.03
        $ruleIds.Add("SP")
        $reasons.Add("Special attack gains value while keeping readable telegraph")
    }

    if ($aoe) {
        $aoeRadiusMul *= 1.05
        $windupMul *= 1.04
        $activeMul *= 1.03
        $weightMul *= 0.98
        $ruleIds.Add("AOE")
        $reasons.Add("AOE gets wider coverage with slightly lower spam weight")
    }
    else {
        if ($targetPlayer) {
            $rangeMul *= 1.03
            $ruleIds.Add("TP")
            $reasons.Add("Targeted attack gets a small effective range increase")
        }
    }

    switch (Boss-Type $prefabPath) {
        "eel" {
            $cooldownMul *= 0.98
            $recoveryMul *= 0.97
            $rangeMul *= 1.02
            $ruleIds.Add("EEL")
            $reasons.Add("Eel keeps high mobility pressure")
        }
        "guardian" {
            $damageMul *= 1.03
            $windupMul *= 1.03
            $activeMul *= 1.02
            $knockbackMul *= 1.03
            $ruleIds.Add("GUA")
            $reasons.Add("Guardian strengthens heavy-hit feel")
        }
        default {
            $ruleIds.Add("GEN")
            $reasons.Add("General fallback rule")
        }
    }

    $round5Damage = Round3 (Clamp ($damage * $damageMul) 20 280)
    $round5Cooldown = Round3 (Clamp ($cooldown * $cooldownMul) 2.2 14)
    $round5Selection = Round3 (Clamp ($selectionWeight * $weightMul) 0.35 1.6)
    $round5Windup = Round3 (Clamp ($windup * $windupMul) 0.2 1.2)
    $round5Active = Round3 (Clamp ($active * $activeMul) 0.15 0.6)
    $round5Recovery = Round3 (Clamp ($recovery * $recoveryMul) 0.35 1.2)
    $round5Range = Round3 (Clamp ($range * $rangeMul) 3.5 12)
    $round5Knockback = Round3 (Clamp ($knockback * $knockbackMul) 4 16)
    $round5AoeRadius = Round3 (Clamp ($aoeRadius * $aoeRadiusMul) 0 10)

    $deltaCore =
        [math]::Abs($round5Damage - $damage) +
        [math]::Abs($round5Cooldown - $cooldown) +
        [math]::Abs($round5Selection - $selectionWeight)
    if ($deltaCore -lt 0.15 -and $priority -eq "P2") {
        $priority = "P3"
    }

    $suggestions.Add([pscustomobject]@{
        prefab_path                = $prefabPath
        attack_id                  = $attackId
        attack_name                = $attackName
        current_damage             = To-Text $damage
        round5_damage              = To-Text $round5Damage
        current_cooldown           = To-Text $cooldown
        round5_cooldown            = To-Text $round5Cooldown
        current_selection_weight   = To-Text $selectionWeight
        round5_selection_weight    = To-Text $round5Selection
        current_windup_time        = To-Text $windup
        round5_windup_time         = To-Text $round5Windup
        current_active_time        = To-Text $active
        round5_active_time         = To-Text $round5Active
        current_recovery_time      = To-Text $recovery
        round5_recovery_time       = To-Text $round5Recovery
        current_range              = To-Text $range
        round5_range               = To-Text $round5Range
        current_knockback_force    = To-Text $knockback
        round5_knockback_force     = To-Text $round5Knockback
        current_aoe_radius         = To-Text $aoeRadius
        round5_aoe_radius          = To-Text $round5AoeRadius
        is_special                 = if ($isSpecial) { "true" } else { "false" }
        requires_phase2            = if ($requiresPhase2) { "true" } else { "false" }
        requires_phase3            = if ($requiresPhase3) { "true" } else { "false" }
        target_player              = if ($targetPlayer) { "true" } else { "false" }
        aoe                        = if ($aoe) { "true" } else { "false" }
        priority                   = $priority
        rule_id                    = (($ruleIds | Select-Object -Unique) -join "+")
        reason                     = (($reasons | Select-Object -Unique) -join " | ")
    }) | Out-Null
}

$outDir = Split-Path -Parent $OutputCsv
if (!(Test-Path $outDir)) {
    New-Item -Path $outDir -ItemType Directory | Out-Null
}

$sorted = $suggestions | Sort-Object prefab_path, attack_id
$sorted | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

${total} = @($sorted).Count
$p1 = @($sorted | Where-Object { $_.priority -eq "P1" }).Count
$p2 = @($sorted | Where-Object { $_.priority -eq "P2" }).Count
$p3 = @($sorted | Where-Object { $_.priority -eq "P3" }).Count

$avgDamageDelta = 0.0
if ($total -gt 0) {
    $sum = 0.0
    foreach ($r in $sorted) {
        $sum += ([math]::Abs((Parse-Double $r.round5_damage 0) - (Parse-Double $r.current_damage 0)))
    }

    $avgDamageDelta = $sum / $total
}

$summaryLines = @(
    "# Boss Attack Round5 Auto Suggestions",
    "",
    "- Input: $InputCsv",
    "- Output: $OutputCsv",
    "- Rows: $total",
    "- Priority: P1=$p1, P2=$p2, P3=$p3",
    ("- Avg |damage delta|: {0:0.###}" -f $avgDamageDelta),
    "",
    "Note: This table is an auto-suggestion baseline. Fine-tune fill.csv before import if needed."
)

$summaryDir = Split-Path -Parent $SummaryMd
if (!(Test-Path $summaryDir)) {
    New-Item -Path $summaryDir -ItemType Directory | Out-Null
}

$summaryLines | Set-Content -Path $SummaryMd -Encoding UTF8

Write-Output "Input: $InputCsv"
Write-Output "Suggestions: $OutputCsv"
Write-Output "Summary: $SummaryMd"
Write-Output ("Rows: " + $total)
