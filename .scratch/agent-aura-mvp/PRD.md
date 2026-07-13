# Agent Aura MVP

Status: ready-for-agent

## Problem Statement

Codex CLI users often run long tasks or several Threads in parallel while switching between projects and applications. Once the terminal is no longer in view, it is easy to forget which Thread is still running, which has finished, and which is blocked waiting for approval or input. Returning to every terminal to check wastes attention and makes interruptions easy to miss.

The user needs a quiet, glanceable Windows 11 observation surface that stays available without occupying the taskbar, preserves the identity of resumable Codex Threads, and makes user-relevant state changes visible without forcing a new way of launching Codex.

## Solution

Agent Aura is a lightweight, semi-transparent Windows 11 desktop application that passively observes local Codex CLI Threads. It presents one Agent Item per resumable Codex Thread in a compact always-available window and uses comfortable, low-saturation status treatments to distinguish running, attention, successful, failed, interrupted, and unknown states.

The application lives primarily in the system tray and does not create a taskbar button. When the observation window is hidden and a Thread reaches a user-relevant state, the tray icon flashes until the user opens the window. Agent Items expand from one line to four on hover, expose useful Thread details and edit/delete controls, and preserve a user-defined Thread Alias by stable Codex thread ID across Agent Aura and Codex CLI restarts.

Agent Aura does not require users to launch Codex from inside the application. A user-approved Codex integration is installed, checked, repaired, and removed through the application. The exact supported integration transport must be selected from verified Codex capabilities before production implementation.

## User Stories

