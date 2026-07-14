# Prototype the Tauri observation shell

Type: prototype
Status: resolved
Blocked by: 02, 12

## Question

Can a disposable Windows 11 Tauri shell reproduce the accepted WPF observation-window interaction model—tray-only presence, no taskbar button, transparent frameless Window Pin State with a stationary Agent Message Item list, 150 ms concurrent hover handoffs, ellipsized overflow text, tray flashing, close-choice handling, and persisted window/settings behavior—while meeting the same framework-dependent distribution and resource measurement protocol? Record equivalent package size, startup, private memory, working set, CPU, handle-growth, stability, and Windows-specific capability gaps so the runtime-selection decision can compare like with like.

## Answer

This comparison is ruled out of scope for the current MVP route. The accepted .NET 10 + WPF prototype implements the required Windows shell capabilities, so Tauri is not a parallel performance experiment or a response to resource-budget misses. It becomes relevant only if a required core capability cannot be implemented with .NET/WPF.

No Tauri prototype or measurement artifact is retained. The WPF resource baseline remains useful for optimization and regression detection within the selected .NET architecture.
