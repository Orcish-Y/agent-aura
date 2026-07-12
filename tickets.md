# Tickets: Agent Aura MVP

These tickets build the Windows 11 Agent Aura MVP described in `.scratch/agent-aura-mvp/PRD.md`. Every ticket is `ready-for-agent`.

Work the **frontier**: any ticket whose blockers are all done.

## Validate the Windows observation shell

**What to build:** A disposable .NET 10 + WPF shell that proves the proposed Windows experience and distribution shape before production architecture is committed.

**Blocked by:** None — can start immediately.

- [ ] The prototype runs as a tray application with no taskbar button and can show, hide, minimise, restore, and focus its observation window.
- [ ] The observation window demonstrates a transparent frameless surface, pinned always-on-top mode, auto-hidden Header while pinned, and an always-visible Header while unpinned.
- [ ] A sample Agent Item expands from one line to four on hover and demonstrates edit/delete controls, low-saturation state styling, non-colour state cues, and overflowing text that scrolls without a tooltip.
- [ ] Reduced-motion mode disables flashing, transition animation, and text scrolling while keeping content understandable.
- [ ] The tray icon demonstrates flashing and acknowledgement when the observation window is restored.
- [ ] Window size, position, and pin state persist, and an off-screen saved position recovers onto an available monitor.
- [ ] A self-contained Windows build launches without a separately installed .NET Runtime.
- [ ] Cold startup, steady-state idle memory, idle CPU, package size, and obvious stability problems are recorded against explicit pass/fail thresholds.
- [ ] The outcome recommends WPF or a like-for-like Tauri comparison, with every failed core capability or unacceptable cost identified.

## Resolve the passive Codex integration contract

**What to build:** A verified integration contract showing how Agent Aura can observe Codex CLI Threads launched from ordinary terminals without requiring users to start them through Agent Aura.

**Blocked by:** None — can start immediately.

- [ ] Supported Codex mechanisms are compared using current primary evidence: hooks, plugins, app-server or remote-control events, and local session records.
- [ ] The selected mechanism exposes or derives stable Thread identity, working directory, title, lifecycle, approval/input waiting, success, failure, interruption, and liveness signals.
- [ ] Captured fixtures document every event needed by the MVP and distinguish verified fields from inferred fields.
- [ ] Minimum supported and currently tested Codex CLI versions are stated, including behaviour when the installed version is incompatible.
- [ ] Security, trust, local transport, authentication, data exposure, and configuration ownership boundaries are explicit.
- [ ] The contract explains how arbitrary terminal-launched TUI instances connect without silently changing the user's launch workflow.
- [ ] Installation, health inspection, repair, upgrade, and removal requirements are defined so unrelated Codex configuration is preserved.
- [ ] Remaining unsupported states or transport limitations have an explicit product fallback rather than an optimistic assumption.

## Observe one real Codex Thread end to end

**What to build:** The first production-shaped path from a real Codex CLI Thread through the chosen local integration into a visible WPF Agent Item.

**Blocked by:** Validate the Windows observation shell; Resolve the passive Codex integration contract.

- [ ] A Codex Thread launched from an ordinary supported terminal appears automatically as one Agent Item.
- [ ] The Agent Item is keyed by stable thread ID and displays available title and working-directory context.
- [ ] Multiple turns in the same Codex Thread update the existing Agent Item rather than adding duplicates.
- [ ] The visible path demonstrates running, Attention State, and at least one terminal state from real or contract-fixture events.
- [ ] The product-behaviour acceptance harness can inject a controlled Thread event stream and observe the real WPF surface.
- [ ] The integration-contract harness validates the same events independently from WPF implementation details.
- [ ] Closing and reopening Agent Aura starts with an empty runtime list while preserving the ability to observe the next Thread update.
- [ ] Missing or malformed identity is rejected or surfaced as degraded integration health rather than creating an untrackable Agent Item.

## Deliver the complete Agent Item state machine

**What to build:** Deterministic, user-visible state for every observed Codex Thread, including recovery from unreliable or disordered local events.

**Blocked by:** Observe one real Codex Thread end to end.

- [ ] Verified Codex events map to running, attention, succeeded, failed, interrupted, and unknown with documented transition rules.
- [ ] Approval and user-input waiting both produce the Attention State and leave it when Codex resumes work.
- [ ] Success, failure, cancellation, process exit, retry, disconnect, and stale-liveness scenarios have deterministic outcomes.
- [ ] Duplicate and out-of-order events cannot regress an Agent Item to an invalid earlier state.
- [ ] A reconnect or resumed Codex Thread updates the existing Agent Item when the stable thread ID matches.
- [ ] A state change triggers exactly two item flashes unless reduced motion is enabled.
- [ ] State is represented by both comfortable low-saturation colour and a non-colour cue.
- [ ] Controlled event-stream tests cover rapid transitions, reconnects, stale state, duplicate events, and concurrent Threads.

## Deliver Agent Item details and persistent Thread Aliases

**What to build:** A compact Agent Item that reveals useful Thread context on demand and lets the user name resumable conversations reliably.

