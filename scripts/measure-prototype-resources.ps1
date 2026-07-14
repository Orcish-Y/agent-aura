param(
    [ValidateRange(1, 20)]
    [int]$StartupRuns = 5,
    [ValidateRange(0, 3600)]
    [int]$StartupCooldownSeconds = 10,
    [ValidateRange(1, 3600)]
    [int]$WarmupSeconds = 15,
    [ValidateRange(1, 86400)]
    [int]$InteractionSeconds = 300,
    [ValidateRange(1, 86400)]
    [int]$ObservationSeconds = 1500,
    [ValidateRange(1, 300)]
    [int]$SampleIntervalSeconds = 5,
    [string]$PublishDirectory,
    [string]$OutputDirectory,
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $PublishDirectory) {
    $PublishDirectory = Join-Path $repoRoot 'src\AgentAura.Prototype\bin\Release\net10.0-windows\win-x64\publish'
}

if (-not $OutputDirectory) {
    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $OutputDirectory = Join-Path $repoRoot ".scratch\agent-aura-mvp\evidence\resource-baseline-$timestamp"
}

$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$samplesPath = Join-Path $OutputDirectory 'samples.csv'
$startupPath = Join-Path $OutputDirectory 'startup-runs.csv'
$environmentPath = Join-Path $OutputDirectory 'environment.json'
$summaryPath = Join-Path $OutputDirectory 'summary.json'
$publishLogPath = Join-Path $OutputDirectory 'publish.log'
$localRunDirectory = Join-Path ([IO.Path]::GetTempPath()) "AgentAura-resource-measurement-$PID-$([Guid]::NewGuid().ToString('N'))"

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class AgentAuraResourceMeasurementNativeMethods
{
    public delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr window, out RECT rectangle);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr window);

    [DllImport("kernel32.dll")]
    public static extern uint SetThreadExecutionState(uint executionState);
}
'@

function Get-Median {
    param([double[]]$Values)

    if ($Values.Count -eq 0) {
        return $null
    }

    $ordered = @($Values | Sort-Object)
    $middle = [int][Math]::Floor($ordered.Count / 2)
    if ($ordered.Count % 2 -eq 1) {
        return [double]$ordered[$middle]
    }

    return ([double]$ordered[$middle - 1] + [double]$ordered[$middle]) / 2
}

function Get-Percentile {
    param(
        [double[]]$Values,
        [ValidateRange(0, 1)]
        [double]$Percentile
    )

    if ($Values.Count -eq 0) {
        return $null
    }

    $ordered = @($Values | Sort-Object)
    $index = [Math]::Max(0, [Math]::Ceiling($Percentile * $ordered.Count) - 1)
    return [double]$ordered[$index]
}

function Get-SlopePerMinute {
    param(
        [object[]]$Rows,
        [string]$Property
    )

    if ($Rows.Count -lt 2) {
        return 0.0
    }

    $xMean = ($Rows | Measure-Object -Property PhaseElapsedMinutes -Average).Average
    $yMean = ($Rows | Measure-Object -Property $Property -Average).Average
    $numerator = 0.0
    $denominator = 0.0

    foreach ($row in $Rows) {
        $xDelta = [double]$row.PhaseElapsedMinutes - $xMean
        $yDelta = [double]$row.$Property - $yMean
        $numerator += $xDelta * $yDelta
        $denominator += $xDelta * $xDelta
    }

    if ($denominator -eq 0) {
        return 0.0
    }

    return $numerator / $denominator
}

