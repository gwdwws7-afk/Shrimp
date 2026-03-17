param(
    [string]$ProjectPath = "C:\test\Shrimp",
    [string]$UnityPath = "",
    [string]$LogFile = "C:\test\Shrimp\Logs\EnemyTypeSceneGate.log",
    [string]$ExecuteMethod = "EnemyTypeBindingValidationMenu.ValidateBindingsInScenesCiGate",
    [int]$WaitForProjectUnlockSeconds = 30,
    [int]$ProcessTimeoutSeconds = 1200,
    [switch]$NoGraphics
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-UnityPath([string]$projectPath, [string]$explicitUnityPath) {
    if (![string]::IsNullOrWhiteSpace($explicitUnityPath)) {
        if (!(Test-Path $explicitUnityPath)) {
            throw "Unity executable not found: $explicitUnityPath"
        }

        return $explicitUnityPath
    }

    $projectVersionFile = Join-Path $projectPath "ProjectSettings\ProjectVersion.txt"
    if (!(Test-Path $projectVersionFile)) {
        throw "ProjectVersion.txt not found: $projectVersionFile"
    }

    $versionLine = Get-Content $projectVersionFile | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionLine)) {
        throw "Cannot parse m_EditorVersion from $projectVersionFile"
    }

    $version = ($versionLine.Split(':')[1]).Trim()
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "C:\PROGRA~1\Unity\Hub\Editor\$version\Editor\Unity.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Unity executable not found for version $version. Checked: $($candidates -join '; ')"
}

function Get-UnityProjectProcesses([string]$projectPath) {
    $normalized = $projectPath.Replace('/', '\')
    $allUnity = Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" -ErrorAction SilentlyContinue
    if ($null -eq $allUnity) {
        return @()
    }

    return @($allUnity | Where-Object {
            $cmd = [string]$_.CommandLine
            if ([string]::IsNullOrWhiteSpace($cmd)) {
                return $false
            }

            $cmd.Replace('/', '\').IndexOf($normalized, [StringComparison]::OrdinalIgnoreCase) -ge 0
        })
}

function Wait-ForProjectUnlock([string]$projectPath, [int]$timeoutSeconds) {
    $waitSeconds = [Math]::Max(0, $timeoutSeconds)
    $deadline = (Get-Date).AddSeconds($waitSeconds)

    while ($true) {
        $running = @(Get-UnityProjectProcesses -projectPath $projectPath)
        if ($running.Count -eq 0) {
            return $true
        }

        if ((Get-Date) -ge $deadline) {
            return $false
        }

        Start-Sleep -Seconds 2
    }
}

function Invoke-UnityExecuteMethod(
    [string]$unityExe,
    [string]$projectPath,
    [string]$executeMethod,
    [string]$logFile,
    [switch]$noGraphics,
    [int]$timeoutSeconds
) {
    $args = New-Object System.Collections.Generic.List[string]
    $args.Add("-batchmode")
    if ($noGraphics.IsPresent) {
        $args.Add("-nographics")
    }

    $args.Add("-quit")
    $args.Add("-projectPath")
    $args.Add($projectPath)
    $args.Add("-executeMethod")
    $args.Add($executeMethod)
    $args.Add("-logFile")
    $args.Add($logFile)

    $process = Start-Process -FilePath $unityExe -ArgumentList $args -PassThru
    $timeoutMs = [Math]::Max(1, $timeoutSeconds) * 1000
    $completed = $process.WaitForExit($timeoutMs)
    if (-not $completed) {
        try {
            Stop-Process -Id $process.Id -Force
        }
        catch {
        }

        return 124
    }

    return $process.ExitCode
}

$projectPathResolved = (Resolve-Path $ProjectPath).Path
$unityExe = Resolve-UnityPath -projectPath $projectPathResolved -explicitUnityPath $UnityPath

$logDir = Split-Path -Parent $LogFile
if (![string]::IsNullOrWhiteSpace($logDir)) {
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
}

if (-not (Wait-ForProjectUnlock -projectPath $projectPathResolved -timeoutSeconds $WaitForProjectUnlockSeconds)) {
    throw "Project is already open by another Unity process: $projectPathResolved"
}

Write-Host "[EnemyTypeSceneGate] run method=$ExecuteMethod unity=`"$unityExe`""
$exitCode = Invoke-UnityExecuteMethod `
    -unityExe $unityExe `
    -projectPath $projectPathResolved `
    -executeMethod $ExecuteMethod `
    -logFile $LogFile `
    -noGraphics:$NoGraphics `
    -timeoutSeconds $ProcessTimeoutSeconds

if (Test-Path $LogFile) {
    Write-Host "[EnemyTypeSceneGate] log file: $LogFile"
    $summaryLines = Select-String `
        -Path $LogFile `
        -Pattern '^\[EnemyTypeBindingValidation\] Scanned scenes:|^ Scanned scene enemies:|^ Scanned scene strongholds:|^ Scanned scene wave groups:|^ Errors:|^ Warnings:'
    foreach ($line in $summaryLines) {
        Write-Host "[EnemyTypeSceneGate] $($line.Line)"
    }
}

if ($exitCode -eq 124) {
    throw "Enemy type scene gate timed out after $ProcessTimeoutSeconds s."
}

if ($exitCode -ne 0) {
    throw "Enemy type scene gate failed (exit=$exitCode). Check log: $LogFile"
}

Write-Host "[EnemyTypeSceneGate] passed (exit=$exitCode)"
exit $exitCode
