using System;
using Env0.Terminal.Config.Pocos;
using System.Net;
using System.Collections.Generic;
using System.Linq;


namespace Env0.Terminal.Network
{
    // Handles device lookups, nmap, ping, and interface listings for current device/network.
    public class NetworkManager
    {
        private readonly List<DeviceInfo> _devices;

        // Optionally: Track current device context (for SSH hopping, etc.)
        public DeviceInfo CurrentDevice { get; set; }

        public NetworkManager(List<DeviceInfo> devices, DeviceInfo initialDevice)
        {
            _devices = devices ?? throw new ArgumentNullException(nameof(devices));
            CurrentDevice = initialDevice ?? throw new ArgumentNullException(nameof(initialDevice));
        }

        // Lookup device by IP
        public DeviceInfo GetDeviceByIp(string ip)
        {
            return _devices.FirstOrDefault(d => string.Equals(d.Ip, ip, StringComparison.OrdinalIgnoreCase));
        }

        // Lookup device by hostname
        public DeviceInfo GetDeviceByHostname(string hostname)
        {
            return _devices.FirstOrDefault(d => string.Equals(d.Hostname, hostname, StringComparison.OrdinalIgnoreCase));
        }

        //Subnet/CIDR matching helper
        public static bool IsIpInSubnet(string ipString, string cidr)
        {
            var ip = IPAddress.Parse(ipString);
            var parts = cidr.Split('/');
            if (parts.Length != 2) return false;

            var network = IPAddress.Parse(parts[0]);
            int maskLength = int.Parse(parts[1]);

            var ipBytes = ip.GetAddressBytes();
            var networkBytes = network.GetAddressBytes();

            int bits = maskLength;
            for (int i = 0; i < ipBytes.Length; i++)
            {
                int mask = bits >= 8 ? 255 : (bits <= 0 ? 0 : 256 - (1 << (8 - bits)));
                if ((ipBytes[i] & mask) != (networkBytes[i] & mask))
                    return false;
                bits -= 8;
            }
            return true;
        }
        
        // Return all devices on the current subnet (simulate nmap)
        public List<DeviceInfo> GetDevicesOnSubnet(string cidr)
        {
            return _devices.Where(d =>
                d.Interfaces != null && d.Interfaces.Any(iface => IsIpInSubnet(iface.Ip, cidr))
            ).ToList();
        }


        // nmap: Show all devices visible from the current device's network context
        public List<NmapResult> Nmap()
        {
            // Assume CurrentDevice has Subnet info
            string subnet = CurrentDevice?.Subnet ?? "default";
            var foundDevices = GetDevicesOnSubnet(subnet);

            var results = new List<NmapResult>();
            foreach (var device in foundDevices)
            {
                results.Add(new NmapResult
                {
                    Ip = device.Ip,
                    Hostname = device.Hostname,
                    Description = string.IsNullOrEmpty(device.Description) ? "No description available." : device.Description,
                    Ports = device.Ports ?? new List<string>()
                });
            }
            return results;
        }

        public IReadOnlyList<DeviceInfo> GetAllDevices()
        {
            return _devices.AsReadOnly();
        }

        
        // ifconfig: List all interfaces for the current device
        public List<DeviceInterface> ListInterfaces()
        {
            return CurrentDevice?.Interfaces ?? new List<DeviceInterface>();
        }

        // ping: Simulate ping with randomized TTL, delay, and possible packet loss
        public List<PingResult> Ping(DeviceInfo targetDevice, int packetCount = 4)
        {
            var results = new List<PingResult>();
            var rand = new Random();

            for (int i = 1; i <= packetCount; i++)
            {
                bool dropped = rand.NextDouble() < 0.15; // 15% packet loss
                int ttl = rand.Next(50, 70);             // Random TTL for realism
                double ms = Math.Round(rand.NextDouble() * 100, 1); // up to 100ms

                results.Add(new PingResult
                {
                    Sequence = i,
                    Dropped = dropped,
                    Ttl = ttl,
                    TimeMs = ms
                });
            }
            return results;
        }

        // Lookup device by IP or hostname (for ping, ssh, etc.)
        public DeviceInfo FindDevice(string ipOrHostname)
        {
            if (string.IsNullOrWhiteSpace(ipOrHostname))
                return null;

            // Try IP first
            var device = GetDeviceByIp(ipOrHostname);
            if (device != null)
                return device;

            // If not found by IP, try hostname
            return GetDeviceByHostname(ipOrHostname);
        }

        
        // --- Models for nmap/ping output (can be moved elsewhere) ---
        public class NmapResult
        {
            public string Ip { get; set; }
            public string Hostname { get; set; }
            public string Description { get; set; }
            public List<string> Ports { get; set; }
        }

        public class PingResult
        {
            public int Sequence { get; set; }
            public bool Dropped { get; set; }
            public int Ttl { get; set; }
            public double TimeMs { get; set; }
        }
    }
}