function Get-ProcessMeasurement {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Phase,
        [DateTime]$MeasurementStartUtc,
        [hashtable]$CpuState
    )

    $Process.Refresh()
    if ($Process.HasExited) {
        throw 'Agent Aura exited during resource measurement.'
    }

    $now = [DateTime]::UtcNow
    $totalCpu = $Process.TotalProcessorTime.TotalSeconds
    $cpuPercent = 0.0
    $sampleGapSeconds = 0.0
    if ($null -ne $CpuState.PreviousTimeUtc) {
        $wallSeconds = ($now - $CpuState.PreviousTimeUtc).TotalSeconds
        $sampleGapSeconds = $wallSeconds
        if ($wallSeconds -gt 0) {
            $cpuSeconds = $totalCpu - $CpuState.PreviousTotalCpuSeconds
            $cpuPercent = 100.0 * $cpuSeconds / $wallSeconds / [Environment]::ProcessorCount
        }
    }

    $CpuState.PreviousTimeUtc = $now
    $CpuState.PreviousTotalCpuSeconds = $totalCpu
    $elapsedMinutes = ($now - $MeasurementStartUtc).TotalMinutes

    return [pscustomobject][ordered]@{
        TimestampUtc = $now.ToString('o')
        ElapsedSeconds = [Math]::Round(($now - $MeasurementStartUtc).TotalSeconds, 3)
        ElapsedMinutes = [Math]::Round($elapsedMinutes, 5)
        PhaseElapsedMinutes = 0.0
        SampleGapSeconds = [Math]::Round($sampleGapSeconds, 3)
        Phase = $Phase
        PrivateMiB = [Math]::Round($Process.PrivateMemorySize64 / 1MB, 3)
        WorkingSetMiB = [Math]::Round($Process.WorkingSet64 / 1MB, 3)
        CpuPercent = [Math]::Round($cpuPercent, 4)
        HandleCount = $Process.HandleCount
    }
}

function Wait-AndMeasure {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Phase,
        [int]$DurationSeconds,
        [int]$IntervalSeconds,
        [DateTime]$MeasurementStartUtc,
        [hashtable]$CpuState,
        [System.Collections.ArrayList]$Samples,
        [scriptblock]$BeforeSample
    )

    $sampleCount = [int][Math]::Ceiling($DurationSeconds / [double]$IntervalSeconds)
    for ($sampleNumber = 1; $sampleNumber -le $sampleCount; $sampleNumber++) {
        $iterationStart = [DateTime]::UtcNow
        if ($null -ne $BeforeSample) {
            & $BeforeSample
        }

        $remainingMilliseconds = [int](1000 * ($IntervalSeconds - ([DateTime]::UtcNow - $iterationStart).TotalSeconds))
        if ($remainingMilliseconds -gt 0) {
            Start-Sleep -Milliseconds $remainingMilliseconds
        }

        $measurement = Get-ProcessMeasurement -Process $Process -Phase $Phase -MeasurementStartUtc $MeasurementStartUtc -CpuState $CpuState
        $measurement.PhaseElapsedMinutes = [Math]::Round($sampleNumber * $IntervalSeconds / 60.0, 5)
        [void]$Samples.Add($measurement)
    }
}

function Start-DeliveredPrototype {
    param([string]$Launcher)

    $existingIds = @(Get-Process -Name 'AgentAura.Prototype' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $launcherProcess = Start-Process -FilePath $Launcher -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    $process = $null

    while ($null -eq $process -and [DateTime]::UtcNow -lt $deadline) {
        $candidates = @(Get-Process -Name 'AgentAura.Prototype' -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $existingIds })
        foreach ($candidate in $candidates) {
            $candidate.Refresh()
            if ((Get-ProcessWindowHandle -Process $candidate) -ne [IntPtr]::Zero) {
                $process = $candidate
                break
            }
        }

        if ($null -eq $process) {
            Start-Sleep -Milliseconds 50
        }
    }

    $stopwatch.Stop()
    if ($null -eq $process) {
        foreach ($candidate in $candidates) {
            Stop-MeasurementProcess -Process $candidate
        }
        throw 'The delivered launcher did not produce an Agent Aura window within 15 seconds.'
    }

    return [pscustomobject]@{
        Process = $process
        ReadyMilliseconds = $stopwatch.Elapsed.TotalMilliseconds
        LauncherExitCode = if ($launcherProcess.HasExited) { $launcherProcess.ExitCode } else { $null }
    }
}

function Get-ProcessWindowHandle {
    param([System.Diagnostics.Process]$Process)

    $handles = New-Object System.Collections.ArrayList
    $callback = [AgentAuraResourceMeasurementNativeMethods+EnumWindowsCallback]{
        param([IntPtr]$window, [IntPtr]$parameter)

        $windowProcessId = [uint32]0
        [AgentAuraResourceMeasurementNativeMethods]::GetWindowThreadProcessId($window, [ref]$windowProcessId) | Out-Null
        if ($windowProcessId -eq $Process.Id -and [AgentAuraResourceMeasurementNativeMethods]::IsWindowVisible($window)) {
            [void]$handles.Add($window)
        }

        return $true
    }

    [AgentAuraResourceMeasurementNativeMethods]::EnumWindows($callback, [IntPtr]::Zero) | Out-Null
    if ($handles.Count -eq 0) {
        return [IntPtr]::Zero
    }

    return [IntPtr]$handles[0]
}

