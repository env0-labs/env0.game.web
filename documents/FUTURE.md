# FUTURE

This document describes the intended future shape of the project, its operating model,
and its design constraints.

Nothing in this file is guaranteed to be implemented.
If something described here becomes true in the repository, it should be reflected in
README.md.

The repository already implements multiple contexts (Maintenance, Records, Terminal)
with both console and WPF runners. The intent remains a single system experienced
through different operational modes, even if the implementation routes between
contexts.

Each mode exposes a different surface of the same underlying work.

---

## High-Level Direction

The system is always doing work.

What changes over time is:
- how much of that work the player performs directly
- how much of it they delegate or automate
- how closely they choose to observe its consequences

Movement between modes does not represent progress, success, or enlightenment.
It represents **distance**.

Distance from action.
Distance from responsibility.
Distance from consequence.

---

## Roadmap (Non-Binding)

- Stabilise Maintenance Mode as a persistent background system.
- Expand Maintenance Mode into multiple interchangeable maintenance variants.
- Continue expanding Records Mode as a navigable interpretive layer.
- Evolve the existing Terminal Mode into a CLI-based Automation Mode that alters how maintenance is performed.
- Introduce a Compliance System spanning all modes.
- Add a final Interpretation Mode once system behaviour is stable.

---

## Maintenance Mode (In Progress)

### Purpose

Maintenance Mode establishes procedural compliance through repetition.

The player performs work correctly without understanding its meaning or scope.
Over time, the work becomes routine and attention shifts away from content and toward
completion.

Maintenance is designed to become ignorable, not satisfying.

### Player Belief

“I’m doing necessary work. Nothing is wrong.”

---

### Core Interface Constraints

- Terminal-only interface.
- All maintenance terminals expose the `status` command.
- Most maintenance terminals expose `process` or an equivalent procedural action.
- Any unsupported input returns a flat, procedural error.

No terminal explains why its work exists.
No terminal explains how its work relates to other terminals.

---

### Work Model (Shared Across Variants)

- Work is represented as parent and child process containers.
- The player processes child items within a parent container.
- Parent containers are finite.
- When a parent completes, a new one is assigned automatically.
- Container size is unknown until processing begins.
- Scope is revealed only through action, not planning.

Backlog must feel normal.
It must not feel punitive or exceptional.

---

### Maintenance Variants (Planned)

Maintenance Mode is not singular.

Multiple maintenance variants exist simultaneously, accessed through different terminals.
Each variant represents a different bureaucratic framing of similar underlying work.
The current build uses identical variants with distinct machine identifiers, pending
variant-specific mechanics.

Examples of variation (non-exhaustive):
- Sequential processing with strict ordering
- Allocation or routing work with no feedback
- Interrupt-driven work with frequent acknowledgements
- Verification-heavy work with redundant confirmation
- Monitoring or reconciliation work with low agency

All variants are correct.
None are optimal.
None explain consequences.

Variants may differ in effort and friction.
They must not differ in moral weight or narrative importance.

---

### Status Presentation (Invariant)

The `status` command is invariant across all maintenance variants.

- It always reports real, current system state.
- It uses minimal ASCII structure.
- It shows only local containers and their child items.
- It reveals no global totals.
- It provides no progress indicators.
- It never signals completion.

`status` is the ground truth of the system.

---

### Language Constraints

- Output is procedural, flat, and indifferent.
- No moral framing.
- No ethical warnings.
- No narrative commentary.

Terms such as `kill-child` and `kill-parent` may appear as legitimate process-control
language:
- introduced early
- repeated often
- operationally dull
- never explained
- never escalated or dramatized

---

### Experience Constraints

- Maintenance must be dull.
- Repetition is the mechanism.
- Do not add variety to relieve boredom.
- Output should become skimmable.
- The player should optimise attention away from reading.

There are no puzzles.
There is no exploration.
There is no help system.

---

### Transition Condition

After sufficient repetition, the system allows access to Records Mode.

This is not framed as success or completion.
It is framed as availability.

Maintenance does not end.
It becomes background labour.

---

## Records Mode (In Progress)

### Purpose

Records Mode exposes institutional artifacts:
logs, policies, memos, terminals, and historical records.

It allows investigation without resolution.

Maintenance remains mandatory.
Interpretation remains optional.

---

### Structure

- The player can move between rooms or locations.
- Locations contain records and terminals.
- Terminals route into Maintenance (contextual variants) and return to the originating room.
- Records are locally coherent and globally incomplete.

Terminal access is mapped per room in `Config/Jsons/Devices.json` via `recordsRoomId`.

No single record explains the system.
Contradictions are allowed.
Gaps are allowed.

If the player feels confident they understand the system, too much has been revealed.

---

### Constraints

- Records must not unlock truth.
- Records must not provide answers.
- Records must not resolve ambiguity.

The player may form theories.
Those theories should remain plausible and incomplete.

---

## Terminal Mode (CLI / Configuration Mode)

### Purpose

Terminal Mode allows the player to inspect and (eventually) alter *how* work is performed.

It introduces delegation, automation, and procedural distance.

Automation reduces visible labour while increasing abstraction.

---

### Interface

- Linux-like CLI.
- Read access to system configuration (current implementation focus).
- Indirect write access via patch scripts or automation definitions (planned).

No direct editing.
All changes are applied procedurally.

---

### Constraints

- Automation is framed as relief, not mastery.
- No celebratory feedback.
- No efficiency praise.
- No moral framing.

Patch scripts provide procedural confirmation only:
- “This will change X. Are you sure? (y/n)”
- later: `-y`

---

### Critical Rule

Automation never eliminates maintenance.

The player must always be able to access a maintenance terminal and run:

`status`

and receive real, current, unsanitised output.

Maintenance remains the ground truth.

---

## Compliance System (Cross-Mode)

### Purpose

The compliance system mirrors institutional reward and punishment structures.

It exists to sustain participation.
It does not enforce correctness.
It does not explain itself.

---

### Recognition

- Appears to be a reward system.
- Is arbitrary and persistent.
- Has no mechanical effect.
- Cannot be optimised or pursued deliberately.

Recognition signals visibility without meaning.

---

### Punishment

- Is arbitrary and temporary.
- Introduces friction without instruction.
- Does not block progress.
- Expires silently.

Punishment is framed as “enhanced compliance”.

Recognition and punishment never interact.

---

## Interpretation Mode (Planned)

### Purpose

Interpretation Mode provides fluent, persuasive explanations.

These explanations are plausible but not authoritative.

---

### Asymmetry

- Maintenance and Automation provide truth without meaning.
- Interpretation provides meaning without guaranteed truth.

Ignorance must become a choice.

---

## Cross-Mode Invariants

- The system never lies.
- The system never cares.
- No mode invalidates another.
- Maintenance is always the ground truth.
- Optimisation creates distance.
- Understanding arrives after action.
- Horror is recognition, not malfunction.

---

## Non-Goals

This project explicitly avoids:

- Power or rebellion fantasies.
- Cyberpunk spectacle.
- Apocalypse framing.
- Punishment for curiosity.
- Lore dumps that explain intent instead of embodying it.