**Blocked by:** Observe one real Codex Thread end to end.

- [ ] An Agent Item is one line while collapsed and smoothly expands to four lines on hover.
- [ ] The first line shows state, Thread Alias or fallback title, and hover-only edit and delete controls.
- [ ] The second line shows project name and working directory.
- [ ] The third line shows state, latest state-change time, and current-turn duration when available.
- [ ] The fourth line shows the most useful available current activity, waiting reason, error summary, or final outcome.
- [ ] Every line remains single-line; overflowing text begins a gentle internal scroll after a short delay, pauses at both ends, and never uses a tooltip.
- [ ] Reduced motion replaces internal scrolling with static truncation.
- [ ] Users can create, edit, and remove a Thread Alias, and title fallback uses Codex title, project folder, then a generic time-based label.
- [ ] Thread Aliases persist by stable thread ID across Agent Aura restart and `codex resume`, without leaking to a newly created Thread.
- [ ] Deleting an Agent Item dismisses it from the current list without deleting or altering the underlying Codex Thread.

## Deliver list priority, capacity, clear, and reappearance

**What to build:** A predictable observation list that keeps fresh user-relevant changes visible without allowing abandoned Attention State items to dominate forever.

**Blocked by:** Deliver the complete Agent Item state machine.

- [ ] Entering Attention State temporarily pins an Agent Item above non-pinned items.
- [ ] Significant Updates count only a new turn, entry into Attention State, or a terminal transition; streaming text, tool activity, and progress refreshes do not count.
- [ ] The default Attention Pin Span is ten Significant Updates from other Threads and can be configured from 1 to 50 or always pinned.
- [ ] New activity or renewed Attention State on the pinned Thread resets its Attention Pin Span.
- [ ] An expired Attention pin leaves the item in Attention State but returns it to latest-state-change ordering.
- [ ] Running items receive no special priority over recently completed or failed items.
- [ ] Collapsed and expanded capacities default to five and fifteen and validate ranges of 1–10 and 5–30, with expanded not less than collapsed.
- [ ] The list scrolls internally beyond expanded capacity, hides the expand control when unnecessary, and never displays a hidden-item count.
- [ ] Clear removes succeeded, failed, interrupted, and unknown items while preserving running and Attention State items.
- [ ] A deleted Agent Item reappears when its Thread later emits a Significant Update.
- [ ] Multi-Thread tests cover more items than both capacities and more Attention State items than collapsed capacity.

## Deliver tray-first window lifecycle

**What to build:** The complete quiet Windows lifecycle in which Agent Aura lives in the system tray and surfaces meaningful changes without toast notifications, sounds, or a taskbar button.

**Blocked by:** Observe one real Codex Thread end to end.

- [ ] Agent Aura has no taskbar button and remains controllable through its system tray icon.
- [ ] When the observation window is hidden, attention, succeeded, failed, or interrupted makes the tray icon flash; running and unknown alone do not.
- [ ] Several qualifying changes maintain one flashing alert instead of stacking animations.
- [ ] Left-clicking the tray icon restores and focuses the observation window and acknowledges the flashing state.
- [ ] The tray menu exposes show/hide, pin/unpin, settings, and exit.
- [ ] Pinned mode is always on top and hides the Header until the pointer enters; unpinned mode uses normal stacking and keeps the Header visible.
- [ ] The visible Header supports dragging the window.
- [ ] Minimise hides to the tray.
- [ ] Close offers hide to tray, exit, cancel, and remember-choice; cancel changes nothing and remembered behaviour is resettable.
- [ ] Window size, position, and pin state persist, and disconnected-monitor positions recover to a visible screen.
- [ ] No MVP path emits a Windows toast notification or alert sound.

## Deliver configurable and accessible presentation

**What to build:** A settings experience that makes Agent Aura comfortable on different Windows 11 displays and preserves every confirmed user preference.

**Blocked by:** Deliver Agent Item details and persistent Thread Aliases; Deliver list priority, capacity, clear, and reappearance; Deliver tray-first window lifecycle.

- [ ] Settings expose collapsed capacity, expanded capacity, Attention Pin Span, background opacity, theme, UI scale, reduced motion, sign-in startup, startup destination, and close-button behaviour.
- [ ] Background opacity ranges from 60% to 100% with an 88% default and does not unnecessarily fade text or state icons.
- [ ] Theme supports system, light, and dark with system as default.
- [ ] UI scale ranges from 80% to 150% without clipping the Header, Agent Item controls, or settings content.
- [ ] Reduced motion consistently disables item flashing, tray flashing alternatives as defined by accessibility behaviour, expansion transitions, and text scrolling without hiding state changes.
- [ ] Windows high-contrast mode remains usable and every state has a non-colour cue.
- [ ] Sign-in startup is off by default; when enabled, startup enters the tray by default.
- [ ] Settings and Thread Aliases persist, while the Agent Item runtime list and expanded/collapsed list state do not persist.
- [ ] Invalid combinations are prevented or corrected with a clear explanation.
- [ ] Users can restore all settings to defaults without deleting Thread Aliases unless explicitly chosen.

