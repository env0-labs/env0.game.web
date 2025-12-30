using Xunit;
using Env0.Terminal;
using Env0.Terminal.API_DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Env0.Terminal.Tests
{
    public class TerminalEngineAPI_PsychoticBrodyTests
    {
        // === 1. API Initialization and Double/Invalid Init ===

        [Fact]
        public void Initialize_CalledTwice_DoesNotLeakOrCrash()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            // Re-init, which should be idempotent or safely reentrant
            api.Initialize();
            var state = api.Execute("");
            Assert.NotNull(state);
            Assert.True(state.Phase == TerminalPhase.Booting || state.Phase == TerminalPhase.Login);
        }
        [Fact]
        public void Initialize_DoesNotCrash_WhenConfigsMissingOrDefault()
        {
            var api = new TerminalEngineAPI();
            try
            {
                api.Initialize();
            }
            catch (Exception ex)
            {
                // Accept only a clear config exception, not a silent or generic crash
                Assert.Contains("config", ex.Message, StringComparison.OrdinalIgnoreCase);
                return;
            }
            // If it doesn't throw, must reach a valid state
            var state = api.Execute("");
            Assert.NotNull(state);
        }


        [Fact]
        public void Initialize_WithCorruptOrEmptyConfigs_GracefulError()
        {
            var api = new TerminalEngineAPI();
            var ex = Record.Exception(() => api.Initialize());
            if (ex != null)
                Assert.Contains("config", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Execute_BeforeInitialize_ThrowsOrSafeError()
        {
            var api = new TerminalEngineAPI();
            Assert.ThrowsAny<Exception>(() => api.Execute("ls"));
        }

        // === 2. State Sync / Render State Sanity ===

        [Fact]
        public void RenderState_NeverNullOrIncoherent()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            var state = api.Execute("");
            Assert.NotNull(state);
            Assert.True(Enum.IsDefined(typeof(TerminalPhase), state.Phase));
            // Output and Error should not BOTH be set for successful command
            if (!state.IsError)
                Assert.True(string.IsNullOrEmpty(state.ErrorMessage));
        }

        [Fact]
        public void Prompt_IsConsistentWithSessionState()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); // boot
            api.Execute(""); // login
            api.Execute("alice");
            api.Execute("hunter2");
            var state = api.Execute("pwd");
            // Prompt and session should be coherent
            Assert.Contains(state.CurrentDirectory, state.Prompt);
            Assert.StartsWith(state.Prompt.Split('@')[0], state.Prompt);
        }

        // === 3. Input Edge Cases ===

        [Fact]
        public void Execute_ExtremeCommandLengths_AreHandled()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");
            // Ridiculously long command
            var input = new string('x', 10000);
            var state = api.Execute(input);
            Assert.True(state.IsError);
            Assert.Contains("command not found", state.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Execute_CommandInjection_AreTreatedAsLiteral()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");
            var state = api.Execute("ls; rm -rf /");
            Assert.True(state.IsError);
            Assert.Contains("command not found", state.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Execute_NullOrControlChars_RejectedOrSafe()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");
            var state = api.Execute("\0\0\t\r\n");
            // Should not crash, error or no-op
            Assert.Equal(TerminalPhase.Terminal, state.Phase);
        }

        // === 4. State Corruption / Session Stack Overflows ===

        [Fact]
        public void SSH_SessionStackOverflow_DetectedAndSafe()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            // Push SSH to the absolute limit (simulate contract max depth)
            int maxDepth = 10; // If there's a contract limit, adjust this
            for (int i = 0; i < maxDepth; i++)
            {
                var state = api.Execute($"ssh localhost");
                if (state.IsError && state.ErrorMessage.ToLower().Contains("stack"))
                {
                    Assert.Contains("stack", state.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                    return;
                }
            }
            // Should either throw or refuse after max stack
            var final = api.Execute("ssh localhost");
            Assert.True(final.IsError);
        }

        [Fact]
        public void SSH_SessionStackUnderflow_NoCrash()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");
            // Try to exit more times than entered
            for (int i = 0; i < 10; i++)
                api.Execute("exit");
            var state = api.Execute("ls");
            Assert.Equal(TerminalPhase.Terminal, state.Phase);
        }

        // === 5. Filesystem/Device/Network Corruptions ===

        [Fact]
        public void CatCommand_HugeFile_HandledOrCapped()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");
            // Simulate giant file (if file loading allows)
            var state = api.Execute("cat bigfile.txt");
            // Should not crash; either truncated, error, or paged
            Assert.True(state.Output.Length < 100_000);
        }

        [Fact]
        public void Nmap_UnsupportedSubnet_ReportsError()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");
            var state = api.Execute("nmap 10.10.0.0/16");
            Assert.True(state.IsError || 
                        (state.ErrorMessage?.ToLowerInvariant().Contains("invalid subnet") ?? false) ||
                        (state.Output?.ToLowerInvariant().Contains("invalid subnet") ?? false)
            );
        }


        // === 6. Debug/Dev Mode Brutality ===

        [Fact]
        public void DebugMode_CanBeToggled_AndOnlyAffectsDebugCommands()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            api.SetDebugMode(true);
            var debugCmd = api.Execute("show filesystems");
            Assert.True(debugCmd.Output.Contains("filesystem", StringComparison.OrdinalIgnoreCase) || debugCmd.IsError);

            api.SetDebugMode(false);
            var failCmd = api.Execute("show filesystems");
            Assert.True(failCmd.IsError);
        }

        // === 7. Concurrency/Threaded Hell ===

        [Fact]
        public async Task Execute_ConcurrentCalls_DoNotCrashOrDesync()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");

            var tasks = Enumerable.Range(0, 20).Select(i =>
                Task.Run(() => api.Execute(i % 2 == 0 ? "ls" : "cd /"))
            ).ToArray();

            await Task.WhenAll(tasks);

            foreach (var t in tasks)
                Assert.NotNull(await t);
        }

        // === 8. MOTD, Prompt, and Banner Sanity ===

        [Fact]
        public void MOTD_ShownOnLoginIfConfigured()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); var state = api.Execute("alice"); state = api.Execute("hunter2");
            // Should display MOTD banner flag and message if device config specifies it
            Assert.True(state.ShowMOTD == false || !string.IsNullOrEmpty(state.MOTD));
        }

        // === 9. Absolute Null Zone ===

        [Fact]
        public void AllPublicFields_NeverNullUnlessOptional()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            api.Execute(""); api.Execute(""); api.Execute("alice"); api.Execute("hunter2");
            var state = api.Execute("ls");
            // Every public field that's not explicitly optional must be non-null
            Assert.NotNull(state.Prompt);
            Assert.NotNull(state.Output);
            Assert.NotNull(state.CurrentDirectory);
        }

        // === 10. Contract Compliance Fuzz ===

        [Fact]
        public void RenderState_MatchesContract_AfterAllActions()
        {
            var api = new TerminalEngineAPI();
            api.Initialize();
            // Slam through all phases
            var states = new List<TerminalRenderState>();
            states.Add(api.Execute(""));
            states.Add(api.Execute("")); // login
            states.Add(api.Execute("alice"));
            states.Add(api.Execute("hunter2"));
            states.Add(api.Execute("ls"));
            states.Add(api.Execute("cd /home"));
            states.Add(api.Execute("cat file.txt"));
            states.Add(api.Execute("read file.txt"));
            states.Add(api.Execute("exit"));

            foreach (var state in states)
            {
                Assert.NotNull(state);
                Assert.True(Enum.IsDefined(typeof(TerminalPhase), state.Phase));
                // Output or ErrorMessage must always be set if IsError
                if (state.IsError)
                    Assert.False(string.IsNullOrEmpty(state.ErrorMessage));
            }
        }
    }
}
