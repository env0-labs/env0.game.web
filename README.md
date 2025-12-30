# env0.game

A modular, text-based systems experience built around maintenance,
procedural correctness, and delayed interpretation.

This repository contains one system implemented as multiple epistemic
modes ("Acts"), each exposing different permissions, affordances, and
abstractions over the same underlying workflow.

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
    |  +-- act1                  # Maintenance (process/status loop)
    |  +-- act2                  # Records / bureaucratic interpretation
    |  +-- act3                  # Terminal / config inspection & patching
    |  +-- act4                  # Placeholder
    |  +-- runner                # Top-level runner / act selector
    +-- tests
    +-- env0.game.sln

### Core

`src/core/Env0.Core` contains only shared contracts:

- Output models
- Act interface (`IActModule`)
- Session state

There is no narrative logic and no IO in Core.

### Acts

Each `actX` folder contains a self-contained module that:

- Implements `IActModule`
- Owns its own behavior and state transitions
- Does not directly reference other acts

Current status:

- Act 1: module (`Act1Module`) exists and is wired to the runner
- Act 2: module (`Act2Module`) exists and is wired to the runner; a
  standalone console `Program.cs` also exists
- Act 3: module (`Act3Module`) exists; includes a standalone playground
  app and internal docs
- Act 4: placeholder folder exists

### Runner

The runner is a deliberately dumb console application that:

- Lets you select which act to load
- Passes raw input to the active act
- Prints returned output lines
- Returns to the menu when the act signals completion

The runner enforces no game rules.

------------------------------------------------------------------------

## Architectural Principles

- Core owns contracts, not behavior
- Acts own behavior, not wiring
- Runner owns wiring, not rules

No act should:

- Know which other acts exist
- Control process lifetime directly
- Perform direct console IO

No runner should:

- Enforce act-specific rules
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

The runner will prompt you to select an act to load.

### Run act-specific entry points

Act 2 can still be run directly:

```bash
dotnet run --project src/act2
```

Act 3 has a playground app:

```bash
dotnet run --project src/act3/env0.act3.playground
```

------------------------------------------------------------------------

## Act Lifecycle

Acts signal completion via shared session state.

When an act sets `SessionState.IsComplete = true`, the runner exits the
act input loop and returns control to the menu.

This avoids:

- Control flow hidden in output text
- Special runner commands like `exit`
- Exceptions used as flow control

------------------------------------------------------------------------

## Development Practices

- Large refactors should be done by porting, not stripping, where
  possible.
- Shared utilities should only be moved into Core once multiple acts
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

- Act 1: module wired to runner (process/status)
- Act 2: module wired to runner; standalone console entry present
- Act 3: module wired to runner; tests and playground present
- Act 4: placeholder folder
