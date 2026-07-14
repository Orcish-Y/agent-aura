# Agent Aura MVP — implementation specification

Status: ready-for-agent

## 1. Purpose and scope

Agent Aura is a Windows 11 tray application that provides a quiet, glanceable
observation window for **Observed Codex Threads**. An Observed Codex Thread is a
local Codex CLI Thread connected to an Agent Aura-managed Codex App Server; all
other `codex` sessions are outside this MVP.

The product delivers:

- One **Agent Message Item** per stable Codex Thread ID, displaying its latest
  authoritative state and project context.
- A Windows-native WPF observation surface, tray interaction, settings, and
  local persistence.
- Windows-native and one-active-distribution WSL observation paths, each with a
  Guardian that preserves remote TUIs when the front end exits.
- Explicit setup, repair, removal, prerequisite detection, and diagnostics.

The MVP excludes macOS/Linux applications, Codex IDE/App/cloud sources, Dev
Container observation, ordinary non-remote CLI sessions, toast/sound alerts,
privacy mode, screen-capture guarantees, history/inbox semantics, and automatic
terminal activation. Exact visual styling is an implementation concern provided
the behavior and accessibility requirements below hold.

## 2. Architectural constraints

- Target `net10.0-windows`, x64, Windows 11; use WPF and MVVM.
- Ship a framework-dependent `win-x64` package. Do not bundle, download, or
  silently install .NET. A launcher detects the x64 .NET 10 Windows Desktop
  Runtime before starting Aura; if missing, it explains the prerequisite and
  offers a user-invoked link to Microsoft's installer.
- Use the Agent Aura-managed Codex App Server as the only lifecycle authority.
  The Windows remote-TUI command is `codex --remote <endpoint>`; App Server
  `completed`, `failed`, and `interrupted` outcomes are authoritative.
- The front end owns UI, tray behavior, settings, current runtime items, and
  Guardian supervision. A shared .NET library owns protocol handling, connection
  epochs, state transitions, configuration contracts, and IPC DTOs. Windows and
  WSL Guardians own their respective App Server lifecycle and remote-TUI ingress.
- Persist only settings, Thread Aliases, WSL control token, and default WSL
  distribution. Never persist Agent Message Items or their runtime history.
- All UI-initiated cleanup is asynchronous. It must never wait for WebSocket
  shutdown or child-process termination on the WPF UI thread.

The accepted architectural records are [ADR 0001](../../docs/adr/0001-use-agent-aura-managed-codex-app-server.md),
[ADR 0002](../../docs/adr/0002-use-an-app-server-guardian-after-aura-exit.md),
[ADR 0003](../../docs/adr/0003-use-a-wsl-local-guardian-and-conditional-codex-wrapper.md),
and [ADR 0004](../../docs/adr/0004-fix-the-mvp-on-dotnet-wpf.md).

## 3. Product behavior

### 3.1 Agent Message Items and state

The stable Codex Thread ID is item identity and the key for Thread Aliases. An
item has: Thread ID, working directory, Codex Thread Title, optional Thread
Alias, per-item Fallback Thread Title, current state, current Turn ID, latest
state-change time, attention-pin counter, and connection epoch. State is one
of `observed`, `running`, `attention`, `completed`, `failed`, `interrupted`, or
`connection disconnected`.

| Authority input | Required result | Significant Update |
| --- | --- | --- |
| First Thread observation | Create item, apply alias, use `observed` absent active-turn information. | No |
| `turn/started` | `running`; clear attention and old outcome. | Yes |
| Approval/input/MCP elicitation/permission request | `attention`; never infer it from text or time. | On entry only |
| `serverRequest/resolved` | `running`, unless a concurrent terminal event wins. | No |
| `turn/completed` | Exact `completed`, `failed`, or `interrupted`. | Yes |
| Transport loss | `connection disconnected`, retaining any previous outcome. | No |

Correlate events by Thread ID and Turn ID, process transport order within an
epoch, and deduplicate terminal results by Turn ID. On reconnect, create a newer
epoch, obtain the App Server's current snapshot and subscriptions, and discard
late events from older epochs. Aura restart starts with no items, then
rediscovers current Threads; it does not replay local history.

