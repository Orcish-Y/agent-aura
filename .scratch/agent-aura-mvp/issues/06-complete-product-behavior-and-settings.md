# Complete the product behavior and settings contract

Type: grilling
Status: resolved
Blocked by:

## Question

What remaining interaction and settings rules must the MVP specification make explicit, including deletion and reappearance, clear behavior, sorting after temporary Attention pinning, tray flashing acknowledgement, close-choice persistence, startup behavior and runtime-prerequisite presentation, capacity validation, alias editing, error presentation, integration health/repair, accessibility, empty states, and reset-to-default behavior?

## Resolved constraints

- Reduced motion is not an MVP setting. Overflowing Agent Item title and detail text is always ellipsized, while the detail fade and tray flashing remain part of the default interaction.
- The observation window has no enclosing border. Window Pin State is distinct from Attention Pin Span. While the window is pinned, the Header content and translucent surface hide outside the observation window but retain their vertical footprint; entering any part of the window restores the Header above stationary Agent Message Item positions.
- Agent Message Items transition between their one-line and four-line forms over 150 ms. Direct movement between items runs the outgoing collapse and incoming expansion concurrently.

## Answer

### List lifecycle, priority, and capacity

- **Dismiss Agent Message Item** removes only the current runtime entry. It does not delete the Codex Thread or its Thread Alias; a later valid event for that Thread creates a new item.
- **Clear Agent Message Items** removes every current runtime entry, regardless of state. It also preserves Codex Threads and Thread Aliases; items return only when their Threads later produce valid events.
- An Agent Message Item entering Attention State is temporarily pinned. When its Attention Pin Span expires, it remains where it is rather than returning to its original recency position. Later Significant Updates from other Threads insert above it and progressively move it down.
- Collapsed capacity defaults to 5 and accepts 1–10. Expanded capacity defaults to 15 and accepts 5–30; expanded capacity cannot be lower than collapsed capacity. Beyond expanded capacity, the list scrolls internally without a hidden-item count.

### Tray and window lifecycle

- While the observation window is hidden, entering Attention State or reaching any terminal Turn Outcome State starts one aggregate Tray Alert. Further qualifying updates do not stack alerts.
- A left click on the tray icon restores and focuses the observation window, then acknowledges and stops the Tray Alert. Automatic window display, pointer movement, and later updates do not acknowledge it.
- Minimise always hides to the tray. The default close action asks the user to **Hide to tray**, **Exit**, or **Cancel**, with no exit action preselected. The user may remember Hide or Exit; Settings can restore the default “ask every time” behavior.
- Sign-in startup is off by default. When enabled, Agent Aura starts only in the tray. With Silent Startup disabled, a subsequent Significant Update automatically shows the observation window; with it enabled, no such automatic show occurs. Manual launch always shows the observation window.

### Appearance, settings, and accessibility

- Settings provide custom background color, text color, an individual color for each Agent Message Item state, background opacity, and whole-interface scale. State colors cover Observed State, running, Attention State, completed, failed, interrupted, and connection disconnected. Each color can be restored to its default.
- Background opacity ranges from 30% to 100%, with an 88% default; it does not reduce text, state-color, or control contrast.
- Interface scale ranges from 80% to 150% and scales the complete interface; there is no separate font-size control. The layout must remain usable without clipping at every supported scale.
- Agent Aura follows Windows High Contrast. In that mode, Windows system colors override the user's background, text, and state-color preferences. The MVP has no additional keyboard, screen-reader, or reduced-motion commitment.
- Settings retain a **Restore defaults** action behind confirmation. It resets appearance, capacities, Attention Pin Span, startup and close behavior, window geometry, and Window Pin State; it leaves Thread Aliases, Codex integration, and the current runtime Agent Message Item list unchanged.

### Alias, errors, integration, and empty states

- The hover-expanded item exposes alias editing. Enter saves, Escape cancels, and clearing the value (or using Remove alias) returns the item to its separately specified default-title strategy. Thread Aliases persist by stable Thread ID and take precedence over the App Server-supplied Codex Thread Title.
- Thread-specific errors appear in the Agent Message Item with concise corrective text. Application-level failures appear as non-blocking notices in the observation window or Settings. Both provide Retry and Copy diagnostics; the MVP avoids modal error dialogs and silent failures.
- Settings report Codex integration as **Connected**, **Checking**, **Needs repair**, or **Unavailable**, with Check, Repair, and Remove actions. Every state-changing action previews the changes and requires confirmation. Repair and removal only touch Agent Aura-owned App Server processes, endpoint metadata, and launcher artifacts.
- An empty, healthy list says that Agent Aura is waiting for Codex Thread activity and keeps Settings and integration-health access available. An unhealthy integration instead states the cause and offers Check or Repair. Clearing the list returns to the same waiting state and shows no history or hidden-item count.
