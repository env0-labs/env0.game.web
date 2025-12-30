using System.Collections.Generic;

namespace Env0.Terminal.Terminal
{
    public class CommandResult
    {
        // Structured output lines with type badges
        public List<TerminalOutputLine> OutputLines { get; set; } = new List<TerminalOutputLine>();

        // Legacy support: concatenated plain output string from OutputLines
        public string Output => OutputLines.Count > 0
            ? string.Join("\n", OutputLines.ConvertAll(l => l.Text))
            : string.Empty;

        // True if any OutputLine is an error
        public bool IsError => OutputLines.Exists(l => l.Type == OutputType.Error);

        // Pagination flag
        public bool RequiresPaging { get; set; } = false;

        // Indicates if the command caused a session state change
        public bool StateChanged { get; set; } = false;

        // Holds updated session state if any
        public SessionState UpdatedSession { get; set; }

        // Default constructor
        public CommandResult() { }

        // One-liner constructor with OutputType, defaulting to Standard
        public CommandResult(string text, OutputType type = OutputType.Standard)
        {
            OutputLines.Add(new TerminalOutputLine(text, type));
        }

        // Add a line with an explicit OutputType
        public void AddLine(string text, OutputType type = OutputType.Standard)
        {
            OutputLines.Add(new TerminalOutputLine(text, type));
        }
    }
}