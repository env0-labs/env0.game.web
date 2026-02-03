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
                result.AddLine("process: check 'inbox' for new directives.\n\n", OutputType.Standard);
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

            // Special units are rare drops. They still "process", but they also change the world.
            bool isSpecial = nextName.IndexOf("U-00020", StringComparison.OrdinalIgnoreCase) >= 0
                             || nextName.IndexOf("U-00021", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!string.IsNullOrWhiteSpace(tripped))
            {
                RouteToExceptions(session, inDir, unit, nextName, $"tripped:{tripped}");
                session.ProcessedUnits++;
                session.ExceptionUnits++;
                AppendLog(session, $"[EX] {nextName} -> exceptions (reason={tripped})");

                result.AddLine($"[EX] ROUTED   {nextName} -> /queue/exceptions\n", OutputType.Standard);
                result.AddLine(Quip(nextName, ok: false) + "\n\n", OutputType.Standard);

                // When the queue empties, something always happens.
                if (inDir.Children.Count == 0)
                    OnQueueEmpty(session);

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
            session.SealedUnits++;
            AppendLog(session, $"[OK] {nextName} -> {containerName}");

            result.AddLine($"[OK] SEALED   {nextName} -> {containerName}\n", OutputType.Standard);

            // Rare drops: inject extra "something happened" moments.
            if (isSpecial)
            {
                TriggerSpecial(session, nextName);
                result.AddLine("M&C NOTE: Congratulations. You have been selected for additional compliance.\n", OutputType.Standard);
            }
            else
            {
                result.AddLine(Quip(nextName, ok: true) + "\n", OutputType.Standard);
            }

            result.AddLine(string.Empty, OutputType.Standard);

            // When the queue empties, something always happens.
            if (inDir.Children.Count == 0)
                OnQueueEmpty(session);

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

        private static string Quip(string unitName, bool ok)
        {
            // Deterministic, Fallout-y corporate one-liners.
            var poolOk = new[]
            {
                "M&C NOTE: Nice. Don't make it weird.",
                "M&C NOTE: Great job. Please stop being great.",
                "M&C NOTE: You are within acceptable human variance.",
                "M&C NOTE: Productivity detected. Concerning.",
                "M&C NOTE: Remember: curiosity is a resource leak.",
            };

            var poolEx = new[]
            {
                "M&C NOTE: Excellent catch. Now forget it happened.",
                "M&C NOTE: You flagged an anomaly. Do not bond with it.",
                "M&C NOTE: That's an exceptions problem. Congratulations: it's still your problem.",
                "M&C NOTE: Thank you for your honesty. It has been recorded.",
                "M&C NOTE: We noticed you noticing.",
            };

            var pool = ok ? poolOk : poolEx;
            var h = StableHash(unitName ?? string.Empty);
            return pool[Math.Abs(h) % pool.Length];
        }

        private static void TriggerSpecial(SessionState session, string unitName)
        {
            // Special units: file mutations + immediate "something happened".
            // Keep it lightweight and reversible.
            if (session?.FilesystemManager == null) return;

            // Drop a note into inbox.
            if (session.FilesystemManager.TryGetEntry("/mail/inbox", out var inbox, out _) && inbox != null && inbox.IsDirectory)
            {
                if (inbox.Children == null)
                    inbox.Children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

                var msgName = $"{Sanitize(unitName)}_mc.msg";
                inbox.Children[msgName] = new FileEntry
                {
                    Type = "file",
                    Content = $"SUBJECT: Monitoring Event ({unitName})\n\nWe detected an operator interaction with a Special Unit.\n\nThis is normal.\nThis is also not normal.\n\n- Monitoring & Compliance\n"
                };
            }

            // Lightly adjust the log (feels like the system is paying attention).
            AppendLog(session, $"[MC] special unit observed: {unitName}");
        }

        private static void OnQueueEmpty(SessionState session)
        {
            if (session?.FilesystemManager == null) return;

            // When you finish a micro-shift, immediately nudge the player: check inbox.
            if (session.FilesystemManager.TryGetEntry("/mail/inbox", out var inbox, out _) && inbox != null && inbox.IsDirectory)
            {
                if (inbox.Children == null)
                    inbox.Children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

                var name = $"shift_complete_{DateTime.UtcNow:HHmmss}.msg";
                inbox.Children[name] = new FileEntry
                {
                    Type = "file",
                    Content = "SUBJECT: Shift complete (micro)\n\nGreat news: you are done.\nBad news: you are still here.\n\nNext: check 'status', then check 'inbox'.\n\n- Department for Change\n"
                };
            }
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < s.Length; i++)
                    hash = (hash * 31) + s[i];
                return hash;
            }
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
