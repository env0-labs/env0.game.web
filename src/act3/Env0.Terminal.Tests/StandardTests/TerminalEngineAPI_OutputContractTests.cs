using System.Linq;
using Xunit;
using Env0.Terminal;
using Env0.Terminal.API_DTOs;
using Env0.Terminal.Terminal;

namespace Env0.Terminal.Tests
{
    public class TerminalEngineAPI_OutputContractTests
    {
        [Fact]
        public void OutputLines_AggregateIntoOutput_ForCommandResults()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            var state = api.Execute("help");

            Assert.NotNull(state.OutputLines);
            Assert.True(state.OutputLines.Count > 0);
            var joined = string.Join("\n", state.OutputLines.Select(l => l.Text));
            Assert.Equal(joined, state.Output);
        }

        [Fact]
        public void WhitespaceCommand_IsNoOp_WithEmptyOutput()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            var state = api.Execute("   ");

            Assert.Equal(TerminalPhase.Terminal, state.Phase);
            Assert.Equal(string.Empty, state.Output);
            Assert.NotNull(state.OutputLines);
            Assert.Empty(state.OutputLines);
        }

        [Fact]
        public void InvalidCommand_UsesErrorOutputLines()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            var state = api.Execute("ld");

            Assert.True(state.IsError);
            Assert.NotNull(state.OutputLines);
            Assert.True(state.OutputLines.Count > 0);
            Assert.Contains(state.OutputLines, l => l.Type == OutputType.Error);
        }
    }
}
