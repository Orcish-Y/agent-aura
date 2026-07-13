# Agent Aura MVP Wayfinding Map

Label: wayfinder:map
Status: open

## Destination

Produce an implementation-ready MVP product and technical specification for Agent Aura on Windows 11, with no material decisions left unresolved before the work can be decomposed into implementation tickets.

## Notes

- This map plans the product; it does not implement the production application.
- MVP monitors local Codex CLI Threads on Windows 11 through a passive, user-approved integration.
- Start with .NET 10 + WPF. Treat it as provisional until the technical prototype passes the agreed feasibility gates; compare Tauri only if a core gate fails or resource/distribution costs are unacceptable.
- Every session must read `CONTEXT.md` and relevant `docs/adr/` files before working.
- Use `/openai-docs` for current Codex behavior, `/research` for primary-source investigation, `/prototype` for concrete UI or integration spikes, and `/domain-modeling` whenever project language changes.
- Refer to tickets by their titles, not bare numbers.
- Confirmed product constraints include: one Agent Item per resumable Codex Thread; aliases persist by stable thread ID; Attention State uses a configurable Attention Pin Span; the main window has no taskbar button; alerts are limited to the observation window and flashing tray icon; no privacy mode in MVP.

## Decisions so far

<!-- Resolved ticket pointers are appended here. -->

- The WPF prototype does not include a Reduced motion setting. Agent Item title and detail text are always ellipsized within their available line; the existing detail fade and tray flashing remain available.

## Not yet specified

- Exact visual styling and motion values beyond the agreed interaction model may need refinement after the first WPF shell prototype.
- The final installer, upgrade, and recovery UX depends on the chosen Codex integration and the WPF distribution spike.
- The clean-machine compatibility and performance test matrix depends on the final runtime architecture.

## Out of scope

- macOS and Linux support.
- Monitoring Codex IDE, Codex App, or cloud tasks as first-class sources.
- Windows toast notifications and notification sounds.
- Privacy mode and guaranteed screen-capture exclusion.
- Production implementation during this wayfinding effort.
