---
status: accepted
---

# Use an Agent Aura-managed Codex App Server

The MVP observes only Codex Threads whose terminal UI connects to an Agent Aura-managed local Codex App Server through `codex --remote`. Exact `completed`, `failed`, and `interrupted` turn results are core product data, and lifecycle Hooks for an ordinary Codex TUI cannot provide those typed outcomes. The App Server is therefore the authoritative source for lifecycle, Attention State, Turn Outcome State, and Codex Thread Title.

## Considered options

- Observe ordinary `codex` sessions through lifecycle Hooks. This preserves the unmodified command but can report only that a turn stopped, not its exact result.
- Manage a local App Server and require the observed TUI to use `codex --remote`. This changes the launch command but exposes the supported Thread, Turn, approval, title, and streamed-event protocol needed by Agent Aura.

## Consequences

- Users may launch Codex from their ordinary terminal, but an MVP-observed session must use the Agent Aura-provided remote endpoint or launcher; ordinary `codex` sessions are outside MVP monitoring.
- Agent Aura owns App Server startup, health, reconnection, and shutdown behavior. It does not use `codex remote-control`, which serves a different managed-remote workflow.
- The Codex-to-WPF prototype must prove that Agent Aura can discover and subscribe to Threads started by a separate remote TUI client, receive exact `turn/completed` results, and recover subscriptions after either side reconnects.
- Lifecycle Hooks are not an MVP state source. They may be reconsidered later only as an explicitly degraded compatibility mode.

The supported remote-TUI topology and typed turn events are documented in the [Codex App Server guide](https://learn.chatgpt.com/docs/app-server#connect-the-cli-terminal-ui) and its [turn events](https://learn.chatgpt.com/docs/app-server#turn-events).
