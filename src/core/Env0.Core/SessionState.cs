namespace Env0.Core;

public sealed class SessionState
{
    public bool IsComplete { get; set; }
    public ActRoute NextAct { get; set; }
}
