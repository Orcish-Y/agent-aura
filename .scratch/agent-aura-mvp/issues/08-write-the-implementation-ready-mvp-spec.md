# Write the implementation-ready MVP specification

Type: task
Status: resolved
Blocked by: 03, 04, 06, 07

## Question

Synthesize the resolved product, integration, state-machine, Windows interaction, runtime, installation, failure-handling, accessibility, and distribution decisions into one implementation-ready MVP specification with explicit scope, user flows, data model, interfaces, acceptance criteria, risks, and implementation-ticket boundaries. What ambiguities, if any, remain before delivery can begin?

## Answer

The implementation-ready MVP specification is published in
[PRD.md](../PRD.md). It consolidates the accepted Windows and WSL runtime
boundaries, authoritative App Server state model, Guardian lifecycle,
observation-window and tray contract, data/interface seams, clean-machine
distribution path, verification gates, risks, and seven implementation-ticket
boundaries.

No product or architecture decision remains unresolved before implementation.
The remaining version-sensitive Codex behavior is a release verification gate
(the current/minimum CLI smoke matrix), not a design ambiguity. Terminal-window
activation is explicitly out of scope; the user-initiated connected-resume copy
command is the complete MVP recovery path.
