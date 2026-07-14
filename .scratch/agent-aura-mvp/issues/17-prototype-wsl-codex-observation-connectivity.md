# Prototype WSL Codex observation connectivity

Type: prototype
Status: resolved
Blocked by:

## Question

Can the Windows 11 WPF Agent Aura front end reliably observe a Codex CLI Thread whose remote TUI runs inside WSL 2, without exposing an unauthenticated App Server beyond the local machine? Compare a WSL-hosted loopback App Server (with Windows-to-WSL localhost forwarding) against a Windows-hosted App Server reached from WSL, establish the supported launcher endpoint for default NAT and mirrored networking, and verify fresh-epoch reattachment plus App Server Guardian cleanup across the WSL boundary.

## Why this matters

The existing bridge prototype validates a Windows-native remote TUI only. WSL 2 has distinct NAT and mirrored-networking behavior, so its endpoint, authentication, process ownership, and cleanup semantics must be measured before the MVP runtime architecture is selected.

## Answer

For the tested mirrored-network configuration, the supported WSL topology is **a WSL-hosted loopback App Server, a WSL remote TUI, and a Windows WPF observer connected through localhost**. The inverse topology is not supported: a Windows-hosted App Server rejected a WSL TUI before Thread creation because its POSIX working directory could not be deserialized as a Windows absolute path.

This Windows 11 machine uses `networkingMode=mirrored` with the WSL firewall enabled. A WSL App Server bound solely to `ws://127.0.0.1:4517` returned `/readyz` successfully from both WSL and Windows; a Windows App Server bound solely to `ws://127.0.0.1:4518` was likewise reachable from WSL. No listener was bound to a LAN address. The user-copyable WSL launch command is:

```text
codex --remote ws://127.0.0.1:4517 --no-alt-screen
```

Default NAT mode was not switched on or measured during this run, so its localhost-forwarding compatibility must not be promised from the mirrored-mode result.

The Windows WPF prototype attached to the WSL server, completed `initialize`, and polled `thread/loaded/list`. A WSL TUI Thread was discovered. Before its first turn, `thread/resume` correctly returned `no rollout found`; after the user-authorized minimal turn completed, the observer resumed the Thread and received `turn/completed(status=completed)`. Restarting only the WPF observer while preserving the WSL App Server and TUI created a fresh observer connection, initialized it, and resumed the same stable Thread successfully.

The WSL App Server remains a local-only endpoint, satisfying the plain-WebSocket locality requirement. The official App Server documentation permits plain `ws://` only for localhost or SSH forwarding; this design uses localhost.

App Server Guardian cleanup is **not yet implemented or verified for a WSL-hosted service**. The existing Guardian decision covers a Windows-owned server only. Production WSL support therefore requires the separate Guardian lifecycle decision before the runtime architecture can claim reliable exit/recovery behavior.
