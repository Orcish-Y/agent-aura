# Establish the post-change resource baseline and regression check

Type: task
Status: resolved
Blocked by: 09, 10, 11

## Goal

Produce a repeatable, user-representative resource measurement for the delivered distribution and normal observation-window use, suitable for WPF optimization and later regression detection.

## Context

The distribution path and the Window Pin State and Agent Message Item interactions change both the package footprint and the behaviour worth measuring. Report storage and resident-resource measurements separately, and state the budget that would trigger investigation and optimization within the selected .NET/WPF architecture.

## Dependencies

- Deliver the runtime-prerequisite startup path
- Preserve Agent Message Item positions in Window Pin State
- Animate Agent Message Item hover transitions

## Acceptance criteria

- The measurement records distribution size for the framework-dependent package and distinguishes it from any separately installed runtime.
- The measurement samples startup and representative long-running private memory, working set, CPU, and handle growth while exercising Window Pin State and Agent Message Item interactions.
- The result documents a concrete performance budget and the growth or stability condition that would require investigation and optimization.
- The measurement is repeatable enough to detect a material regression in later changes and reports its Windows, runtime, and scenario assumptions.
- Results use user-meaningful terms and do not present reclaimable working-set pages as permanently exclusive memory.

## Verification

Use a documented Windows 11 scenario that includes normal startup, pinned Header enter/leave transitions, repeated Agent Message Item hover handoffs, and an extended idle/observation period. Retain the raw measurements and report the summary in the repository.

## Non-goals

- Selecting Tauri automatically from a single measurement.
- Treating a framework-dependent application package as if it included a machine-wide runtime.

## Answer

The delivered framework-dependent WPF prototype now has a repeatable Windows 11 resource protocol and a complete post-change baseline. The valid run produced all 360 expected steady-state samples with a 5.099-second maximum gap: five minutes of real Window Pin State and three-item hover handoffs followed by 25 minutes of pinned-window observation.

The Agent Aura package measured 0.220 MiB; the separately installed, machine-wide .NET 10 Core and Windows Desktop shared runtimes measured 170.367 MiB and are not counted as application storage. Five fresh-process launches had a 2.451-second median. Across the full scenario, private memory P95 was 234.289 MiB, working set P95 was 308.535 MiB, and CPU averaged 0.359%. During the 25-minute observation phase, CPU averaged 0.098%; first-to-last five-minute window medians changed by +17.230 MiB private memory, +10.992 MiB working set, and -9 handles. The five private-memory windows were non-monotonic, so the run does not show a sustained leak, but its resident cost remains well above the established 100 MiB lightweight gate.

The lightweight performance targets remain 1.5-second median startup and 100 MiB idle working set. WPF misses both, so they remain optimization targets rather than gates that trigger a parallel Tauri comparison. A separate WPF regression envelope is documented for later comparable runs: complete/uninterrupted samples, package at most 1 MiB, startup median at most 3.0 seconds, private-memory P95 at most 260 MiB, working-set P95 at most 340 MiB, CPU average/P95 at most 1%/3%, and no more than +32 MiB private memory or +32 handles between the first and last five-minute windows without sustained monotonic growth. A numerical breach is repeated once; a confirmed breach triggers a WPF performance investigation. Crashes, incomplete samples, or sustained growth trigger investigation immediately.

- [Repeatable measurement script](../../../scripts/measure-prototype-resources.ps1)
- [Human-readable baseline and budget](../../../docs/resource-baseline.md)
- [Raw valid evidence](../evidence/resource-baseline-20260713T122536Z/)
