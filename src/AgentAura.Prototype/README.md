# Agent Aura Windows observation-shell prototype

This disposable .NET 10 WPF shell validates the proposed Windows interaction shape before it becomes production architecture. It does not connect to Codex, persist Agent Items, or choose the MVP runtime.

## Run on Windows 11

Install the .NET 10 SDK, then run:

```powershell
dotnet run --project src/AgentAura.Prototype/AgentAura.Prototype.csproj
```

The app appears in the system tray without a taskbar button. Its tray menu can show, hide, pin or exit it. Use **Flash tray** while the window is hidden to demonstrate the continuing alert and left-click the tray icon to restore, focus and acknowledge it.

## Publish the framework-dependent package

```powershell
./scripts/publish-framework-dependent.ps1
```

The package is written to `bin\Release\net10.0-windows\win-x64\publish`. It deliberately excludes the .NET runtime. Distribute the whole directory and instruct users to start `Start-AgentAura.cmd`, which checks for the x64 .NET 10 Windows Desktop Runtime. If it is absent, it explains the prerequisite and asks before opening Microsoft's official installer page; it never downloads or installs anything itself. After installation, the user starts the same launcher again.

Validate both external paths on separate Windows 11 environments:

```powershell
./scripts/check-framework-dependent-startup.ps1 -Scenario RuntimePresent
./scripts/check-framework-dependent-startup.ps1 -Scenario RuntimeMissing
```

The validation steps, gates and outcome record are in [prototype-validation.md](prototype-validation.md); the detailed Chinese procedure is in [WINDOWS_VALIDATION_GUIDE.zh-CN.md](WINDOWS_VALIDATION_GUIDE.zh-CN.md), and the post-validation cleanup steps are in [WINDOWS_CLEANUP_GUIDE.zh-CN.md](WINDOWS_CLEANUP_GUIDE.zh-CN.md).

## Proposed product-behaviour seam

The shell's observable public seam is its real WPF window and system-tray icon: start the executable, use its visible controls and tray interactions, and observe window position, header visibility, Agent Item expansion, motion, and acknowledgement. Run scripts/check-prototype-startup.ps1 from the repository root to build the prototype and verify that its process remains alive through the 15-second startup observation window.
