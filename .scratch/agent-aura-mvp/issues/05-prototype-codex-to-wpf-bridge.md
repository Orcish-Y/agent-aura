# Prototype the Codex-to-WPF bridge

Type: prototype
Status: resolved
Blocked by: 01, 02

## Question

Can a disposable end-to-end prototype start and health-check an Agent Aura-managed local Codex App Server, connect an independently launched terminal UI through `codex --remote`, and connect a WPF observer client that discovers and subscribes to the same concurrent Threads? Verify exact `turn/completed` outcomes, Attention State requests, stable Thread identity across resume, Thread Alias reapplication, the exact connected-resume launcher command, any trustworthy launcher-to-terminal-window association, App Server/TUI/observer reconnection and catch-up, and shutdown or repair without modifying unrelated Codex configuration.

## Answer

Yes for the core .NET/WPF managed-observation topology, with two explicit negative findings that become production requirements: the tested classic console host supplied no trustworthy terminal-window association, and the disposable observer did not automatically reconnect after App Server replacement.

The Windows 11 run used an Agent Aura-owned App Server bound only to `ws://127.0.0.1:4500`; `/readyz` returned `200`. By explicit user choice it used the existing Windows Codex home and ChatGPT login rather than the originally planned isolated login. No credential file was opened or copied. An ordinary terminal independently accepted `codex --remote ws://127.0.0.1:4500 --no-alt-screen`; Agent Aura did not have to create the Thread or launch it from its UI.

The WPF observer used a second initialized WebSocket client. It polled `thread/loaded/list`, then called `thread/resume` with `excludeTurns=false` for each newly loaded stable Thread. This subscription step is required: global start/status hints alone are insufficient, and an observer that could not resume a no-rollout ephemeral Thread did not receive the initiator-scoped terminal `turn/completed`. Once joined, the observer received authoritative `completed`, `failed`, and `interrupted` outcomes and an approval request that mapped to Attention State. The user visually confirmed every corresponding WPF state. Two concurrent remote TUIs produced two distinct items, while resume of Thread `019f5c3c-c85a-7510-bfb3-45f50b132ecd` preserved exactly one item.

The CLI accepted this exact connected-resume command:

```text
codex resume --remote ws://127.0.0.1:4500 019f5c3c-c85a-7510-bfb3-45f50b132ecd --no-alt-screen
```

The resumed TUI displayed prior conversation history. A disposable alias map persisted only `stable Thread ID → Thread Alias`; after a complete WPF/observer restart, a fresh snapshot/resume recreated one item and reapplied `Alias 019f5c3c`. This proves the identity key and alias reapplication seam without persisting Agent Aura runtime item history. Codex Thread Title synchronization remains deliberately separate in [Verify Codex App Server title synchronization](15-verify-app-server-title-synchronization.md).

For every new, resumed, and concurrent launcher tree tested under the classic Console Host candidate, recorded PID/session/creation-time metadata led only to processes whose `MainWindowHandle` was `0`; the launcher parent could also exit while the visible console remained. No stable Thread-to-window binding was proven. Agent Aura must not guess from timing, title, working directory, or process tree. This host must use **Copy connected resume command**; the terminal-recovery ticket may evaluate other hosts independently.

Restart behavior is now bounded:

- Stopping/restarting only the remote TUI left the App Server and WPF healthy; connected resume restored the same Thread.
- Restarting only the attach-mode WPF created a fresh observer connection, rediscovered the loaded Thread, resumed it, and reapplied its alias while the App Server/TUI stayed alive.
- Stopping only the App Server changed the live WPF item to `Disconnected` without fabricating a Turn Outcome State. Remote TUI Codex leaves exited on transport loss.
- Starting a replacement App Server did not heal the prototype's already-aborted WebSocket. The tested recovery was to restart the observer and reconnect the remote TUI; `thread/loaded/list` then exposed the Thread and `thread/resume` restored observation.

