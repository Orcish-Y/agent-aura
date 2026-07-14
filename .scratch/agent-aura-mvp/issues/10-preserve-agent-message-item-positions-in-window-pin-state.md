# Preserve Agent Message Item positions in Window Pin State

Type: task
Status: resolved
Blocked by:

## Goal

Keep the pinned observation window compact without making Agent Message Items jump when the Header hides or reappears.

## Context

Window Pin State is the persisted setting that keeps the observation window above other windows. It is distinct from Attention Pin Span. The observation window has no enclosing border. In the pinned state, hiding the Header's content and translucent surface must preserve its vertical footprint so the list stays stationary.

## Acceptance criteria

- When Window Pin State is enabled and the pointer leaves the observation window, the Header's content and translucent surface become hidden while its vertical footprint remains reserved.
- Entering any portion of the pinned observation window reveals the Header and its translucent surface in that reserved space above stationary Agent Message Items.
- Leaving the pinned window hides the Header and its translucent surface again without changing any Agent Message Item layout position or drawing an enclosing window frame.
- When Window Pin State is disabled, the Header and its translucent surface remain visible and normal window stacking is restored; the observation window remains frameless.
- A real-window interaction test verifies pointer transitions, Header-surface visibility, absence of a window frame, and layout stability without asserting private layout fields.

## Verification

Drive pointer enter and leave events against the real WPF window. Observe Header-surface visibility, absence of an enclosing frame, Topmost behaviour, and unchanged screen/layout positions for representative Agent Message Items.

## Non-goals

- Changing list priority or Attention Pin Span behaviour.
- Defining new visual styling beyond the existing observation-window model.

## Answer

The WPF observation window retains the Header's layout footprint while its content and translucent surface are hidden in Window Pin State. Agent Message Items therefore remain stationary while the pointer leaves and re-enters the pinned window; unpinning restores normal stacking and a visible Header. The real-window UI verification covers pointer transitions, Header-surface visibility, frameless rendering, Topmost behaviour, and item layout stability.
