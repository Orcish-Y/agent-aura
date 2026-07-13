param(
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\AgentAura.Prototype\AgentAura.Prototype.csproj'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'src\AgentAura.Prototype\bin\Release\net10.0-windows\win-x64\publish'
}

dotnet publish $project --configuration Release --runtime win-x64 --self-contained false --output $OutputDirectory --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Framework-dependent publish failed with exit code $LASTEXITCODE."
}

$requiredFiles = @(
    'AgentAura.Prototype.exe',
    'AgentAura.Prototype.dll',
    'AgentAura.Prototype.runtimeconfig.json',
    'Start-AgentAura.cmd'
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path (Join-Path $OutputDirectory $file))) {
        throw "Framework-dependent publish is missing $file."
    }
}

$runtimeConfig = Get-Content (Join-Path $OutputDirectory 'AgentAura.Prototype.runtimeconfig.json') -Raw | ConvertFrom-Json
$windowsDesktopFramework = $runtimeConfig.runtimeOptions.frameworks |
    Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' } |
    Select-Object -First 1

if ($null -eq $windowsDesktopFramework -or -not $windowsDesktopFramework.version.StartsWith('10.')) {
    throw 'Published runtime configuration does not require the .NET 10 Windows Desktop Runtime.'
}

$bundledRuntimeFiles = @('coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll', 'PresentationCore.dll', 'PresentationFramework.dll') |
    Where-Object { Test-Path (Join-Path $OutputDirectory $_) }

if ($bundledRuntimeFiles.Count -gt 0) {
    throw "Publish unexpectedly bundled runtime files: $($bundledRuntimeFiles -join ', ')."
}

$sizeBytes = (Get-ChildItem $OutputDirectory -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host "PASS: Framework-dependent win-x64 package published to $OutputDirectory."
Write-Host ("Package size: {0:N2} MiB (the machine-wide .NET runtime is excluded)." -f ($sizeBytes / 1MB))

$dotnetRoot = Join-Path $env:ProgramFiles 'dotnet\shared'
$runtimeDirectories = @(
    'Microsoft.NETCore.App',
    'Microsoft.WindowsDesktop.App'
) | ForEach-Object {
    $runtimeFamilyDirectory = Join-Path $dotnetRoot $_
    Get-ChildItem $runtimeFamilyDirectory -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like '10.*' } |
        Sort-Object { [version]$_.Name } -Descending |
        Select-Object -First 1
}

if ($runtimeDirectories.Count -eq 2) {
    $runtimeSizeBytes = ($runtimeDirectories | Get-ChildItem -Recurse -File | Measure-Object -Property Length -Sum).Sum
    Write-Host ("Machine-wide .NET 10 runtime footprint: {0:N2} MiB (shared prerequisite; excluded from package size)." -f ($runtimeSizeBytes / 1MB))
}
else {
    Write-Host 'Machine-wide .NET 10 runtime footprint: not measured because both required shared runtime directories were not found.'
}
