using System;
using Env0.Terminal.Terminal;
using Env0.Terminal.Config.Pocos;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Network;
using Env0.Terminal.Config;

namespace Env0.Terminal.Login
{
    public class SSHHandler
    {
        private readonly NetworkManager _networkManager;

        public SSHHandler(NetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
        }

        /// <summary>
        /// Utility: Validate if a device (by host/IP) is SSH-able (has port 22 open and is defined).
        /// </summary>
        public DeviceInfo FindSshableDevice(string hostOrIp)
        {
            var device = _networkManager.FindDevice(hostOrIp);
            if (device == null || device.Ports == null || !device.Ports.Contains("22"))
                return null;
            return device;
        }

        /// <summary>
        /// Loads a FilesystemManager for the given device, using POCO-to-runtime conversion.
        /// This is now PUBLIC for use by the TerminalEngineAPI.
        /// </summary>
        public FilesystemManager LoadFilesystemForDevice(DeviceInfo device)
        {
            // Try main FS, then failsafe, else empty root
            if (!JsonLoader.Filesystems.TryGetValue(device.Filesystem, out var fsPoco) || fsPoco == null || fsPoco.Root == null)
            {
                if (!JsonLoader.Filesystems.TryGetValue("Filesystem_11.json", out fsPoco) || fsPoco == null || fsPoco.Root == null)
                {
                    var emptyRoot = new FileEntry
                    {
                        Type = "",
                        Content = null,
                        Children = new System.Collections.Generic.Dictionary<string, FileEntry>()
                    };
                    var emptyFsEntry = FileEntryToFileSystemEntryConverter.Convert("", emptyRoot, null);
                    return new FilesystemManager(emptyFsEntry);
                }
            }

            // Wrap POCO root in a FileEntry node, convert to FileEntry tree for runtime
            var pseudoRoot = new FileEntry
            {
                Type = "",
                Content = null,
                Children = fsPoco.Root
            };
            var fsEntryRoot = FileEntryToFileSystemEntryConverter.Convert("", pseudoRoot, null);

            return new FilesystemManager(fsEntryRoot);
        }
    }
}
