using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;

namespace Env0.Terminal.Tests.Commands
{
    public class HelpCommandTests
    {
        [Fact]
        public void HelpCommand_ReturnsComprehensiveHelp()
        {
            var command = new HelpCommand();
            var session = new SessionState();

            var result = command.Execute(session, new string[0]);

            Assert.NotNull(result);
            Assert.Contains("available commands", result.Output.ToLower());
        }
    }
}