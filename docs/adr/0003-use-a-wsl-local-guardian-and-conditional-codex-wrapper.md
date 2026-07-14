---
status: accepted
---

# Use a WSL-local Guardian and conditional Codex wrapper

For the one active WSL distribution in the MVP, Agent Aura uses a WSL-resident Guardian that proxies and counts WSL-loopback remote TUI connections, instead of requiring users to type `codex --remote` manually. A user-approved persistent shell integration routes `codex` only while the current WSL Connection Session is active and otherwise preserves the original command; this keeps existing TUIs safe while allowing Aura exit and explicit disconnect to drain the session.

## Consequences

- The Guardian authenticates Aura separately and must never expose either its control surface or the App Server beyond local WSL networking.
- Aura must distinguish normal draining disconnect from confirmed Force Disconnect, and may not block its WPF UI while waiting for WSL cleanup.
- Dev Container Codex observation is outside the MVP because it needs a separate container-to-WSL integration boundary.
