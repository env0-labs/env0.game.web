using Env0.Terminal.Terminal;
using Env0.Terminal.Config.Pocos; // For DeviceInfo and NetworkInterfaceInfo
using System.Collections.Generic;

namespace Env0.Terminal.Terminal.Commands
{
    public class IfconfigCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            // Try to get device info from session, else output a stub
            var device = session?.DeviceInfo;

            if (device == null || device.Interfaces == null || device.Interfaces.Count == 0)
            {
                // Fallback: output stub info if nothing real
                string stub = @"
eth0:  inet 10.10.10.99  netmask 255.255.255.0  mac aa:bb:cc:dd:ee:ff
lo:    inet 127.0.0.1     netmask 255.0.0.0
".Trim();

                result.AddLine(stub, OutputType.Standard);
                return result;
            }

            // Otherwise, format actual interfaces from device info
            var lines = new List<string>();
            foreach (var iface in device.Interfaces)
            {
                lines.Add($"{iface.Name}:  inet {iface.Ip}  netmask {iface.Subnet}  mac {iface.Mac}");
            }
            result.AddLine(string.Join("\n", lines), OutputType.Standard);
            return result;
        }
    }
}