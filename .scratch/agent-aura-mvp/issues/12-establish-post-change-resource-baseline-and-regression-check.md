# Establish the post-change resource baseline and regression check

Type: task
Status: ready-for-agent
Blocked by: 09, 10, 11

## Goal

Produce a repeatable, user-representative resource measurement for the delivered distribution and normal observation-window use before another UI-technology decision is considered.

## Context

The distribution path and the Window Pin State and Agent Message Item interactions change both the package footprint and the behaviour worth measuring. Report storage and resident-resource measurements separately, and state the budget that would trigger reconsideration of the selected UI technology.

## Dependencies

- Deliver the runtime-prerequisite startup path
- Preserve Agent Message Item positions in Window Pin State
- Animate Agent Message Item hover transitions

## Acceptance criteria

- The measurement records distribution size for the framework-dependent package and distinguishes it from any separately installed runtime.
- The measurement samples startup and representative long-running private memory, working set, CPU, and handle growth while exercising Window Pin State and Agent Message Item interactions.
- The result documents a concrete performance budget and the growth or stability condition that would require another runtime-architecture decision.
- The measurement is repeatable enough to detect a material regression in later changes and reports its Windows, runtime, and scenario assumptions.
- Results use user-meaningful terms and do not present reclaimable working-set pages as permanently exclusive memory.

## Verification

Use a documented Windows 11 scenario that includes normal startup, pinned Header enter/leave transitions, repeated Agent Message Item hover handoffs, and an extended idle/observation period. Retain the raw measurements and report the summary in the repository.

## Non-goals

- Selecting Tauri automatically from a single measurement.
- Treating a framework-dependent application package as if it included a machine-wide runtime.
