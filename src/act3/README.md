# env0.terminal

A pure C# logic engine for simulating an authentic, modular Linux terminal.  
**No UI, no rendering, no host glue-just robust backend logic.**  
All front-end (CLI, custom UI, etc.) interacts with this as a DLL or black-box API.

---

## Snapshot / Baseline (December 2025)

This repository is now the baseline launch point. Prior experiments and branches are intentionally abandoned; start from the state described here. Target framework is .NET 8.0 across all projects (`Env0.Terminal`, `Env0.Terminal.Tests`, `Env0.Terminal.Playground`).

### Quick start
- Prereq: .NET SDK 8.0.x installed (`dotnet --list-sdks` to verify).
- Restore/build: `dotnet restore env0.terminal.sln && dotnet build env0.terminal.sln`
- Run playground shell: `dotnet run --project Env0.Terminal.Playground`
  - Boot text shows once, then username/password prompts (defaults: `player` / `password`).
  - Commands: `ls`, `cd`, `cat tutorial.txt`, `ssh workstation2.node.zero`, `exit`.

### Tests
- Run all tests: `dotnet test env0.terminal.sln`
- Note: Tests expect deterministic sudo output ("Nice try.") and specific SSH/login flows; use this repo state as the contract.
- Psychotic tests are enabled and passing in the current baseline.

### Configs and content
- JSON configs live under `Env0.Terminal/Config/Jsons/` (BootConfig, UserConfig, Devices, Filesystem_*). Tests and playground consume these paths relative to repo root.
- Canonical behavior is defined by `docs/env0.terminal_REFERENCE.md` and `docs/env0.terminal_Contracts.md`; if behavior and docs diverge, update code or docs to realign.

---

## 🚩 What is this Project?

**env0.terminal** is the core simulation for narrative, puzzle, or adventure games built on a Linux-style terminal experience.

- Implements strict Linux-style terminal navigation, file access, and SSH-based network traversal.
- All world data (filesystems, devices, boot sequences, users) is defined via JSON and fully session-driven.
- Built to be consumed as a DLL—front-end is one possible host, but the logic is fully standalone.
- **Note:** All narrative and AAI (Artificial Artificial Intelligence) features are now developed in a separate project. This repository contains only the terminal core logic.

---

## ✅ Current Status

- All converters are unified under a single canonical FileEntry model.
- Filesystem loading, conversion, and debug output now use `DebugUtility` exclusively—no `Console.WriteLine` in core logic.
- Full debug traces and context are available in debug mode (dev/test builds only); debug output is toggleable and tagged by context.
- The filesystem tree is fully parent-linked and supports nested navigation with proper error recovery.
- SSH login is robust—supports aborting login at any prompt (`abort` command), blocks cyclic/self-SSH, and ensures session stack integrity.
- SSH and command execution are stable across recursive and multi-hop sessions.
- The **TerminalEngineAPI** is fully implemented and exposes the entire logic stack as a single, canonical public API for any front-end, test harness, or integration.
- Full test coverage exists across standard, hostile, and psychotic cases. Out-of-scope or fragile tests are preserved in the codebase as comments with explanations.
- Narrative/AI features (AAI) are now split out of this codebase.

This is a known-good checkpoint.

---

## 📚 Core Principles

- **Pure C# logic.** No UI, no front-end dependencies—strict backend engine.
- **JSON-driven.** All world content (devices, filesystems, users) is externally defined.
- **Case-insensitive.** Commands, files, and directories are all case-insensitive.
- **Read-only.** No file creation/editing in Milestone 1—filesystem is static per session.
- **Strict errors.** All command errors follow Linux conventions: no hand-holding, no suggestions.
- **Session-only state.** No persistence or mutation outside the runtime session.
- **Debug mode is dev-only.** Controlled by future flags—not exposed to players; all debug output is now routed via DebugUtility and toggleable in dev builds.
- **REFERENCE.md and Contracts.md are law.** They govern system behavior and test expectations. If you’re unsure, read [REFERENCE.md](./docs/env0.terminal_REFERENCE.md) or [Contracts.md](./docs/env0.terminal_Contracts.md).

---

## 🖥️ Rendering & UI Separation

