using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Env0.Terminal.Config.Pocos;

namespace Env0.Terminal.Config
{
    public static class JsonLoader
    {
        // Loaded configs
        public static BootConfig BootConfig { get; private set; }
        public static UserConfig UserConfig { get; private set; }

        // Devices
        public static List<DeviceInfo> Devices { get; private set; } = new List<DeviceInfo>();

        // Filesystems (keyed by filename, e.g., "Filesystem_1.json")
        public static Dictionary<string, Env0.Terminal.Config.Pocos.Filesystem> Filesystems { get; private set; } = new Dictionary<string, Env0.Terminal.Config.Pocos.Filesystem>();

        // Validation errors (visible in debug)
        public static List<string> ValidationErrors { get; private set; } = new List<string>();

        private static string ResolvePath(params string[] pathOptions)
        {
            foreach (var path in pathOptions)
            {
                if (System.IO.File.Exists(path))
                    return path;
            }
            // If none found, return the first anyway (to fail with a clear error)
            return pathOptions.First();
        }

        public static void LoadAll()
        {
            ValidationErrors.Clear();

            // BootConfig
            BootConfig = LoadBootConfig(
                ResolvePath(
                    "Config/Jsons/BootConfig.json",
                    "Env0.Terminal/Config/Jsons/BootConfig.json"
                ),
                out var bootErrors
            );
            ValidationErrors.AddRange(bootErrors);

            // UserConfig
            UserConfig = LoadUserConfig(
                ResolvePath(
                    "Config/Jsons/UserConfig.json",
                    "Env0.Terminal/Config/Jsons/UserConfig.json"
                ),
                out var userErrors
            );
            ValidationErrors.AddRange(userErrors);

            // Devices
            Devices = LoadDevices(
                ResolvePath(
                    "Config/Jsons/Devices.json",
                    "Env0.Terminal/Config/Jsons/Devices.json"
                ),
                out var deviceErrors
            );
            ValidationErrors.AddRange(deviceErrors);

            // Filesystems 1-10
            for (int i = 1; i <= 10; i++)
            {
                var path = ResolvePath(
                    $"Config/Jsons/JsonFilesystems/Filesystem_{i}.json",
                    $"Env0.Terminal/Config/Jsons/JsonFilesystems/Filesystem_{i}.json"
                );

                // === Debug 1: Print raw JSON before deserialization ===
                if (File.Exists(path))
                {
                    var raw = File.ReadAllText(path);
                    DebugUtility.PrintContext("FS", $"==== RAW JSON FOR {path} ====");
                    DebugUtility.PrintContext("FS", raw);
                    DebugUtility.PrintContext("FS", "==================");
                }

                var fs = LoadFilesystem(path, out var fsErrors);

                // === Debug 2: Print fs.Root state after deserialization ===
                if (fs != null)
                {
                    DebugUtility.PrintContext("FS", $"fs.Root is null? {(fs.Root == null)}");
                    if (fs.Root != null)
                        DebugUtility.PrintContext("FS", $"fs.Root keys: {string.Join(", ", fs.Root.Keys)}");
                    else
                        DebugUtility.PrintContext("FS", "fs.Root keys: null");

                    // === Debug 3: Print tutorial.txt content if present ===
                    if (fs.Root != null && fs.Root.ContainsKey("tutorial.txt"))
                        DebugUtility.PrintContext("FS", $"tutorial.txt content (from loader): {fs.Root["tutorial.txt"].Content ?? "NULL"}");
                    else
                        DebugUtility.PrintContext("FS", "tutorial.txt not found in fs.Root!");
                }

                Filesystems[$"Filesystem_{i}.json"] = fs;
                ValidationErrors.AddRange(fsErrors);
            }

            // Filesystem_11.json (safe fallback)
            var safePath = ResolvePath(
                "Config/Jsons/JsonFilesystems/Filesystem_11.json",
                "Env0.Terminal/Config/Jsons/JsonFilesystems/Filesystem_11.json"
            );
            if (File.Exists(safePath))
            {
                var raw = File.ReadAllText(safePath);
                DebugUtility.PrintContext("FS", $"==== RAW JSON FOR {safePath} ====");
                DebugUtility.PrintContext("FS", raw);
                DebugUtility.PrintContext("FS", "==================");
            }

            var safeFs = LoadFilesystem(safePath, out var safeErrors);

            if (safeFs != null)
            {
                DebugUtility.PrintContext("FS", $"safeFs.Root is null? {safeFs.Root == null}");
                if (safeFs.Root != null)
                    DebugUtility.PrintContext("FS", $"safeFs.Root keys: {string.Join(", ", safeFs.Root.Keys)}");
                else
                    DebugUtility.PrintContext("FS", "safeFs.Root keys: null");
            }

            Filesystems["Filesystem_11.json"] = safeFs;
            ValidationErrors.AddRange(safeErrors);
        }

        // --- Rest of your methods are unchanged! ---

