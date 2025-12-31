# Maintenance Modes

This document describes the maintenance layer of the system.

Maintenance is not a single activity. It is a class of operational work
exposed through multiple terminals. Each terminal presents a different
maintenance variant over the same underlying system.

All maintenance variants are correct.
All are partial.
None explain the consequences of the work performed.

---

## Core Invariants

The following rules apply to all maintenance variants without exception:

- Maintenance work is mandatory.
- Maintenance work is procedural.
- Maintenance work does not explain itself.
- Maintenance work does not surface outcomes.

The `status` command is invariant across all maintenance variants:
- Same format
- Same content
- Same tone
- Same truth

`status` is the only cross-terminal ground truth.

---

## Purpose of Multiple Maintenance Variants

Multiple maintenance variants exist to normalise different kinds of
bureaucratic friction.

Each variant answers a different institutional question, implicitly:

- Why does this work require human attention?
- Why can’t this be automated (yet)?
- Why is this split across departments?
- Why does this feel inefficient but tolerated?

No variant is more important than another.
No variant is more ethical than another.

Players may prefer some variants over others.
The system does not.

---

## Variant Types (Seed Set)

The following variants are representative starting points.
They are not exhaustive.

### 1. Sequential Processing Terminal

**Type:** Sequencing-heavy  
**Primary Friction:** Order obedience

Characteristics:
- Work proceeds in fixed stages.
- Each stage requires explicit confirmation.
- No stage can be skipped.
- No decision-making is involved.

The player’s role is to acknowledge progression.
Failure is represented only as delay.

This variant exists because order is treated as risk mitigation.

---

### 2. Routing / Allocation Terminal

**Type:** Allocation-based  
**Primary Friction:** Decision without feedback

Characteristics:
- Work items must be assigned to queues, pools, or destinations.
- All destinations are valid.
- No destination is explained.
- No immediate feedback is provided.

Consequences may exist elsewhere.
They are not visible here.

This variant exists because responsibility has been decentralised.

---

### 3. Interrupt-Driven Terminal

**Type:** Noise management  
**Primary Friction:** Attention fragmentation

Characteristics:
- Processing is periodically interrupted.
- Interruptions require acknowledgement.
- Interruptions do not change outcomes.
- Interruptions are expected and routine.

The player manages flow, not problems.

This variant exists because instability is tolerated.

---

## Expansion Guidance

When adding new maintenance variants:

- Do not add puzzles.
- Do not add optimal strategies.
- Do not add success/failure states.
- Do not add moral framing.

Variants should differ in *effort*, not *meaning*.

If a player can say “I figured this one out,” the variant is incorrect.

---

## Relationship to Other Modes

- Records Mode explains *why* these terminals exist, not what they do.
- Automation Mode alters *how* maintenance is performed, not whether it exists.
- Interpretation Mode may speculate about meaning, but never alters maintenance truth.

Maintenance remains active even when the player stops interacting with it.