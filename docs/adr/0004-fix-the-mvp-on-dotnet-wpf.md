---
status: accepted
---

# Fix the MVP on .NET 10 and WPF

The Agent Aura MVP uses .NET 10 throughout: a WPF Windows front end, a detached Windows App Server Guardian, and an on-demand WSL App Server Guardian. Tauri is not part of the MVP architecture or contingency plan.

## Context

The WPF prototype demonstrated every required Windows observation-window capability. The Codex App Server bridge, title synchronization, Windows Guardian lifecycle, and WSL observation topology all fit the .NET protocol and process model. Although the WPF baseline misses the aspirational lightweight targets, it has a repeatable regression envelope and no required capability is blocked.

## Decision

- The WPF front end owns UI, tray interaction, settings, current Agent Message Item runtime state, and Guardian connection supervision.
- The Windows Guardian owns the Windows-local Codex App Server, remote-TUI connection counting, front-end leases, and safe post-exit draining.
- The WSL Guardian owns its WSL-local App Server, proxy ingress, WSL Connection Session, and Conditional Codex Wrapper.
- A shared .NET library owns App Server protocol handling, connection epochs, Thread state transitions, configuration contracts, and IPC DTOs.
- Durable local state contains only user settings, Thread Aliases, and WSL connection credentials/default-distribution data; Agent Message Item runtime history is not persisted.
- Distribution is a framework-dependent `win-x64` package. Missing .NET 10 Windows Desktop Runtime stops startup and presents a user-invoked link to Microsoft's official installer. A WSL Guardian is started or deployed only when the user connects its target distribution.
- The 1.5-second median-startup and 100 MiB idle-working-set values remain product optimization targets. The established WPF regression envelope governs comparable future runs; confirmed regressions trigger WPF investigation and optimization, never a UI-stack change.

## Consequences

- The implementation has one primary language/runtime and explicit IPC boundaries rather than a Tauri/Rust integration boundary.
- Runtime-prerequisite handling and WSL Guardian lifecycle must be included in the implementation specification and delivery tests.
- Performance work remains planned and measurable within the accepted WPF architecture.
