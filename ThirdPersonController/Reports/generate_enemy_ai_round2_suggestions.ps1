param(
    [string]$MetricsCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p1_sampling_metrics_template.csv",
    [string]$OutputCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_round2_auto_suggestions.csv"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$assetMap = @{
    "grunt"  = "C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Grunt.asset"
    "rusher" = "C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Rusher.asset"
    "tank"   = "C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Tank.asset"
    "elite"  = "C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Elite.asset"
}

function Parse-Double([object]$value) {
    if ($null -eq $value) { return [double]::NaN }
    $text = [string]$value
    if ([string]::IsNullOrWhiteSpace($text)) { return [double]::NaN }
    $num = 0.0
    if ([double]::TryParse($text, [ref]$num)) { return $num }
    return [double]::NaN
}

function Normalize-Ratio([object]$value) {
    $num = Parse-Double $value
    if ([double]::IsNaN($num)) { return [double]::NaN }
    if ($num -gt 1.0) { $num = $num / 100.0 }
    if ($num -lt 0.0) { $num = 0.0 }
    if ($num -gt 1.0) { $num = 1.0 }
    return $num
}

function Clamp([double]$value, [double]$min, [double]$max) {
    if ($value -lt $min) { return $min }
    if ($value -gt $max) { return $max }
    return $value
}

function To-RoundText([double]$value) {
    return ("{0:0.###}" -f $value)
}

function Get-FieldCurrent([string]$assetPath, [string]$field) {
    if (!(Test-Path $assetPath)) { return $null }
    $pattern = '^  ' + [regex]::Escape($field) + ':\s*(.+)$'
    $line = Select-String -Path $assetPath -Pattern $pattern | Select-Object -First 1
    if ($null -eq $line) { return $null }
    return $line.Matches[0].Groups[1].Value.Trim()
}

if (!(Test-Path $MetricsCsv)) {
    throw "Metrics csv not found: $MetricsCsv"
}

$rows = Import-Csv -Path $MetricsCsv
$suggestions = [System.Collections.Generic.List[object]]::new()

function Add-Suggestion(
    [string]$archetype,
    [string]$field,
    [double]$suggestedNumericValue,
    [double]$min,
    [double]$max,
    [string]$ruleId,
    [string]$priority,
    [string]$reason
) {
    $aKey = $archetype.Trim().ToLowerInvariant()
    if (!$assetMap.ContainsKey($aKey)) { return }
    $assetPath = $assetMap[$aKey]
    $currentText = Get-FieldCurrent $assetPath $field
    if ($null -eq $currentText) { return }
    $current = Parse-Double $currentText
    if ([double]::IsNaN($current)) { return }

    $target = Clamp $suggestedNumericValue $min $max
    if ([math]::Abs($target - $current) -lt 0.0001) { return }

    $delta = $target - $current
    $suggestions.Add([pscustomobject]@{
        asset_path       = $assetPath
        archetype        = $archetype
        field            = $field
        current_value    = To-RoundText $current
        round2_value     = To-RoundText $target
        delta            = To-RoundText $delta
        priority         = $priority
        rule_id          = $ruleId
        reason           = $reason
    }) | Out-Null
}

