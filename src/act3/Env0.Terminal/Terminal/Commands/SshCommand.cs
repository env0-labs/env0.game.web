using Env0.Terminal.Terminal;
using System;

namespace Env0.Terminal.Terminal.Commands
{
    public class SshCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            // Contract: Only API is allowed to mutate session, stack, or manage SSH flow.
            // Here, we ONLY validate arguments and give canonical SSH command output if obviously malformed.

            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                return new CommandResult("bash: ssh: Missing host\n\n", OutputType.Error);
            }

            string user = null, host = null;
            var target = args[0].Trim();
            if (target.Contains("@"))
            {
                var split = target.Split('@');
                user = split[0];
                host = split[1];
            }
            else
            {
                host = target;
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                return new CommandResult("bash: ssh: Invalid or missing host\n\n", OutputType.Error);
            }

            // All actual SSH logic (including device lookup, login, stack push, prompt management) is handled in TerminalEngineAPI.
            // We just return "ok" to signal to API to take over.

            return new CommandResult(string.Empty, OutputType.Auth); // Explicit for compiler
        }
    }
}