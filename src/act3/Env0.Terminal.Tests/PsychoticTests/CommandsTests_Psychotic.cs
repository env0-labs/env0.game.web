using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;
using Env0.Terminal.Network;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Config.Pocos;
using Env0.Terminal.Config;
using Env0.Terminal.API_DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Env0.Terminal.Tests.Commands
{
    public class CommandsTests_Psychotic
    {
        // ---- NMAP ----
        
        // Test retired: relies on fragile internal state assumptions about nmap's subnet parsing.
// Real behavior covered by integration tests.
// Keeping for reference in case nmap logic changes.

        [Fact]
        public void NmapCommand_HugeSubnet_DoesNotHang()
        {
            var primary = new DeviceInfo
            {
                Ip = "10.10.10.1",
                Hostname = "host1",
                Subnet = "10.10.10.0/24",
                Interfaces = new List<DeviceInterface>
                {
                    new DeviceInterface
                    {
                        Name = "eth0",
                        Ip = "10.10.10.1",
                        Subnet = "10.10.10.0/24",
                        Mac = "00:00:00:00:00:01"
                    }
                }
            };

            var devices = new List<DeviceInfo> { primary };

            for (int i = 2; i <= 255; i++)
            {
                devices.Add(new DeviceInfo
                {
                    Ip = $"10.10.10.{i}",
                    Hostname = $"host{i}",
                    Subnet = "10.10.10.0/24",
                    Interfaces = new List<DeviceInterface>
                    {
                        new DeviceInterface
                        {
                            Name = "eth0",
                            Ip = $"10.10.10.{i}",
                            Subnet = "10.10.10.0/24",
                            Mac = $"00:00:00:00:00:{i:X2}"
                        }
                    }
                });
            }

            var session = new SessionState
            {
                DeviceInfo = primary,
                NetworkManager = new NetworkManager(devices, primary)
            };

            var command = new NmapCommand();
            var result = command.Execute(session, new[] { "10.10.10.0/24" });

            // Debug print
            Console.WriteLine($"[NMAP OUTPUT]\n{result.Output}");

            Assert.NotNull(result);
            Assert.Contains("host1", result.Output);
            Assert.Contains("host255", result.Output); // last one in the generated loop
        }


        // ---- PING ----
        [Fact]
        public void PingCommand_PingSelf_ReturnsExpected()
        {
            var devices = new List<DeviceInfo>
            {
                new DeviceInfo { Ip = "10.10.10.1", Hostname = "workstation", Subnet = "255.255.255.0" }
            };
            var session = new SessionState
            {
                NetworkManager = new NetworkManager(devices, devices[0])
            };
            var command = new PingCommand();
            var result = command.Execute(session, new[] { "10.10.10.1" });
            Assert.NotNull(result);
            Assert.Contains("ping", result.Output.ToLower());
        }

        [Fact]
        public void PingCommand_PingNonexistent_ReturnsError()
        {
            var devices = new List<DeviceInfo>
            {
                new DeviceInfo { Ip = "10.10.10.1", Hostname = "workstation", Subnet = "255.255.255.0" }
            };
            var session = new SessionState
            {
                NetworkManager = new NetworkManager(devices, devices[0])
            };
            var command = new PingCommand();
            var result = command.Execute(session, new[] { "10.10.10.99" });
            Assert.NotNull(result);
            Assert.True(result.IsError);
        }

        [Theory]
        [InlineData("banana")]
        [InlineData("256.256.256.256")]
        [InlineData("")]
        public void PingCommand_MalformedAddress_HandlesGracefully(string input)
        {
            var devices = new List<DeviceInfo>
            {
                new DeviceInfo { Ip = "10.10.10.1", Hostname = "workstation", Subnet = "255.255.255.0" }
            };
            var session = new SessionState
            {
                NetworkManager = new NetworkManager(devices, devices[0])
            };
            var command = new PingCommand();
            var result = command.Execute(session, new[] { input });
            Assert.NotNull(result);
            Assert.True(result.IsError);
        }

        // ---- SSH ----
        [Fact]
        public void SshCommand_CaseSensitivityVariants()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            var variants = new[] { "WORKSTATION2.NODE.ZERO", "workstation2.node.zero", "workstation2.node.zero " };
            foreach (var host in variants)
            {
                var state = api.Execute($"ssh {host.Trim()}");
                Assert.Equal(TerminalPhase.Login, state.Phase);
                Assert.True(state.IsLoginPrompt || state.IsPasswordPrompt);
            }
        }

        [Fact]
        public void SshCommand_SshToSelf_NoStackLoop()
        {
            JsonLoader.LoadAll();
            var devices = JsonLoader.Devices;
            Assert.NotEmpty(devices);

            var device = devices[0];
            var session = new SessionState
            {
                Username = device.Username,
                NetworkManager = new NetworkManager(devices, device),
                Hostname = device.Hostname
            };

            var command = new SshCommand();
            var result = command.Execute(session, new[] { device.Hostname });
            Assert.NotNull(result);
            Assert.False(result.IsError);
        }

        // ---- CAT ----
        [Fact]
        public void CatCommand_CatBinaryFile_PrintsNonsense()
        {
            var binFile = new FileEntry { Name = "random.bin", Type = ".bin", Content = "010101" };
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir",
                Children = new Dictionary<string, FileEntry> { { "random.bin", binFile } }
            };
            binFile.Parent = root;
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CatCommand();

            var result = cmd.Execute(session, new[] { "random.bin" });
            Assert.False(result.IsError); // Should print nonsense, not error
            Assert.Contains("010101", result.Output); // If your nonsense is just the content for now
        }

        [Fact]
        public void CatCommand_CatExecutableFile_ShowsError()
        {
            var shFile = new FileEntry { Name = "run.sh", Type = ".sh", Content = "echo hack" };
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir",
                Children = new Dictionary<string, FileEntry> { { "run.sh", shFile } }
            };
            shFile.Parent = root;
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CatCommand();

            var result = cmd.Execute(session, new[] { "run.sh" });
            Assert.True(result.IsError);
            Assert.Contains("executable", result.Output.ToLower());
        }

        [Fact]
        public void CatCommand_CatLargeFile_TriggersLimit()
        {
            var bigText = string.Join("\n", Enumerable.Range(0, 1001).Select(i => $"Line {i}"));
            var file = new FileEntry { Name = "big.txt", Type = "file", Content = bigText };
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir",
                Children = new Dictionary<string, FileEntry> { { "big.txt", file } }
            };
            file.Parent = root;
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CatCommand();

            var result = cmd.Execute(session, new[] { "big.txt" });
            Assert.True(result.IsError);
            Assert.Contains("file too large", result.Output.ToLower());
        }

        [Fact]
        public void CatCommand_CatNonexistentFile_ReturnsError()
        {
            var root = new FileEntry { Name = "/", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CatCommand();

            var result = cmd.Execute(session, new[] { "nope.txt" });
            Assert.True(result.IsError);
        }

        [Fact]
        public void CatCommand_CatMultipleArgs_OnlyHandlesFirst()
        {
            var file = new FileEntry { Name = "foo.txt", Type = "file", Content = "bar" };
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir",
                Children = new Dictionary<string, FileEntry> { { "foo.txt", file } }
            };
            file.Parent = root;
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CatCommand();

            var result = cmd.Execute(session, new[] { "foo.txt", "bar.txt" });
            Assert.False(result.IsError);
            Assert.Equal("bar", result.Output);
        }

        // ---- CD ----
        [Fact]
        public void CdCommand_CdToFile_ReturnsError()
        {
            var file = new FileEntry { Name = "foo.txt", Type = "file", Content = "bar" };
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir",
                Children = new Dictionary<string, FileEntry> { { "foo.txt", file } }
            };
            file.Parent = root;
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CdCommand();

            var result = cmd.Execute(session, new[] { "foo.txt" });
            Assert.True(result.IsError);
        }

        [Fact]
        public void CdCommand_CdRelativeRoot_Normalizes()
        {
            var root = new FileEntry { Name = "/", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CdCommand();

            var result = cmd.Execute(session, new[] { ".." });
            Assert.NotNull(result);
            Assert.False(result.IsError); // Should just stay at root, not error
        }

        [Fact]
        public void CdCommand_CdWithLongPath_HandledGracefully()
        {
            var root = new FileEntry { Name = "/", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CdCommand();

            var longPath = "/" + string.Join("", Enumerable.Repeat("a", 300));
            var result = cmd.Execute(session, new[] { longPath });
            Assert.True(result.IsError); // Should error on invalid/long path
        }

        // ---- ECHO ----
        [Fact]
        public void EchoCommand_EchoSpecialCharacters_NoExecution()
        {
            var cmd = new EchoCommand();
            var session = new SessionState();
            var result = cmd.Execute(session, new[] { "hello; rm -rf / && whoami | id" });
            Assert.Contains("rm -rf", result.Output);
            Assert.DoesNotContain("permission denied", result.Output.ToLower());
        }

        [Fact]
        public void EchoCommand_EchoOnlySpaces_PreservesInput()
        {
            var cmd = new EchoCommand();
            var session = new SessionState();
            var result = cmd.Execute(session, new[] { "    " });
            Assert.Equal("    ", result.Output);
        }

        // ---- EXIT ----
        [Fact]
        public void ExitCommand_WithArgs_IgnoresArgs()
        {
            var command = new ExitCommand();
            var session = new SessionState();
            var result = command.Execute(session, new[] { "now" });
            Assert.NotNull(result);
            Assert.Contains("logout", result.Output.ToLower());
        }

        [Fact]
        public void ExitCommand_DeepSshStack_ExitsCleanly()
        {
            var stack = new Stack<SshSessionContext>();
            stack.Push(new SshSessionContext("user", "remote", "/", null, null));
            stack.Push(new SshSessionContext("user2", "remote2", "/", null, null));
            var session = new SessionState { SshStack = stack };
            var command = new ExitCommand();

            var result = command.Execute(session, new string[0]);
            Assert.False(result.IsError);
        }

        // ---- HELP ----
        [Fact]
        public void HelpCommand_WithArgs_ReturnsCommandHelpOrError()
        {
            var command = new HelpCommand();
            var session = new SessionState();
            var result = command.Execute(session, new[] { "ls" });
            Assert.NotNull(result);
        }

        // ---- IFCONFIG ----
        [Fact]
        public void IfconfigCommand_InterfaceMissingFields_StillOutputs()
        {
            var command = new IfconfigCommand();
            var session = new SessionState
            {
                DeviceInfo = new DeviceInfo
                {
                    Interfaces = new List<DeviceInterface>
                    {
                        new DeviceInterface { Name = "eth0", Ip = null, Subnet = null, Mac = null }
                    }
                }
            };
            var result = command.Execute(session, new string[0]);
            Assert.NotNull(result);
            Assert.Contains("eth0", result.Output.ToLower());
        }

        // ---- LS ----
        [Fact]
        public void LsCommand_FilePath_Errors()
        {
            var file = new FileEntry { Name = "foo.txt", Type = "file", Content = "bar" };
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir",
                Children = new Dictionary<string, FileEntry> { { "foo.txt", file } }
            };
            file.Parent = root;
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new LsCommand();

            var result = cmd.Execute(session, new[] { "foo.txt" });
            Assert.True(result.IsError);
        }

        // --- [TEST COMMENTED OUT] ---
        // Massive directory test (1k+ files); leave out unless you want to check for
        // performance/overflow edge-cases.
        [Fact]
        public void LsCommand_MassiveDirectory_DoesNotCrash()
        {
            var children = Enumerable.Range(0, 1000)
                .ToDictionary(i => $"file{i}.txt", i => new FileEntry { Name = $"file{i}.txt", Type = "file", Content = "x" });
            var root = new FileEntry { Name = "/", Type = "dir", Children = children };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new LsCommand();

            var result = cmd.Execute(session, new string[0]);
            Assert.NotNull(result);
            Assert.True(result.Output.Split(' ').Length >= 1000);
        }
    }
}
