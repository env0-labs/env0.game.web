using System;
using System.Collections.Generic;
using System.Linq;
using Env0.Terminal.Config.Pocos;

namespace Env0.Terminal.Terminal.Commands
{
    /// <summary>
    /// Processes a single unit from /queue/in into a sealed container in /queue/out.
    /// This is the "job" loop for the Processing terminal.
    /// </summary>
    public class ProcessCommand : ICommand
    {
        private const string InPath = "/queue/in";
        private const string OutPath = "/queue/out";
        private const string ExceptionsPath = "/queue/exceptions";
        private const string LogPath = "/log/processing.log";

        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            if (session?.FilesystemManager == null)
                return new CommandResult("process: Filesystem not initialized.\n\n", OutputType.Error);

            // Ensure shift id exists.
            if (string.IsNullOrWhiteSpace(session.ShiftId))
                session.ShiftId = $"SHIFT-{DateTime.UtcNow:yyyyMMdd}-A";

            if (!session.FilesystemManager.TryGetDirectory(InPath, out var inDir, out var inErr) || inDir == null)
                return new CommandResult($"process: {inErr}\n\n", OutputType.Error);

            if (inDir.Children == null || inDir.Children.Count == 0)
            {
                result.AddLine("process: no units in /queue/in.\n", OutputType.Standard);
                result.AddLine("process: you may idle. (Or pretend you are idling.)\n\n", OutputType.Standard);
                return result;
            }

            // Pick first file in stable order.
            var nextName = inDir.Children.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(nextName))
                return new CommandResult("process: queue unreadable.\n\n", OutputType.Error);

            var unit = inDir.Children[nextName];
            if (unit == null || unit.IsDirectory)
                return new CommandResult("process: malformed unit encountered. Routed to exceptions.\n\n", OutputType.Error);

            // Very light "heuristics": any unit file that contains forbidden words goes to exceptions.
            var content = unit.Content ?? string.Empty;
            var forbidden = new[] { "door", "sky", "outie", "name", "help" };
            var tripped = forbidden.FirstOrDefault(w => content.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrWhiteSpace(tripped))
            {
                RouteToExceptions(session, inDir, unit, nextName, $"tripped:{tripped}");
                session.ProcessedUnits++;
                AppendLog(session, $"[EX] {nextName} -> exceptions (reason={tripped})");

                result.AddLine($"process: routed {nextName} to /queue/exceptions\n", OutputType.Standard);
                result.AddLine("process: good catch. great instincts. please do not develop a personality.\n\n", OutputType.Standard);
                return result;
            }

            // Normal path: create sealed container in /queue/out.
            if (!session.FilesystemManager.TryGetDirectory(OutPath, out var outDir, out var outErr) || outDir == null)
                return new CommandResult($"process: {outErr}\n\n", OutputType.Error);

            var containerName = $"container_{Sanitize(nextName)}.sealed";
            if (outDir.Children == null)
                outDir.Children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

            var container = new FileEntry
            {
                Type = "file",
                Content = $"SEALED CONTAINER\nSHIFT={session.ShiftId}\nUNIT={nextName}\nSTATUS=SEALED\n\nDo not open.\n"
            };
            container.Name = containerName;
            container.Parent = outDir;

            // Remove unit from /queue/in and create container.
            inDir.Children.Remove(nextName);
            outDir.Children[containerName] = container;

            session.ProcessedUnits++;
            AppendLog(session, $"[OK] {nextName} -> {containerName}");

            result.AddLine($"process: sealed {nextName} into {containerName}\n", OutputType.Standard);
            result.AddLine("process: remember: the work is mysterious and important.\n\n", OutputType.Standard);
            return result;
        }

        private static void RouteToExceptions(SessionState session, FileEntry inDir, FileEntry unit, string unitName, string reason)
        {
            if (!session.FilesystemManager.TryGetDirectory(ExceptionsPath, out var exDir, out _))
                return;

            if (exDir.Children == null)
                exDir.Children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

            inDir.Children.Remove(unitName);

            var renamed = $"{Sanitize(unitName)}.ex";
            unit.Name = renamed;
            unit.Parent = exDir;
            unit.Content = (unit.Content ?? string.Empty) + $"\n\n[ROUTED TO EXCEPTIONS]\nREASON={reason}\n";
            exDir.Children[renamed] = unit;
        }

        private static void AppendLog(SessionState session, string line)
        {
            if (!session.FilesystemManager.TryGetEntry(LogPath, out var log, out _))
                return;
            if (log == null || log.IsDirectory)
                return;

            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            log.Content = (log.Content ?? string.Empty) + $"\n[{stamp}] {line}";
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "unit";
            // keep simple: letters/digits/_/- only
            var chars = name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            return new string(chars);
        }
    }
}
