# Baseline Reset (December 2025)

This file contains historical reference material from prior milestones. It is preserved for context.

True north for the new baseline:
- The engine is pure backend logic (no UI or rendering).
- Local login is username assignment only; SSH is the only authenticated path.
- Commands, filesystem, and network behavior are deterministic and test-driven.
- No persistence, no shell features beyond the defined command set.

---
# 🧼 REFERENCE.md — Canonical Project Source (C# Logic Engine, Milestone 1)

> **Project Status: 2025-05-25**
> All filesystem loading/conversion and SSH login logic are now robust, milestone-complete, and fully debugged.
> - All debug output is routed through DebugUtility with context tagging and toggleable visibility.
> - SSH login phase now supports ‘abort’ to cancel credentials, and cyclic/self-SSH attempts are blocked.

---

## Table of Contents

1. [How to Use This File](#how-to-use-this-file)
2. [Test Philosophy & Hostile Cases](#test-philosophy--hostile-cases)
3. [Known Gaps & Deviations](#known-gaps--deviations)
4. [Project Laws (Non-Negotiables)](#project-laws-non-negotiables)
5. [Project Roles & Boundaries](#project-roles--boundaries)
6. [Development Cycle](#development-cycle)
7. [Terminal Rendering and Input](#terminal-rendering-and-input)
8. [Prompt Construction](#prompt-construction)
9. [State Machine (TerminalStateManager)](#state-machine-terminalstatemanager)
10. [Session State](#session-state)
11. [Login and SSH Handling](#login-and-ssh-handling)
12. [Command System](#command-system)
13. [Filesystem & Directories](#filesystem--directories)
14. [Network and Devices](#network-and-devices)
15. [Debug Mode](#debug-mode)
16. [JSON Schema Appendix (Summary)](#json-schema-appendix-summary)
17. [Edge Cases & Validation](#edge-cases--validation)

---

## How to Use This File

- **Before coding, changing architecture, or asking for help, check here first.**
- **Never make assumptions about file contents**—if you do not hold the exact schema or code in memory, request it.
- This file is updated whenever major design, logic, or system-level decisions change.

---

## Test Philosophy & Hostile Cases

- All critical and contractually required edge cases are covered by tests.
- *Psychotic/hostile* edge-case tests (those requiring code injection, abuse, or “how would anyone do this” input) are commented out, preserved, and annotated in the test suite, with a rationale.
- **Milestone closure is based on contract/REFERENCE.md compliance**—hostile tests that exceed contract are NOT blockers.
- Tests are *never deleted*—all commented cases are preserved for future review.
- See also: [Contracts.md](Contracts.md) for details on testing contract and test commenting policy.

---

## Known Gaps & Deviations

These are **not bugs**—they are known, accepted deviations from “real Linux” or theoretical maximum security, and do not block milestone closure:

- **Absolute path handling** is not fully POSIX-complete (e.g., complex path collapse or odd symlinks are not supported). This is *out of scope* for 1A/1B.
- **No session persistence**: Every run is a fresh start by design.
- **No multi-user support**: Only single-user per device (for Milestone 1).
- **No advanced shell features** (pipes, chaining, environment variables, wildcards, etc.): Strictly not supported unless called out below.
- **Psychotic/hostile tests** that would require kernel-level simulation, code injection, or violate the narrative game frame are not in scope and do not block milestone status.

---

## Project Laws (Non-Negotiables)

1. **There is no "local login" authentication.**
    - The initial prompt after boot is *not* a login; it is simply a username (and password, if desired) assignment.
    - **No validation, no lookup, no security boundary.**
    - The entered username is used solely for the prompt and flavor; the password is flavor only (may be stored for future use but is not checked).
    - There is never any way for a user to "fail" to log in locally.
2. **All device authentication/validation only applies to SSH/network logins.**
3. **Separation of concerns:**
    - Each major component (TerminalStateManager, NetworkManager, FilesystemManager, LoginHandler, CommandHandler, DeviceInfo) must not duplicate logic or state.
    - Data flows only through *clear interfaces*—never direct cross-class mutation.
4. **Prompt construction must always reflect the current session context** (see [Prompt Construction](#prompt-construction)).
5. **This document overrides all other design notes, past chats, or legacy behaviors.**

---

## Project Roles & Boundaries

- **env0.terminal** is a *pure C#* logic engine.
- All terminal, command, filesystem, and network logic is handled here.
- No visual/UI, audio, or front-end responsibilities—these are for front-end or any other consumer via DLL.
- **Milestone 1 goal:** *Fully functional, Linux-style virtual terminal with local username assignment (no local authentication), SSH (with authentication), strict navigation, JSON-driven filesystems, and a clean CLI command system.*

---

## Development Cycle

Strictly enforced for all core and feature work:
1. Decide the next feature or fix
2. Build the feature (code, logic, or config)
3. Build the test (unit/integration/hostile case as appropriate)
4. Test and debug until all automated tests pass
    - Includes standard and hostile/psychotic edge-case suites
    - Regularly interrogate and expand hostile test coverage
5. Modify for user test in Playground (manual verification, if relevant)
6. Commit once happy (atomic, meaningful commit; no “test/fix/test/fix” in main)
7. Update the Tasklist (env0.terminal_tasklist.md)

---

## Terminal Rendering and Input

- **Terminal buffer:** Text-only, managed in C# (not UI). Output is ephemeral (no scrollback), just like old school terminals.
- **Input:** Insert-only, single-line. ASCII-only for all commands, usernames, passwords, and file content. Non-ASCII input is blocked.
- **Prompt:** Always appended as the last line of output, except during BOOT and "username assignment" (formerly local login) and during SSH login, where it is suppressed.
- **Command History:**
    - Up to 20 entries, session-only, cleared on restart.
    - Consecutive duplicates are not stored.
    - Failed/mistyped commands are stored.
- **Case Sensitivity:**
    - **All commands, file, and directory lookups are case-insensitive.**
    - All JSON keys for files/dirs are normalized to lowercase at load.
- **Clear command:** Wipes only visible terminal content (not history/buffer).
- **Input limit:** 256 characters per command; dangerous/special chars (`;`, `&&`, `|`, etc.) are stripped.
- **Terminal height:** Defaults to 24 rows (classic). Can be set by front-end at initialization; if not set, remains 24. Used for paging in `read` and similar commands.

---

## Prompt Construction

**The shell prompt must always accurately reflect the current session context, including username, device hostname, and working directory.**

- **Format:**  
  `<username>@<hostname>:<cwd>$`
- Where `<username>` and `<hostname>` are session-specific variables, and `<cwd>` is current working directory (`~` or full path).
- **Local (“login” / username assignment):**
    - Username entered during initial prompt is used for the local session.
    - Hostname is the local device’s hostname.
- **SSH:**
    - On SSH, prompt updates to reflect the username entered (either parsed from command or prompted) and the remote device’s hostname.
    - On SSH hopping, each session maintains its own username/hostname/cwd context (stacked).
    - Exiting SSH restores the previous prompt/context.
- **At all times, the prompt is dynamically generated from the current session stack.**
- **This is the only meaningful “connection” between local and SSH sessions.**

---

## State Machine (TerminalStateManager)

- **States:**
    - BOOT → USERNAME_ASSIGN (was LOGIN) → SHELL
    - From SHELL: may enter SSH (pushes to SSH_STACK)
    - DEBUG is a flag, not a state.
    - **SSH Stack:** Max depth of 10. Attempting to SSH deeper:
    - -Stack overflow: Too many nested SSH sessions. Connection reset to local SBC.
  - Forces return to local SBC prompt.
- **Transitions:** Only valid transitions permitted (see full state diagram).

---

## Session State

- **Session state object holds:**
- Username (from USERNAME_ASSIGN input, always ASCII-only)
- (Optionally) password (stored but unused for local login)
- Current device context (for local shell or SSH; tracks which device is "active")
- Current working directory (per device/session)
- Command history (last 20 commands)
- SSH stack (list of device/user contexts up to depth 10)
- Debug flag (true/false)
- No persistent state beyond session; everything resets on restart.

---

## Login and SSH Handling

- **Local Username Assignment ("local login"):**
- On first boot, user is prompted to enter a username (required) and password (optional, can be blank).
- No validation, no check, no authentication. Username is used in prompt; password is stored for flavor or possible future use, but not checked for anything.
- Password is hidden input (classic Unix behavior: input not shown).
- Warns user not to use a real password ("WARNING: Do not use a real password. This system is not secure.").
- After entry, username and password are stored in session and never checked again.
- **SSH:**
- CommandHandler parses `ssh <target>`; asks NetworkManager to resolve the target.
- NetworkManager returns the Device object for the target (by IP or hostname).
- LoginHandler prompts for username (if not supplied as `user@host`), then password.
- Device authentication (username/password) is checked against the Device object.
- Unlimited login attempts, each with a randomized delay (2–5 seconds).
- Failed attempts: "Login failed" (no hints, no mercy). Delayed before new input.
- MOTD is shown on every successful SSH login.
- Only one user per device (no root/admin distinction).
- Exiting SSH returns to previous session (SSH stack).
- If at root, "You are already at the local terminal."
- No Ctrl+C/manual interrupt.
- SSH login now supports entering ‘abort’ at any prompt to exit SSH login cleanly, with user feedback.
- Cyclic SSH and self-SSH attempts are detected and blocked, maintaining session stack integrity and user clarity.

---

## Command System

- **Supported Commands:**
- `ls` / `dir` — List directory contents (dirs first, then files, alpha).
- `cd` — Change directory (absolute or relative).
- `cat` — Display file contents (raw text, including .bin, up to 1000 lines). `.sh` returns "file is executable and cannot be read".
- `read` — Paginated file reader, shows "(empty file)" if file is empty or whitespace only. Can quit with `q`. Page number is shown. Max 1000 lines per file. Page size is terminal height - 1.
- `echo` — Text output only. No variable substitution, redirection, or escape chars. Multiple spaces, tabs, and special chars are preserved.
- `clear` — Clears only the current device’s terminal output.
- `ping` — Simulates 4 packets, random delay (max 100ms), random TTL, random packet loss, per device’s network only.
- `nmap` — Scans and lists all devices on current network, with open/closed ports. Shows device description, or "No description available".
- `ssh` — SSH to remote device (see above).
- `exit` — Close SSH, return to previous session, or shows "You are already at the local terminal." at root.
- `sudo` — Always returns "Nice try." (Easter egg).
- `help` — Comprehensive, paginated help for all commands/aliases/examples, grouped by category.
- **Error Handling:**
- Errors are slightly vague, but clear (e.g., `bash: No such file or directory`). No hints or auto-correction.
- All errors always leave a blank line below before the prompt.
- Identical failed command twice: suggest `help`.
- **Specific errors:**
- **SSH stack overflow:**  
  `Stack overflow: Too many nested SSH sessions. Connection reset to local SBC.`
- **File too large:**  
    `bash: read: File too large (1,000 line limit).`
- **Command buffer overflow:**  
    `bash: Input exceeds 256 characters. Command ignored.`
- **Invalid JSON:**  
    `JSON Validation Error: [filename] [details] — using safe defaults.`
- **Empty password:**  
    `Yikes... no password? Living dangerously.`

---

## Filesystem & Directories

- **JSON-driven, fully loaded at startup.**
- All filesystems stored in `/Config/Jsons/Filesystems/Filesystem_[N].json` (1–10 for game filesystems, 11 for failsafe/empty).
- Supports max 4 directory levels; filesystems are read-only.
- **Case-insensitive** lookup for all files/dirs; all keys normalized to lower-case at load.
- **File Types:**
- `.txt`, `.log`, `.bin`: Readable with `cat`/`read` (up to 1000 lines).
- `.bin` files are flavor files, always nonsense text.
- `.sh`: Not readable (`cat`/`read` print "file is executable and cannot be read").
- **Failsafe Filesystem:**
- Filesystem_11.json (must exist) contains a single congratulatory file (`/congrats.txt: "You broke in! But there’s nothing here."`).
- If any device references a missing/corrupt filesystem, load this one as a fallback.
- **File Size Limit:**
- Max 1000 lines per file; larger files fail to open with error.
- **Empty Files/Dirs:**
- `cat/read` on empty file: shows "(empty file)".
- `ls` on empty dir: shows nothing (just prompt).

---

## Network and Devices

- **Devices defined in Devices.json:**
- `ip`, `hostname`, `mac`, `username`, `password`, `filesystem` (which FS JSON to load), `motd`, `description`, `ports` (array), `interfaces` (array of name/ip/subnet/mac).
- **Network Commands:**
- `ifconfig` — Lists all interfaces (e.g., eth0, wlan0), IP, subnet, MAC, per device.
- `nmap` — Lists all devices on current device's subnet, showing open/closed ports, description.
- `ping` — Can ping any device on the current network, simulates random delay/loss.
- **Network Topology:**
- Devices can bridge multiple subnets.
- Each device has a single user account.
- Network/device list is static at runtime.
- **NetworkManager:**
- Responsible for all device lookups by IP/hostname.
- No login/auth logic—just returns Device objects for CommandHandler/LoginHandler to use.

---

## Debug Mode

- **Only available in dev/debug builds; never shipped in release.**
- **Toggled with `debug` command**; shows “DEBUG MODE ACTIVE” message.
- **Commands** (case-insensitive, not in help unless debug is on):
- `show filesystems` — List all loaded filesystems.
- `list devices` — Show all network devices (including hidden).
- `teleport <path>` — Instantly move to any directory (confirmation required).
- `clear debug` — Clears both main and debug info.
- **JSON validation errors** are shown only in debug mode, in a dedicated section, labeled by JSON type.
- **Debug info** is shown in bright yellow for visibility.
- **Unsafe by design:** debug commands can break normal flow and are for developer use only.
- **NOTE:** Debug command logic is specified in the contract, but is **not implemented** as of Milestone 1B.
- DebugUtility is now used for all debug output during JSON loading, filesystem parsing, and conversion. Console.WriteLine is deprecated.
- Debug messages are only visible when debug mode is enabled, and include full context tags and trace output.

---

## JSON Schema Appendix (Summary)
*(As in original, not repeated here for brevity.)*

---

## Edge Cases & Validation
*(As in original, not repeated here for brevity. Includes notes on device interfaces, failsafe handling, terminal height, input validation, session reset, etc.)*

---

> **If any rule here is ambiguous, or if a new edge case is discovered, update this section and document exactly how it should be handled before proceeding.**