- **All rendering, screen clearing, cursor, and visual effects are handled entirely by the consuming application** (CLI, front-end, etc.).  
  This engine provides only logic, state, and output—never visual formatting or presentation.

---

## 📝 Development Cycle

The following discipline is enforced for all code and content changes:

1. Decide the next feature or fix
2. Build the feature (code, logic, or config)
3. Build the test (unit/integration/hostile case)
4. Test and debug until all automated tests pass
   - Includes standard, hostile, and edge-case coverage
5. Modify for manual test in Playground (if needed)
6. Commit once happy (atomic, meaningful commit only)
7. Update the Tasklist (`env0.terminal_tasklist.md`)

This process is non-negotiable. All contributors must adhere.

---

## ⚠️ Out of Scope

- No UI, rendering, or audio logic
- No persistent session state or save/load
- No fuzzy matching, command history, or autocomplete
- No permission system or multi-user logic (yet)
- No write access or script execution
- No environment variables or piping

---

## 🛠️ Project Structure

- /
  - .idea/
  - docs/
    - env0.terminal_Contracts.md
    - env0.terminal_Milestones.md
    - env0.terminal_Overview.md
    - env0.terminal_Q&A.txt
    - env0.terminal_REFERENCE.md
    - env0.terminal_Test.Suite.md
    - logins.md
    - archive/
      - env0.terminal_tasklist.md
  - Env0.Terminal/
    - Env0.Terminal.csproj
    - Config/
      - AssemblyInfo.cs
      - JsonLoader.cs
      - Jsons/
        - BootConfig.json
        - Devices.json
        - UserConfig.json
        - JsonFilesystems/
          - Filesystem_1.json to Filesystem_11.json
      - Pocos/
        - BootConfig.cs
        - Devices.cs
        - FileEntry.cs
        - FileEntryConverter.cs
        - Filesystem.cs
        - UserConfig.cs
    - Filesystem/
      - FileEntryToFileSystemEntryConverter.cs
      - FilesystemManager.cs
    - Login/
      - LoginHandler.cs
      - LoginResultStatus.cs
      - SSHHandler.cs
    - Network/
      - NetworkManager.cs
    - Terminal/
      - CommandHandler.cs
      - CommandParser.cs
      - CommandResult.cs
      - ICommand.cs
      - SessionState.cs
      - TerminalStateManager.cs
      - Commands/
        - CatCommand.cs
        - CdCommand.cs
        - Clear.cs
        - EchoCommand.cs
        - ExitCommand.cs
        - HelpCommand.cs
        - IfconfigCommand.cs
        - LsCommand.cs
        - NmapCommand.cs
        - PingCommand.cs
        - ReadCommand.cs
        - SshCommand.cs
        - SudoCommand.cs
  - Env0.Terminal.Playground/
    - Env0.Terminal.Playground.csproj
    - Program.cs
  - Env0.Terminal.Tests/
    - Env0.Terminal.Tests.csproj
    - UnitTest1.cs
    - StandardTests/
      - CommandParserTests.cs
      - CommandSystemTests.cs
      - FilesystemManagerTests.cs
      - JsonLoaderTests.cs
      - StateManagerTests.cs
      - CommandsTests/
        - CatCommandsTests.cs
        - CdCommandTests.cs
        - EchoCommandTests.cs
        - ExitCommandTests.cs
        - HelpCommandTests.cs
        - IfconfigCommandTests.cs
        - LsCommandTests.cs
        - NmapCommandTests.cs
        - PingCommandTests.cs
        - ReadCommandTests.cs
        - SshCommandTests.cs
        - SudoCommandTests.cs
    - PsychoticTests/
      - CommandParser_PsychoticBrody.cs
      - CommandsTests_Psychotic.cs
      - FilesystemManager_PsychoticBrody.cs
      - JsonLoader_PsychoticBrody.cs
      - StateManager_PsychoticBrody.cs
    - FilesystemManagerHostileUserTests.cs
  - .gitignore
  - env0.terminal.sln
  - env0.terminal.code-workspace
  - README.md

---

## 📄 License

Copyright © Ewan Matheson  
MIT License

---

**For all implementation rules and edge cases, `REFERENCE.md` and `Contracts.md` are canonical. When in doubt, check there or ask.**