function Stop-MeasurementProcess {
    param([System.Diagnostics.Process]$Process)

    if ($null -eq $Process) {
        return
    }

    $Process.Refresh()
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit(5000) | Out-Null
    }
}

function Get-AutomationRoot {
    param([System.Diagnostics.Process]$Process)

    $Process.Refresh()
    $windowHandle = Get-ProcessWindowHandle -Process $Process
    if ($windowHandle -eq [IntPtr]::Zero) {
        throw 'Agent Aura has no automation window handle.'
    }

    return [System.Windows.Automation.AutomationElement]::FromHandle($windowHandle)
}

function Enable-WindowPinState {
    param([System.Diagnostics.Process]$Process)

    $root = Get-AutomationRoot -Process $Process
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        'Pin')
    $pinButton = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $pinButton) {
        throw 'UI Automation could not find the Pin button.'
    }

    $invokePattern = $pinButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invokePattern.Invoke()
    Start-Sleep -Milliseconds 250
}

function Set-CursorPosition {
    param(
        [double]$X,
        [double]$Y
    )

    if (-not [AgentAuraResourceMeasurementNativeMethods]::SetCursorPos([int][Math]::Round($X), [int][Math]::Round($Y))) {
        throw 'Could not move the pointer for the interaction scenario.'
    }
}

function Move-CursorOutsideWindow {
    param([System.Diagnostics.Process]$Process)

    $Process.Refresh()
    $windowHandle = Get-ProcessWindowHandle -Process $Process
    if ($windowHandle -eq [IntPtr]::Zero) {
        throw 'Agent Aura has no visible window to exercise.'
    }
    $rectangle = New-Object AgentAuraResourceMeasurementNativeMethods+RECT
    if (-not [AgentAuraResourceMeasurementNativeMethods]::GetWindowRect($windowHandle, [ref]$rectangle)) {
        throw 'Could not read the Agent Aura window bounds.'
    }

    Set-CursorPosition -X ($rectangle.Left - 40) -Y ($rectangle.Top - 40)
}

function Get-AgentItemElements {
    param([System.Diagnostics.Process]$Process)

    $root = Get-AutomationRoot -Process $Process
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)
    return @($root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition))
}

function Move-CursorToElement {
    param([System.Windows.Automation.AutomationElement]$Element)

    $bounds = $Element.Current.BoundingRectangle
    if ($bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) {
        throw 'An Agent Message Item had no usable screen bounds.'
    }

    Set-CursorPosition -X ($bounds.Left + [Math]::Min($bounds.Width * 0.45, 180)) -Y ($bounds.Top + $bounds.Height / 2)
}

function Invoke-RepresentativeInteractionCycle {
    param([System.Diagnostics.Process]$Process)

    Move-CursorOutsideWindow -Process $Process
    Start-Sleep -Milliseconds 250

    $items = @(Get-AgentItemElements -Process $Process)
    if ($items.Count -lt 3) {
        throw "Expected at least three Agent Message Items, but UI Automation found $($items.Count)."
    }

    Move-CursorToElement -Element $items[0]
    Start-Sleep -Milliseconds 220

    $items = @(Get-AgentItemElements -Process $Process)
    Move-CursorToElement -Element $items[1]
    Start-Sleep -Milliseconds 220

    $items = @(Get-AgentItemElements -Process $Process)
    Move-CursorToElement -Element $items[2]
    Start-Sleep -Milliseconds 220

    Move-CursorOutsideWindow -Process $Process
    Start-Sleep -Milliseconds 250
}

function Get-DirectorySizeBytes {
    param([string]$Path)

    return (Get-ChildItem -Path $Path -Recurse -File | Measure-Object -Property Length -Sum).Sum
}

function Get-LatestRuntimeDirectories {
    $runtimeRoot = Join-Path $env:ProgramFiles 'dotnet\shared'
    return @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App') | ForEach-Object {
        Get-ChildItem (Join-Path $runtimeRoot $_) -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like '10.*' } |
            Sort-Object { [version]$_.Name } -Descending |
            Select-Object -First 1
    }
}

