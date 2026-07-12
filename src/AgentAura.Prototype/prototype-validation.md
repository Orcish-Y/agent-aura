# WPF observation-shell validation record

Run this protocol on a clean Windows 11 VM and record results in the table below. The prototype is accepted only if every core gate passes. A failed core gate starts a like-for-like Tauri comparison; it does not automatically select Tauri.

## Core capability gates

| Gate | Procedure | Pass threshold | Result |
| --- | --- | --- | --- |
| Tray-only lifecycle | Start the shell; use tray show/hide; use Minimise; left-click the tray icon. | No taskbar button; show, hide, minimise, restore and focus all work. | Not measured |
| Window behavior | Toggle pinning and enter/leave the window. Resize and drag using the header. | Frameless translucent surface; pinned window stays topmost; header auto-hides only while pinned; unpinned header stays visible. | Not measured |
| Agent Item behavior | Hover each sample Agent Item and inspect its state cue, controls and long text. | One collapsed line becomes four lines; every state has a shape/text cue; text scrolls without a tooltip. | Not measured |
| Reduced motion | Enable **Reduced motion**, then hover an Agent Item and its text and use **Flash tray**. | No detail-transition, text, or tray animation starts; all state text remains understandable. | Not measured |
| Tray acknowledgement | Hide the window, start a tray flash, then left-click the tray icon. | One continuing alert is visible; restore/focus stops it. | Not measured |
| Window recovery | Place and resize the window, exit, restart; then edit its saved coordinates to an unavailable monitor and restart. | Position, size and pin persist; an off-screen position returns to a visible primary display. | Not measured |
| Self-contained distribution | Publish `win-x64` self-contained, then launch on a clean VM without .NET. | Starts successfully without a preinstalled .NET runtime. | Not measured |

## Cost and stability gates

| Measure | Method | Pass threshold | Result |
| --- | --- | --- | --- |
| Cold startup | Five cold starts on the target VM; time from process launch to visible tray icon. | Median at or below 1.5 seconds. | Not measured |
| Idle working set | Leave the shell shown for five minutes without interaction; use Task Manager or PerfMon. | At or below 100 MiB. | Not measured |
| Idle CPU | Measure over the same five-minute interval. | Average at or below 1%. | Not measured |
| Package size | Measure the published `win-x64` directory. | At or below 200 MiB. | Not measured |
| Stability | Exercise tray show/hide, pinning, hover expansion and restarts for eight hours. | No crash, hang, stuck tray icon, or unrecoverable off-screen window. | Not measured |

## Decision record

**Current recommendation:** undecided. The shell must be run and measured on Windows before WPF can be recommended or a Tauri comparison justified.

If a gate fails, record the Windows version, machine/VM specification, .NET SDK version, exact reproduction steps and observed result here before opening the comparison ticket.
