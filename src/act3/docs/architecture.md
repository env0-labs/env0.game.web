# env0.terminal - Architecture (Baseline)

This document defines the target architecture for the new baseline. It is the "true north" for refactors and new work.

---

## Goals

- Keep the engine headless and deterministic.
- Maintain a clean separation between core logic and any host/UI.
- Prefer small, testable components behind a single entrypoint.
- Make configuration source pluggable (JSON is default).

---

## Public Surface

We are free to rename and reshape the public API. The intent is one entrypoint with minimal surface area:

Suggested shape:
- `TerminalEngine.Initialize(IConfigProvider configProvider)` (or default JSON provider)
- `TerminalEngine.Step(string input) -> TerminalRenderState`
- `TerminalEngine.Reset()`
- `TerminalEngine.SetDebugMode(bool enabled)`

This class is the only public entrypoint. All other types are internal.

---

## Core Layers

1) Engine Core
- `TerminalEngine` (public facade and coordinator)
- `TerminalContext` (internal: session state + flow state + current phase)
- Flow handlers (internal):
  - `BootFlow`
  - `LocalLoginFlow`
  - `SshLoginFlow`
  - `ShellFlow`

2) Services (internal)
- `FilesystemManager`
- `NetworkManager`
- `LoginHandler`
- `SSHHandler`
- `JsonLoader` or `IConfigProvider` implementation

3) Commands (internal)
- `CommandHandler`
- `CommandParser`
- `ICommand` implementations (`ls`, `cd`, `cat`, etc.)

---

## Data Contracts

- `TerminalRenderState` is the single output contract for all host consumers.
- `SessionState` remains internal and is not exposed.
- Output is provided via:
  - `OutputLines` (typed lines)
  - `Output` (legacy string, optional)

---

## Configuration

Default configuration is JSON-backed:
- Boot config
- User config
- Devices
- Filesystems

Use an interface for injection in tests and alternate hosts:
- `IConfigProvider.LoadAll()`
- `IConfigProvider.BootConfig/UserConfig/Devices/Filesystems`

---

## Non-Goals

- UI/rendering or terminal emulation visuals
- Persistence or save/load
- Shell features beyond the defined command set
- Multi-user or permissions model

---

## Migration Notes

- The current `TerminalEngineAPI` is a monolith and will be split into flows and context state.
- Tests should move toward validating flows and state transitions rather than string matching where possible.
- Historical contracts in `docs/env0.terminal_REFERENCE.md` and `docs/env0.terminal_Contracts.md` are retained for context only until updated.
