# env0.terminal - Project Overview

---

## Baseline Reset (December 2025)

This repository is a fresh baseline. Prior experiments are preserved as historical context only.

---

## What Is This?

env0.terminal is a pure C# logic engine for simulating a fully modular, authentic Linux-style terminal.
It is designed as a black-box DLL for consumption by any front end (CLI, custom UI, etc.), powering narrative, puzzle, or adventure games.

- No UI or rendering is included.
- All terminal, command, filesystem, and network logic is handled within this C# core.
- World state (filesystems, devices, users, boot sequence) is loaded via JSON.

---

## Project Vision

- Authenticity: emulate the feel and flow of classic Linux/Unix terminals, including realistic error handling and navigation.
- Modularity: all world data and rules are JSON-driven for fast iteration and content swapping.
- Separation of concerns: the logic core never touches visuals, audio, or UI.
- Robustness: designed to be resilient against hostile user input and edge cases.

---

## Repo Orientation

- REFERENCE.md: current rules, schemas, system behaviors, and edge-case handling.
- README.md: project structure, developer setup, and quickstart instructions.
- docs/: legacy design docs, full Q&A, task tracking, and historical material.

---

## Who Is This For?

- Developers building narrative or puzzle games based on authentic virtual terminals.
- Anyone needing a strict, headless terminal simulation engine for education, testing, or integration with custom front ends.

---

## Where To Start

1. Read README.md for structure and setup.
2. Consult REFERENCE.md for current rules and schema.
3. Explore the playground/CLI project for a quick hands-on test environment.
4. Open docs/ for historical context if needed.

---

## Philosophy

env0.terminal is designed for maintainability, strict separation of concerns, and creative extensibility.
If you are unsure whether a feature belongs here, ask:
"Is it pure terminal logic, or is it presentation/front-end?"
If in doubt, leave it out.

---

Historical note: Contracts.md and Milestones.md contain legacy plans and status markers. Use them for context only unless explicitly updated in this baseline.