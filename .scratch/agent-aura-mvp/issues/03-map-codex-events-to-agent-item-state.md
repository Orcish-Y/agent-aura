# Map Codex events to Agent Item state

Type: grilling
Status: resolved
Blocked by: 01

## Question

Given the authoritative managed Codex App Server event source, what deterministic state machine maps real events and disconnect conditions into `running`, `attention`, `completed`, `failed`, `interrupted`, Observed State, and `connection disconnected`; defines Significant Updates and Attention Pin Span resets; prevents stale or out-of-order updates; and keeps exactly one Agent Message Item per resumable Codex Thread?

## Resolved constraints

- App Server `turn/started` unconditionally transitions the Codex Thread to `running`, clearing any prior Attention State or Turn Outcome State, and is a Significant Update.
- App Server `turn/completed` supplies the exact `completed`, `failed`, or `interrupted` Turn Outcome State. It is a Significant Update exactly once per turn; duplicate terminal deliveries do not create another update.
- Explicit approval, structured user-input, MCP elicitation, and permission requests enter Attention State. Agent Aura must not infer Attention State from assistant-message wording or elapsed time.
- App Server is the sole MVP state authority. Lifecycle Hooks and private Codex session records are not fallback state sources.
- A lost App Server connection is presented as `connection disconnected`, retaining the last known Turn Outcome State without claiming that an active turn ended.
- Within one connection epoch, process App Server notifications in transport order and correlate them by stable Thread ID and Turn ID. After reconnect, establish a new epoch from an authoritative App Server snapshot/subscription; late work from an older epoch cannot overwrite it.
- Codex automatic approvals do not enter Attention State and are not Significant Updates. They do not consume another Thread's Attention Pin Span; only a subsequent qualifying transition does.
- The first authoritative observation of a Codex Thread creates the one runtime Agent Message Item keyed by stable Thread ID and applies any persisted Thread Alias. A Thread observed before any active turn signal starts in neutral Observed State and does not restore Agent Aura-owned runtime history.
- In App Server mode, `serverRequest/resolved` clears Attention State and restores `running` without a Significant Update. If `turn/completed` has also arrived, its terminal Turn Outcome State wins and `running` is not shown.
- On an Agent Aura restart, clear the Agent Message Item list and all runtime state/history. Persist only Thread Aliases by stable Thread ID, so a later observed session receives its existing alias.
- After restart or reconnect, rebuild live Agent Message Items only from the App Server's current snapshot and subsequent subscribed events, including Threads that began before Agent Aura. The Codex-to-WPF prototype must verify the exact discovery and subscription mechanism.

## Answer

Agent Aura has one runtime Agent Message Item per stable Codex Thread ID. The managed App Server supplies exact Turn Outcome State, so the MVP has neither an `unknown` nor an `ended with outcome unconfirmed` state. A lost transport maps to `connection disconnected` without claiming an active turn ended. `completed` is the canonical successful outcome, matching the App Server protocol term.

| Incoming event or condition | Agent Message Item transition | Significant Update |
| --- | --- | --- |
| First authoritative Thread observation | Create the item, apply any persisted Thread Alias, and use Observed State when there is no active-turn signal. | No |
| App Server `turn/started` | `running`; clears prior Attention State and Turn Outcome State. | Yes |
| App Server approval, structured user-input, MCP elicitation, or permission request | `attention`. Never infer this from assistant wording or elapsed time. | Yes, only on entry |
| App Server `serverRequest/resolved` | `running`; clear Attention State. A concurrent App Server terminal event wins. | No |
| App Server `turn/completed` | Map exact protocol result to `completed`, `failed`, or `interrupted`. | Yes |
| App Server transport loss | `connection disconnected`, retaining the last known outcome. | No |

Codex automatic approvals neither enter Attention State nor count as Significant Updates, so they do not consume Attention Pin Span. When a Thread enters Attention State, reset its pin counter to the configured span and temporarily place it above the ordinary most-recently-updated list. Each Significant Update from a different Thread decrements it; the Thread's own updates do not. Re-entry resets the counter; duplicates never decrement it twice. The placement after the counter expires is specified by [Complete the product behavior and settings contract](06-complete-product-behavior-and-settings.md).

App Server is the only state authority. Within a connection epoch, correlate events by stable Thread ID and Turn ID, process them in transport order, and deduplicate terminal results by Turn ID. Reconnection creates a new epoch from the server's authoritative current snapshot and subscriptions; events from an older connection cannot revive or overwrite newer state. The bridge prototype must establish the exact supported snapshot and subscription calls rather than assuming cross-client broadcasts.

On Agent Aura restart, clear every runtime Agent Message Item and its runtime history. Persist only Thread Aliases by stable Thread ID. Recreate items from the App Server's current authoritative view and later subscribed events, including an Observed Codex Thread that began before Agent Aura; Agent Aura never replays its own persisted runtime history.

The compact Agent Message Item represents state with a small leading color square rather than an icon or status text. Tooltip and accessible naming expose the full state.

This decision now depends on the Agent Aura-managed App Server architecture recorded in [Determine the reliable Codex event integration](01-determine-codex-event-integration.md) and [Use an Agent Aura-managed Codex App Server](../../../docs/adr/0001-use-agent-aura-managed-codex-app-server.md).
