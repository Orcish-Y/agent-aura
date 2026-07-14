# Tickets: Agent Aura MVP

These `ready-for-agent` tickets deliver the Windows 11 MVP specified in `.scratch/agent-aura-mvp/PRD.md`. They supersede the earlier exploration-era breakdown.

Work the **frontier**: any ticket whose blockers are all done.

## Establish shared domain contracts and durable settings

**What to build:** The deterministic foundation that turns App Server inputs into current Agent Message Items and safely retains only the user choices that must survive a restart.

**Blocked by:** None — can start immediately.

- [ ] Agent Message Items have stable Codex Thread identity, exact state reduction, Significant Update classification, Attention Pin Span accounting, and stale/duplicate event rejection.
- [ ] Connection epochs prevent an old observer connection from changing an item after a newer one reconnects.
- [ ] Settings, Thread Aliases, and WSL connection credentials validate and persist atomically; Agent Message Items and their runtime history never persist.
- [ ] Deterministic tests cover turn outcomes, Attention State, title precedence, aliases, ordering, dismissal/recreation, and reconnect snapshots.

## Deliver Windows App Server observation

**What to build:** A user can connect a local Codex TUI through Agent Aura's managed remote endpoint and see its current Observed Codex Thread accurately represented by Agent Aura.

**Blocked by:** Establish shared domain contracts and durable settings.

- [ ] Agent Aura starts or attaches to its Windows-local managed App Server and exposes the connected remote-TUI command.
- [ ] Separately launched connected TUIs are discovered, loaded, subscribed, and represented as one Agent Message Item per stable Thread ID.
- [ ] Running, Attention State, exact completed/failed/interrupted outcomes, and Codex Thread Title changes reach the shared state contract from supported App Server inputs.
- [ ] Transport loss marks affected items disconnected without inventing an outcome; reconnect uses a fresh epoch and current App Server snapshot.
- [ ] A current and minimum supported Codex CLI smoke matrix verifies the real observation path and reports incompatible versions as unavailable with diagnostics.

## Deliver Windows App Server Guardian lifecycle

**What to build:** Exiting Agent Aura leaves a user-owned remote TUI running, while an App Server Guardian safely drains and eventually recovers the managed server.

**Blocked by:** Establish shared domain contracts and durable settings; Deliver Windows App Server observation.

- [ ] A detached Guardian owns the Windows-local App Server after the front end exits and tracks remote-TUI connections plus Front End Leases.
- [ ] Fifteen-second lease heartbeats and forty-five-second expiry distinguish normal detach from an unexpected front-end loss without inspecting user terminal processes.
- [ ] Once Aura is absent and the final remote TUI disconnects, the Guardian requests graceful shutdown and forces termination only after five seconds.
- [ ] Aura restart reattaches to a live Guardian and reconstructs observations through a fresh epoch rather than replacing a live App Server.
- [ ] Exit, drain, reattachment, server-failure, and UI-responsiveness tests prove that no WPF UI operation waits for WebSocket or child-process shutdown.

## Deliver the WPF observation window and tray experience

**What to build:** A quiet, accessible Windows observation surface where Agent Message Items are readable and actionable, and user-meaningful changes are visible through the tray-first lifecycle.

**Blocked by:** Establish shared domain contracts and durable settings.

- [ ] The real WPF window renders state, title, project context, time, activity/outcome, alias editing, dismiss, and user-initiated connected-resume copying for Agent Message Items.
- [ ] Collapsed and four-line item forms transition over 150 ms, including concurrent collapse/expand when moving directly between items; all text ellipsizes within its available line.
- [ ] Attention Pin Span, ordinary latest-change ordering, list capacities, internal scrolling, Clear Agent Message Items, and later valid-event recreation behave according to the contract.
- [ ] The frameless, no-taskbar window supports pinned Header hiding without moving items, tray alert acknowledgement, tray commands, minimize-to-tray, close choices, and off-screen geometry recovery.
- [ ] Keyboard controls, UI-scale bounds, Windows High Contrast, and non-colour state cues are verifiable through a controllable event stream driving the real window.

## Deliver settings, integration management, and runtime prerequisite handling

**What to build:** Users can configure Agent Aura, inspect and repair only Aura-owned integration artifacts, and understand how to start the framework-dependent app on a compatible Windows machine.

**Blocked by:** Establish shared domain contracts and durable settings; Deliver Windows App Server observation; Deliver the WPF observation window and tray experience.

- [ ] Settings expose the specified presentation, capacity, pin, sign-in, Silent Startup, close-behaviour, and restore-defaults choices while preserving aliases and current runtime items appropriately.
- [ ] Integration health reports Connected, Checking, Needs repair, or Unavailable, with non-modal notices, Retry, and copyable diagnostics that never expose secrets.
- [ ] Check, Repair, Remove, and user-approved setup preview their owned processes, endpoints, and artifacts, require confirmation, and never alter unrelated Codex configuration.
- [ ] A framework-dependent win-x64 package starts when .NET 10 Windows Desktop Runtime is available; otherwise the launcher explains the prerequisite and offers only a user-invoked official installer link.
- [ ] Repeatable tests cover runtime-present/missing startup, configuration ownership boundaries, and reversible integration maintenance.

## Deliver the WSL Connection Session

**What to build:** A user can connect one selected WSL distribution for local-only Codex observation, preserve existing remote TUIs safely, and recover from expected WSL disruptions.

**Blocked by:** Establish shared domain contracts and durable settings; Deliver Windows App Server Guardian lifecycle; Deliver settings, integration management, and runtime prerequisite handling.

- [ ] Agent Aura creates and controls one authenticated WSL-local Guardian and App Server through a persisted high-entropy control token and local-only endpoints.
- [ ] A user-approved Conditional Codex Wrapper routes new WSL-shell `codex` commands only while the WSL Connection Session marker exists; otherwise it invokes the original command unchanged.
- [ ] Windows-to-WSL observation relies on Windows localhost readiness checks and fails safely for unsupported NAT or mirrored paths without changing WSL or network configuration.
- [ ] Normal Disconnect WSL prevents new connections and drains existing TUIs; confirmed Force Disconnect ends them, and neither operation blocks the UI.
- [ ] Reattachment, a new connection epoch, one-second no-wake WSL reconnection, cancellation, default distribution behavior, and one-active-distribution switching are covered by behavior tests.

## Deliver packaging and MVP quality gates

**What to build:** A releasable Agent Aura MVP whose installation, Windows/WSL observation behavior, accessibility, and resource envelope are proven on representative environments.

**Blocked by:** Deliver Windows App Server Guardian lifecycle; Deliver the WPF observation window and tray experience; Deliver settings, integration management, and runtime prerequisite handling; Deliver the WSL Connection Session.

- [ ] Clean Windows 11 validation covers package startup with and without the required runtime, setup/repair/remove ownership, and the uninstall integration-removal choice.
- [ ] Windows and WSL smoke suites verify the MVP's authoritative observation, lifecycle, recovery, and disconnect promises against supported Codex CLI versions.
- [ ] End-to-end acceptance covers aliases, title synchronization, Attention Pin Span, tray/window behavior, degraded states, accessibility, and user-initiated connected-resume copying.
- [ ] Resource automation samples comparable runs and enforces the accepted package, startup, memory, CPU, handle-growth, and sustained-growth regression envelope.
- [ ] Release documentation states supported versions, setup and recovery instructions, diagnostics, known limitations, and the explicit MVP scope.
