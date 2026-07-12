# Map Codex events to Agent Item state

Type: grilling
Status: open
Blocked by: 01

## Question

Given the verified Codex event source, what deterministic state machine maps real events and disconnect conditions into `running`, `attention`, `succeeded`, `failed`, `interrupted`, and `unknown`; defines Significant Updates and Attention Pin Span resets; prevents stale or out-of-order updates; and keeps exactly one Agent Item per resumable Codex Thread?

