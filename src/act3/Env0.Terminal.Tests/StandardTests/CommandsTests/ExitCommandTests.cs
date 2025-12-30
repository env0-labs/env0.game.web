using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;

namespace Env0.Terminal.Tests.Commands
{
    public class ExitCommandTests
    {
        [Fact]
        public void ExitCommand_AtRoot_ReturnsLogoutNotImplemented()
        {
            var command = new ExitCommand();
            var session = new SessionState(); // At root by default

            var result = command.Execute(session, new string[0]);

            Assert.NotNull(result);
            Assert.Contains("logout: not implemented", result.Output.ToLower());
        }
    }
}