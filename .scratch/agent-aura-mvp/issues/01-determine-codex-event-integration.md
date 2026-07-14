# Determine the reliable Codex event integration

Type: research
Status: resolved
Blocked by:

## Question

What supported Codex CLI integration can provide stable Thread identity, working directory, title, lifecycle, approval/input waiting, exact completion/failure/interruption, and liveness while letting users launch an observed TUI from their ordinary terminal? Establish the required launch boundary, distinguish App Server remote TUI mode from `codex remote-control`, and define version, security, ownership, repair, and removal constraints.

## Answer

Use an Agent Aura-managed local Codex App Server as the MVP's sole authoritative event source. Agent Aura starts and health-checks `codex app-server --listen <local-endpoint>`; an observed terminal UI connects through the Agent Aura-provided `codex --remote <local-endpoint>` launch path. Users still launch Codex from an ordinary terminal, but ordinary `codex` sessions that do not use the managed endpoint are outside MVP monitoring.

The App Server protocol supplies stable Thread identity, working directory, Attention State requests, Codex Thread Title, streamed lifecycle events, and `turn/completed` with the exact `completed`, `failed`, or `interrupted` result. These exact results are core product data. Lifecycle Hooks are rejected as the MVP state source because their `Stop` event proves only that a turn stopped; Hook-only monitoring would require an `ended with outcome unconfirmed` product state that the selected MVP does not need.

Agent Aura must not use `codex remote-control` for this local protocol integration; it is distinct from a TUI connected with `codex --remote`. The remaining prototype must verify the supported local transport, whether an Agent Aura observer connection can discover and subscribe to Threads started by a separate remote TUI client, reconnection and catch-up behavior, version smoke testing, and safe App Server process ownership. Until that prototype passes, the protocol capability is documented but the multi-client observation topology remains unproven.

This answer supersedes the earlier Hook-first conclusion retained in the historical [Codex event integration finding](../research/codex-event-integration.md). The architectural decision is recorded in [Use an Agent Aura-managed Codex App Server](../../../docs/adr/0001-use-agent-aura-managed-codex-app-server.md), with official protocol evidence in the [Codex App Server guide](https://learn.chatgpt.com/docs/app-server).
