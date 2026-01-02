# env0.game

A modular, text-based systems experience built around maintenance,
procedural correctness, and delayed interpretation.

This repository contains one system implemented as multiple contexts,
each exposing different permissions, affordances, and abstractions over
the same underlying workflow.

This is not a traditional game project. There is no engine dependency,
no renderer, and no attempt at spectacle. The experience is
intentionally minimal, procedural, and slow.

------------------------------------------------------------------------

## High-Level Structure

The project is a single solution with multiple modules:

    env0.game
    +-- src
    |  +-- core
    |  |  +-- Env0.Core          # Shared contracts only (no behavior)
    |  +-- maintenance           # Maintenance (process/status loop)
    |  +-- records               # Records / bureaucratic interpretation
    |  +-- terminal              # Terminal / config inspection & patching
    |  +-- context               # Placeholder
    |  +-- runner                # Top-level runner / context launcher
    +-- tests
    +-- env0.game.sln

### Core

`src/core/Env0.Core` contains only shared contracts:

- Output models
- Context interface (`IContextModule`)
- Session state

There is no narrative logic and no IO in Core.

### Contexts

Each context folder contains a self-contained module that:

- Implements `IContextModule`
- Owns its own behavior and state transitions
- Does not directly reference other contexts

Current status:

- Maintenance: module (`MaintenanceModule`) exists and is wired to the runner; process includes a batch confirmation gate
- Records: module (`RecordsModule`) exists and is wired to the runner; terminals route into the CLI and return to the originating room
- Terminal: module (`TerminalModule`) exists; includes a standalone playground
  app and internal docs
- Context: placeholder folder exists

### Runner

The runner is a deliberately dumb console application that:

- Launches Maintenance by default
- Passes raw input to the active context
- Prints returned output lines
- Routes to the next context when the current one signals completion

The runner enforces no game rules.

------------------------------------------------------------------------

## Architectural Principles

- Core owns contracts, not behavior
- Contexts own behavior, not wiring
- Runner owns wiring, not rules

No context should:

- Know which other contexts exist
- Control process lifetime directly
- Perform direct console IO

No runner should:

- Enforce context-specific rules
- Interpret command meaning
- Contain narrative logic

------------------------------------------------------------------------

## Running the Project

### Build everything

From the repo root:

```bash
dotnet build
```

### Run the top-level runner

```bash
dotnet run --project src/runner
```

The runner starts in Maintenance by default.

### Run context-specific entry points

Records can still be run directly:

```bash
dotnet run --project src/records
```

Terminal has a playground app:

```bash
dotnet run --project src/terminal/env0.terminal.playground
```

------------------------------------------------------------------------

## Context Lifecycle

Contexts signal completion via shared session state.

When a context sets `SessionState.IsComplete = true`, the runner exits
the input loop and routes to the next context.

This avoids:

- Control flow hidden in output text
- Special runner commands like `exit`
- Exceptions used as flow control

------------------------------------------------------------------------

## Records Terminals

Terminal access in Records is mapped in `Config/Jsons/Devices.json`.
Each device includes a `recordsRoomId` and `filesystem`, and Records uses
that mapping to launch the terminal with the correct filesystem. On
`quit`, the player returns to the originating Records room.

------------------------------------------------------------------------

## Development Practices

- Large refactors should be done by porting, not stripping, where
  possible.
- Shared utilities should only be moved into Core once multiple contexts
  genuinely require them.

If something feels like it "belongs everywhere", question it first.

------------------------------------------------------------------------

## Intent (Non-Spoiler)

This project explores what happens when:

- Responsibility is procedural
- Ethics are abstracted
- Correctness replaces understanding

The system does not lie.
The system does not warn.
The system does not care.

Everything works.

------------------------------------------------------------------------

## Status

Active development.

- Maintenance: module wired to runner (process/status + batch confirmation gate)
- Records: module wired to runner; standalone console entry present
- Terminal: module wired to runner; tests and playground present
- Context: placeholder folder

