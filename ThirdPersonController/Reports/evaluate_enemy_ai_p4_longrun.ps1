param(
    [string]$MetricsCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_metrics.csv",
    [string]$GateCsv = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_gate_config.csv",
    [string]$OutputMd = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_p4_longrun_gate_report.md",
    [string]$BaselineCsv = ""
)

$root = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$gateScript = Join-Path $root "evaluate_enemy_ai_p4_gate.ps1"
if (!(Test-Path $gateScript)) {
    throw "Base gate script not found: $gateScript"
}

& $gateScript -MetricsCsv $MetricsCsv -GateCsv $GateCsv -OutputMd $OutputMd -BaselineCsv $BaselineCsv
exit $LASTEXITCODE
