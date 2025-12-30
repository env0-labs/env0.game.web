using System;
using System.Linq;
using Env0.Terminal.Terminal;
using Env0.Terminal.Network;
using Env0.Terminal.Config.Pocos;

namespace Env0.Terminal.Terminal.Commands
{
    public class NmapCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            if (session?.NetworkManager == null || session.DeviceInfo == null)
            {
                result.AddLine("bash: nmap: network or device not initialized\n", OutputType.Error);
                return result;
            }

            // No arguments: print bash error
            if (args.Length == 0)
            {
                result.AddLine("bash: nmap: target or subnet required\n", OutputType.Error);
                return result;
            }

            var target = args[0].Trim();

            // If target is CIDR (only /24 for now)
            if (IsValidCidr(target))
            {
                var devices = session.NetworkManager.GetDevicesOnSubnet(target);
                if (devices == null || devices.Count == 0)
                {
                    result.AddLine("No devices found on the subnet.\n", OutputType.Scan);
                    return result;
                }

                var output = string.Join("\n", devices.Select(d =>
                    $"{d.Ip,-15} {d.Hostname,-25} [Ports: {string.Join(", ", d.Ports)}] {d.Description}"
                ));
                result.AddLine($"Starting Nmap scan for subnet {target}...\n", OutputType.Scan);
                result.AddLine(output, OutputType.Scan);
                return result;
            }

            // Host/IP lookup
            var device = session.NetworkManager.FindDevice(target);
            if (device != null)
            {
                var output = $"{device.Ip,-15} {device.Hostname,-25} [Ports: {string.Join(", ", device.Ports)}] {device.Description}";
                result.AddLine($"Starting Nmap scan for host {target}...\n", OutputType.Scan);
                result.AddLine(output, OutputType.Scan);
                return result;
            }

            // If it looks like a subnet (contains /), but is not valid
            if (target.Contains("/"))
            {
                result.AddLine("bash: nmap: invalid subnet format\n", OutputType.Error);
                return result;
            }

            // Otherwise, host not found
            result.AddLine("bash: nmap: host not found\n", OutputType.Error);
            return result;
        }

        // Only /24 support for now
        private bool IsValidCidr(string input)
        {
            var parts = input.Split('/');
            if (parts.Length != 2) return false;
            int mask;
            return System.Net.IPAddress.TryParse(parts[0], out _) && int.TryParse(parts[1], out mask) && mask == 24;
        }
    }
}
