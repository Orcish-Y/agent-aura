## Agent skills

## Execution environment

The current working environment is WSL 2. You may temporarily use the Windows 11 host for verification, interaction, or tasks that cannot be completed within WSL. Host settings (including configuration items and the registry) may only be modified if they can be fully reverted afterwards. Do not install any software on the host.

### Issue tracker

Issues are tracked as local markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Six labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`, `done`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context — one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
