namespace Env0.Core;

public sealed class SessionState
{
    public bool IsComplete { get; set; }
    public ContextRoute NextContext { get; set; }
}

