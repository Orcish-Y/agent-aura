# Select the MVP runtime architecture

Type: grilling
Status: resolved
Blocked by: 02, 05, 12, 15, 16, 17, 18, 19

## Question

Given that the WPF shell satisfies the required Windows capabilities, do the remaining Codex bridge and title-synchronization prototypes fit cleanly within the .NET 10 + WPF MVP? Confirm the resulting component boundaries, data ownership, distribution path, and WPF performance/regression budget. Reconsider Tauri only if a required core capability proves impossible in .NET/WPF.

## Resolved constraints

- The default distribution candidate is framework-dependent publication rather than carrying the .NET runtime in every install.
- When the required .NET 10 Windows Desktop Runtime is unavailable, startup stops with an explanation and a user-invoked link to Microsoft's official installer. Agent Aura neither downloads nor silently installs the prerequisite.
- Tauri is a contingency only if a required core capability cannot be implemented in .NET/WPF; resource-budget misses drive WPF optimization and regression work, not an automatic UI-technology comparison.

## Answer

The MVP architecture is fixed on .NET 10 + WPF; Tauri is not an MVP option or contingency. The WPF front end owns UI/tray behavior, settings, current Agent Message Item runtime state, and Guardian connection supervision. A detached Windows App Server Guardian owns the local App Server, remote-TUI counting, front-end leases, and safe draining after the front end exits. An on-demand WSL App Server Guardian owns the corresponding WSL-local App Server, proxy ingress, WSL Connection Session, and Conditional Codex Wrapper. A shared .NET library owns App Server protocol handling, connection epochs, Thread state transitions, configuration contracts, and IPC DTOs.

Only user settings, Thread Aliases, and WSL control-token/default-distribution data persist locally; Agent Message Item runtime history does not. Distribution is framework-dependent `win-x64`; missing .NET 10 Windows Desktop Runtime blocks startup and offers a user-invoked Microsoft installer link. The WSL Guardian is started or deployed only when the user connects that WSL distribution.

The 1.5-second median-startup and 100 MiB idle-working-set values remain optimization targets. Comparable future builds use the established WPF regression envelope; a confirmed breach, crash, incomplete sampling, or sustained growth requires WPF investigation and optimization, not a technology-stack change.

The durable decision is recorded in [Fix the MVP on .NET 10 and WPF](../../../docs/adr/0004-fix-the-mvp-on-dotnet-wpf.md).
