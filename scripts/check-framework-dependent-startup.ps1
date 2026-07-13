param(
    [ValidateSet('RuntimePresent', 'RuntimeMissing')]
    [string]$Scenario,
    [string]$PublishDirectory,
    [int]$ObservationSeconds = 15
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $Scenario) {
    throw 'Specify -Scenario RuntimePresent on a Windows 11 machine with .NET 10 Windows Desktop Runtime, or -Scenario RuntimeMissing on one without it.'
}

if (-not $PublishDirectory) {
    $PublishDirectory = Join-Path $repoRoot 'src\AgentAura.Prototype\bin\Release\net10.0-windows\win-x64\publish'
}

& (Join-Path $PSScriptRoot 'publish-framework-dependent.ps1') -OutputDirectory $PublishDirectory

$launcher = Join-Path $PublishDirectory 'Start-AgentAura.cmd'
& $launcher --check-runtime | Out-Null
$runtimeAvailable = $LASTEXITCODE -eq 0

if ($Scenario -eq 'RuntimePresent') {
    if (-not $runtimeAvailable) {
        throw 'RuntimePresent requires a machine with the .NET 10 Windows Desktop Runtime installed.'
    }

    $existingProcessIds = @(Get-Process -Name 'AgentAura.Prototype' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
    $launcherProcess = Start-Process $launcher -PassThru
    $launcherProcess.WaitForExit(5000) | Out-Null

    $process = $null
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    while ($null -eq $process -and [DateTime]::UtcNow -lt $deadline) {
        $process = Get-Process -Name 'AgentAura.Prototype' -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $existingProcessIds } |
            Select-Object -First 1
        if ($null -eq $process) {
            Start-Sleep -Milliseconds 100
        }
    }

    if ($null -eq $process) {
        throw 'Start-AgentAura.cmd did not start Agent Aura with the required runtime present.'
    }

    try {
        if ($process.WaitForExit($ObservationSeconds * 1000)) {
            throw "Agent Aura exited during the $ObservationSeconds-second startup observation window (exit code $($process.ExitCode))."
        }

        Write-Host "PASS: Framework-dependent Agent Aura stayed alive for $ObservationSeconds seconds with the required runtime present."
    }
    finally {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
    }

    exit 0
}

if ($runtimeAvailable) {
    throw 'RuntimeMissing must run on a separate Windows 11 environment without the .NET 10 Windows Desktop Runtime.'
}

$diagnostics = 'N' | & $launcher
if ($LASTEXITCODE -eq 0) {
    throw 'Start-AgentAura.cmd reported success in the runtime-missing environment.'
}

$diagnosticsText = $diagnostics -join [Environment]::NewLine
if ($diagnosticsText -notmatch 'requires the Microsoft .NET 10 Windows Desktop Runtime for x64' -or
    $diagnosticsText -notmatch [regex]::Escape('https://dotnet.microsoft.com/download/dotnet/10.0/runtime') -or
    $diagnosticsText -notmatch 'Open the official Microsoft download page now') {
    throw 'The runtime-missing launcher path did not present the required user-controlled Microsoft installer recovery prompt.'
}

Write-Host 'PASS: The runtime-missing environment is blocked before application launch and presents the user-controlled Microsoft installer recovery path.'
