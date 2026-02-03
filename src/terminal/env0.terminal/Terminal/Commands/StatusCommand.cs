using System;
using System.Linq;
using Env0.Terminal.Terminal.Progress;

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
            var sealedThisSession = session.SealedUnits;
            var exThisSession = session.ExceptionUnits;

            // Provide a visual hook: total work includes what's left + what's already done this shift.
            int totalSeen = Math.Max(1, inCount + sealedThisSession + exThisSession);

            var queueBar = AsciiMeter.Bar(totalSeen - inCount, totalSeen, width: 16);
            var outBar = AsciiMeter.Bar(sealedThisSession, totalSeen, width: 16);
            var exBar = AsciiMeter.Bar(exThisSession, totalSeen, width: 16, fill: '!', empty: '-');

            // Dumb but satisfying: a compliance "score" that goes down if you're routing a lot to exceptions.
            int score = 100;
            if (processed > 0)
            {
                var exRate = (int)Math.Round((double)exThisSession / processed * 100);
                score = Math.Max(0, 100 - exRate);
            }

            var text =
                $"STATUS // {session.Hostname}\n" +
                $"Shift: {(string.IsNullOrWhiteSpace(session.ShiftId) ? "UNASSIGNED" : session.ShiftId)}\n" +
                $"Objective: empty /queue/in -> /queue/out\n" +
                $"\n" +
                $"QUEUE  {queueBar}  {totalSeen - inCount}/{totalSeen} progressed\n" +
                $"OUT    {outBar}  {sealedThisSession} sealed\n" +
                $"EXCEPT {exBar}  {exThisSession} flagged\n" +
                $"\n" +
                $"M&C SCORE: {score}% (higher is more compliant)\n" +
                $"\n" +
                $"/queue/in: {inCount} | /queue/out: {outCount} | /queue/exceptions: {exCount}\n\n";

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