        internal static BootConfig LoadBootConfig(string path, out List<string> errors)
        {
            errors = new List<string>();
            if (!File.Exists(path))
            {
                errors.Add($"BootConfig missing: {path}");
                return null;
            }
            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    errors.Add("BootConfig: file is empty.");
                    return null;
                }
                var config = JsonConvert.DeserializeObject<BootConfig>(json);
                if (config?.BootText == null || config.BootText.Count == 0)
                    errors.Add("BootConfig: BootText is missing or empty.");
                return config;
            }
            catch (Exception ex)
            {
                errors.Add($"BootConfig failed to load: {ex.Message}");
                return null;
            }
        }

        internal static UserConfig LoadUserConfig(string path, out List<string> errors)
        {
            errors = new List<string>();

            if (!File.Exists(path))
            {
                errors.Add($"UserConfig missing: {path} (defaulting to player/password)");
                return new UserConfig { Username = "player", Password = "password" };
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<UserConfig>(json);

                if (string.IsNullOrWhiteSpace(config?.Username) || string.IsNullOrWhiteSpace(config?.Password))
                {
                    errors.Add("UserConfig: username or password is missing or empty (defaulting to player/password)");
                    return new UserConfig { Username = "player", Password = "password" };
                }

                if (!IsAscii(config.Username) || !IsAscii(config.Password))
                {
                    errors.Add("UserConfig: username or password contains non-ASCII characters (defaulting to player/password)");
                    return new UserConfig { Username = "player", Password = "password" };
                }

                return config;
            }
            catch (Exception ex)
            {
                errors.Add($"UserConfig failed to load: {ex.Message} (defaulting to player/password)");
                return new UserConfig { Username = "player", Password = "password" };
            }
        }

        internal static List<DeviceInfo> LoadDevices(string path, out List<string> errors)
        {
            errors = new List<string>();

            if (!File.Exists(path))
            {
                errors.Add($"Devices missing: {path}");
                return new List<DeviceInfo>();
            }

            try
            {
                var json = File.ReadAllText(path);
                var devices = JsonConvert.DeserializeObject<List<DeviceInfo>>(json);

                if (devices == null || devices.Count == 0)
                {
                    errors.Add("Devices.json is empty or invalid.");
                    return new List<DeviceInfo>();
                }

                for (int i = 0; i < devices.Count; i++)
                {
                    var device = devices[i];
                    if (string.IsNullOrWhiteSpace(device.Ip)) errors.Add($"Device {i}: IP missing.");
                    if (string.IsNullOrWhiteSpace(device.Hostname)) errors.Add($"Device {i}: Hostname missing.");
                    if (string.IsNullOrWhiteSpace(device.Mac)) errors.Add($"Device {i}: MAC missing.");
                    if (string.IsNullOrWhiteSpace(device.Username)) errors.Add($"Device {i}: Username missing.");
                    if (string.IsNullOrWhiteSpace(device.Password)) errors.Add($"Device {i}: Password missing.");
                    if (string.IsNullOrWhiteSpace(device.Filesystem)) errors.Add($"Device {i}: Filesystem missing.");
                    if (device.Interfaces == null || device.Interfaces.Count == 0) errors.Add($"Device {i}: No interfaces defined.");
                    if (device.Ports == null) device.Ports = new List<string>();
                    if (string.IsNullOrWhiteSpace(device.Motd)) device.Motd = $"Welcome to {device.Hostname}";

                    if (device.Interfaces != null)
                    {
                        for (int j = 0; j < device.Interfaces.Count; j++)
                        {
                            var iface = device.Interfaces[j];
                            if (string.IsNullOrWhiteSpace(iface.Name)) errors.Add($"Device {i} Interface {j}: Name missing.");
                            if (string.IsNullOrWhiteSpace(iface.Ip)) errors.Add($"Device {i} Interface {j}: IP missing.");
                            if (string.IsNullOrWhiteSpace(iface.Subnet)) errors.Add($"Device {i} Interface {j}: Subnet missing.");
                            if (string.IsNullOrWhiteSpace(iface.Mac)) errors.Add($"Device {i} Interface {j}: MAC missing.");
                        }
                    }
                }

                return devices;
            }
            catch (Exception ex)
            {
                errors.Add($"Devices failed to load: {ex.Message}");
                return new List<DeviceInfo>();
            }
        }

        public static Env0.Terminal.Config.Pocos.Filesystem LoadFilesystem(string path, out List<string> errors)
        {
            errors = new List<string>();

            if (!File.Exists(path))
            {
                errors.Add($"Filesystem missing: {path}");
                return new Env0.Terminal.Config.Pocos.Filesystem(); // Safe empty FS
            }

            try
            {
                var json = File.ReadAllText(path);
                var fs = JsonConvert.DeserializeObject<Env0.Terminal.Config.Pocos.Filesystem>(json);

                if (fs?.Root == null || fs.Root.Count == 0)
                {
                    errors.Add($"{Path.GetFileName(path)}: Root is missing or empty.");
                    return new Env0.Terminal.Config.Pocos.Filesystem();
                }

                ValidateEntries(fs.Root, errors, Path.GetFileName(path), "/");
                return fs;
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(path)} failed to load: {ex.Message}");
                return new Env0.Terminal.Config.Pocos.Filesystem();
            }
        }

        private static void ValidateEntries(Dictionary<string, FileEntry> entries, List<string> errors, string fsName, string path)
        {
            foreach (var kvp in entries)
            {
                var name = kvp.Key;
                var entry = kvp.Value;

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"{fsName}: Empty or invalid entry name at {path}");
                    continue;
                }

                if (entry.Type == "file")
                {
                    if (entry.Content == null)
                        errors.Add($"{fsName}: File '{name}' at {path} missing 'content'.");
                    if (entry.Children != null)
                        errors.Add($"{fsName}: File '{name}' at {path} should not have children.");
                }
                else
                {
                    if (entry.Children == null)
                        errors.Add($"{fsName}: Directory '{name}' at {path} missing children dictionary.");
                    else
                        ValidateEntries(entry.Children, errors, fsName, path + name + "/");
                }
            }
        }

        private static bool IsAscii(string value)
        {
            return !string.IsNullOrEmpty(value) && value.All(c => c <= 127);
        }
    }
}
