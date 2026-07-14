# Define non-blocking managed App Server shutdown and recovery

Type: grilling
Status: resolved
Blocked by: 05

## Question

When Agent Aura owns the Codex App Server and an observed remote TUI remains connected, what shutdown and recovery contract prevents Agent Aura's WPF UI, tray menu, and launching terminal from freezing? Define the asynchronous cleanup boundary, timeouts and forced-termination behavior, user-visible remote-TUI disconnection/reconnection path, the fresh observer-WebSocket epoch after App Server replacement, and the smoke-test gate that prevents a synchronous UI-thread wait on WebSocket or child-process shutdown.

## Observed failure

During the disposable bridge prototype, selecting Exit while an observed remote TUI remained connected froze the Aura window, tray-menu actions, and the launching terminal's Ctrl+C path. The current prototype synchronously waits on `BridgePrototypeController.DisposeAsync()` from the WPF UI thread; its observer cleanup awaits WebSocket shutdown and can require UI-dispatcher progress. This must not become production behavior.

## Answer

Agent Aura uses a detached **App Server Guardian** to retain ownership of its managed Codex App Server after the Aura front end exits while one or more remote TUIs remain connected. Aura exit therefore closes the WPF window and tray interaction promptly but does not terminate the user's `codex --remote` process or its App Server. While the Aura front end remains running, zero remote-TUI connections never triggers App Server shutdown.

The Guardian closes the App Server only when both conditions hold: the Aura front end has exited and the remote-TUI connection count is zero. It requests graceful shutdown immediately, waits at most five seconds, then force-terminates the App Server and exits. A zero-connection shutdown cannot newly disrupt a remote TUI; the existing bridge validation shows that terminating an App Server while a remote TUI is still connected causes that TUI to lose its transport and exit, which is precisely why the Guardian must not stop it while a remote connection exists.

On Aura restart, the new front end reconnects to the existing Guardian and App Server rather than replacing either. Its observer creates a strictly newer WebSocket epoch, performs `initialize`/`initialized`, reads `thread/loaded/list`, resumes available Threads, and rejects all late events from older epochs. A live App Server failure keeps Aura responsive: Aura starts a replacement service for new connections, marks affected items disconnected without inventing a Turn Outcome State, and offers the proven connected-resume command. Existing remote TUIs must reconnect explicitly; automatic TUI reconnection is not a supported promise.

All UI-facing exit, tray, and menu paths only initiate asynchronous handoff or cleanup and return without awaiting WebSocket closure, observer disposal, or child-process termination. The smoke-test gate starts a remote TUI, invokes Aura exit, and asserts that the UI/tray command returns within 250 ms, no synchronous UI-thread wait occurs, the remote TUI remains alive, and a restarted Aura can reattach and rebuild observation through a fresh epoch. A separate zero-connection test verifies graceful shutdown and the five-second forced-termination fallback.
