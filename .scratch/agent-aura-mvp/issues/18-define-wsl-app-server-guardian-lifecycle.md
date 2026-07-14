# Define the WSL App Server Guardian lifecycle

Type: grilling
Status: resolved
Blocked by: 17

## Question

How does Agent Aura retain, reattach to, and finally stop a WSL-hosted loopback Codex App Server when the Windows WPF front end exits or restarts, while preserving connected WSL remote TUIs and never exposing the App Server beyond localhost? Define the WSL Guardian's ownership boundary, launch/control path, final-disconnect detection, bounded shutdown, and recovery contract needed before WSL support can enter the MVP runtime architecture.

## Answer

MVP WSL support manages one user-selected **Active WSL Distribution** at a time. Aura can save that distribution as its default and connect it on startup; a different distribution is connected only after the current one has been safely disconnected. Dev Containers are expressly outside the MVP.

On connection, Aura starts or reattaches to a WSL-resident **WSL App Server Guardian**. The Guardian owns the WSL-local App Server and exposes only WSL-loopback endpoints. Aura authenticates its Guardian control/observer connection with a high-entropy **WSL Guardian Control Token** retained in the current Windows user's configuration. Aura holds a **Front End Lease**, refreshed every 15 seconds and expired after 45 seconds if Aura crashes or disappears; normal exit sends a non-blocking detach.

The user approves a one-time, persistent shell-initialization integration. It loads a **Conditional Codex Wrapper** in newly started WSL shells. Only while a current WSL Connection Session marker exists does the wrapper route `codex` to the Guardian's local remote-TUI endpoint; otherwise it invokes the original local Codex command unchanged. Existing shells are not retroactively modified.

The Guardian is the local proxy boundary: TUI clients use its sole WSL-loopback WebSocket ingress, while Aura uses a separate authenticated observer ingress. This lets the Guardian count TUI connections without relying on an undocumented App Server connection-count API. The production implementation must prove that the proxy preserves the supported `codex --remote` protocol while retaining loopback-only exposure.

Aura exit releases the front-end lease. If no TUI remains, the Guardian ends the WSL Connection Session; if TUI connections remain, it keeps serving them until the last one disconnects. The normal **Disconnect WSL** command stops admitting new remote TUIs by removing the runtime marker, preserves existing TUI connections, and drains the session after the final TUI exits. **Force Disconnect WSL** is a separate confirmed action that immediately ends the App Server and connected TUIs. In every automatic or drained shutdown, the Guardian requests graceful App Server termination, waits at most five seconds, then force-terminates and removes session runtime state. None of these waits may block the WPF UI.

On Aura restart, a live authenticated Guardian is automatically reattached with a new observer connection epoch; Aura rediscovers available Observed Codex Threads and never restores stale Agent Message Items or resumes a TUI automatically. If WSL restarts while Aura is open, Aura immediately shows the integration as disconnected and checks once per second whether the already-running Active WSL Distribution has returned. Once it has, Aura recreates the Guardian and observer connection automatically. It never starts a user-stopped WSL distribution as part of this recovery. A visible Cancel Reconnection action stops this retry loop until the user explicitly connects again. A WSL restart may have ended remote TUIs; Aura does not claim to restore them.

## Verification

- Verify default and manual connection, shell-wrapper fallback, and that an existing shell remains unchanged.
- Verify the Guardian counts TUI ingress separately from Aura's observer, and does not bind any App Server or Guardian listener beyond WSL localhost.
- Verify Aura exit, Disconnect WSL, and Force Disconnect WSL with zero, one, and multiple TUI connections; confirm draining preserves existing TUIs and force disconnect requires confirmation.
- Verify reattachment after Aura restart uses a new observer epoch, and WSL restart recovery occurs within the one-second check interval without waking a stopped distribution; verify Cancel Reconnection halts retries.
- Verify graceful termination completes within five seconds or is force-terminated without blocking the WPF UI.
