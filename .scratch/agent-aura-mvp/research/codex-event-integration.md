# Codex event integration finding

> **Superseded:** This investigation's Hook-first recommendation is historical. The MVP now requires exact Turn Outcome State and therefore uses an Agent Aura-managed Codex App Server with a TUI connected through `codex --remote`. See [Use an Agent Aura-managed Codex App Server](../../../docs/adr/0001-use-agent-aura-managed-codex-app-server.md) and [Determine the reliable Codex event integration](../issues/01-determine-codex-event-integration.md).

## Conclusion

There is **no documented supported way to passively attach Agent Aura to an
already-running, ordinarily launched Codex CLI/TUI process** and subscribe to
its complete live event stream.  App-server is a rich-client protocol, but the
documented CLI/TUI connection is explicit: start `codex app-server` and start
the TUI with `codex --remote …`. That is a different launch path, not a
sidecar attachment to an arbitrary TUI. [App Server: connect the CLI
terminal UI](https://learn.chatgpt.com/docs/app-server#connect-the-cli-terminal-ui)

The supported passive mechanism is a **user-approved Codex lifecycle-hook
plugin**. Its hooks run in normal Codex CLI sessions once enabled/trusted, so
the user can still launch `codex` from any usual terminal. This is the best
MVP transport only if the product accepts a **best-effort terminal/liveness
model**. It cannot meet the PRD's exact `succeeded`/`failed`/`interrupted` and
reliable liveness requirements from supported signals alone.

If those states are non-negotiable, the MVP must relax the no-launch-change
constraint and make the supported remote-TUI/app-server topology its required
launch mode. App-server then provides typed thread/turn events, including
`turn/completed` with `completed`, `interrupted`, or `failed`, and status
changes including `waitingOnApproval`. [App Server: events and
status](https://learn.chatgpt.com/docs/app-server#events)

## Capability comparison

| Transport | Arbitrary normal TUI | Useful supported signals | Material gap |
| --- | --- | --- | --- |
| User/plugin lifecycle hooks | Yes, after user installs, enables, and trusts it | `session_id`, `cwd`, `turn_id`, submitted prompt, permission request, final assistant message | No documented terminal outcome or heartbeat; transcript format is explicitly unstable. |
| App-server owned by Agent Aura | No | Stable thread/turn identity, list/read metadata, `thread/status/changed`, approvals/input requests, and typed completed/failed/interrupted turn result | Only observes the server/remote TUI Agent Aura starts or to which TUI is explicitly connected; no documented attach to a different local TUI. |
| Local rollout/session files or state database | Could discover historical sessions | At most a recovery/reconciliation hint | Files/database are not published as an observation API; hooks explicitly warn that the transcript format may change. Do not use for authoritative live state. |

## Passive hook contract

Codex discovers hooks at user-level `~/.codex/hooks.json` or
`~/.codex/config.toml`, and enabled plugins can bundle the same hooks. User
hooks still load when a project is untrusted. Non-managed hooks must be
reviewed and trusted by the user; Codex hashes the definition and skips changed
hooks pending another review. [Hooks: discovery and
trust](https://learn.chatgpt.com/docs/hooks#where-codex-looks-for-hooks)

Prefer an Agent Aura plugin with a `hooks/hooks.json`, rather than editing an
existing user hook file. Codex provides `PLUGIN_ROOT` and `PLUGIN_DATA` to the
hook command; a plugin's hook is still subject to the same explicit trust
review. [Build plugins: bundled lifecycle
hooks](https://learn.chatgpt.com/docs/build-plugins#bundled-mcp-servers-and-lifecycle-hooks)

Recommended event mapping (all are documented hook inputs):

| Hook event | Agent Aura event/state | Supported fields |
| --- | --- | --- |
| `SessionStart` | upsert Thread; identity and project context | `session_id`, `cwd`, `transcript_path`, model; source is `startup`, `resume`, `clear`, or `compact` |
| `UserPromptSubmit` | `running`; new significant update | `session_id`, `turn_id`, `prompt` |
| `PermissionRequest` | `attention` / waiting for approval | `session_id`, `turn_id`, `tool_name`, tool input and optional description |
| `Stop` | terminal **observed** (not an outcome) | `session_id`, `turn_id`, `last_assistant_message` |
| `PostToolUse` (optional diagnostic) | tool result evidence | tool input/response; Bash fires even after a non-zero command exit |

The shared hook schema documents the stable session and turn identifiers and
cwd, while warning that `transcript_path` is only a convenience and its format
is not stable. [Hooks: common
fields](https://learn.chatgpt.com/docs/hooks#common-input-fields) `Stop`
contains the final assistant message but no documented final outcome enum.
[Hooks: Stop](https://learn.chatgpt.com/docs/hooks#stop) `PermissionRequest`
runs when Codex is about to request approval, but it is not an event for user
input requests; no hook signal found documents "waiting for input" separately.
[Hooks: PermissionRequest](https://learn.chatgpt.com/docs/hooks#permissionrequest)

Therefore map `Stop` to `unknown` (or a separate internal "terminal observed"
state), not `succeeded`. Infer no failure/interruption from missing hook events.
A process crash, killed terminal, blocked input, or an Agent Aura transport
failure must age into `unknown` after a product-defined timeout; that timeout
is inference, not a Codex-confirmed liveness signal.

## Why app-server does not close this gap passively

App-server's documented model is to start/resume a thread and keep reading
notifications on **that active transport**. It offers `thread/list` and
`thread/loaded/list`, but the docs do not describe joining another process's
ordinary TUI connection. Its full lifecycle stream has the desired data:
thread status can be `active` with `waitingOnApproval`, and `turn/completed`
has the three final states. [App Server: threads and
status](https://learn.chatgpt.com/docs/app-server#list-threads-with-pagination--filters)
[App Server: turn events](https://learn.chatgpt.com/docs/app-server#turn-events)

The app-server WebSocket transport is marked experimental/unsupported; expose
it only on loopback (or with TLS and configured authentication remotely).
Non-loopback listeners can be unauthenticated by default during rollout.
[App Server: transport
security](https://learn.chatgpt.com/docs/app-server#protocol)

## Consent, install, repair, and removal

Use a named Agent Aura plugin and make the user review the exact hook command
and trust it through `/hooks`. The hook should send a small JSON event to an
Agent Aura loopback endpoint or named pipe; it must not read or exfiltrate the
transcript. Keep user prompts and tool arguments out of the default telemetry
payload.

On the observed `codex-cli 0.144.1`, the first-party CLI exposes the reversible
commands `codex plugin marketplace add`, `codex plugin add`, `codex plugin
list`, `codex plugin remove`, and `codex plugin marketplace remove` (verified
locally with `--help`). The documented marketplace commands also say to use
them rather than editing `config.toml` by hand. [Build plugins: marketplace
CLI](https://learn.chatgpt.com/docs/build-plugins#add-a-marketplace-from-the-cli)

Suggested consented flow:

1. Preflight `codex --version` and `codex plugin --help`; require a version
   whose hook schema passes a small real-Codex smoke test.
2. Show the plugin source, hook executable path, localhost/pipe destination,
   fields sent, and the exact marketplace/add commands; execute only after
   consent.
3. Direct the user to trust the displayed hash in `/hooks`; install is not
   healthy until a hook smoke event arrives from a normal separately launched
   `codex` TUI.
4. Repair by inspecting `codex plugin list`, marketplace list, plugin files,
   and hook trust/health; rerun the owned add/enable/trust step only. Never
   rewrite unrelated config or hooks.
5. Remove with `codex plugin remove agent-aura@agent-aura`, then remove the
   **Agent Aura-owned** marketplace only if no other installed plugin needs it;
   delete only the Agent Aura plugin data. Confirm a normal TUI produces no
   Agent Aura hook event.

The app-server `plugin/install`/`plugin/uninstall` API is documented as "under
development" and explicitly not for production clients, so Agent Aura should
not rely on it for this flow. [App Server API
overview](https://learn.chatgpt.com/docs/app-server#api-overview)

## Version and test gate

No official minimum version for this particular composition was found. Pin the
minimum only after fixture/smoke tests against the target Codex release, and
fail preflight if hooks are disabled by configuration or organisation policy.
The docs state hooks can be disabled with `[features].hooks = false` and
enterprise requirements can force managed-only hooks. [Hooks: enablement and
managed policy](https://learn.chatgpt.com/docs/hooks#turn-hooks-off)

The acceptance test must launch an **independent** normal TUI after install and
verify `SessionStart`, `UserPromptSubmit`, `PermissionRequest`, and `Stop`
delivery plus identity continuity on `codex resume`. Separately verify that a
killed TUI is displayed as `unknown`, not failed/interrupted. This documents
the supported boundary instead of relying on private rollout parsing.
