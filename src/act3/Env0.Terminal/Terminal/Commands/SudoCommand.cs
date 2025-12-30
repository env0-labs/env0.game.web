using System;
using Env0.Terminal.Terminal;

namespace Env0.Terminal.Terminal.Commands
{
    public class SudoCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            // Deterministic response keeps tests stable and matches contract expectation.
            return new CommandResult("Nice try.\n", OutputType.Error);
        }
    }
}