1. As a Codex CLI user, I want to see all currently observed Codex Threads in one compact window, so that I do not need to revisit several terminals.
2. As a Codex CLI user, I want exactly one Agent Item per resumable Codex Thread, so that multiple turns do not create duplicate entries.
3. As a Codex CLI user, I want an Agent Item to retain its identity after `codex resume`, so that a resumed conversation remains recognisable.
4. As a Codex CLI user, I want Agent Aura to observe Codex launched from my usual terminals, so that I do not need to change my workflow.
5. As a Codex CLI user, I want running Threads to have a distinct state, so that I can tell work is still in progress.
6. As a Codex CLI user, I want Threads waiting for approval or input to enter the Attention State, so that I know where action is required.
7. As a Codex CLI user, I want successful Threads to have a familiar muted green treatment, so that completion is recognisable at a glance.
8. As a Codex CLI user, I want failed Threads to have a familiar muted red treatment, so that errors are recognisable at a glance.
9. As a Codex CLI user, I want interrupted Threads to be distinguishable from failures, so that cancellation and errors are not conflated.
10. As a Codex CLI user, I want stale or disconnected Threads to become unknown, so that an abandoned running state is not presented as certain.
11. As a Codex CLI user, I want a state change to flash twice in the observation window, so that a fresh change catches my eye.
12. As a Codex CLI user, I want state to be communicated by an icon or shape as well as colour, so that colour is not the only signal.
13. As a Codex CLI user, I want Attention State items temporarily placed first, so that requests for action are initially easy to find.
14. As a Codex CLI user, I want an old Attention State item to stop being pinned after a configurable number of Significant Updates elsewhere, so that an abandoned Thread does not permanently dominate the list.
15. As a Codex CLI user, I want the default Attention Pin Span to be ten Significant Updates, so that temporary priority has a sensible starting point.
16. As a Codex CLI user, I want to set the Attention Pin Span from 1 to 50 or choose always pinned, so that priority matches my workflow.
17. As a Codex CLI user, I want streaming text and tool progress excluded from Significant Update counting, so that noisy Threads do not prematurely demote an Attention State item.
18. As a Codex CLI user, I want a Thread's new activity or renewed Attention State to reset its pin span, so that a newly relevant request returns to the top.
19. As a Codex CLI user, I want non-pinned Agent Items ordered by their latest state change, so that newly completed or failed work remains prominent.
20. As a Codex CLI user, I want the collapsed window to show five Agent Items by default, so that it stays compact.
21. As a Codex CLI user, I want the expanded window to show fifteen Agent Items by default, so that I can inspect more concurrent work.
22. As a Codex CLI user, I want to configure collapsed capacity from 1 to 10 and expanded capacity from 5 to 30, so that the window fits my display and workload.
23. As a Codex CLI user, I want expanded capacity to remain at least the collapsed capacity, so that settings cannot create contradictory behaviour.
24. As a Codex CLI user, I want the list to scroll internally beyond its expanded capacity, so that the window does not grow indefinitely.
25. As a Codex CLI user, I do not want the expand control to display a hidden-item count, so that the control remains visually quiet.
26. As a Codex CLI user, I want the expand control hidden when there are no additional items, so that unnecessary controls do not appear.
27. As a Codex CLI user, I want each Agent Item collapsed to one line, so that I can scan many Threads quickly.
28. As a Codex CLI user, I want an Agent Item to expand to four lines on hover, so that details are available without opening another screen.
29. As a Codex CLI user, I want the first expanded line to show state, Thread Alias or title, edit, and delete controls, so that identity and actions are together.
30. As a Codex CLI user, I want the second expanded line to show project name and working directory, so that I can locate the Thread's work.
31. As a Codex CLI user, I want the third expanded line to show state, last-change time, and current-turn duration, so that I understand recency and elapsed work.
32. As a Codex CLI user, I want the fourth expanded line to show the current activity, waiting reason, error summary, or final outcome, so that I can judge what happened.
33. As a Codex CLI user, I want overflowing title and detail text to be ellipsized within its line without a tooltip, so that the observation window remains compact and predictable.
34. As a Codex CLI user, I want text truncation to remain the same regardless of visual settings, so that visible content does not depend on a motion preference.
35. As a Windows user, I do not want a Reduced motion setting, so that the settings surface stays focused and text overflow behavior is consistent.
36. As a Codex CLI user, I want to assign a persistent Thread Alias, so that I can recognise a conversation by my own name.
37. As a Codex CLI user, I want a Thread Alias keyed by stable thread ID, so that the correct alias follows a resumed conversation.
38. As a Codex CLI user, I want title fallback to use Codex title, project folder, and a generic time-based name in that order, so that every Agent Item has a useful label.
39. As a Codex CLI user, I want deleting an Agent Item to remove it from the current view, so that I can dismiss irrelevant entries.
40. As a Codex CLI user, I want a deleted Agent Item to reappear when its Thread produces a later Significant Update, so that dismissal does not disable monitoring.
41. As a Codex CLI user, I want the clear action to remove terminal and unknown items only, so that active running and Attention State Threads remain visible.
42. As a Codex CLI user, I do not want an unread/read model, so that Agent Aura remains an observation window rather than an inbox.
43. As a Codex CLI user, I do not want the current Agent Item list restored after Agent Aura restarts, so that stale observations do not return.
44. As a Codex CLI user, I want aliases and settings restored after Agent Aura restarts, so that intentional customisation persists.
45. As a Windows user, I want Agent Aura to live in the system tray without a taskbar button, so that it does not occupy taskbar space.
46. As a Windows user, I want the tray icon to flash when the window is hidden and a Thread enters attention, succeeded, failed, or interrupted, so that I notice meaningful changes.
47. As a Windows user, I want several changes to produce one continuing tray alert rather than stacked animations, so that alerts remain calm.
48. As a Windows user, I want a left click on the tray icon to restore and focus the window and stop flashing, so that acknowledgement is immediate.
49. As a Windows user, I want a tray menu with show/hide, pin, settings, and exit actions, so that core controls remain available while the window is hidden.
50. As a Windows user, I want clicking the close button to ask whether to hide to tray or exit, so that closing is intentional.
51. As a Windows user, I want the close dialog to offer “remember my choice,” so that repeated closing is efficient.
52. As a Windows user, I want to reset the remembered close behaviour in settings, so that the choice is reversible.
53. As a Windows user, I want cancellation in the close dialog to leave the application unchanged, so that accidental clicks are harmless.
54. As a Windows user, I want a pinned mode that keeps the window above other windows, so that status remains glanceable.
55. As a Windows user, I want the Header hidden while pinned and revealed when the pointer enters the window, so that the pinned window stays minimal.
56. As a Windows user, I want the Header always visible while unpinned, so that normal-window controls are discoverable.
57. As a Windows user, I want to drag the window through the visible Header, so that I can position it easily.
58. As a Windows user, I want pin state, window position, and window size persisted, so that my layout survives restarts.
59. As a multi-monitor Windows user, I want an off-screen saved window moved back onto an available display, so that monitor changes cannot strand it.
60. As a Windows user, I want optional launch at Windows sign-in, so that monitoring can start automatically.
61. As a Windows user, I want sign-in launch disabled by default, so that installation does not silently add startup behaviour.
62. As a Windows user, I want startup to enter the tray by default, so that automatic launch is unobtrusive.
63. As a Windows user, I want window opacity configurable from 60% to 100% with an 88% default, so that visibility fits my desktop.
64. As a Windows user, I want opacity to affect the window surface without unnecessarily fading text and state icons, so that details remain legible.
65. As a Windows user, I want system, light, and dark themes with system as default, so that Agent Aura matches my desktop.
66. As a Windows user, I want UI scale configurable from 80% to 150%, so that the compact window remains readable.
67. As a Windows user, I want Windows high-contrast support, so that the interface remains usable with accessibility settings.
68. As a Windows user, I want Agent Aura to avoid toast notifications and sounds, so that observation stays quiet.
69. As a Codex CLI user, I want an installer to detect whether Codex and a compatible version are available, so that setup failures are understandable.
70. As a Codex CLI user, I want to review and approve integration changes before installation, so that global Codex configuration is not changed silently.
71. As a Codex CLI user, I want one-click install, status check, repair, and removal of the Agent Aura integration, so that setup is manageable without manual configuration.
72. As a Codex CLI user, I want integration removal to delete only Agent Aura-owned plugin, marketplace, and configuration entries, so that unrelated Codex configuration is preserved.
73. As a Codex CLI user, I want application uninstall to ask whether to remove the Codex integration, so that I control what remains.
74. As a Codex CLI user, I want failed setup operations to show the failed action and a copyable diagnostic, so that I can recover or request help.
75. As a Codex CLI user, I want Agent Aura to recover from local connection failures and show degraded status, so that missing observations are not silently presented as healthy.
76. As a Codex CLI user, I want clicking an Agent Item to restore or flash the correct terminal when technically reliable, so that I can act on a Thread quickly.
77. As a Codex CLI user, I want a clear fallback when exact terminal activation is unavailable, so that clicking never targets the wrong window.
78. As an installer user, I want a self-contained Windows package, so that I do not need to install a separate .NET Runtime.
79. As a project maintainer, I want the initial WPF prototype measured for startup, idle memory, package size, and stability, so that the runtime choice is evidence-based.
80. As a project maintainer, I want a Tauri comparison only when WPF fails a core capability or has unacceptable runtime/distribution costs, so that fallback work is purposeful.

