namespace Env0.Terminal.Terminal
{
    public class TerminalOutputLine
    {
        public string Text { get; set; }
        public OutputType Type { get; set; }

        public TerminalOutputLine(string text, OutputType type)
        {
            Text = text;
            Type = type;
        }
    }
}