using Env0.Terminal.Terminal;
using Env0.Terminal.Filesystem;
using System;
using System.Collections.Generic;

namespace Env0.Terminal.Terminal.Commands
{
    public class CdCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            if (session?.FilesystemManager == null)
            {
                result.AddLine("bash: cd: Filesystem not initialized.\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }

            string targetDir = args.Length == 0 || string.IsNullOrWhiteSpace(args[0])
                ? "/" // Or session.HomeDirectory if implemented
                : args[0];

            // If targetDir is ".", treat as no-op using current directory
            if (targetDir == ".")
                targetDir = session.CurrentWorkingDirectory;

            if (session.FilesystemManager.ChangeDirectory(targetDir, out var error))
            {
                // Update session with the new absolute path
                session.CurrentWorkingDirectory = session.FilesystemManager.GetCurrentDirectoryPath();

                DebugUtility.PrintContext("CdCommand", $"cd success: now at {session.CurrentWorkingDirectory}");

                return result; // empty output on success
            }
            else
            {
                DebugUtility.PrintContext("CdCommand", $"cd failed: {error}");
                result.AddLine($"bash: cd: {error}\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }
        }
    }
}