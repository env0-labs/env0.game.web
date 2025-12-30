using System.Collections.Generic;


namespace Env0.Terminal.Config.Pocos
{
    public class DeviceInterface
    {
        public string Name { get; set; } = "";
        public string Ip { get; set; } = "";
        public string Subnet { get; set; } = "";
        public string Mac { get; set; } = "";
    }

    public class DeviceInfo
    {
        public string Ip { get; set; } = "";
        public string Subnet { get; set; } = "";
        public string Hostname { get; set; } = "";
        public string Mac { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Filesystem { get; set; } = "";
        public string Motd { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Ports { get; set; } = new List<string>();
        public List<DeviceInterface> Interfaces { get; set; } = new List<DeviceInterface>();
    }
}
