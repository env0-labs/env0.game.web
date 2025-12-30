using System;
using System.Collections.Generic;
using Env0.Terminal.Terminal.Commands;

namespace Env0.Terminal.Terminal
{
    public class CommandHandler
    {
        private readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        public CommandHandler(bool debugMode)
        {
            // Register commands and aliases
            RegisterCommand("ls", new LsCommand());
            RegisterCommand("dir", new LsCommand()); // Alias
            RegisterCommand("cd", new CdCommand());
            RegisterCommand("cat", new CatCommand());
            RegisterCommand("read", new ReadCommand());
            RegisterCommand("echo", new EchoCommand());
            RegisterCommand("clear", new ClearCommand());
            RegisterCommand("ping", new PingCommand());
            RegisterCommand("nmap", new NmapCommand());
            RegisterCommand("ssh", new SshCommand());
            RegisterCommand("exit", new ExitCommand());
            RegisterCommand("sudo", new SudoCommand());
            RegisterCommand("help", new HelpCommand());
            RegisterCommand("ifconfig", new IfconfigCommand());

            //  TODO: once everything is stable we can look at implementing debug commands as they require full stack for operation. Await Milestone 1B succesful test output before implementation.
            /*// Debug-only commands
            if (debugMode)
            {
                RegisterCommand("show filesystems", new ShowFilesystemsCommand());
                RegisterCommand("list devices", new ListDevicesCommand());
                RegisterCommand("teleport", new TeleportCommand());
                RegisterCommand("clear debug", new ClearDebugCommand());
            }*/
        }

        private void RegisterCommand(string name, ICommand command)
        {
            if (!_commands.ContainsKey(name))
                _commands.Add(name, command);
        }

        public CommandResult Execute(string input, SessionState session)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new CommandResult(string.Empty, OutputType.Standard);

            var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0];
            var args = parts.Length > 1 ? parts[1].Split(' ') : Array.Empty<string>();

            if (_commands.TryGetValue(cmd, out var command))
            {
                return command.Execute(session, args);
            }
            return new CommandResult($"bash: {cmd}: command not found\n\n", OutputType.Error);
        }
    }
}
