using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;

namespace Env0.Terminal.Tests.Commands
{
    public class EchoCommandTests
    {
        /// <summary>
        /// EchoCommand echoes input.
        /// </summary>
        [Fact]
        public void EchoCommand_EchoesInput()
        {
            var cmd = new EchoCommand();
            var session = new SessionState();
            var result = cmd.Execute(session, new[] { "hello", "world" });

            Assert.Contains("hello", result.Output);
            Assert.Contains("world", result.Output);
        }
    }
}