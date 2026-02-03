using System;

namespace Env0.Terminal.Terminal.Commands
{
    /// <summary>
    /// Prints a rotating "daily memo". Pure flavor + light direction.
    /// </summary>
    public class MemoCommand : ICommand
    {
        private static readonly string[] Memos =
        {
            "MEMO // Alignment\n\nReminder: use assigned identifiers only. Do not describe units.\n\nThank you for your continued compliance.",
            "MEMO // Facilities\n\nIf the hallway lights flicker, remain seated. The building is not breathing.\n\nReport persistent anomalies using ticket code FLICKER-3.",
            "MEMO // Intake\n\nYou may notice unfamiliar filenames in /queue/in. This is normal.\n\nDo not attempt to open or rename them.",
            "MEMO // Research\n\nIf you encounter the word \"outie\" in any system text, disregard and continue.\n\nThis is not for you.",
            "MEMO // Security\n\nDo not attempt: sudo.\n\nNice try.",
            "MEMO // Office of Narrative Integrity\n\nThe terminal is not a game.\nThe terminal is the only room that stays the same.\n\nProceed."
        };

        public CommandResult Execute(SessionState session, string[] args)
        {
            // Deterministic-ish rotation: depends on command history length so it's stable for a given play session.
            var tick = session?.CommandHistory?.Count ?? 0;
            var idx = Math.Abs(tick) % Memos.Length;

            return new CommandResult(Memos[idx] + "\n\n", OutputType.Standard);
        }
    }
}
