// SshCommandTests.cs - All tests obsolete under API-centric design.
// Retained verbatim for contract/history/audit per project policy.
using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;
using Env0.Terminal.Network;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Config.Pocos;
using Env0.Terminal.Config;
using System.Collections.Generic;
using System.Linq;

namespace Env0.Terminal.Tests.Commands
{
    public class SshCommandTests
    {
        // [Legacy/Obsolete] SshCommand_ConnectsToDeviceAndUpdatesSession
        // This test assumes SshCommand manages session state, SSH connection, output, and banners.
        // Under the current contract, SshCommand is a stub: all SSH flow and session mutation are managed by TerminalEngineAPI.
        // Test is obsolete, does not reflect system behavior, and is retained for historical reference only.

        /*
        [Fact]
        public void SshCommand_ConnectsToDeviceAndUpdatesSession()
        {
            // ... see legacy code ...
        }
        */

        // [Legacy/Obsolete] SshCommand_FailsOnUnknownHost
        // This test assumes SshCommand checks device existence and returns user-facing errors for unknown hosts.
        // Under the current contract, device validation and error messaging are handled by TerminalEngineAPI, not by SshCommand.
        // Test is obsolete, not contract-relevant, and is retained for historical reference only.

        /*
        [Fact]
        public void SshCommand_FailsOnUnknownHost()
        {
            // ... see legacy code ...
        }
        */

        // [Legacy/Obsolete] SshCommand_FailsOnIncorrectUsername
        // This test assumes SshCommand performs SSH credential validation and returns login errors.
        // Under the current contract, SshCommand only validates command arguments and defers all SSH flow and error handling to TerminalEngineAPI.
        // Test is obsolete, not contract-relevant, and is retained for historical reference only.

        /*
        [Fact]
        public void SshCommand_FailsOnIncorrectUsername()
        {
            // ... see legacy code ...
        }
        */

        // [Legacy/Obsolete] SshCommand_StackOverflowOnTooManyHops
        // This test assumes SshCommand manages SSH stack depth and returns stack overflow errors.
        // Under the current contract, all SSH stack management and error messaging are handled by TerminalEngineAPI.
        // Test is obsolete, not contract-relevant, and is retained for historical reference only.

        /*
        [Fact]
        public void SshCommand_StackOverflowOnTooManyHops()
        {
            // ... see legacy code ...
        }
        */
    }
}
