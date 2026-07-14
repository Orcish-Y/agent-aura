# Prototype WSL NAT observation connectivity

Type: prototype
Status: resolved
Blocked by: 17

## Question

When WSL 2 uses default NAT networking rather than mirrored networking, can a Windows WPF observer attach to a WSL-hosted loopback Codex App Server and can a Windows-local endpoint safely reach the WSL remote TUI without a LAN-exposed listener? Define the supported endpoint, required WSL/Hyper-V forwarding settings, and the user-visible fallback if the local forwarding path is unavailable. This requires explicit approval before changing the user's WSL networking configuration or restarting WSL.

## Answer

**Yes, for the observer-to-App-Server path.** In the tested WSL 2 NAT configuration, a WSL-hosted Codex App Server
bound only to `127.0.0.1:4521` was reachable from Windows at `ws://127.0.0.1:4521`. A disposable Windows .NET
`ClientWebSocket` probe completed `initialize`, `initialized`, and `thread/loaded/list`; the App Server identified
itself as Unix/Linux and returned the expected empty loaded-Thread list. The WPF observer uses the same .NET WebSocket
transport and protocol initialization path, so its attachment endpoint is the same Windows localhost URL.

The supported topology is deliberately **not** a Windows endpoint directly reaching a remote TUI. Both the WSL remote
TUI and the Windows observer independently connect to the WSL-local App Server:

```text
WSL remote TUI:       codex --remote ws://127.0.0.1:4521 --no-alt-screen
Windows WPF observer: ws://127.0.0.1:4521
WSL App Server bind:  ws://127.0.0.1:4521

This keeps the remote-TUI connection inside WSL and uses WSL NAT's localhost forwarding only for the Windows observer.
The listener was confirmed as 127.0.0.1:4521 inside WSL. A request through the machine's WLAN IPv4 address timed out,
so this validation found no LAN listener or LAN route for the App Server.

For a deterministic supported NAT configuration, Agent Aura requires:

[wsl2]
networkingMode=nat
localhostForwarding=true
firewall=true

localhostForwarding is currently documented as true by default, but the product should check it with a localhost
readiness request and treat that request—not configuration inference—as the capability gate. No netsh interface
portproxy, wildcard bind, Hyper-V firewall exception, or LAN firewall rule is required or permitted for this topology.
Such settings would broaden exposure rather than enable the local observer path. Configuration changes or a mode
switch require the user's approval and a WSL restart; this prototype made neither change.

If the readiness request cannot connect from Windows to the WSL loopback App Server, Agent Aura must show: “无法连接到
WSL 的本地 Codex App Server；此 WSL 发行版暂不可观察。” It offers Retry and an explanation that the user may enable
NAT localhost forwarding or return to their preferred mirrored mode, then restart WSL. It must not change .wslconfig,
add port proxies, or wake/restart WSL itself. Existing remote TUIs remain untouched; the user can continue using
ordinary unobserved Codex or a Windows-native observed session.

Evidence is retained in ../evidence/wsl-nat-observation-20260714/: Invoke-WslNatObserverProbe.ps1 is the one-command
disposable probe and observer-probe.jsonl records the successful protocol exchange. Microsoft documents that NAT-mode
WSL services are reachable from Windows through localhost, and that localhostForwarding controls this host-local
reachability: https://learn.microsoft.com/en-us/windows/wsl/networking
(https://learn.microsoft.com/en-us/windows/wsl/networking) and
https://learn.microsoft.com/en-us/windows/wsl/wsl-config (https://learn.microsoft.com/en-us/windows/wsl/wsl-config).