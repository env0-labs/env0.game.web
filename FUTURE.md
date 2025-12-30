# FUTURE

This document describes intended future state, design constraints, and non-binding plans.
Nothing in this file is guaranteed to be implemented. If something here becomes true in
the repo, it should be reflected in README.md.

---

## Roadmap

- Implement Act 4.
- Complete consolidation of Act 2 and Act 3 into the shared Core/Runner architecture
  (including retiring the standalone Act 2 console entry point).

---

## Act 1 - Maintenance (In Progress)

### Purpose
Act 1 establishes compliance through repetition: the player performs procedural work
correctly, without understanding, until language becomes invisible and obligation
becomes self-sustaining.

### Player Belief
“I’m processing data. This is necessary.”

### Interface Constraints
- Terminal-only interface.
- Only two accepted commands:
  - `process`
  - `status`
- Any other input must return an error that reasserts the allowed commands.

### Experience Constraints
- Act 1 must be dull. If it is interesting, it fails.
- Repetition is the mechanism. Do not “add variety” to relieve boredom.
- Output should become skimmable quickly; the player should optimise attention away
  from reading.
- No puzzles. No exploration. No “help”. No hinting.

### Work Model
- Work is represented as parent/child process containers.
- The player processes children within the current parent container.
- Parent containers are finite; the next parent is assigned when the current one completes.
- The size of a new parent container is unknown until processing begins (scope reveals
  itself through action, not planning).
- Backlog must not feel like punishment or sabotage; it should feel like normal scheduling
  and deferred release.

### Status Presentation
- `status` includes a minimal ASCII representation of the current container structure:
  parent container + child items as processed/pending.
- `status` must not reveal global totals, completion percentages, or a finish line.
- No progress bars, XP, achievements, or reward language.

### Language Constraints
- Output is procedural, flat, and indifferent.
- No moral framing. No ethical warnings. No narrative commentary.
- Only procedural confirmations are allowed.
- Terms `kill-child` and `kill-parent` may appear as legitimate process-control language:
  - present early
  - repeated often
  - operationally dull
  - no escalation, synonyms, or dramatization
  - not explained in Act 1

### Forbidden Outcomes
Act 1 fails if the player feels:
- punished
- judged
- rewarded
- “clever” in a way that reads as authorial approval

Act 1 succeeds if the player reaches, unprompted:
- “Nothing went wrong.”
- “I did exactly what the system needed.”

### Open Questions
- Exact pacing and duration required for habituation (how long Act 1 must run before
  it reliably “clicks” as maintenance).
- Exact parent container sizing distribution (range/variance) to avoid resentment while
  sustaining obligation.
- Whether Act 1 ever ends “naturally” or only transitions when the system permits.

---

## Act 2 — Interpretive Overlay (Present but Under Consolidation)

### Intended Role
Act 2 provides records, policies, logs, and bureaucratic language that allows investigation
without answers. Maintenance remains mandatory; interpretation remains optional.

### Critical Asymmetry
- Maintenance is mandatory.
- Interpretation is optional.

---

## Act 3 — Breach / False Optimisation (Present but Under Consolidation)

### Intended Role
Act 3 introduces read access to the system’s own configuration (e.g. JSON defining Acts),
and limited, indirect write access via patch scripts.

### Constraints
- Read access reveals boring, clean, procedural configs (archaeology, not lore).
- No direct editing; changes are applied via patch scripts only.
- Patch scripts provide procedural confirmations only:
  - “This will do X, Y, Z. Are you sure? (y/n)”
  - later: `-y`
- No ethical framing. No warnings. No punishment for curiosity.

### Intended Trap
Automation is framed as relief and competence, but reduces moral proximity by increasing
distance between action and consequence.

---

## Act 4 — Interpretation Layer (Planned)

### Purpose
Act 4 provides fluent, persuasive interpretation that is plausible but not authoritative.

### Asymmetry
- Terminal = truth without meaning.
- Interpretation layer = meaning without truth.

### Constraints
- Tempt the player toward belief instead of verification.
- Must not become authoritative truth.
- Ignorance must become a choice.

---

## Cross-Act Invariants

- Optimisation creates distance.
- Understanding arrives after action.
- Systems never lie.
- Systems never care.
- No ethical framing. No “this will have consequences.” No narrative warnings.
- The system assumes competence; responsibility follows.
- Horror is retroactive recognition, not malfunction.

### Success Anchors
The experience succeeds if the player reaches, unprompted:
- “All systems reported healthy.”
- “Nothing went wrong. I did exactly what the system needed.”

---

## Non-Goals

- Cyberpunk aesthetics as spectacle (no neon, no rebellion fantasy).
- Player empowerment fantasy (no chosen one, no “hero hacker” arc).
- Apocalypse framing.
- Punishment for curiosity.
- Lore dumps that explain the thesis instead of embodying it.