foreach ($row in $rows) {
    $archetype = ([string]$row.archetype).Trim().ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($archetype)) { continue }
    if (!$assetMap.ContainsKey($archetype)) { continue }

    $attackRatio = Normalize-Ratio $row.attack_ratio
    $chargeRatio = Normalize-Ratio $row.charge_ratio
    $dodgeRatio = Normalize-Ratio $row.dodge_ratio
    $blockRatio = Normalize-Ratio $row.block_ratio
    $dominantRatio = Normalize-Ratio $row.dominant_ratio
    $tokenRejectRate = Normalize-Ratio $row.token_reject_rate
    $tokenUtilization = Normalize-Ratio $row.token_utilization
    $dominantState = ([string]$row.dominant_state).Trim().ToLowerInvariant()

    # Generic rules
    if (![double]::IsNaN($tokenUtilization) -and $tokenUtilization -lt 0.30) {
        Add-Suggestion $archetype "ringStandoffDistance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "ringStandoffDistance")) - 0.2) 1.0 4.5 "G1" "P1" "Low token utilization: tighten ring distance"
        Add-Suggestion $archetype "attackCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "attackCooldown")) - 0.08) 0.75 3.0 "G2" "P1" "Low token utilization: raise attack frequency"
    }

    if (![double]::IsNaN($tokenRejectRate) -and $tokenRejectRate -gt 0.55) {
        Add-Suggestion $archetype "attackCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "attackCooldown")) + 0.08) 0.75 3.0 "G3" "P2" "High token reject rate: soften attack contention"
    }

    switch ($archetype) {
        "grunt" {
            if (![double]::IsNaN($attackRatio) -and $attackRatio -lt 0.20) {
                Add-Suggestion $archetype "attackCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "attackCooldown")) - 0.08) 1.1 2.2 "GR1" "P1" "Grunt attack share too low"
                Add-Suggestion $archetype "chaseSpeed" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chaseSpeed")) + 0.15) 3.5 5.2 "GR2" "P2" "Grunt chase pressure too weak"
            }
            if (![double]::IsNaN($attackRatio) -and $attackRatio -gt 0.35) {
                Add-Suggestion $archetype "attackCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "attackCooldown")) + 0.08) 1.1 2.2 "GR3" "P2" "Grunt attack share too high"
            }
            if (![double]::IsNaN($dodgeRatio) -and $dodgeRatio -gt 0.05) {
                Add-Suggestion $archetype "dodgeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "dodgeChance")) - 0.01) 0.0 0.06 "GR4" "P2" "Grunt dodge share should remain low"
            }
        }
        "rusher" {
            if (![double]::IsNaN($chargeRatio) -and $chargeRatio -lt 0.12) {
                Add-Suggestion $archetype "chargeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chargeChance")) + 0.05) 0.10 0.60 "RU1" "P1" "Rusher charge share too low"
                Add-Suggestion $archetype "chargeMaxDistance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chargeMaxDistance")) + 0.25) 3.8 6.2 "RU2" "P2" "Expand charge trigger distance"
            }
            if (![double]::IsNaN($chargeRatio) -and $chargeRatio -gt 0.25) {
                Add-Suggestion $archetype "chargeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chargeChance")) - 0.04) 0.10 0.60 "RU3" "P2" "Rusher charge share too high"
                Add-Suggestion $archetype "chargeCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chargeCooldown")) + 0.2) 2.0 5.0 "RU4" "P2" "Reduce charge cadence"
            }
            if (![double]::IsNaN($dodgeRatio) -and $dodgeRatio -lt 0.10) {
                Add-Suggestion $archetype "dodgeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "dodgeChance")) + 0.03) 0.20 0.50 "RU5" "P2" "Rusher dodge share too low"
                Add-Suggestion $archetype "dodgeCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "dodgeCooldown")) - 0.12) 1.2 3.0 "RU6" "P3" "Increase dodge retry frequency"
            }
            if (![double]::IsNaN($dodgeRatio) -and $dodgeRatio -gt 0.20) {
                Add-Suggestion $archetype "dodgeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "dodgeChance")) - 0.03) 0.20 0.50 "RU7" "P2" "Rusher dodge share too high"
            }
        }
        "tank" {
            if (![double]::IsNaN($blockRatio) -and $blockRatio -lt 0.18) {
                Add-Suggestion $archetype "blockChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "blockChance")) + 0.05) 0.25 0.70 "TA1" "P1" "Tank block share too low"
                Add-Suggestion $archetype "blockDuration" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "blockDuration")) + 0.08) 0.40 1.00 "TA2" "P2" "Extend block window slightly"
            }
            if (![double]::IsNaN($blockRatio) -and $blockRatio -gt 0.35) {
                Add-Suggestion $archetype "blockChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "blockChance")) - 0.05) 0.25 0.70 "TA3" "P1" "Tank block share too high"
                Add-Suggestion $archetype "blockCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "blockCooldown")) + 0.18) 1.8 4.0 "TA4" "P2" "Open more windows after block"
            }
            if (![double]::IsNaN($attackRatio) -and $attackRatio -lt 0.18) {
                Add-Suggestion $archetype "attackCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "attackCooldown")) - 0.10) 1.6 3.0 "TA5" "P2" "Tank attack share too low"
            }
        }
        "elite" {
            if (![double]::IsNaN($dominantRatio) -and $dominantRatio -gt 0.55) {
                switch ($dominantState) {
                    "charge" {
                        Add-Suggestion $archetype "chargeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chargeChance")) - 0.05) 0.15 0.50 "EL1" "P1" "Elite over-dominant charge"
                        Add-Suggestion $archetype "dodgeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "dodgeChance")) + 0.02) 0.20 0.45 "EL2" "P2" "Increase behavior variety"
                    }
                    "dodge" {
                        Add-Suggestion $archetype "dodgeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "dodgeChance")) - 0.04) 0.20 0.45 "EL3" "P1" "Elite over-dominant dodge"
                        Add-Suggestion $archetype "blockChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "blockChance")) + 0.03) 0.20 0.45 "EL4" "P2" "Increase defense swap"
                    }
                    "block" {
                        Add-Suggestion $archetype "blockChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "blockChance")) - 0.04) 0.20 0.45 "EL5" "P1" "Elite over-dominant block"
                        Add-Suggestion $archetype "chargeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chargeChance")) + 0.03) 0.15 0.50 "EL6" "P2" "Increase offensive swap"
                    }
                    default {
                        Add-Suggestion $archetype "attackCooldown" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "attackCooldown")) + 0.08) 0.9 2.2 "EL7" "P2" "Elite over-dominant attack"
                        Add-Suggestion $archetype "chargeChance" ((Parse-Double (Get-FieldCurrent $assetMap[$archetype] "chargeChance")) + 0.02) 0.15 0.50 "EL8" "P3" "Increase behavior variety"
                    }
                }
            }
        }
    }
}

$outDir = Split-Path -Parent $OutputCsv
if (!(Test-Path $outDir)) {
    New-Item -Path $outDir -ItemType Directory | Out-Null
}

$priorityRank = @{
    "P1" = 1
    "P2" = 2
    "P3" = 3
}

$merged = foreach ($group in ($suggestions | Group-Object archetype, field)) {
    $candidates = $group.Group
    $best = $candidates |
        Sort-Object `
            @{ Expression = { if ($priorityRank.ContainsKey($_.priority)) { $priorityRank[$_.priority] } else { 99 } } }, `
            @{ Expression = { -[math]::Abs((Parse-Double $_.delta)) } } |
        Select-Object -First 1

    $ruleIds = ($candidates | Select-Object -ExpandProperty rule_id -Unique) -join "+"
    $reasons = ($candidates | Select-Object -ExpandProperty reason -Unique) -join " | "

    [pscustomobject]@{
        asset_path    = $best.asset_path
        archetype     = $best.archetype
        field         = $best.field
        current_value = $best.current_value
        round2_value  = $best.round2_value
        delta         = $best.delta
        priority      = $best.priority
        rule_id       = $ruleIds
        reason        = $reasons
    }
}

$sorted = $merged | Sort-Object archetype, priority, field
$sorted | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

Write-Output "Metrics: $MetricsCsv"
Write-Output "Suggestions: $OutputCsv"
Write-Output ("Rows: " + $sorted.Count)
