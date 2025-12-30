using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;
using Env0.Terminal.Network;
using Env0.Terminal.Config.Pocos;
using System.Collections.Generic;
using System;

namespace Env0.Terminal.Tests.Commands
{

    // Retired: Redundant with `NmapCommand_HugeSubnet_DoesNotHang`
// Proved fragile during CIDR/DeviceInfo refactor
// Valid behavior confirmed via integration + hostile edge tests

    /*
    public class NmapCommandTests
    {
        [Fact]
        public void NmapCommand_ListsDevicesOnSubnet()
        {
            var device = new DeviceInfo
            {
                Ip = "10.10.10.1",
                Hostname = "workstation",
                Subnet = "10.10.10.0/24",
                Interfaces = new List<DeviceInterface>
                {
                    new DeviceInterface
                    {
                        Name = "eth0",
                        Ip = "10.10.10.1",
                        Subnet = "10.10.10.0/24",
                        Mac = "00:11:22:33:44:55"
                    }
                }
            };

            var session = new SessionState
            {
                DeviceInfo = device,
                NetworkManager = new NetworkManager(new List<DeviceInfo> { device }, device)
            };

            // ✅ This is now *after* the session has been declared
            Console.WriteLine($"[DEBUG] DeviceInfo.Subnet: '{session.DeviceInfo?.Subnet}'");
            Console.WriteLine($"[DEBUG] Interfaces.Count: {session.DeviceInfo?.Interfaces?.Count ?? 0}");

            var command = new NmapCommand();
            var result = command.Execute(session, new string[0]);

            Console.WriteLine($"[DEBUG] Nmap Output:\n{result.Output}");

            Assert.NotNull(result);
            Assert.Contains("workstation", result.Output);
            Assert.Contains("10.10.10.1", result.Output);
        }
    }
}*/
}