# Prototype the WPF observation shell

Type: prototype
Status: resolved
Blocked by:

## Question

Can a disposable .NET 10 + WPF prototype deliver the agreed Windows 11 shell reliably: tray-only presence, no taskbar button, transparent frameless always-on-top window without an enclosing border, Window Pin State behavior that hides the Header content and translucent surface without releasing its vertical footprint and restores it above unchanged Agent Message Item positions when the pointer enters the window, collapsed/expanded capacity, a one-to-four-line Agent Message Item hover transition that expands and collapses over 150 ms (including concurrent transitions between items), low-saturation state treatment, ellipsized overflow text without a Reduced motion setting, tray flashing, close-choice dialog, and persisted window/settings behavior? Record package size, idle memory, startup time, and any failures against the agreed feasibility and resource gates.

## Answer

The interaction model is accepted: a tray-first compact observation window, one-line Agent Message Items that expand on hover, and a Header that hides when the pinned window loses the pointer. Exact visual styling is deliberately deferred to implementation and does not block the MVP specification.

The Windows 11 prototype demonstrates the requested shell interactions, persistence, tray acknowledgement, and framework-dependent runtime-prerequisite path. Its framework-dependent package measured about 0.22 MB; the previously reported 170 MB package was the superseded self-contained publication. The measured WPF baseline was nevertheless about 152 MiB private memory and 212 MiB working set, above the agreed 100 MiB idle-working-set gate. WPF therefore satisfies the required shell capabilities; its resource measurements establish optimization and regression targets rather than requiring a parallel UI-technology comparison. The shell validation and measurement evidence are linked below.

- [Prototype validation record](../../src/AgentAura.Prototype/prototype-validation.md)
- [Resource usage analysis](../../docs/resource-usage-analysis.md)
- [Observation-shell screenshot](../../image.png)
