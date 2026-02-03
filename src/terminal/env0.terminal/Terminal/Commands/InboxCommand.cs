using System;
using System.Linq;
using Env0.Terminal.Filesystem;

namespace Env0.Terminal.Terminal.Commands
{
    /// <summary>
    /// Lists short "corporate inbox" messages.
    /// Purely local: backed by files under /mail/inbox.
    /// </summary>
    public class InboxCommand : ICommand
    {
        private const string InboxPath = "/mail/inbox";

        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            if (session?.FilesystemManager == null)
                return new CommandResult("bash: inbox: Filesystem not initialized.\n\n", OutputType.Error);

            if (!session.FilesystemManager.TryGetEntry(InboxPath, out var inbox, out var error) || inbox == null)
            {
                result.AddLine($"inbox: {error}\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }

            if (!inbox.IsDirectory)
            {
                result.AddLine("inbox: invalid mailbox.\n", OutputType.Error);
                result.AddLine(string.Empty, OutputType.Error);
                return result;
            }

            if (inbox.Children == null || inbox.Children.Count == 0)
            {
                result.AddLine("Inbox: (empty)\n", OutputType.Standard);
                result.AddLine(string.Empty, OutputType.Standard);
                return result;
            }

            // Stable-ish order: alphabetical by filename.
            foreach (var kvp in inbox.Children.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var entry = kvp.Value;
                if (entry == null || entry.IsDirectory)
                    continue;

                var content = entry.Content ?? string.Empty;
                var firstLine = content
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))
                    ?.Trim();

                // Simple one-line listing.
                if (string.IsNullOrWhiteSpace(firstLine)) firstLine = "(no subject)";
                result.AddLine($"- {entry.Name}: {firstLine}\n", OutputType.Standard);
            }

            result.AddLine(string.Empty, OutputType.Standard);
            return result;
        }
    }
}
