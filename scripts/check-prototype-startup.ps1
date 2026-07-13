param(
    [int]$ObservationSeconds = 15
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\AgentAura.Prototype\AgentAura.Prototype.csproj'
$executable = Join-Path $repoRoot 'src\AgentAura.Prototype\bin\Debug\net10.0-windows\AgentAura.Prototype.exe'
$stdout = Join-Path $env:TEMP 'agent-aura-prototype-startup.stdout.log'
$stderr = Join-Path $env:TEMP 'agent-aura-prototype-startup.stderr.log'

dotnet build $project --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    throw "Prototype build failed with exit code $LASTEXITCODE."
}

Remove-Item $stdout, $stderr -Force -ErrorAction SilentlyContinue
$process = Start-Process $executable -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr

try {
    if ($process.WaitForExit($ObservationSeconds * 1000)) {
        $diagnostics = @(
            Get-Content $stdout -Raw -ErrorAction SilentlyContinue
            Get-Content $stderr -Raw -ErrorAction SilentlyContinue
        ) -join [Environment]::NewLine

        throw "Prototype exited during the $ObservationSeconds-second startup observation window (exit code $($process.ExitCode)).`n$diagnostics"
    }

    Write-Host "PASS: Prototype remained alive for $ObservationSeconds seconds after startup."
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }

    Remove-Item $stdout, $stderr -Force -ErrorAction SilentlyContinue
}