An Attention State entry resets that item's configurable **Attention Pin Span**
(default 10). Significant Updates from *other* Threads decrement it exactly
once; the item's own events do not. Re-entry resets it. A value of 1–50 or
Always pinned is supported. After expiry the item remains in place; newer
ordinary updates insert above it. Other items order by latest state change.

**Dismiss Agent Message Item** removes only a runtime entry; a subsequent valid
event recreates it. **Clear Agent Message Items** removes every runtime entry.
Neither operation deletes the Thread or Thread Alias.

### 3.2 Titles and item interaction

The display-title precedence is Thread Alias, Codex Thread Title, then Fallback
Thread Title. The fallback is `<working-directory leaf> · <first-observed local
time>`, computed once per item lifecycle. Never obtain, save, or summarise raw
prompts. A Codex title rename updates a no-alias item immediately without moving
it or counting as a Significant Update; an alias continues to win until removed.

A collapsed item is one line. Hover expands it to four lines over 150 ms:

1. state, title, edit-alias action, dismiss action;
2. project name and working directory;
3. state, last-change time, current-turn duration;
4. current activity, wait reason, concise error, or final outcome.

All title/detail text ellipsizes in available space, with no tooltip. Directly
moving between items concurrently begins the prior 150 ms collapse and next
150 ms expansion. State uses a small leading color square plus accessible name
and/or text, so color is never the only cue.

Selecting an item acknowledges a Tray Alert but must not activate, flash, or
guess a terminal. It offers **Copy connected resume command**:

```text
codex resume --remote <agent-aura-endpoint> <stable-thread-id> --no-alt-screen
```

This is user-initiated copy only; Aura never launches or resumes Codex on the
user's behalf.

### 3.3 Window, tray, and settings

The observation window is frameless, transparent/semi-transparent, has no
enclosing border or taskbar button, and is tray-resident. It supports collapsed
and expanded lists: defaults 5 and 15; collapsed range 1–10; expanded range
5–30 and never less than collapsed. Overflow scrolls inside the expanded list.
Show no hidden-item count and hide the expand control when nothing is hidden.

**Window Pin State** means Topmost. While pinned, the Header's content and
translucent surface hide when the pointer leaves, but the Header retains its
layout height so Agent Message Item positions never move. Entering any window
area restores it above unchanged items. Unpinned windows always show the Header;
the visible Header is the drag region. Persist pin state, size, and position;
relocate an off-screen restored position to an available monitor.

When hidden, entering Attention State or a terminal outcome creates one
aggregate **Tray Alert**. Further qualifying updates do not stack it. Left-click
on the tray restores and focuses the window, then acknowledges/stops the alert.
The tray menu contains Show/Hide, Pin, Settings, and Exit. Minimize hides to
tray. Default close offers Hide to tray, Exit, and Cancel with no preselected
exit; a remembered Hide/Exit choice is resettable in Settings.

Settings include custom background/text/state colors (each restorable), 30–100%
background opacity (default 88%, never reducing text/control contrast), 80–150%
whole-interface scale, capacities, Attention Pin Span, Window Pin State,
sign-in launch, Silent Startup, and close behavior. Sign-in launch defaults off;
when enabled, sign-in starts hidden. A manually launched Aura always shows its
window. With Silent Startup disabled, the next Significant Update shows the
sign-in-launched window; with it enabled, it does not. Windows High Contrast
overrides user colors. Restore defaults requires confirmation and resets all
settings/geometry but not aliases, integration, or current runtime items.

Provide keyboard-reachable visible controls, usable layouts at every scale,
non-color state cues, and high-contrast rendering. No additional screen-reader
or reduced-motion feature is committed for this MVP.

### 3.4 Integration health and errors

Settings shows `Connected`, `Checking`, `Needs repair`, or `Unavailable`, with
Check, Repair, and Remove. State-changing actions preview the affected
Agent-Aura-owned processes, endpoint metadata, and launcher/wrapper artifacts
and require confirmation. They never alter unrelated Codex configuration.
Failures are non-modal notices in the window or Settings, identify the failed
operation, and expose Retry plus copyable diagnostics. A healthy empty state
says Aura is waiting for Codex Thread activity; an unhealthy empty state states
the cause and offers Check/Repair.

