# Complete the product behavior and settings contract

Type: grilling
Status: open
Blocked by:

## Question

What remaining interaction and settings rules must the MVP specification make explicit, including deletion and reappearance, clear behavior, sorting after temporary Attention pinning, tray flashing acknowledgement, close-choice persistence, startup behavior and runtime-prerequisite presentation, capacity validation, alias editing, error presentation, integration health/repair, accessibility, empty states, and reset-to-default behavior?

## Resolved constraints

- Reduced motion is not an MVP setting. Overflowing Agent Item title and detail text is always ellipsized, while the detail fade and tray flashing remain part of the default interaction.
- The observation window has no enclosing border. Window Pin State is distinct from Attention Pin Span. While the window is pinned, the Header content and translucent surface hide outside the observation window but retain their vertical footprint; entering any part of the window restores the Header above stationary Agent Message Item positions.
- Agent Message Items transition between their one-line and four-line forms over 150 ms. Direct movement between items runs the outgoing collapse and incoming expansion concurrently.
