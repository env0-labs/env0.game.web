# Task List / Project Progress — emv0.terminal

This file tracks all major system milestones and features as they are completed.
Update this list every time a new feature is finished and covered by tests.

---

## ✅ Completed

- [x] **Project structure:** Solution and project split (core, tests, playground)
- [x] **.gitignore:** Clean ignore rules for all platforms and build tools
- [x] **TerminalStateManager:** Core state machine (boot, login, shell, SSH)
- [x] **CommandParser:** Command parsing and normalization (case-insensitive, dangerous char filtering)
- [x] **FilesystemManager:** Full read-only virtual FS with:
    - [x] Directory listing (`ls`)
    - [x] Directory navigation (`cd`, `cd ..`, `cd /path`)
    - [x] Parent directory traversal
    - [x] Absolute/relative path navigation
    - [x] Case-insensitive lookup for all files and directories
    - [x] File content read (`cat`)
    - [x] Error/edge handling for invalid/empty/missing files, directory/file confusion
    - [x] Empty directories, empty files, large file (>1000 lines) error
    - [x] File extension/type handling (.sh, .bin, unsupported, etc.)
    - [x] Directory/file name validation (no dot, dot-dot, slashes, or duplicates)
- [x] **BootConfig loader and validation:**
    - [x] BootConfig POCO
    - [x] Loader/parser integration with BootSequence
    - [x] Tests (standard and hostile: malformed, missing, out-of-spec)
    - [x] Debug output for validation errors
- [x] **Test coverage:**
    - [x] Standard test suite (20/20 green) for all core FS logic
    - [x] **Hostile User Test Suite** (`FilesystemManagerHostileUserTests.cs`):  
          - 27/27 “maniacs only” tests green  
          - Includes name collisions, path traversal, invalid names, edge-case validation, and more
- [x] **Playground console app:**  
      - Live interactive terminal for hands-on navigation and file access
- [x] **Comprehensive JSON Loader Suite:**
    - [x] UserConfig, Devices, BootConfig, Filesystem loader logic, POCOs, and integration.
    - [x] All loaders tested with both standard and "psychotic" edge-case test suites.
    - [x] Any test cases that are impossible by design (e.g. keyboard can’t enter control chars, loader always returns fallback) are **commented out or skipped in test code, with a clear explanation and rationale**.
    - [x] All passing tests (including hostile and pathological input) are documented directly in test files for future reference.
    - [x] Loader is now robust against binary, malformed, over-nested, deeply hostile, or otherwise cursed configs; all cases handled via fallback, error logging, or explicit rejection.

---

## 🔜 In Progress / Next Up

- [ ] LoginHandler: Local and SSH login with user/session state
- [ ] CommandHandler: Command dispatch and error output
- [ ] Integration: Connect all systems for real “terminal” flow

---

## 📝 Notes

- Directory/file name validation is now active:  
  - Names must not be empty, dot, dot-dot, or contain slashes
  - Adding a duplicate (case-insensitive) throws exception
- All “actively hostile user” edge cases now handled and tested
- FS manager is robust and future-proofed for future cursed logic experiments

- Update this file **every time you commit a major feature or pass a test milestone**
- Use as a single source of truth for migrating to new chat sessions or onboarding contributors
