using Env0.Terminal.Terminal;
using Env0.Terminal.Filesystem;

namespace Env0.Terminal.Terminal.Commands
{
    public class CatCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            // Defensive: session and FilesystemManager must be present
            if (session?.FilesystemManager == null)
            {
                result.AddLine("bash: cat: Filesystem not initialized.\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }

            // Argument required
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                result.AddLine("bash: cat: No file specified.\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }

            // Only one file at a time per contract (ignore any additional args)
            string target = args[0].Trim();
            if (!session.FilesystemManager.GetFileContent(target, out var content, out var error))
            {
                result.AddLine($"bash: cat: {error}\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }

            // Success output (the file content), assume Standard output
            result.AddLine(content, OutputType.Standard);
            return result;
        }
    }
}