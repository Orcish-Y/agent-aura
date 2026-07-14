# Define the Agent Message Item title strategy

Type: grilling
Status: resolved
Blocked by: 01

## Question

The selected managed Codex App Server exposes an optional user-facing Thread `name`, but it may be empty, unavailable during reconnection, or not yet observed. Before a user creates a Thread Alias, what text must an Agent Message Item display, when may it change, and how should that rule balance recognisability, privacy, and a stable list while preserving aliases by stable Thread ID?

## Answer

Use this display-name precedence for every Agent Message Item:

1. The persisted Thread Alias for the stable Codex Thread ID.
2. The latest non-empty Codex Thread Title supplied by the supported Codex App Server interface.
3. A Fallback Thread Title formatted as `<working-directory leaf> · <first-observed local time>`.

The managed Codex App Server is the required MVP source for both lifecycle events and Codex Thread Title. Agent Aura must not receive, persist, or summarise the raw submitted prompt for title generation. It may display the user-facing Codex Thread Title that Codex supplies.

When Codex App Server reports a title change, store it against the stable Codex Thread ID. If no Thread Alias exists, update the Agent Message Item immediately without moving it in the list and without treating the rename as a Significant Update. If a Thread Alias exists, keep displaying the alias while retaining the latest Codex Thread Title; removing the alias reveals that title. Agent Aura does not write the title back to Codex.

A null, empty, unavailable, or not-yet-observed Codex Thread Title uses the Fallback Thread Title. Compute that fallback once when the Agent Message Item is created and keep it unchanged for that item's lifetime. Dismiss, Clear, or an Agent Aura restart discards it; a later recreated item receives a new first-observed time. Duplicate fallback labels are allowed and never trigger renumbering because stable Codex Thread ID, not display text, is item identity.

The App Server protocol exposes an optional Thread `name` and a `thread/name/updated` notification, but a separate prototype must verify that the Agent Aura observer connection can obtain and promptly observe `/rename` changes made in a TUI connected through `codex --remote`. Failure of that validation blocks title synchronization in the chosen managed integration; it does not permit reading private Codex session records or falling back to raw prompt content.
