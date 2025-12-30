using Env0.Terminal.Terminal;

namespace Env0.Terminal.Terminal.Commands
{
    public class EchoCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            // Join all args with space, even if empty
            string output = args == null || args.Length == 0
                ? ""
                : string.Join(" ", args);

            // Return as Standard output explicitly
            return new CommandResult(output, OutputType.Standard);
        }
    }
}