## Deliver reversible Codex integration setup

**What to build:** A user-approved setup and maintenance flow that installs the selected Codex integration, proves it works, repairs it, and removes only Agent Aura-owned changes.

**Blocked by:** Resolve the passive Codex integration contract; Observe one real Codex Thread end to end.

- [ ] Setup detects missing or incompatible Codex CLI versions and explains the supported remediation.
- [ ] Before changing global Codex configuration, setup shows what will be installed and requires explicit confirmation.
- [ ] One action installs the Agent Aura integration and performs an end-to-end connection test.
- [ ] Settings show integration health and offer check, repair, and uninstall actions.
- [ ] A damaged, partially removed, or outdated Agent Aura integration can be repaired without manual file editing.
- [ ] Failures show the failed operation, actionable error, retry option, and copyable diagnostic details.
- [ ] Uninstall removes only Agent Aura-owned plugin, marketplace, hook, and configuration entries and preserves unrelated configuration.
- [ ] Application uninstall asks whether to remove the Codex integration.
- [ ] Isolated configuration tests prove install, check, repair, upgrade, and removal are reversible and non-destructive.

## Deliver safe terminal recovery

**What to build:** A safe Agent Item action that returns the user to the correct terminal when a reliable association exists and degrades honestly when it does not.

**Blocked by:** Resolve the passive Codex integration contract; Observe one real Codex Thread end to end.

- [ ] Supported terminal hosts and the identifiers required to associate a Codex Thread with a terminal window are documented from verified Windows behaviour.
- [ ] Clicking an Agent Item restores, focuses, or flashes the correct terminal only when the association is unambiguous.
- [ ] The action handles minimised, hidden, background, and already-focused supported terminal windows.
- [ ] Permission or foreground-activation restrictions produce a useful, non-destructive fallback.
- [ ] Unsupported or ambiguous terminal associations never activate a guessed window.
- [ ] The fallback exposes available project or Thread context and explains that exact terminal recovery is unavailable.
- [ ] Automated or repeatable smoke cases cover every supported terminal host and the safe fallback path.

## Harden concurrent and degraded operation

**What to build:** Reliable observation under realistic parallel Codex work, local failures, event bursts, and application restarts.

**Blocked by:** Deliver the complete Agent Item state machine; Deliver list priority, capacity, clear, and reappearance; Deliver tray-first window lifecycle; Deliver reversible Codex integration setup.

- [ ] Several concurrent Codex Threads remain independently identified and correctly ordered during overlapping turns and state changes.
- [ ] Event bursts do not freeze the UI, lose the latest state, duplicate Agent Items, or inflate Significant Update counts.
- [ ] Codex, plugin, hook, app-server, or local transport restart scenarios recover automatically when supported and expose degraded health otherwise.
- [ ] Agent Aura enforces a single desktop instance and routes a second launch to the existing instance.
- [ ] Startup with missing, incompatible, or broken integration remains usable and directs the user to repair.
- [ ] Runtime state is safely discarded on Agent Aura exit while settings and Thread Aliases remain intact.
- [ ] Malformed, oversized, duplicated, stale, and out-of-order inputs are bounded and cannot crash the application.
- [ ] Idle CPU, idle memory, event-burst responsiveness, and long-running stability meet the thresholds established by the runtime prototype.
- [ ] External-behaviour tests cover concurrency and degradation without depending on private WPF or persistence structure.

## Ship the self-contained Windows 11 MVP

**What to build:** A cleanly installable and removable Windows 11 release candidate that satisfies the complete PRD on a machine without a separately installed .NET Runtime.

**Blocked by:** Deliver configurable and accessible presentation; Deliver reversible Codex integration setup; Deliver safe terminal recovery; Harden concurrent and degraded operation.

- [ ] The release artifact installs and launches on a clean supported Windows 11 environment without a separately installed .NET Runtime.
- [ ] Installation offers no silent sign-in startup or Codex integration changes; both require the confirmed user flows.
- [ ] Optional sign-in startup works and opens Agent Aura in the tray.
- [ ] Upgrade preserves settings and Thread Aliases and preserves or safely migrates Agent Aura-owned integration state.
- [ ] Uninstall removes application-owned files and asks whether to remove the Codex integration.
- [ ] A clean-machine acceptance run covers real Codex Thread observation, all six states, alias resume continuity, Attention Pin Span, list capacity, tray lifecycle, settings, degraded integration, and safe terminal recovery.
- [ ] Accessibility acceptance covers keyboard operation, high contrast, non-colour state cues, UI scaling, reduced motion, and readability at minimum opacity.
- [ ] Final cold startup, idle CPU, idle memory, package size, event-burst responsiveness, and long-running stability measurements are recorded.
- [ ] Known limitations, supported Codex CLI versions, supported terminal hosts, diagnostics, repair, and uninstall instructions are included with the release.
- [ ] If WPF failed a prototype gate, the release cannot proceed until the approved Tauri comparison and runtime decision are complete.

