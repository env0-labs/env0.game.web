using Env0.Terminal.Terminal;
using System.Collections.Generic;
using System.Linq;

namespace Env0.Terminal.Terminal.Commands
{
    public class PingCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            if (session?.NetworkManager == null)
            {
                result.AddLine("ping: Network not initialized.\n", OutputType.Error);
                return result;
            }

            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                result.AddLine("ping: Missing hostname or IP.\n", OutputType.Error);
                return result;
            }

            string target = args[0].Trim();
            var device = session.NetworkManager.FindDevice(target);

            if (device == null)
            {
                result.AddLine($"ping: {target}: Host unreachable\n", OutputType.Error);
                return result;
            }

            // Optional: parse count from args (e.g., "ping -c 2 host")
            int packetCount = 4;
            if (args.Length > 2 && args[0] == "-c" && int.TryParse(args[1], out var c))
            {
                packetCount = c;
                target = args[2];
            }

            var results = session.NetworkManager.Ping(device, packetCount);

            foreach (var res in results)
            {
                if (res.Dropped)
                {
                    result.AddLine($"Request timeout for icmp_seq {res.Sequence}", OutputType.Ping);
                }
                else
                {
                    result.AddLine($"Reply from {device.Ip}: icmp_seq={res.Sequence} ttl={res.Ttl} time={res.TimeMs} ms", OutputType.Ping);
                }
            }

            result.AddLine($"\n--- {device.Hostname} ping statistics ---", OutputType.Ping);

            int received = results.Count(r => !r.Dropped);
            int lost = results.Count(r => r.Dropped);
            double lossPercent = lost / (double)results.Count * 100;

            result.AddLine($"{results.Count} packets transmitted, {received} received, {lossPercent:0.#}% packet loss", OutputType.Ping);

            return result;
        }
    }
}
