using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;

namespace Env0.Terminal.Tests.Commands
{
    public class SudoCommandTests
    {
        /// <summary>
        /// SudoCommand always returns Easter egg "Nice try."
        /// </summary>
        [Fact]
        public void SudoCommand_AlwaysReturnsEasterEgg()
        {
            var cmd = new SudoCommand();
            var session = new SessionState();
            var result = cmd.Execute(session, new string[0]);

            Assert.Contains("Nice try", result.Output);
        }
    }
}