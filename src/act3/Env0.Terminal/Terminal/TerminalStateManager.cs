namespace Env0.Terminal.Terminal
{
public enum TerminalState
{
    Boot,
    Login,
    Shell,
    Ssh
}

public class TerminalStateManager
{
    public TerminalState CurrentState { get; private set; } = TerminalState.Boot;

    public void TransitionTo(TerminalState newState)
    {
        // Add basic logic, maybe restrict illegal transitions later
        CurrentState = newState;
    }
}
}