# Preserve Agent Message Item positions in Window Pin State

Type: task
Status: ready-for-agent
Blocked by:

## Goal

Keep the pinned observation window compact without making Agent Message Items jump when the Header hides or reappears.

## Context

Window Pin State is the persisted setting that keeps the observation window above other windows. It is distinct from Attention Pin Span. In the pinned state, hiding the Header must preserve its vertical footprint so the list stays stationary.

## Acceptance criteria

- When Window Pin State is enabled and the pointer leaves the observation window, the Header becomes hidden while its vertical footprint remains reserved.
- Entering any portion of the pinned observation window reveals the Header in that reserved space above stationary Agent Message Items.
- Leaving the pinned window hides the Header again without changing any Agent Message Item layout position.
- When Window Pin State is disabled, the Header remains visible and normal window stacking is restored.
- A real-window interaction test verifies pointer transitions and layout stability without asserting private layout fields.

## Verification

Drive pointer enter and leave events against the real WPF window. Observe the Header visibility, Topmost behaviour, and unchanged screen/layout positions for representative Agent Message Items.

## Non-goals

- Changing list priority or Attention Pin Span behaviour.
- Defining new visual styling beyond the existing observation-window model.
