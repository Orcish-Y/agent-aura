# Verify Codex App Server title synchronization

Type: prototype
Status: resolved
Blocked by: 05, 13

## Question

Given the connection and subscription topology proven by the Codex-to-WPF bridge prototype, can the Agent Aura observer fetch the current optional Thread `name` by stable Thread ID and receive `thread/name/updated` promptly when a separately launched `codex --remote` TUI runs `/rename`, without reading private Codex session records? Establish title catch-up after observer or App Server reconnection, version or smoke-test gates, and failure behavior for the managed App Server MVP integration.

## Answer

Yes. A separately launched remote TUI's `/rename` immediately changed the corresponding Agent Message Item title. Restarting the observer then rediscovered the same Observed Codex Thread and restored the renamed Codex Thread Title through its `thread/loaded/list` plus `thread/resume` catch-up path.

The App Server replacement path was also exercised. Stopping the server leaves the current disposable observer in `connection disconnected`; it does not reconnect automatically. After a replacement App Server was ready, restarting the observer and reconnecting the remote TUI caused the Thread to reappear with the prior renamed title. The replacement server cannot list that Thread until the remote TUI reconnects.

The MVP integration smoke gate must use a current supported Codex CLI version and verify all of the following against a separately launched `codex --remote` TUI: initial title catch-up, prompt `/rename` propagation, observer restart catch-up, App Server replacement, fresh observer connection epoch, remote-TUI reconnection, and title restoration. On an observer or App Server loss, Agent Aura must report the disconnected integration state without inventing a title or Turn Outcome State; the production reconnect supervisor and non-blocking shutdown policy remain to be specified by [Define non-blocking managed App Server shutdown and recovery](16-define-nonblocking-managed-app-server-shutdown.md).