Production must replace that last manual observer restart with a reconnect supervisor: stop polling a dead socket, create a fresh `ClientWebSocket`, establish a monotonically newer connection epoch, re-run `initialize`/`initialized`, obtain `thread/loaded/list`, resume each available Thread, and reject late events from older epochs. Health/repair UX must distinguish a healthy server from a disconnected observer. After server replacement, unloaded Threads cannot be rediscovered until their remote TUI reconnects; the product should offer the proven connected-resume command rather than claiming background recovery.

Shutdown used a recursive ownership inventory and stopped exactly the WPF, supervised App Server, and prototype launcher trees. Port `4500` was released; unrelated ordinary Codex CLI trees, the Codex desktop application's server, and a user-owned PowerShell process remained alive. The scripts perform no Codex config write, and request-scoped failure-provider overrides were not persisted. Because the user-approved shared-home run lacked a pre-run hash and `config.toml` metadata changed while unrelated Codex processes were also active, byte-for-byte non-change is not attributable; future compatibility runs requiring that proof must capture a privacy-preserving pre/post hash or use an isolated home. Full evidence is in [run-05](../evidence/codex-bridge-prototype/run-05/README.md).

## Comments

### 2026-07-13 — prototype handoff

This is a human-in-the-loop prototype: its answer requires a Windows 11 machine, a real TUI connected through `codex --remote`, and explicit consent before starting local Codex or Agent Aura processes. Use an isolated `CODEX_HOME` and a loopback or otherwise local-only endpoint. Do not install lifecycle Hooks or modify the user's regular Codex configuration.

The prototype must demonstrate this exact sequence:

1. Start the disposable WPF observer and have it launch `codex app-server --listen <local-endpoint>` with explicit process ownership and health reporting.
2. From an ordinary terminal, launch the TUI with the prototype-provided `codex --remote <local-endpoint>` command. Confirm the user did not have to launch the Thread from inside Agent Aura.
3. Prove how the WPF observer discovers and subscribes to a Thread started by the separate TUI connection; do not assume App Server broadcasts every Thread event to every initialized client.
4. Exercise new turns, approvals or structured input, exact completed/failed/interrupted endings, concurrent Threads, and resume. Confirm one Agent Message Item per stable Thread ID and persisted Thread Alias reapplication. Record the exact user-copyable launcher command that reconnects to the managed endpoint and resumes a selected Thread; do not assume flag ordering or publish it before the real CLI accepts it.
5. On each candidate terminal host, test whether Agent Aura-owned launcher metadata can bind that stable Thread ID to the correct visible window across new, resumed, and concurrently launched remote TUIs. Reject event timing, title, working-directory, or process-tree guesses; verify handle/PID/session/creation-time metadata again before any best-effort activation. Hosts without a proven association must use the connected-resume copy fallback.
6. Restart the WPF observer, the remote TUI, and the App Server separately. Record the supported snapshot/subscription calls, connection-epoch behavior, catch-up limits, and `connection disconnected` presentation without fabricating a Turn Outcome State.
7. Stop and repair only Agent Aura-owned processes, endpoint metadata, or launcher artifacts. Confirm the isolated Codex configuration and unrelated user configuration remain unchanged.

This handoff supersedes the earlier Hook-first prototype plan. The current workspace is Linux/WSL and cannot itself complete the required Windows WPF and real-TUI checks. The ticket remains claimed pending a Windows 11 validation run and explicit approval to start the isolated App Server and remote TUI processes.

### 2026-07-14 — existing-login live run in progress

By explicit user choice, `run-05` uses the normal Windows Codex home and existing ChatGPT login instead of an isolated `CODEX_HOME`; no credential file was opened or copied. The Agent Aura-owned WPF process launched a loopback App Server whose `readyz` returned 200. A separately launched ordinary terminal ran `codex --remote ws://127.0.0.1:4500 --no-alt-screen`. The observer discovered Thread `019f5c3c-c85a-7510-bfb3-45f50b132ecd`, successfully joined it with `thread/resume` after its first rollout existed, and received exact `turn/completed(status=completed)` notifications for two Turns. The user visually confirmed that WPF showed the corresponding `Codex Thread 019f5c3c…` item as completed. Evidence: [run-05](../evidence/codex-bridge-prototype/run-05/README.md).
