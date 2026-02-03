namespace Runner.Avalonia;

public sealed class TerminalInputModel
{
    public string Text { get; set; } = string.Empty;
    public int Caret { get; set; }

    public void Clear()
    {
        Text = string.Empty;
        Caret = 0;
    }
}
