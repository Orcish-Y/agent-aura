$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'tests\AgentAura.Prototype.UiTests\AgentAura.Prototype.UiTests.csproj'

dotnet run --project $project --nologo
exit $LASTEXITCODE
