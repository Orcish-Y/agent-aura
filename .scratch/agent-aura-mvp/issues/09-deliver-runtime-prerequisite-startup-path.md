# Deliver the runtime-prerequisite startup path

Type: task
Status: resolved
Blocked by:

## Goal

Deliver a small framework-dependent Agent Aura distribution that starts normally when the required .NET 10 Windows Desktop Runtime is installed, and provides a clear, user-controlled recovery path when it is not.

## Context

This ticket implements the approved distribution supplement recorded in the MVP map. It supersedes the older self-contained-package expectation where they conflict. The runtime is a prerequisite, not an application-owned dependency to download or install silently.

## Acceptance criteria

- A framework-dependent package contains Agent Aura application output without bundling the .NET Windows Desktop Runtime.
- On supported Windows 11 with the required runtime installed, Agent Aura opens normally.
- Without the required runtime, startup stops with a clear prerequisite explanation and a user-invoked link to Microsoft's official installer.
- Agent Aura neither downloads nor silently installs the runtime; restarting after the user installs it succeeds.
- A repeatable external startup test covers the runtime-present and runtime-missing paths.

## Verification

Run the external startup check on Windows 11 in two environments: one with the required runtime and one without it. Record the package size separately from the machine-wide runtime footprint.

## Non-goals

- Bundling a self-contained .NET runtime.
- Downloading, elevating, or silently installing a runtime.
- Changing Codex integration or sign-in startup behaviour.

## Answer

The prototype now publishes a framework-dependent `win-x64` package and includes a launcher that verifies the x64 .NET 10 Windows Desktop Runtime before starting Agent Aura. If the prerequisite is unavailable, the launcher stops before application launch, explains the requirement, and opens Microsoft's official runtime page only after the user opts in. The repeatable external checks cover runtime-present and runtime-missing Windows 11 environments, and record the package separately from the shared machine-wide runtime.
