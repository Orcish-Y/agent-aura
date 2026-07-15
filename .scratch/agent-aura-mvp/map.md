# Agent Aura MVP Wayfinding Map

Label: wayfinder:map
Status: open

## Destination

Produce an implementation-ready MVP product and technical specification for Agent Aura on Windows 11, including its runtime-prerequisite distribution path and refined observation-window interactions, with no material decisions left unresolved before the work can be decomposed into implementation tickets.

## Notes

- This map plans the product; it does not implement the production application.
- MVP monitors only local Codex CLI Threads whose TUI connects through an Agent Aura-managed Codex App Server endpoint using the user-approved `codex --remote` launch path.
- Use .NET 10 + WPF for the MVP. Reconsider Tauri only if a required core capability cannot be implemented in .NET/WPF; resource or distribution costs drive WPF optimization and regression work rather than an automatic technology comparison.
- Every session must read `CONTEXT.md` and relevant `docs/adr/` files before working.
- Use `/openai-docs` for current Codex behavior, `/research` for primary-source investigation, `/prototype` for concrete UI or integration spikes, and `/domain-modeling` whenever project language changes.
- Refer to tickets by their titles, not bare numbers.
- Confirmed product constraints include: one Agent Item per resumable Codex Thread; App Server is authoritative for exact `completed`, `failed`, and `interrupted` outcomes; aliases persist by stable thread ID; Attention State uses a configurable Attention Pin Span; the main window has no taskbar button; alerts are limited to the observation window and flashing tray icon; no privacy mode in MVP.
- The default distribution is a framework-dependent .NET 10 WPF build. If the required Windows Desktop Runtime is missing, Agent Aura must explain the prerequisite and offer a link to Microsoft's official installer; it must not download or silently install the runtime. Resource work establishes optimization targets and regression guards for WPF.
- The observation window has no enclosing border. In Window Pin State, the Header retains its vertical footprint while hidden, but its content and translucent surface disappear. It reappears above unchanged Agent Message Item positions when the pointer enters any part of the observation window and hides when the pointer leaves.
- An Agent Message Item transitions between its one-line and four-line forms over 150 ms. Moving directly from one item to another starts the previous item's collapse and the next item's expansion concurrently.

## Decisions so far

<!-- Resolved ticket pointers are appended here. -->

- [Determine the reliable Codex event integration](issues/01-determine-codex-event-integration.md) — Agent Aura manages the App Server and observes only TUIs connected through `codex --remote`, gaining authoritative lifecycle, Attention State, title, and typed turn outcomes.
- [Prototype the WPF observation shell](issues/02-prototype-wpf-observation-shell.md) — its interaction model and required Windows capabilities are accepted; measured resource costs become WPF optimization and regression targets.
- [Map Codex events to Agent Item state](issues/03-map-codex-events-to-agent-item-state.md) — an App Server-authoritative, connection-epoch-protected state machine uses exact outcomes and keeps no Agent Aura runtime history across restarts.

- [Deliver the runtime-prerequisite startup path](issues/09-deliver-runtime-prerequisite-startup-path.md): Agent Aura uses a framework-dependent `win-x64` package; a user-controlled launcher handles the missing-runtime recovery path.
- [Preserve Agent Message Item positions in Window Pin State](issues/10-preserve-agent-message-item-positions-in-window-pin-state.md): hiding the pinned Header retains its layout footprint, so Agent Message Items do not move.
- [Animate Agent Message Item hover transitions](issues/11-animate-agent-message-item-hover-transitions.md): expansion and collapse each take 150 ms, including concurrent direct item-to-item handoffs.
- [Complete the product behavior and settings contract](issues/06-complete-product-behavior-and-settings.md) — defines lifecycle, list priority, tray/window behavior, configurable appearance, error and integration handling, and empty/reset states.
- [Establish the post-change resource baseline and regression check](issues/12-establish-post-change-resource-baseline-and-regression-check.md) — a continuous 30-minute Windows run separates the 0.220 MiB package from the shared runtime and establishes WPF optimization targets and regression guards.

