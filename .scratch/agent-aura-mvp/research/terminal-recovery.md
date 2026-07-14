# Windows terminal recovery finding

> **Superseded:** This finding assumes a lifecycle Hook running inside the observed TUI. The MVP now observes a separate `codex --remote` client through an Agent Aura-managed App Server, whose documented protocol does not provide a Thread-to-terminal-window binding. Whether the Agent Aura-owned launcher can establish an equivalent safe binding is unresolved and must be prototyped. See [Determine terminal recovery behavior](../issues/04-determine-terminal-recovery-behavior.md) and [Use an Agent Aura-managed Codex App Server](../../../docs/adr/0001-use-agent-aura-managed-codex-app-server.md).

## Decision

Agent Aura must not infer a terminal window from a Codex Thread by title,
working directory, shell name, or a process-tree search. The selected passive
Codex hook transport has no supported terminal-window identifier, and those
values are neither unique nor stable enough to choose a window safely.

Automatic recovery is supported only for a **verified, visible Console Host
window**. The hook helper captures its current console `HWND` when
`SessionStart` runs and sends it with the Codex Thread ID. `GetConsoleWindow`
returns the calling process's console handle, but in a pseudoconsole it returns
a message-only, non-displayed window—so that value is a Console Host candidate,
not a generic terminal identifier. [GetConsoleWindow](https://learn.microsoft.com/en-us/windows/console/getconsolewindow)
Agent Aura persists the mapping only after validating that the handle is a top-level desktop-app
window, that `GetWindowThreadProcessId` returns the recorded owner PID, and
that the owner belongs to Agent Aura's Windows user session. `EnumWindows`
enumerates top-level desktop-app windows and `GetWindowThreadProcessId`
associates a handle with its creating process. [EnumWindows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows)
[GetWindowThreadProcessId](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid)
[ProcessIdToSessionId](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-processidtosessionid)

For a verified mapping, restore the window if needed and call
`SetForegroundWindow` as a best-effort action caused by the user's click. A
successful call activates the target; a zero result is a failure. Windows
deliberately restricts foreground changes and can deny even an otherwise
eligible process, so this must never be presented as guaranteed. [SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
If activation is denied but the validated window still exists, call
`FlashWindowEx`: it signals attention without changing the active window.
[FlashWindowEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-flashwindowex)

## Host capability boundary

| Host | MVP recovery capability | Reason |
| --- | --- | --- |
| Visible Console Host (`conhost.exe`) | Verified-window restore, best-effort foreground activation, otherwise flash | A hook process sharing the console can capture a real console window handle, which Agent Aura can validate before use. |
| Windows Terminal | Unsupported for exact recovery | Windows Terminal is a host for multiple shells, tabs, and panes. Its `wt -w` option accepts a window ID only to send a command to that window; the published command line does not provide a stable binding from an arbitrary child shell/Codex Thread to that ID, tab, or pane. [Windows Terminal overview](https://learn.microsoft.com/en-us/windows/terminal/) [Windows Terminal command line](https://learn.microsoft.com/en-us/windows/terminal/command-line-arguments) |
| VS Code integrated terminal and other pseudo-console hosts | Unsupported for exact recovery | No host-neutral Win32 API maps a pseudo-console child process or Codex Thread to a visible editor/terminal pane. Treat it as unbound rather than guessing an editor window. |
| Elevated or different-user terminal | Unsupported | Agent Aura runs without elevation or `uiAccess`; Windows foreground and UI-privilege protections mean it must not attempt to drive a higher-integrity or other-user window. [UIAccess and UIPI](https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-10/security/threat-protection/security-policy-settings/user-account-control-only-elevate-uiaccess-applications-that-are-installed-in-secure-locations) |

## Required hook metadata and expiry

For a Console Host candidate, the hook helper records: stable Codex Thread ID,
`HWND`, owner PID, Windows session ID, observed process creation time, and
capture time. It does not retain window title or terminal contents. Before
every recovery attempt, Agent Aura repeats handle, PID, user-session, and
visibility checks; a failed check deletes the mapping. This avoids reusing a
stale handle or recycled PID.

The app must not use `AllowSetForegroundWindow`, `AttachThreadInput`, synthetic
input, elevation, or UIAccess to bypass the user's focus choice. The foreground
permission is temporary and intentionally controlled by Windows.
[AllowSetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-allowsetforegroundwindow)

## Exact fallback interaction

When there is no verified Console Host mapping, or restore/activation fails:

1. Do **not** activate, flash, or select a guessed terminal/editor window.
2. Keep the Agent Message Item selected and acknowledge its Agent Aura alert.
3. Display `Terminal unavailable — this Thread is not bound to a recoverable Console Host window.`
4. Offer a user-initiated **Copy resume command** action containing
   `codex resume <thread-id>`; do not launch a shell or resume the Thread
   automatically.

The fallback is intentionally deterministic and safe for every terminal host.
It makes recovery possible through a terminal the user chooses, without
claiming that Agent Aura found the terminal that owns the running Thread.