## Implementation Decisions

- The MVP targets Windows 11 only and monitors local Codex CLI Threads only.
- The default runtime candidate is .NET 10 with WPF and an MVVM-style separation. This is provisional until a disposable technical prototype verifies tray-only behaviour, window behaviour, animation, Codex connectivity, packaging, and acceptable resource use.
- Tauri is the explicit fallback candidate. It is evaluated only if WPF fails a core capability or produces unacceptable package size, startup time, idle memory, stability, or distribution behaviour.
- Agent Aura is a tray application with no taskbar button. Its observation window is semi-transparent, frameless, optionally always on top, and collapsible/expandable.
- A stable Codex-generated thread ID is the canonical identity for a Codex Thread and the persistence key for Thread Alias data.
- Agent Item runtime state is not restored after Agent Aura restarts. User settings and Thread Alias mappings are persisted.
- The target state vocabulary is `running`, `attention`, `succeeded`, `failed`, `interrupted`, and `unknown`. A dedicated state-machine decision must map verified Codex events into these states, including stale, disconnect, retry, cancellation, and out-of-order cases.
- A Significant Update is limited to a Thread entering Attention State, reaching a terminal state, or starting a new turn. Streaming text, tool activity, and progress refreshes are excluded.
- Attention State items are temporarily pinned. The Attention Pin Span defaults to ten Significant Updates from other Threads, is configurable from 1 to 50, and supports an always-pinned option. New activity on that Thread resets the span.
- After temporary Attention pinning expires, all Agent Items use most-recent state-change ordering; running items receive no special priority.
- The clear action removes succeeded, failed, interrupted, and unknown items while retaining running and Attention State items.
- Deleting an Agent Item dismisses it until its Thread emits a later Significant Update.
- Agent Items use a one-line collapsed form and a four-line hover form. Detail lines cover identity/actions, project context, state/timing, and current or final activity.
- Overflowing title and detail text is always ellipsized within its line and does not use tooltips. There is no Reduced motion mode or setting.
- Collapsed and expanded capacities default to five and fifteen. Their configurable ranges are 1–10 and 5–30; expanded capacity cannot be lower than collapsed capacity. Overflow uses internal scrolling and the expand control does not show hidden count.
- Pinned mode means always on top with an auto-hidden Header. Unpinned mode uses normal window stacking with the Header always visible.
- The system tray is the only out-of-window alert surface. Agent Aura does not use Windows toast notifications or sounds in MVP.
- The tray icon flashes while the window is hidden and a Thread enters attention, succeeded, failed, or interrupted. Opening the observation window acknowledges and stops the tray animation.
- Closing presents hide-to-tray, exit, cancel, and remember-choice behaviour. Remembered behaviour is resettable from settings.
- Visual settings include background opacity, theme, UI scale, collapsed capacity, expanded capacity, Attention Pin Span, pin state, startup options, and close-button behaviour.
- Status styling uses comfortable low-saturation colours plus non-colour cues. Windows high-contrast mode must remain usable.
- The Codex integration must be passive from the user's perspective: Codex may be launched from the user's ordinary terminal rather than through Agent Aura.
- Current Codex app-server protocol evidence shows stable UUIDv7 Thread identity and exposes working directory, name, preview, timestamps, source, CLI version, status, and related events. Production implementation must still verify the supported transport for arbitrary Codex TUI instances rather than assuming app-server events are automatically available from them.
- The selected Codex integration must support explicit-consent installation, health inspection, repair, and removal. It must preserve unrelated Codex plugins, marketplaces, hooks, and configuration.
- Terminal restoration is best effort and must never activate a terminal that cannot be associated reliably. The exact Windows and terminal-host capability matrix is a research prerequisite.
- The distributable is self-contained so end users do not need a separately installed .NET Runtime.
- No architectural decision record is created for the runtime until the WPF prototype resolves the provisional choice.