$measurementProcess = $null
$stateDirectory = Join-Path $env:LOCALAPPDATA 'AgentAura\Prototype'
$statePath = Join-Path $stateDirectory 'window-state.json'
$stateBackupPath = "$statePath.resource-measurement-backup"
$hadOriginalState = Test-Path $statePath
$originalCursor = New-Object AgentAuraResourceMeasurementNativeMethods+POINT
$cursorWasRead = [AgentAuraResourceMeasurementNativeMethods]::GetCursorPos([ref]$originalCursor)
$continuousExecutionState = [uint32]2147483648
$keepSystemAwakeExecutionState = [uint32]2147483649
$executionStateWasSet = $false

if (Test-Path $stateBackupPath) {
    throw "A previous measurement backup already exists at $stateBackupPath. Restore or remove it before measuring."
}

if (Get-Process -Name 'AgentAura.Prototype' -ErrorAction SilentlyContinue) {
    throw 'Close every running Agent Aura prototype before measuring.'
}

try {
    $executionStateWasSet = [AgentAuraResourceMeasurementNativeMethods]::SetThreadExecutionState($keepSystemAwakeExecutionState) -ne 0
    if (-not $executionStateWasSet) {
        throw 'Windows did not accept the request to prevent automatic system sleep during measurement.'
    }

    if ($hadOriginalState) {
        Move-Item -Path $statePath -Destination $stateBackupPath
    }

    if (-not $SkipPublish) {
        & (Join-Path $PSScriptRoot 'publish-framework-dependent.ps1') -OutputDirectory $PublishDirectory *>&1 |
            Tee-Object -FilePath $publishLogPath
        if ($LASTEXITCODE -ne 0) {
            throw "Framework-dependent publish failed with exit code $LASTEXITCODE."
        }
    }

    $null = New-Item -ItemType Directory -Path $localRunDirectory -Force
    Copy-Item -Path (Join-Path $PublishDirectory '*') -Destination $localRunDirectory -Recurse -Force

    $launcher = Join-Path $localRunDirectory 'Start-AgentAura.cmd'
    if (-not (Test-Path $launcher)) {
        throw "The delivered launcher was not found at $launcher."
    }

    $runtimeDirectories = @(Get-LatestRuntimeDirectories)
    $runtimeSizeBytes = $null
    if ($runtimeDirectories.Count -eq 2) {
        $runtimeSizeBytes = ($runtimeDirectories | Get-ChildItem -Recurse -File | Measure-Object -Property Length -Sum).Sum
    }

    $gitCommand = Get-Command git.exe -ErrorAction SilentlyContinue
    $gitCommit = 'unavailable'
    $gitStatus = @('unavailable')
    if ($null -ne $gitCommand) {
        $gitCommit = (& $gitCommand.Source -C $repoRoot rev-parse HEAD 2>$null) -join ''
        $gitStatus = @(& $gitCommand.Source -C $repoRoot status --short 2>$null)
    }

    $dotnetHost = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    $dotnetRuntimes = @()
    if (Test-Path $dotnetHost) {
        $dotnetRuntimes = @(& $dotnetHost --list-runtimes)
    }

    $operatingSystem = Get-CimInstance Win32_OperatingSystem
    $computerSystem = Get-CimInstance Win32_ComputerSystem
    $processor = Get-CimInstance Win32_Processor | Select-Object -First 1
    $environment = [ordered]@{
        CapturedAtUtc = [DateTime]::UtcNow.ToString('o')
        WindowsCaption = $operatingSystem.Caption
        WindowsVersion = $operatingSystem.Version
        WindowsBuild = $operatingSystem.BuildNumber
        Architecture = $operatingSystem.OSArchitecture
        Manufacturer = $computerSystem.Manufacturer
        Model = $computerSystem.Model
        Processor = $processor.Name
        LogicalProcessors = [Environment]::ProcessorCount
        InstalledMemoryMiB = [Math]::Round($computerSystem.TotalPhysicalMemory / 1MB, 0)
        DotnetRuntimes = $dotnetRuntimes
        GitCommit = $gitCommit
        GitStatus = $gitStatus
        Scenario = [ordered]@{
            StartupRuns = $StartupRuns
            StartupCooldownSeconds = $StartupCooldownSeconds
            WarmupSeconds = $WarmupSeconds
            InteractionSeconds = $InteractionSeconds
            ObservationSeconds = $ObservationSeconds
            SampleIntervalSeconds = $SampleIntervalSeconds
            PublishedPackageDirectory = $PublishDirectory
            LocalExecutionDirectory = $localRunDirectory
            AutomaticSystemSleepPrevented = $true
            Interaction = 'Window Pin State enabled; pointer leaves and re-enters the window; three Agent Message Items receive direct hover handoffs.'
            Observation = 'Window remains pinned and visible with the pointer outside it; no synthetic Codex events are generated.'
        }
    }
    $environment | ConvertTo-Json -Depth 6 | Set-Content -Path $environmentPath -Encoding UTF8

    $startupRows = New-Object System.Collections.ArrayList
    for ($run = 1; $run -le $StartupRuns; $run++) {
        $started = Start-DeliveredPrototype -Launcher $launcher
        [void]$startupRows.Add([pscustomobject][ordered]@{
            Run = $run
            ReadyMilliseconds = [Math]::Round($started.ReadyMilliseconds, 3)
            LauncherExitCode = $started.LauncherExitCode
        })

        if ($run -lt $StartupRuns) {
            Stop-MeasurementProcess -Process $started.Process
            Start-Sleep -Seconds $StartupCooldownSeconds
        }
        else {
            $measurementProcess = $started.Process
        }
    }
    $startupRows | Export-Csv -Path $startupPath -NoTypeInformation -Encoding UTF8

    Enable-WindowPinState -Process $measurementProcess
    $measurementStartUtc = [DateTime]::UtcNow
    $cpuState = @{
        PreviousTimeUtc = $null
        PreviousTotalCpuSeconds = 0.0
    }
    $samples = New-Object System.Collections.ArrayList
    [void]$samples.Add((Get-ProcessMeasurement -Process $measurementProcess -Phase 'StartupWarmup' -MeasurementStartUtc $measurementStartUtc -CpuState $cpuState))

    Wait-AndMeasure -Process $measurementProcess -Phase 'StartupWarmup' -DurationSeconds $WarmupSeconds -IntervalSeconds $SampleIntervalSeconds -MeasurementStartUtc $measurementStartUtc -CpuState $cpuState -Samples $samples -BeforeSample $null
    Wait-AndMeasure -Process $measurementProcess -Phase 'Interaction' -DurationSeconds $InteractionSeconds -IntervalSeconds $SampleIntervalSeconds -MeasurementStartUtc $measurementStartUtc -CpuState $cpuState -Samples $samples -BeforeSample {
        Invoke-RepresentativeInteractionCycle -Process $measurementProcess
    }
    Move-CursorOutsideWindow -Process $measurementProcess
    Wait-AndMeasure -Process $measurementProcess -Phase 'Observation' -DurationSeconds $ObservationSeconds -IntervalSeconds $SampleIntervalSeconds -MeasurementStartUtc $measurementStartUtc -CpuState $cpuState -Samples $samples -BeforeSample $null
    $samples | Export-Csv -Path $samplesPath -NoTypeInformation -Encoding UTF8

    $steadyRows = @($samples | Where-Object { $_.Phase -in @('Interaction', 'Observation') })
    $observationRows = @($samples | Where-Object { $_.Phase -eq 'Observation' })
    $privateValues = @($steadyRows | ForEach-Object { [double]$_.PrivateMiB })
    $workingSetValues = @($steadyRows | ForEach-Object { [double]$_.WorkingSetMiB })
    $cpuValues = @($steadyRows | ForEach-Object { [double]$_.CpuPercent })
    $sampleGaps = @($steadyRows | ForEach-Object { [double]$_.SampleGapSeconds })
    $firstObservation = $observationRows | Select-Object -First 1
    $lastObservation = $observationRows | Select-Object -Last 1
    $stabilityWindowSeconds = [Math]::Max(1, [Math]::Min(300, [Math]::Floor($ObservationSeconds / 2)))
    $stabilityWindowSamples = [Math]::Max(1, [Math]::Min([Math]::Floor($observationRows.Count / 2), [Math]::Ceiling($stabilityWindowSeconds / [double]$SampleIntervalSeconds)))
    $firstStabilityRows = @($observationRows | Select-Object -First $stabilityWindowSamples)
    $lastStabilityRows = @($observationRows | Select-Object -Last $stabilityWindowSamples)
    $firstPrivateValues = @($firstStabilityRows | ForEach-Object { [double]$_.PrivateMiB })
    $lastPrivateValues = @($lastStabilityRows | ForEach-Object { [double]$_.PrivateMiB })
    $firstWorkingSetValues = @($firstStabilityRows | ForEach-Object { [double]$_.WorkingSetMiB })
    $lastWorkingSetValues = @($lastStabilityRows | ForEach-Object { [double]$_.WorkingSetMiB })
    $firstHandleValues = @($firstStabilityRows | ForEach-Object { [double]$_.HandleCount })
    $lastHandleValues = @($lastStabilityRows | ForEach-Object { [double]$_.HandleCount })
    $packageSizeBytes = Get-DirectorySizeBytes -Path $PublishDirectory
    $startupMilliseconds = @($startupRows | ForEach-Object { [double]$_.ReadyMilliseconds })

    $summary = [ordered]@{
        CapturedAtUtc = [DateTime]::UtcNow.ToString('o')
        EvidenceDirectory = $OutputDirectory
        Package = [ordered]@{
            FrameworkDependentPackageMiB = [Math]::Round($packageSizeBytes / 1MB, 3)
            MachineWideSharedRuntimeMiB = if ($null -eq $runtimeSizeBytes) { $null } else { [Math]::Round($runtimeSizeBytes / 1MB, 3) }
        }
        Startup = [ordered]@{
            Runs = $StartupRuns
            MedianMilliseconds = [Math]::Round((Get-Median -Values $startupMilliseconds), 3)
            MaximumMilliseconds = [Math]::Round(($startupMilliseconds | Measure-Object -Maximum).Maximum, 3)
        }
        Continuity = [ordered]@{
            ExpectedSteadySampleCount = [int][Math]::Ceiling($InteractionSeconds / [double]$SampleIntervalSeconds) + [int][Math]::Ceiling($ObservationSeconds / [double]$SampleIntervalSeconds)
            ActualSteadySampleCount = $steadyRows.Count
            ObservationSampleCount = $observationRows.Count
            MaximumSampleGapSeconds = [Math]::Round(($sampleGaps | Measure-Object -Maximum).Maximum, 3)
            Interrupted = (($sampleGaps | Measure-Object -Maximum).Maximum -gt (2 * $SampleIntervalSeconds))
        }
        SteadyState = [ordered]@{
            SampleCount = $steadyRows.Count
            PrivateMiBMedian = [Math]::Round((Get-Median -Values $privateValues), 3)
            PrivateMiBP95 = [Math]::Round((Get-Percentile -Values $privateValues -Percentile 0.95), 3)
            PrivateMiBMaximum = [Math]::Round(($privateValues | Measure-Object -Maximum).Maximum, 3)
            WorkingSetMiBMedian = [Math]::Round((Get-Median -Values $workingSetValues), 3)
            WorkingSetMiBP95 = [Math]::Round((Get-Percentile -Values $workingSetValues -Percentile 0.95), 3)
            WorkingSetMiBMaximum = [Math]::Round(($workingSetValues | Measure-Object -Maximum).Maximum, 3)
            CpuPercentAverage = [Math]::Round(($cpuValues | Measure-Object -Average).Average, 4)
            CpuPercentP95 = [Math]::Round((Get-Percentile -Values $cpuValues -Percentile 0.95), 4)
            CpuPercentMaximum = [Math]::Round(($cpuValues | Measure-Object -Maximum).Maximum, 4)
        }
        ObservationGrowth = [ordered]@{
            PrivateMiBChange = [Math]::Round(([double]$lastObservation.PrivateMiB - [double]$firstObservation.PrivateMiB), 3)
            PrivateMiBPerMinute = [Math]::Round((Get-SlopePerMinute -Rows $observationRows -Property 'PrivateMiB'), 4)
            WorkingSetMiBChange = [Math]::Round(([double]$lastObservation.WorkingSetMiB - [double]$firstObservation.WorkingSetMiB), 3)
            WorkingSetMiBPerMinute = [Math]::Round((Get-SlopePerMinute -Rows $observationRows -Property 'WorkingSetMiB'), 4)
            HandleCountStart = [int]$firstObservation.HandleCount
            HandleCountEnd = [int]$lastObservation.HandleCount
            HandleCountChange = [int]$lastObservation.HandleCount - [int]$firstObservation.HandleCount
            HandlesPerMinute = [Math]::Round((Get-SlopePerMinute -Rows $observationRows -Property 'HandleCount'), 4)
        }
        EndpointStability = [ordered]@{
            WindowSeconds = $stabilityWindowSeconds
            FirstPrivateMiBMedian = [Math]::Round((Get-Median -Values $firstPrivateValues), 3)
            LastPrivateMiBMedian = [Math]::Round((Get-Median -Values $lastPrivateValues), 3)
            PrivateMiBMedianChange = [Math]::Round((Get-Median -Values $lastPrivateValues) - (Get-Median -Values $firstPrivateValues), 3)
            FirstWorkingSetMiBMedian = [Math]::Round((Get-Median -Values $firstWorkingSetValues), 3)
            LastWorkingSetMiBMedian = [Math]::Round((Get-Median -Values $lastWorkingSetValues), 3)
            WorkingSetMiBMedianChange = [Math]::Round((Get-Median -Values $lastWorkingSetValues) - (Get-Median -Values $firstWorkingSetValues), 3)
            FirstHandleCountMedian = [Math]::Round((Get-Median -Values $firstHandleValues), 1)
            LastHandleCountMedian = [Math]::Round((Get-Median -Values $lastHandleValues), 1)
            HandleCountMedianChange = [Math]::Round((Get-Median -Values $lastHandleValues) - (Get-Median -Values $firstHandleValues), 1)
        }
    }
    $summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8

    Write-Host "PASS: Resource measurement completed."
    Write-Host "Evidence: $OutputDirectory"
    Write-Host ("Package: {0:N3} MiB; shared runtime: {1}" -f $summary.Package.FrameworkDependentPackageMiB, $(if ($null -eq $summary.Package.MachineWideSharedRuntimeMiB) { 'not measured' } else { "$($summary.Package.MachineWideSharedRuntimeMiB) MiB" }))
    Write-Host ("Startup median: {0:N0} ms across {1} runs." -f $summary.Startup.MedianMilliseconds, $summary.Startup.Runs)
    Write-Host ("Continuity: {0}/{1} steady samples; maximum sample gap {2:N1} seconds; interrupted={3}." -f $summary.Continuity.ActualSteadySampleCount, $summary.Continuity.ExpectedSteadySampleCount, $summary.Continuity.MaximumSampleGapSeconds, $summary.Continuity.Interrupted)
    Write-Host ("Steady private memory median/p95: {0:N1}/{1:N1} MiB." -f $summary.SteadyState.PrivateMiBMedian, $summary.SteadyState.PrivateMiBP95)
    Write-Host ("Steady working set median/p95: {0:N1}/{1:N1} MiB (resident pages, partly reclaimable)." -f $summary.SteadyState.WorkingSetMiBMedian, $summary.SteadyState.WorkingSetMiBP95)
    Write-Host ("CPU average/p95: {0:N3}%/{1:N3}%; observation private/handle change: {2:N1} MiB/{3}." -f $summary.SteadyState.CpuPercentAverage, $summary.SteadyState.CpuPercentP95, $summary.ObservationGrowth.PrivateMiBChange, $summary.ObservationGrowth.HandleCountChange)
    Write-Host ("First-to-last {0}-second window median changes: private {1:N1} MiB; working set {2:N1} MiB; handles {3:N1}." -f $summary.EndpointStability.WindowSeconds, $summary.EndpointStability.PrivateMiBMedianChange, $summary.EndpointStability.WorkingSetMiBMedianChange, $summary.EndpointStability.HandleCountMedianChange)
}
finally {
    Stop-MeasurementProcess -Process $measurementProcess

    if ($executionStateWasSet) {
        [AgentAuraResourceMeasurementNativeMethods]::SetThreadExecutionState($continuousExecutionState) | Out-Null
    }

    if ($cursorWasRead) {
        [AgentAuraResourceMeasurementNativeMethods]::SetCursorPos($originalCursor.X, $originalCursor.Y) | Out-Null
    }

    if ($hadOriginalState) {
        if (Test-Path $statePath) {
            Remove-Item -Path $statePath -Force
        }
        if (Test-Path $stateBackupPath) {
            Move-Item -Path $stateBackupPath -Destination $statePath
        }
    }
    elseif (Test-Path $statePath) {
        Remove-Item -Path $statePath -Force
    }

    if (Test-Path $localRunDirectory) {
        Remove-Item -Path $localRunDirectory -Recurse -Force
    }
}
