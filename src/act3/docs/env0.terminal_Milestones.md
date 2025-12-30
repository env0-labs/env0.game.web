# MILESTONES.md

This document defines major project milestones for `env0.terminal`.
Milestones 1a and 1b are strictly defined.
Milestones 2+ are directional and may be revised as project needs evolve.

---

## Project Status (as of 2025-05-23)

| Feature/Module                | Status     |
|-------------------------------|------------|
| Filesystem Manager            | ✅ Complete |
| JSON Loader                   | ✅ Complete |
| Devices/Network Manager       | ✅ Complete |
| SSH/Login/Session Stack       | ✅ Complete |
| Command Suite (ls/cd/cat/etc) | ✅ Complete |
| Nmap/Ifconfig/Ping            | ✅ Complete |
| Error Handling                | ✅ Complete |
| Hostile/Psychotic Tests       | ✅ Out-of-scope tests commented and justified |
| Debug Commands/DebugManager   | 🚧 Not implemented (spec only) |
| TerminalEngineAPI             | 🚧 Not implemented (spec only) |

---

## Milestone 1a — Terminal Core, Single Filesystem (IN MEMORY)

**Objective:**  
Build a fully working core terminal experience with hardcoded in-memory filesystem, basic navigation, and command execution.

**Scope:**
- Terminal command loop accepts raw user input.
- Commands implemented: `ls`, `cd`, `cat` (strict, Linux-like error handling).
- Hardcoded, in-memory filesystem (`FileEntry` or equivalent).
- No JSON loading, device abstraction, or network context.
- Command handler/dispatcher in place.
- Filesystem operations function as intended; errors on invalid paths/commands.
- Minimal but clear test coverage for all commands and core FS logic.
- Manual gameplay tests prove system loop.

**Exclusions:**
- No device/network abstraction.
- No SSH/login.
- No JSON file loading.
- No advanced features (autocomplete, history, FX, etc).

**Status:** ✅ COMPLETE

---

## Milestone 1b — JSON Loader, Devices, and Network Context

**Objective:**  
Modularize filesystem loading, add device/network context, and enable SSH/login simulation.

**Scope:**
- Filesystem loads from external JSON files (not hardcoded).
- Device abstraction: multiple filesystems/devices, each with its own config.
- SSH/login flow: context switch updates prompt, filesystem, and session state.
- JSON loader/POCO model robust to malformed or missing data (with fallback or error).
- Error handling for failed device/SSH operations.
- Integration test (“Milestone Master Test”) proves full command-to-output pipeline across device/session boundaries.

**Exclusions:**
- No full permissions or advanced security.
- No multi-user logic.
- No narrative or AI program.
- Minimal network realism—focus is on working abstraction, not security or simulation.

**Status:** ✅ COMPLETE

---

## Milestone 2 — Conversational Narrative Layer

**Objective:**  
Integrate an adventure-style, AI-powered program into the terminal.

**Scope (directional):**
- Launchable in-terminal “AI” with verb+noun parser.
- Predefined response arrays, branching hints, and narrative fragments.
- Supports both “System AI” (polite, robotic) and “Cursed AI” (hostile/sarcastic) modes.
- Player interaction with the AI influences story/progression.

**Status:** Planned (not started)

---

## Milestone 3 — Seamless Expandability

**Objective:**  
Ensure the system is easy to expand, update, and maintain.

**Scope (directional):**
- All new devices, filesystems, commands, and narrative content can be added via JSON or external config/tools.
- No hardcoding of future expansion.
- Documentation and modular architecture enforced.

**Status:** Planned (not started)

---

## Milestone 4 — Immersive Presentation & Atmosphere

**Objective:**  
Create a visually and sonically authentic “haunted terminal” experience.

**Scope (directional):**
- Retro visuals, terminal FX, ambient/interactive audio.
- Visual and audio feedback matches narrative tone.
- Presentation handled at the front-end (or chosen frontend) layer, not in core logic.

**Status:** Planned (not started)

---

## Milestone 5+ — Ongoing Evolution

- Additional milestones to be defined based on project needs, creative direction, and user feedback.
- Examples: Advanced security, multiplayer, world/story expansion, editor tooling, etc.

---

## Hostile/Psychotic Test Handling

- **All critical contract behaviors and plausible edge cases** are tested.
- *“Psychotic”* or out-of-scope edge-case tests (kernel simulation, code injection, unrealistic abuse) are **commented out** and annotated in the suite, not deleted.
- Hostile/psychotic test failures **do not block milestone closure** unless they indicate a contract or canonical logic failure.

---

## Known Gaps & Not-a-bug

- **POSIX/Absolute path handling:** Not fully POSIX-complete; complex path edge cases are out of scope for 1A/1B.
- **Session persistence:** None by design—fresh state every run.
- **Debug features:** DebugManager and debug-only commands are not implemented as of Milestone 1B, but fully specified in the contract.
- **Multi-user:** Not supported for Milestone 1—single user/device context only.
- **No advanced shell features:** No pipes, wildcards, environment variables, or command chaining.
- **Hostile/psychotic tests:** Not a blocker unless violating contract. All are preserved for future review.

---

**Milestones are living documents—1a and 1b are locked for v1.0, later milestones will adapt as required.**
