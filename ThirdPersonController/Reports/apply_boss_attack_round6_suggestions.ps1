param(
    [string]$SuggestionsCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round6_conservative_suggestions.csv",
    [string]$FillCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round4_fill.csv",
    [string]$ReportCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\boss_attack_tuning_round6_apply_report.csv",
    [string]$BackupCsv = ""
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

function To-Text([double]$value) {
    return ("{0:0.###}" -f $value)
}

function Make-Key([string]$prefabPath, [string]$attackId) {
    $p = if ($null -eq $prefabPath) { "" } else { $prefabPath.Trim().Replace('\', '/') }
    $a = if ($null -eq $attackId) { "" } else { $attackId.Trim() }
    return ($p + "|" + $a).ToLowerInvariant()
}

if (!(Test-Path $SuggestionsCsv)) {
    throw "Suggestions csv not found: $SuggestionsCsv"
}

if (!(Test-Path $FillCsv)) {
    throw "Fill csv not found: $FillCsv"
}

if ([string]::IsNullOrWhiteSpace($BackupCsv)) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $BackupCsv = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($FillCsv), "boss_attack_tuning_round4_fill_backup_$timestamp.csv")
}

Copy-Item -Path $FillCsv -Destination $BackupCsv -Force

$suggestions = Import-Csv -Path $SuggestionsCsv
$fillRows = Import-Csv -Path $FillCsv

$suggestionMap = @{}
foreach ($s in $suggestions) {
    $key = Make-Key ([string]$s.prefab_path) ([string]$s.attack_id)
    if ([string]::IsNullOrWhiteSpace($key)) { continue }
    $suggestionMap[$key] = $s
}

$updatedRows = [System.Collections.Generic.List[object]]::new()
$reportRows = [System.Collections.Generic.List[object]]::new()

foreach ($row in $fillRows) {
    $prefabPath = [string]$row.prefab_path
    $attackId = [string]$row.attack_id
    $key = Make-Key $prefabPath $attackId

    if (!$suggestionMap.ContainsKey($key)) {
        $updatedRows.Add([pscustomobject]@{
            prefab_path       = $row.prefab_path
            attack_id         = $row.attack_id
            attack_name       = $row.attack_name
            damage            = $row.damage
            cooldown          = $row.cooldown
            selection_weight  = $row.selection_weight
            windup_time       = $row.windup_time
            active_time       = $row.active_time
            recovery_time     = $row.recovery_time
            range             = $row.range
            knockback_force   = $row.knockback_force
            is_special        = $row.is_special
            requires_phase2   = $row.requires_phase2
            requires_phase3   = $row.requires_phase3
            target_player     = $row.target_player
            aoe               = $row.aoe
            aoe_radius        = $row.aoe_radius
            note              = $row.note
        }) | Out-Null

        $reportRows.Add([pscustomobject]@{
            prefab_path      = $prefabPath
            attack_id        = $attackId
            status           = "Gap"
            changed_fields   = 0
            note             = "suggestion-missing"
        }) | Out-Null
        continue
    }

    $s = $suggestionMap[$key]
    $changed = 0
    $changes = [System.Collections.Generic.List[string]]::new()

    $damage = To-Text (Parse-Double $s.round6_damage (Parse-Double $row.damage 0))
    $cooldown = To-Text (Parse-Double $s.round6_cooldown (Parse-Double $row.cooldown 0))
    $selection = To-Text (Parse-Double $s.round6_selection_weight (Parse-Double $row.selection_weight 1))
    $windup = To-Text (Parse-Double $s.round6_windup_time (Parse-Double $row.windup_time 0))
    $active = To-Text (Parse-Double $s.round6_active_time (Parse-Double $row.active_time 0))
    $recovery = To-Text (Parse-Double $s.round6_recovery_time (Parse-Double $row.recovery_time 0))
    $range = To-Text (Parse-Double $s.round6_range (Parse-Double $row.range 0))
    $knockback = To-Text (Parse-Double $s.round6_knockback_force (Parse-Double $row.knockback_force 0))
    $aoeRadius = To-Text (Parse-Double $s.round6_aoe_radius (Parse-Double $row.aoe_radius 0))

    if ([string]$row.damage -ne $damage) { $changed++; $changes.Add("damage") | Out-Null }
    if ([string]$row.cooldown -ne $cooldown) { $changed++; $changes.Add("cooldown") | Out-Null }
    if ([string]$row.selection_weight -ne $selection) { $changed++; $changes.Add("selection_weight") | Out-Null }
    if ([string]$row.windup_time -ne $windup) { $changed++; $changes.Add("windup_time") | Out-Null }
    if ([string]$row.active_time -ne $active) { $changed++; $changes.Add("active_time") | Out-Null }
    if ([string]$row.recovery_time -ne $recovery) { $changed++; $changes.Add("recovery_time") | Out-Null }
    if ([string]$row.range -ne $range) { $changed++; $changes.Add("range") | Out-Null }
    if ([string]$row.knockback_force -ne $knockback) { $changed++; $changes.Add("knockback_force") | Out-Null }
    if ([string]$row.aoe_radius -ne $aoeRadius) { $changed++; $changes.Add("aoe_radius") | Out-Null }

    $rule = [string]$s.rule_id
    $note = [string]$row.note
    if (![string]::IsNullOrWhiteSpace($rule)) {
        $tag = "[R6:$rule]"
        if ([string]::IsNullOrWhiteSpace($note)) {
            $note = $tag
        }
        elseif (-not $note.Contains($tag)) {
            $note = ($note.Trim() + " " + $tag).Trim()
        }
    }

    $updatedRows.Add([pscustomobject]@{
        prefab_path       = $row.prefab_path
        attack_id         = $row.attack_id
        attack_name       = $row.attack_name
        damage            = $damage
        cooldown          = $cooldown
        selection_weight  = $selection
        windup_time       = $windup
        active_time       = $active
        recovery_time     = $recovery
        range             = $range
        knockback_force   = $knockback
        is_special        = $row.is_special
        requires_phase2   = $row.requires_phase2
        requires_phase3   = $row.requires_phase3
        target_player     = $row.target_player
        aoe               = $row.aoe
        aoe_radius        = $aoeRadius
        note              = $note
    }) | Out-Null

    $reportRows.Add([pscustomobject]@{
        prefab_path      = $prefabPath
        attack_id        = $attackId
        status           = if ($changed -gt 0) { "Fixed" } else { "Ok" }
        changed_fields   = $changed
        note             = if (@($changes).Count -gt 0) { ($changes -join "|") } else { "" }
    }) | Out-Null
}

$fillDir = Split-Path -Parent $FillCsv
if (!(Test-Path $fillDir)) {
    New-Item -Path $fillDir -ItemType Directory | Out-Null
}

$reportDir = Split-Path -Parent $ReportCsv
if (!(Test-Path $reportDir)) {
    New-Item -Path $reportDir -ItemType Directory | Out-Null
}

$updatedRows | Export-Csv -Path $FillCsv -NoTypeInformation -Encoding UTF8
$reportRows | Export-Csv -Path $ReportCsv -NoTypeInformation -Encoding UTF8

$fixedCount = @($reportRows | Where-Object { $_.status -eq "Fixed" }).Count
$okCount = @($reportRows | Where-Object { $_.status -eq "Ok" }).Count
$gapCount = @($reportRows | Where-Object { $_.status -eq "Gap" }).Count

Write-Output "Suggestions: $SuggestionsCsv"
Write-Output "Fill updated: $FillCsv"
Write-Output "Fill backup: $BackupCsv"
Write-Output "Report: $ReportCsv"
Write-Output ("Summary: total={0} fixed={1} ok={2} gap={3}" -f @($reportRows).Count, $fixedCount, $okCount, $gapCount)
