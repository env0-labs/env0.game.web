using Xunit;
using Env0.Terminal.Terminal; // Make sure this matches your main project namespace

[Trait("TestType", "Critical")]
public class TerminalStateManagerTests
{
    [Fact]
    public void InitialState_IsBoot()
    {
        var sm = new TerminalStateManager();
        Assert.Equal(TerminalState.Boot, sm.CurrentState);
    }

    [Fact]
    public void Transition_BootToLogin_Succeeds()
    {
        var sm = new TerminalStateManager();
        sm.TransitionTo(TerminalState.Login);
        Assert.Equal(TerminalState.Login, sm.CurrentState);
    }

    [Fact]
    public void Transition_LoginToShell_Succeeds()
    {
        var sm = new TerminalStateManager();
        sm.TransitionTo(TerminalState.Login);
        sm.TransitionTo(TerminalState.Shell);
        Assert.Equal(TerminalState.Shell, sm.CurrentState);
    }
}
