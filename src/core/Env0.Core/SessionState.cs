namespace Env0.Core;

public sealed class SessionState
{
    public bool IsComplete { get; set; }
    public ContextRoute NextContext { get; set; }
    public ContextRoute TerminalReturnContext { get; set; } = ContextRoute.None;
    public string? TerminalStartFilesystem { get; set; }
    public string? RecordsReturnSceneId { get; set; }
    public bool ResumeRecords { get; set; }
}