## Testing Decisions

- Good tests assert externally observable product behaviour and stable integration contracts. Tests must not depend on WPF ViewModel structure, private classes, animation implementation, local storage layout, or other replaceable internals.
- The primary product-behaviour seam drives Agent Aura with a controllable Codex Thread event stream and observes the real WPF window. It covers Agent Item identity, state display, Significant Update counting, Attention Pin Span, ordering, clear/delete/reappearance, aliases, capacity, hover expansion, ellipsized text overflow, tray acknowledgement, close behaviour, persistence, and degraded states.
- Product-behaviour scenarios must include several concurrent Threads, repeated turns in one Thread, `codex resume` identity continuity, rapid state changes, stale/disconnected sources, out-of-order events, duplicate events, more Agent Items than expanded capacity, and more Attention State items than collapsed capacity.
- The Codex integration contract seam validates captured fixtures for stable thread ID, working directory, title, lifecycle, approval/input, success, failure, interruption, and liveness. A small real-Codex smoke suite verifies supported behaviour against the minimum and current CLI versions.
- Integration lifecycle tests install into isolated Codex configuration, inspect health, simulate damaged or missing Agent Aura entries, repair them, and remove them. They verify unrelated configuration remains byte-for-byte or semantically unchanged as appropriate.
- The Windows packaging seam runs the self-contained artifact on a clean Windows 11 environment and verifies startup without a preinstalled Runtime, tray presence, no taskbar button, transparent/topmost behaviour, multi-monitor recovery, close semantics, sign-in startup, and clean uninstall.
- Terminal activation tests are organised by supported terminal host. Each verifies correct activation when association is proven and safe fallback when it is not.
- Accessibility checks cover keyboard access to visible controls, high-contrast rendering, non-colour state cues, scale limits, and readable content at the minimum configured opacity.
- Performance gates record cold startup time, steady-state idle memory, package size, idle CPU, event-burst handling, and UI responsiveness. Exact acceptance thresholds are established by the WPF prototype before the runtime is committed.
- Because the repository has no existing implementation or tests, there is no local prior art. The first prototype should establish reusable acceptance-test harnesses at the event-stream-to-window seam rather than creating many narrow module tests.

## Out of Scope

- macOS and Linux support.
- Monitoring Codex IDE extensions, Codex App, or Codex Cloud tasks as first-class sources.
- Forcing users to launch Codex from inside Agent Aura.
- Windows toast notifications, notification centre integration, and alert sounds.
- Read/unread state, acknowledgement history, or inbox semantics.
- Persisting the Agent Item list across Agent Aura restarts.
- Privacy mode or a guarantee that the window is excluded from screenshots and screen sharing.
- A hidden-item count on the expand control.
- Guaranteed terminal activation for terminal hosts that do not expose a reliable association.
- Production implementation before the Codex integration, WPF feasibility, terminal recovery, state mapping, and runtime selection decisions are resolved.
- Cross-platform abstractions added solely for a hypothetical future port.

## Further Notes

- The current development machine is Windows 11 with Ubuntu WSL2. Windows has .NET SDK and Windows Desktop support at version 10.0.9. The actual WPF build and packaging workflow should execute on the Windows side or in a Windows CI runner.
- The current Codex CLI observed during planning is version 0.144.1. Its generated app-server protocol describes stable UUIDv7 Thread IDs and Thread status events, but support policy and the path from arbitrary TUI processes to Agent Aura remain research items.
- The Wayfinder map remains the source of truth for unresolved discovery work. In particular, the specification must be refined with the outcomes of “Determine the reliable Codex event integration,” “Prototype the WPF observation shell,” “Map Codex events to Agent Item state,” “Determine terminal recovery behavior,” “Prototype the Codex-to-WPF bridge,” and “Select the MVP runtime architecture.”
- The initial WPF technical prototype must validate: tray-only operation, transparent frameless topmost window, Header auto-hide, one-to-four-line Agent Item expansion, ellipsized overflow text, tray flashing, Codex connectivity, self-contained packaging, and clean-machine launch.
- If a core WPF capability fails or measured distribution/runtime costs are unacceptable, create a like-for-like Tauri prototype before choosing the production runtime.
