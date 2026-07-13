# Agent Aura Windows observation-shell prototype

This disposable .NET 10 WPF shell validates the proposed Windows interaction shape before it becomes production architecture. It does not connect to Codex, persist Agent Items, or choose the MVP runtime.

## Run on Windows 11

Install the .NET 10 SDK, then run:

```powershell
dotnet run --project src/AgentAura.Prototype/AgentAura.Prototype.csproj
```

The app appears in the system tray without a taskbar button. Its tray menu can show, hide, pin or exit it. Use **Flash tray** while the window is hidden to demonstrate the continuing alert and left-click the tray icon to restore, focus and acknowledge it.

## Publish a self-contained candidate

```powershell
dotnet publish src/AgentAura.Prototype/AgentAura.Prototype.csproj -c Release -r win-x64 --self-contained true
```

Run the resulting artifact on a clean Windows 11 VM that has no .NET runtime installed. The validation steps, gates and outcome record are in [prototype-validation.md](prototype-validation.md); the detailed Chinese procedure is in [WINDOWS_VALIDATION_GUIDE.zh-CN.md](WINDOWS_VALIDATION_GUIDE.zh-CN.md), and the post-validation cleanup steps are in [WINDOWS_CLEANUP_GUIDE.zh-CN.md](WINDOWS_CLEANUP_GUIDE.zh-CN.md).

## Proposed product-behaviour seam

The shell's observable public seam is its real WPF window and system-tray icon: start the executable, use its visible controls and tray interactions, and observe window position, header visibility, Agent Item expansion, motion, and acknowledgement. Run scripts/check-prototype-startup.ps1 from the repository root to build the prototype and verify that its process remains alive through the 15-second startup observation window.