## 4. Runtime paths

### 4.1 Windows-native

Aura starts/health-checks its Windows-local App Server and accepts remote TUIs.
The detached **App Server Guardian** owns the server after Aura exits. It tracks
remote-TUI connections and a Front End Lease. Normal front-end exit detaches;
an unexpected loss expires after 45 seconds without 15-second heartbeats. While
the front end runs, zero TUIs does not stop the server. After Aura has exited,
the Guardian shuts down only once TUI count is zero: graceful request, maximum
five seconds, then force terminate. A running remote TUI must never be ended by
normal Aura exit.

On restart, Aura reattaches instead of replacing a live Guardian/server, creates
a fresh observer epoch, initializes, lists/resumes available Threads, and
rebuilds runtime observations. A server failure leaves Aura responsive, marks
affected items disconnected without inventing an outcome, starts a replacement
for new connections, and shows the copy-resume recovery path. Existing TUIs
explicitly reconnect; automatic TUI reconnection is not promised.

### 4.2 WSL

Support one user-selected **Active WSL Distribution** at a time. A WSL-local
Guardian owns a WSL-loopback App Server, proxy ingress for remote TUIs, a WSL
Connection Session, and an authenticated WSL-loopback control/observer channel.
Aura generates and persists a high-entropy WSL Guardian Control Token and holds
the same 15-second/45-second Front End Lease. Neither App Server nor Guardian
may bind beyond local WSL networking.

On a user-approved one-time shell integration, a **Conditional Codex Wrapper**
routes `codex` through the current session only when its runtime marker exists;
otherwise it executes original `codex` unchanged. Existing shells are unchanged.
Normal **Disconnect WSL** removes the marker, refuses new TUIs, and drains
existing TUIs; **Force Disconnect WSL** requires confirmation and ends them.
The final drain uses the same graceful/five-second-force process and never
blocks the UI.

The Windows observer connects to the WSL-loopback App Server through Windows
localhost. For NAT, require `networkingMode=nat`, `localhostForwarding=true`,
and `firewall=true`, but gate support with a Windows localhost readiness check,
not configuration inference. Do not create port proxies, wildcard/LAN binds,
firewall exceptions, or modify/restart/wake WSL. If unavailable show:
“无法连接到 WSL 的本地 Codex App Server；此 WSL 发行版暂不可观察。” with Retry
and an explanation of user-controlled forwarding/network-mode recovery.

On Aura restart, reattach to an authenticated live Guardian using a new epoch.
After unexpected WSL loss while Aura stays open, show disconnected and check
once per second whether the already-running distribution has returned; never
wake a stopped distribution. On return create a new Guardian/observer session.
Cancel Reconnection stops this loop until explicit connect. Do not promise to
restore terminated TUIs.

## 5. Data and interface contracts

Persist settings, aliases keyed by Thread ID, and WSL connection data in a
per-user local configuration store with atomic replacement/validation. Secrets
must not appear in UI diagnostics. Model the following replaceable interfaces:

| Interface | Required contract |
| --- | --- |
| `ICodexAppServerObserver` | Initialize, list/load/resume Threads, subscribe to events, expose connection epoch and transport loss. |
| `IThreadStateReducer` | Deterministically converts current epoch App Server inputs into item state and Significant Updates; rejects stale/duplicate results. |
| `IGuardianClient` | Start/attach, lease heartbeat/detach, health, drain/force-stop, and event/TUI-count reporting; all calls cancellable and non-blocking to UI. |
| `ISettingsStore` | Validated, atomic settings/alias/WSL credential persistence; never item history. |
| `IIntegrationManager` | Previewed, confirmed Check/Repair/Remove and user-approved setup; owns only Aura artifacts. |
| `ITrayController` | Aggregate alert, acknowledgement, menu commands, and no-taskbar window lifecycle. |

