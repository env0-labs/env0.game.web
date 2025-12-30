using Env0.Terminal.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Env0.Terminal.Terminal.Commands
{
    public class LsCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            var fsManager = session.FilesystemManager;
            string targetDir = args.Length == 0 || string.IsNullOrWhiteSpace(args[0])
                ? session.CurrentWorkingDirectory // USE CWD instead of "."
                : args[0];

            // If someone literally types ".", treat as CWD
            if (targetDir == ".")
                targetDir = session.CurrentWorkingDirectory;

            DebugUtility.PrintContext("LsCommand", $"Listing directory: '{targetDir}'");

            var result = new CommandResult();

            if (!fsManager.TryGetDirectory(targetDir, out var dir, out var error))
            {
                DebugUtility.PrintContext("LsCommand", $"TryGetDirectory failed: {error}");
                result.AddLine($"bash: ls: {error}\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error); // breathing room
                return result;
            }

            if (!dir.IsDirectory)
            {
                DebugUtility.PrintContext("LsCommand", $"Not a directory: {targetDir}");
                result.AddLine($"bash: ls: Not a directory: {targetDir}\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }

            if (dir.Children.Count == 0)
                return result; // empty output, no lines

            // List dirs first, then files (alpha order)
            var dirs = new List<string>();
            var files = new List<string>();
            foreach (var child in dir.Children.Values)
            {
                if (child.IsDirectory)
                    dirs.Add(child.Name);
                else
                    files.Add(child.Name);
            }

            dirs.Sort(StringComparer.OrdinalIgnoreCase);
            files.Sort(StringComparer.OrdinalIgnoreCase);

            var output = string.Join("  ", dirs.Concat(files));
            result.AddLine(output, OutputType.Standard);

            return result;
        }
    }
}
