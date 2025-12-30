using Xunit;
using Env0.Terminal;
using Env0.Terminal.API_DTOs;

namespace Env0.Terminal.Tests
{
    public class TerminalEngineAPI_SshAbortTests
    {
        [Fact]
        public void SshLogin_AbortAtUsername_ReturnsToTerminal()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            var state = api.Execute("ssh workstation2.node.zero");
            Assert.Equal(TerminalPhase.Login, state.Phase);
            Assert.True(state.IsLoginPrompt);

            state = api.Execute("abort");
            Assert.Equal(TerminalPhase.Terminal, state.Phase);
            Assert.Contains("SSH login aborted", state.Output);
        }

        [Fact]
        public void SshLogin_AbortAtPassword_ReturnsToTerminal()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            var state = api.Execute("ssh admin@workstation2.node.zero");
            Assert.Equal(TerminalPhase.Login, state.Phase);
            Assert.True(state.IsPasswordPrompt);

            state = api.Execute("abort");
            Assert.Equal(TerminalPhase.Terminal, state.Phase);
            Assert.Contains("SSH login aborted", state.Output);
        }
    }
}
