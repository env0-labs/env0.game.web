using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;
using Env0.Terminal.Config.Pocos;
using System.Collections.Generic;

namespace Env0.Terminal.Tests.Commands
{
    public class IfconfigCommandTests
    {
        [Fact]
        public void IfconfigCommand_ListsInterfaces()
        {
            var command = new IfconfigCommand();
            var session = new SessionState
            {
                DeviceInfo = new DeviceInfo
                {
                    Interfaces = new List<DeviceInterface>
                    {
                        new DeviceInterface { Name = "eth0", Ip = "10.10.10.99", Subnet = "255.255.255.0", Mac = "aa:bb:cc:dd:ee:ff" }
                    }
                }
            };

            var result = command.Execute(session, new string[0]);

            Assert.NotNull(result);
            Assert.Contains("eth0", result.Output);
        }
    }
}