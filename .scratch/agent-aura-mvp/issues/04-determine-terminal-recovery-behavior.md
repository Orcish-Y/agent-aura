# Determine terminal recovery behavior

Type: research
Status: resolved
Blocked by: 05

## Question

With the selected managed App Server and separate `codex --remote` TUI, can an Agent Aura-owned launcher establish a reliable binding from stable Thread ID to the correct Windows terminal window without lifecycle Hooks? Establish capability by terminal host, new/resumed/concurrent Thread correlation, required metadata, permission limitations, expiry, and the exact fallback when accurate activation is impossible.

## Comments

### 2026-07-13 — architecture correction

The selected architecture does not use a lifecycle Hook inside the remote TUI process. The documented App Server Thread and Turn protocol provides no terminal process ID, window handle, Windows Terminal tab/pane ID, or equivalent binding between a stable Thread ID and the visible terminal owned by the separate `codex --remote` client. This invalidates the earlier Hook-based Console Host answer, but does not by itself prove that an Agent Aura-owned launcher cannot establish the missing binding.

The Codex-to-WPF bridge prototype must test whether launcher metadata can be correlated unambiguously with the App Server Thread across new, resumed, and concurrent remote TUI sessions, then revalidate any candidate window before activation. Agent Aura must never guess from title, working directory, shell name, event timing alone, or a process-tree search, and must not bypass Windows focus protections.

If the prototype cannot prove that association for a host, clicking keeps the item selected, acknowledges the Agent Aura alert, and offers **Copy connected resume command**. The command must reconnect to the managed endpoint and target the selected stable Thread ID; its exact CLI spelling is also a bridge-prototype deliverable. Agent Aura neither activates a guessed window nor launches/resumes Codex automatically.

The earlier Hook-based host capability matrix is retained only as [superseded research](../research/terminal-recovery.md). The current observation boundary is recorded in [Use an Agent Aura-managed Codex App Server](../../../docs/adr/0001-use-agent-aura-managed-codex-app-server.md).

## Answer

No. In the selected managed-App-Server plus separately launched `codex --remote`
TUI topology, Agent Aura cannot establish a reliable stable Codex Thread ID to
visible-terminal-window binding without a new explicit host contract. The App
Server protocol exposes Thread and Turn data but no terminal process or window
identity. The tested classic Console Host launcher tree supplied no
re-verifiable `MainWindowHandle`; `MainWindowHandle`, window enumeration,
process trees, titles, working directories, and event timing are not a safe
substitute. Consequently, this applies equally to new, resumed, and concurrent
Threads.

The MVP supports no automatic terminal activation for classic Console Host,
Windows Terminal, VS Code integrated terminal, or another ConPTY/pseudo-console
host. A future host-specific integration may be considered only if its launch
path explicitly reports a verifiable, exclusive window binding for the stable
Thread ID. Such a mapping must record the HWND, owner PID, owner creation time,
Windows session ID, and capture time; before every use it must revalidate all
of them plus visibility, and expire on process/session change, failed
validation, or a product-defined TTL. `SetForegroundWindow` remains
best-effort, and Agent Aura must not use elevation, UIAccess,
`AttachThreadInput`, synthetic input, or foreground-permission bypasses.

For every currently supported host, clicking an Agent Message Item keeps it
selected, acknowledges the Tray Alert, and offers the user-initiated **Copy
connected resume command** action. It copies the tested command shape:

```text
codex resume --remote <agent-aura-endpoint> <stable-thread-id> --no-alt-screen
```

Agent Aura neither activates/flashes a guessed window nor launches/resumes
Codex automatically. The detailed Windows primary-source evidence is in
[terminal-recovery-launcher.md](../research/terminal-recovery-launcher.md);
the command shape and unbound classic-host result are verified by the
[Codex-to-WPF bridge prototype](../evidence/codex-bridge-prototype/run-05/README.md).
