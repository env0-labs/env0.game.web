using System;
using System.Linq;
using Env0.Terminal.Config.Pocos;

namespace Env0.Terminal.Terminal.Commands
{
    public class StatusCommand : ICommand
    {
        private const string InPath = "/queue/in";
        private const string OutPath = "/queue/out";
        private const string ExceptionsPath = "/queue/exceptions";

        public CommandResult Execute(SessionState session, string[] args)
        {
            if (session?.FilesystemManager == null)
                return new CommandResult("status: Filesystem not initialized.\n\n", OutputType.Error);

            int inCount = CountFiles(session, InPath);
            int outCount = CountFiles(session, OutPath);
            int exCount = CountFiles(session, ExceptionsPath);

            var processed = session.ProcessedUnits;

            var text =
                $"STATUS // {session.Hostname}\n" +
                $"\n" +
                $"Shift: {(string.IsNullOrWhiteSpace(session.ShiftId) ? "UNASSIGNED" : session.ShiftId)}\n" +
                $"Processed (this session): {processed}\n" +
                $"\n" +
                $"Queue /in: {inCount}\n" +
                $"Queue /out: {outCount}\n" +
                $"Queue /exceptions: {exCount}\n\n";

            return new CommandResult(text, OutputType.Standard);
        }

        private static int CountFiles(SessionState session, string path)
        {
            if (!session.FilesystemManager.TryGetEntry(path, out var entry, out _))
                return 0;
            if (entry == null || !entry.IsDirectory || entry.Children == null)
                return 0;

            return entry.Children.Values.Count(v => v != null && !v.IsDirectory);
        }
    }
}
