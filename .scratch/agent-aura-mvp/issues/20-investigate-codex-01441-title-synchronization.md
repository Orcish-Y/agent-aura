# Investigate Codex 0.144.1 App Server title synchronization

Type: task
Status: resolved
Blocked by:

## Problem

On Windows 11 with Codex CLI `0.144.1`, an Observed Codex Thread connected through the managed local App Server did not expose a Codex Thread Title after the user ran `/rename Agent Aura smoke` in the resumed remote TUI.

This conflicts with the earlier prototype conclusion in [Verify Codex App Server title synchronization](15-verify-app-server-title-synchronization.md), so that result must not be treated as covering CLI `0.144.1`.

## Reproduction

1. Start `codex app-server --listen ws://127.0.0.1:4500` on Windows.
2. Connect a terminal UI with `codex --remote ws://127.0.0.1:4500`.
3. Send a message, close and reopen the TUI, resume the same Thread, then run `/rename Agent Aura smoke`.
4. From a separate initialized App Server observer connection, call `thread/loaded/list`, `thread/resume`, and `thread/read` with `includeTurns: false`.

## Observed result

- The Thread is discovered by stable ID and `thread/read` returns its working directory and `idle` status.
- The returned Thread `name` remains null after a delay and after reopening/resuming the TUI.
- A 60-second subscribed observer window receives no `thread/name/updated` notification after repeating `/rename`.

## Expected result

The current Thread `name` should catch up through the supported App Server API, and `/rename` should emit `thread/name/updated`, so Agent Aura can apply `ThreadTitleChanged` without reading private Codex session records.

## Impact

The Windows App Server observation smoke gate is incomplete for the provisional supported CLI version. Agent Aura must retain its fallback-title behavior and must not claim Codex Thread Title synchronization for `0.144.1` until this is resolved or explicitly scoped as unavailable.

## Answer

Resolved on 2026-07-15. The earlier result was a false negative: `/rename` had not been successfully submitted in the remote TUI.

On Windows 11 with Codex CLI `0.144.1`, a fresh `codex --remote ws://127.0.0.1:4513` TUI created Thread `019f63ed-d436-7581-adfa-d81691e0e7b3`. A separately initialized, read-only observer discovered that Thread, subscribed with `thread/resume` using `excludeTurns: false`, and polled `thread/read` with `includeTurns: false`. After the TUI confirmed `Session renamed to Agent Aura fresh smoke`, the observer received:

```json
{"method":"thread/name/updated","params":{"threadId":"019f63ed-d436-7581-adfa-d81691e0e7b3","threadName":"Agent Aura fresh smoke"}}
```

The immediately following `thread/read` response contained `name: "Agent Aura fresh smoke"`. Before the command, the same response contained `name: null`. No private Codex session records were read, and the temporary loopback App Server, remote TUI, observer, and logs were removed after the test.

Therefore the MVP may treat Codex CLI `0.144.1` as title-synchronization-capable for the established managed-App-Server smoke path. The existing fallback title remains required for Threads which have not supplied a Codex Thread Title; no production code change is needed for this investigation.

## Superseded follow-up ideas

These were not needed to resolve the observed regression: the direct Windows `0.144.1` reproduction established the expected event and title catch-up. Retain them only if future version-compatibility coverage is deliberately broadened.

- Confirm the documented/current CLI semantics for `/rename` in remote-TUI mode.
- Repeat with a clean Codex configuration and an independently chosen current CLI version.
- Capture only the supported JSON-RPC title-related notifications and `thread/read` metadata; do not inspect private session records.
