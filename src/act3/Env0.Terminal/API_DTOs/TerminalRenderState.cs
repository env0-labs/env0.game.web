using System.Collections.Generic;
using Env0.Terminal.Config.Pocos;
using Env0.Terminal.Terminal;


namespace Env0.Terminal.API_DTOs
{
    /// <summary>
    /// The top-level API "render snapshot" returned by the TerminalEngineAPI after each command or state change.
    /// </summary>
    public class TerminalRenderState
    {
        // Overall state of the terminal (booting, login, terminal, etc.)
        public TerminalPhase Phase { get; set; }

        // Boot phase output
        public List<string> BootSequenceLines { get; set; }

        // Login phase flags and messages
        public bool IsLoginPrompt { get; set; }
        public bool IsPasswordPrompt { get; set; }
        public string LoginErrorMessage { get; set; }

        // Shell/terminal phase
        public string Prompt { get; set; }                 // e.g., user@host:/cwd$
        public string Output { get; set; }                 // Last command result or output (legacy)
        public List<TerminalOutputLine> OutputLines { get; set; } = new List<TerminalOutputLine>();  // NEW typed output lines

        public string CurrentDirectory { get; set; }
        public List<string> DirectoryListing { get; set; }
        public int SessionStackDepth { get; set; }
        public List<SessionContext> SessionStackView { get; set; }
        public bool ClearScreen { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        // MOTD/banner
        public bool ShowMOTD { get; set; }
        public string MOTD { get; set; }

        // Debug/dev mode info
        public bool DebugMode { get; set; }
        public DebugInfo DebugInfo { get; set; }
        
        // Expose Device Details
        public DeviceInfo DeviceInfo { get; set; }
    }
}