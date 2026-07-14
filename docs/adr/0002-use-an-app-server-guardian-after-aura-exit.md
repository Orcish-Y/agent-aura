---
status: accepted
---

# Use an App Server Guardian after Agent Aura exits

When Agent Aura exits while remote TUIs remain connected, it hands the managed Codex App Server to a detached App Server Guardian rather than stopping it. The Guardian closes the server only when the Aura front end is absent and the remote-TUI connection count reaches zero; this preserves user-owned remote TUI windows while avoiding a permanent orphaned server.

## Consequences

- A new Aura front end can reconnect to the Guardian without replacing the App Server, then establishes a fresh observer-WebSocket epoch and restores observable Threads.
- The Guardian requests graceful App Server shutdown on the final remote-TUI disconnect and force-terminates it after five seconds if required.
- Aura must never synchronously wait for WebSocket or child-process shutdown on the WPF UI thread.