- The WPF prototype does not include a Reduced motion setting. Agent Item title and detail text are always ellipsized within their available line; the existing detail fade and tray flashing remain available.
- [Define the Agent Message Item title strategy](issues/13-define-agent-message-item-title-strategy.md) — Thread Alias overrides an App Server-supplied Codex Thread Title, with a stable per-item directory-and-time fallback and no raw Prompt ingestion.
- [Prototype the Codex-to-WPF bridge](issues/05-prototype-codex-to-wpf-bridge.md) — the .NET/WPF observer can discover separately launched remote-TUI Threads with `thread/loaded/list` plus `thread/resume`, receive exact outcomes and Attention requests, preserve stable identity/aliases across resume, and recover by a fresh connection epoch; production still needs an automatic observer reconnect supervisor, while the tested classic console host must use the connected-resume copy fallback.
- [Determine terminal recovery behavior](issues/04-determine-terminal-recovery-behavior.md) — no current terminal host provides a safe Thread-to-window binding without a Hook or new explicit host contract; every MVP host uses the user-initiated connected-resume copy fallback.
- [Verify App Server title synchronization](issues/15-verify-app-server-title-synchronization.md) — `/rename` updates titles promptly and title catch-up succeeds after observer restart and an App Server replacement once the remote TUI reconnects; the production observer needs supervised reconnect and non-blocking shutdown.
- [Define non-blocking managed App Server shutdown and recovery](issues/16-define-nonblocking-managed-app-server-shutdown.md) — a detached App Server Guardian preserves remote TUIs after Aura exits, reclaims the server only after the final remote disconnect, and supports fresh-epoch Aura reattachment.
- [Prototype WSL Codex observation connectivity](issues/17-prototype-wsl-codex-observation-connectivity.md) — WSL-hosted loopback App Server plus Windows WPF observer works in mirrored networking; Windows-hosted App Server plus WSL TUI is rejected by cross-OS path semantics, and WSL Guardian lifecycle remains a separate blocker.
- [Define the WSL App Server Guardian lifecycle](issues/18-define-wsl-app-server-guardian-lifecycle.md) — a WSL-local, authenticated Guardian proxies and counts remote TUIs, conditionally routes new WSL-shell `codex` commands, drains existing TUIs safely, and supports non-waking one-second recovery after WSL restarts.
- [Prototype WSL NAT observation connectivity](issues/19-prototype-wsl-nat-observation-connectivity.md) — NATlocalhost forwarding supports the Windows observer-to-WSL loopback App Server path without a LAN listener; readiness-gated fallback never changes WSL networking.
- [Select the MVP runtime architecture](issues/07-select-the-mvp-runtime-architecture.md) — the MVP is fixed on .NET 10 + WPF with explicit Windows/WSL Guardian boundaries, framework-dependent distribution, and WPF-only performance regression handling.
- [Write the implementation-ready MVP specification](issues/08-write-the-implementation-ready-mvp-spec.md) — the implementation-ready contract, acceptance gates, risks, and seven delivery-ticket boundaries are consolidated in [PRD.md](PRD.md).
- [Investigate Codex 0.144.1 App Server title synchronization](issues/20-investigate-codex-01441-title-synchronization.md) — the apparent failure was an unsubmitted `/rename`; a Windows `0.144.1` remote TUI emits `thread/name/updated` and `thread/read` immediately catches up.

## Not yet specified

<!-- No in-scope fog remains: the final specification ticket defines the clean-machine compatibility and performance test matrix. -->

## Out of scope

- macOS and Linux support.
- Monitoring Codex IDE, Codex App, or cloud tasks as first-class sources.
- Windows toast notifications and notification sounds.
- Privacy mode and guaranteed screen-capture exclusion.
- Production implementation during this wayfinding effort.
- Exact visual styling beyond the accepted interaction model; it is intentionally deferred to implementation.
- [Prototype the Tauri observation shell](issues/14-prototype-tauri-observation-shell.md) — ruled out because Tauri is only a contingency for an unimplementable .NET/WPF core capability, and the WPF shell passed its capability gates.
- Monitoring ordinary `codex` sessions that are not connected to the Agent Aura-managed App Server endpoint.
- Observing Codex running inside Dev Containers; this requires a separate container-to-WSL App Server integration and is deferred beyond the MVP.
