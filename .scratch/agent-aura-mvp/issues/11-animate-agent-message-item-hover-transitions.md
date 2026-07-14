# Animate Agent Message Item hover transitions

Type: task
Status: resolved
Blocked by:

## Goal

Make inspecting a Codex Thread fluid and predictable while moving between compact and detailed Agent Message Items.

## Context

An Agent Message Item has the established one-line compact form and four-line detailed form. Its expansion and collapse duration is 150 ms. Direct movement from one item to another must hand off without serialising the two transitions.

## Acceptance criteria

- Moving from outside the observation window onto an Agent Message Item expands its one-line form to the established four-line form over 150 ms.
- Leaving an Agent Message Item collapses it to one line over 150 ms.
- Moving directly from one Agent Message Item to another starts the prior item's collapse and the next item's expansion concurrently, each lasting 150 ms.
- Existing direct-hover overflow text behaviour remains available and does not cause rows to collapse, lose their target, or change the transition duration.
- A real-window interaction test verifies observable start, duration, end state, and concurrent handoff without asserting storyboard internals.

## Verification

Exercise enter, leave, and direct item-to-item pointer movement on a real WPF window. Capture timing at the public UI boundary with tolerance appropriate for the test environment.

## Non-goals

- A Reduced motion preference; it is not part of the MVP.
- Replacing the established text-overflow behaviour.

## Answer

Agent Message Items now animate between their compact one-line and detailed four-line forms over 150 ms. Leaving one item while entering another starts the outgoing collapse and incoming expansion concurrently. The real-window UI verification exercises enter, leave, direct handoff timing, and direct-hover overflow text without collapsing or losing the item target.