Keep protocol DTOs and Guardian IPC DTOs in the shared library. App Server calls
must use only the supported protocol; do not read private Codex session records.

## 6. Acceptance criteria and verification

1. A separately launched connected remote TUI is discovered and represented by
   one item through repeated turns and `codex resume`; exact turn outcomes,
   title catch-up/rename, Attention State, alias precedence, and stale-epoch
   rejection are verified against current supported Codex CLI.
2. Dismiss/Clear preserve aliases and recreate items only after valid later
   events. No runtime list appears after restart before rediscovery.
3. Attention Pin Span, duplicate suppression, ordering, capacity, scrolling,
   direct-hover concurrent animation, ellipsis, pin-header stationary layout,
   tray acknowledgement, close behavior, persistence, and degraded state are
   covered through a controllable event stream driving the real WPF window.
4. Window tests prove tray-only/no-taskbar operation, frameless transparency,
   Topmost behavior, multi-monitor recovery, sign-in behavior, scale bounds,
   keyboard controls, high contrast, and non-color state cues.
5. Windows Guardian tests prove Aura Exit returns UI/tray control within 250 ms,
   preserves a remote TUI, drains at last disconnect, uses five-second forced
   fallback, and reattaches with a fresh epoch.
6. WSL tests cover manual/default connect, wrapper fallback, one active
   distribution, loopback-only listeners, NAT/mirrored localhost readiness,
   drain versus confirmed force disconnect, reattachment, no-wake one-second
   recovery, and Cancel Reconnection.
7. Clean Windows 11 package tests cover runtime-present startup, runtime-missing
   user-invoked installer link, setup/repair/remove ownership boundaries, and
   uninstall choice for integration removal.
8. Every comparable resource run collects uninterrupted samples. Regression
   envelope: package <=1 MiB; median startup <=3.0 s; private-memory P95 <=260
   MiB; working-set P95 <=340 MiB; CPU average/P95 <=1%/3%; no >+32 MiB private
   memory or >+32 handles across first-to-last five-minute windows without
   sustained monotonic growth. Repeat numerical breaches once; investigate
   confirmed breaches, crashes, incomplete sampling, or sustained growth in WPF.
   Aspirational optimization targets remain 1.5 s startup and 100 MiB idle
   working set, not architecture-change gates.

## 7. Implementation-ticket boundaries

1. **Solution and shared contracts** — projects, DTOs, reducer, persistence
   schema/migrations, deterministic reducer tests.
2. **Windows App Server observer** — owned-server startup, discovery/subscription,
   epoch recovery, title/attention/outcome contract fixtures and real smoke test.
3. **Windows Guardian and lifecycle** — detached process, leases, remote-TUI
   counting/proxy boundary, drain/re-attach/failure recovery.
4. **WPF observation shell** — list/layout/animation/window pin, item controls,
   accessibility, tray and close lifecycle.
5. **Settings and integration management** — settings UI/store, aliases,
   setup/check/repair/remove, diagnostics, startup and runtime launcher.
6. **WSL connection path** — Guardian deployment/control, wrapper, local proxy,
   readiness gate, drain/force disconnect and recovery UX.
7. **Packaging and quality gates** — clean-machine installer/uninstall coverage,
   behavior acceptance harness, Windows/WSL smoke suites, and resource regression
   automation.

Items 2 and 3 form the Windows observation dependency chain; item 4 can proceed
against shared test fixtures; item 6 depends on the shared contracts and may
proceed after Windows Guardian semantics are fixed. Item 7 runs throughout and
is final release gating.

## 8. Risks and explicit resolution status

- Codex App Server behavior is version-sensitive: pin a tested minimum/current
  CLI smoke matrix and make unsupported versions `Unavailable` with diagnostics.
- Terminal-window identity is intentionally unavailable; copy-resume is the
  complete MVP recovery behavior, not a temporary gap.
- The WPF prototype misses aspirational resource goals; regression guards and
  focused WPF optimization are required, not a UI-stack substitution.
- WSL remains local-only and user-controlled; its forwarding/readiness path must
  fail safely without network or WSL configuration mutation.

No product or architecture decision remains unresolved before implementation.
