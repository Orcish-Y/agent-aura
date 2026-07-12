# Agent Aura

Agent Aura is a lightweight observation surface for the state of local Codex CLI instances, helping users notice when an instance needs attention or finishes while they work elsewhere.

## Language

**Codex Thread**:
A resumable Codex conversation identified by a stable Codex-generated thread ID. It retains its identity across CLI process restarts and contains any number of conversation turns.
_Avoid_: Agent task, CLI process, turn, session

**Agent Item**:
The single observation-window entry representing a Codex Thread and its latest known state.
_Avoid_: Notification, message, task item

**Thread Alias**:
A user-defined display name persistently associated with one Codex Thread by its stable thread ID.
_Avoid_: CLI alias, process name

**Attention State**:
A state indicating that a Codex Thread is blocked waiting for user action, such as approval or input.
_Avoid_: Unread, pending notification

**Significant Update**:
A user-meaningful Thread transition: entering the Attention State, reaching a terminal state, or starting a new turn. Streaming text, tool activity, and progress refreshes are not Significant Updates.
_Avoid_: Message, event, notification

**Attention Pin Span**:
The configurable number of Significant Updates from other Threads for which an Agent Item in the Attention State remains temporarily pinned above the list.
_Avoid_: Attention timeout, unread count
