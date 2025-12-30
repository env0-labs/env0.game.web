using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Env0.Terminal.Config;
using Env0.Terminal.Config.Pocos;

namespace Env0.Terminal.Tests
{
    [Trait("TestType", "Critical")]
    public class JsonLoaderTests
    {
        private const string TestDataDir = "TestData";

        public JsonLoaderTests()
        {
            // Ensure clean slate before each test
            if (!Directory.Exists(TestDataDir))
                Directory.CreateDirectory(TestDataDir);
        }

        // --- BootConfig Tests ---

        [Fact]
        public void LoadBootConfig_ValidJson_LoadsBootText()
        {
            var path = Path.Combine(TestDataDir, "BootConfig_valid.json");
            File.WriteAllText(path, @"{ ""BootText"": [""Line1"", ""Line2""] }");

            var config = JsonLoader.LoadBootConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal(2, config.BootText.Count);
            Assert.Contains("Line1", config.BootText);
            Assert.Empty(errors);
        }

        [Fact]
        public void LoadBootConfig_FileMissing_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "Nonexistent.json");
            if (File.Exists(path)) File.Delete(path);

            var config = JsonLoader.LoadBootConfig(path, out var errors);

            Assert.Null(config);
            Assert.Contains(errors, e => e.ToLower().Contains("missing"));
        }

        [Fact]
        public void LoadBootConfig_EmptyBootText_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "BootConfig_empty.json");
            File.WriteAllText(path, @"{ ""BootText"": [] }");

            var config = JsonLoader.LoadBootConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.NotNull(errors);
            Assert.Contains(errors, e => e.ToLower().Contains("empty"));
        }

        [Fact]
        public void LoadBootConfig_MalformedJson_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "BootConfig_bad.json");
            File.WriteAllText(path, @"{ this is not: valid json }");

            var config = JsonLoader.LoadBootConfig(path, out var errors);

            Assert.Null(config);
            Assert.Contains(errors, e => e.ToLower().Contains("failed to load"));
        }

        // --- UserConfig Tests ---

        [Fact]
        public void LoadUserConfig_ValidJson_LoadsUser()
        {
            var path = Path.Combine(TestDataDir, "UserConfig_valid.json");
            File.WriteAllText(path, @"{ ""Username"": ""player"", ""Password"": ""password"" }");

            var config = JsonLoader.LoadUserConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal("player", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Empty(errors);
        }

        [Fact]
        public void LoadUserConfig_MissingFile_UsesDefault()
        {
            var path = Path.Combine(TestDataDir, "UserConfig_missing.json");
            if (File.Exists(path)) File.Delete(path);

            var config = JsonLoader.LoadUserConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal("player", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Contains(errors, e => e.ToLower().Contains("defaulting"));
        }

        [Fact]
        public void LoadUserConfig_EmptyFields_UsesDefault()
        {
            var path = Path.Combine(TestDataDir, "UserConfig_empty.json");
            File.WriteAllText(path, @"{ ""Username"": """", ""Password"": """" }");

            var config = JsonLoader.LoadUserConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal("player", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Contains(errors, e => e.ToLower().Contains("defaulting"));
        }

        [Fact]
        public void LoadUserConfig_NonAscii_UsesDefault()
        {
            var path = Path.Combine(TestDataDir, "UserConfig_nonascii.json");
            File.WriteAllText(path, @"{ ""Username"": ""płayer"", ""Password"": ""pässword"" }");

            var config = JsonLoader.LoadUserConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal("player", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Contains(errors, e => e.ToLower().Contains("non-ascii"));
        }

        // --- Devices Tests ---

        [Fact]
        public void LoadDevices_ValidJson_LoadsDevice()
        {
            var path = Path.Combine(TestDataDir, "Devices_valid.json");
            File.WriteAllText(path, @"
            [
                {
                    ""ip"": ""10.10.10.1"",
                    ""hostname"": ""workstation.node.zero"",
                    ""mac"": ""00:11:22:33:44:55"",
                    ""username"": ""admin"",
                    ""password"": ""pass"",
                    ""filesystem"": ""Filesystem_1.json"",
                    ""motd"": ""Welcome!"",
                    ""description"": ""Test Device"",
                    ""ports"": [""22""],
                    ""interfaces"": [
                        { ""name"": ""eth0"", ""ip"": ""10.10.10.1"", ""subnet"": ""255.255.255.0"", ""mac"": ""00:11:22:33:44:55"" }
                    ]
                }
            ]");

            var devices = JsonLoader.LoadDevices(path, out var errors);

            Assert.NotNull(devices);
            Assert.Single(devices);
            var dev = devices[0];
            Assert.Equal("10.10.10.1", dev.Ip);
            Assert.Equal("workstation.node.zero", dev.Hostname);
            Assert.Equal("admin", dev.Username);
            Assert.Equal("Filesystem_1.json", dev.Filesystem);
            Assert.Single(dev.Ports);
            Assert.Single(dev.Interfaces);
            Assert.Empty(errors);
        }

        [Fact]
        public void LoadDevices_MissingFields_ReportsErrors()
        {
            var path = Path.Combine(TestDataDir, "Devices_missingfields.json");
            File.WriteAllText(path, @"
            [
                {
                    ""ip"": """",
                    ""hostname"": """",
                    ""mac"": """",
                    ""username"": """",
                    ""password"": """",
                    ""filesystem"": """",
                    ""motd"": """",
                    ""description"": """",
                    ""ports"": [],
                    ""interfaces"": []
                }
            ]");

            var devices = JsonLoader.LoadDevices(path, out var errors);

            Assert.NotNull(devices);
            Assert.Single(devices);
            Assert.True(errors.Count > 0);
            Assert.Contains(errors, e => e.ToLower().Contains("missing"));
        }

        [Fact]
        public void LoadDevices_MissingFile_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "Devices_missing.json");
            if (File.Exists(path)) File.Delete(path);

            var devices = JsonLoader.LoadDevices(path, out var errors);

            Assert.NotNull(devices);
            Assert.Empty(devices);
            Assert.Contains(errors, e => e.ToLower().Contains("missing"));
        }

        // --- Filesystem Tests ---

        [Fact]
        public void LoadFilesystem_ValidJson_LoadsStructure()
        {
            var path = Path.Combine(TestDataDir, "Filesystem_valid.json");
            File.WriteAllText(path, @"
            {
              ""root"": {
                ""home"": {
                  ""user"": {
                    ""tutorial.txt"": {
                      ""type"": ""file"",
                      ""content"": ""Welcome!""
                    }
                  }
                }
              }
            }");
            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.NotNull(fs.Root);
            Assert.True(fs.Root.ContainsKey("home"));
            Assert.True(fs.Root["home"].Children.ContainsKey("user"));
            Assert.True(fs.Root["home"].Children["user"].Children.ContainsKey("tutorial.txt"));
            Assert.Equal("file", fs.Root["home"].Children["user"].Children["tutorial.txt"].Type);
            Assert.Equal("Welcome!", fs.Root["home"].Children["user"].Children["tutorial.txt"].Content);
            Assert.Empty(errors);
        }

        [Fact]
        public void LoadFilesystem_EmptyRoot_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "Filesystem_empty.json");
            File.WriteAllText(path, @"{ ""root"": {} }");

            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.NotNull(fs.Root);
            Assert.True(errors.Count > 0);
            Assert.Contains("Root is missing or empty", errors[0]);
        }

            [Fact]
            public void LoadFilesystem_FileMissingContent_ReportsError()
            {
                var path = Path.Combine(TestDataDir, "Filesystem_file_missing_content.json");
                File.WriteAllText(path, @"
                {
                    ""root"": {
                        ""etc"": {
                            ""hostname.txt"": { ""type"": ""file"" }
                        }
                    }
                }");

                var fs = JsonLoader.LoadFilesystem(path, out var errors);

                foreach (var e in errors)
                    System.Console.WriteLine($"Loader error: {e}");

                Assert.NotNull(fs);
                Assert.True(errors.Any(e => e.Contains("missing 'content'")), "Expected missing content error.");
            }

    }
}
