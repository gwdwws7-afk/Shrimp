Get-CimInstance Win32_Process -Filter "name='Unity.exe'" |
  Where-Object { $_.CommandLine -like '*-projectPath C:\test\Shrimp*' -and $_.CommandLine -like '*-runTests*' } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force; Write-Output ("stopped pid=" + $_.ProcessId) }

$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2c1\Editor\Unity.exe"
$results = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\PlayModeBatchResults_boss_round4_event_storm_smoke.xml"
$log = "C:\test\Shrimp\Assets\ThirdPersonController\Reports\PlayModeBatchRunner_boss_round4_event_storm_smoke.log"
if (Test-Path $results) { Remove-Item $results -Force }

$args = @(
  "-batchmode",
  "-projectPath", "C:\test\Shrimp",
  "-runTests",
  "-testPlatform", "PlayMode",
  "-testResults", $results,
  "-testFilter", "ThirdPersonController.Tests.BossLevel10GateRegressionTests.Level08To10SceneSwitch_BossDefeatEventStorm_ResolvesSingleCompletion",
  "-logFile", $log
)

$process = Start-Process -FilePath $unity -ArgumentList $args -PassThru
$completed = $process.WaitForExit(600000)
if (-not $completed) {
  try { Stop-Process -Id $process.Id -Force } catch {}
  Write-Output "timeout"
  exit 124
}

Write-Output ("exit=" + $process.ExitCode)
if (!(Test-Path $results)) {
  Write-Output "results_missing"
  exit 2
}

[xml]$xml = Get-Content $results
$run = $xml.SelectSingleNode('//test-run')
if ($run -ne $null) {
  Write-Output ("summary total=" + $run.total + " passed=" + $run.passed + " failed=" + $run.failed + " skipped=" + $run.skipped)
}
$case = $xml.SelectSingleNode("//test-case[contains(@fullname,'BossLevel10GateRegressionTests.Level08To10SceneSwitch_BossDefeatEventStorm_ResolvesSingleCompletion')]")
if ($case -ne $null) {
  Write-Output ("case result=" + $case.result + " label=" + $case.label)
}
