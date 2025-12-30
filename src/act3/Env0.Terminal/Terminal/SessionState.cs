using System.Collections.Generic;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Network;
using Env0.Terminal.Config.Pocos;

namespace Env0.Terminal.Terminal
{
    /// <summary>
    /// Holds all mutable state for the current terminal session.
    /// </summary>
    public class SessionState
    {
        // Local user info (assigned at startup, never validated)
        public string Username { get; set; }
        public string Password { get; set; }

        // Device context
        public string Hostname { get; set; }
        public string Domain { get; set; }

        // Working directory string (for the current session/device)
        // Only for prompt; always synced with FilesystemManager.currentDirectory!
        public string CurrentWorkingDirectory { get; set; }

        // Core managers and device pointer
        public FilesystemManager FilesystemManager { get; set; }
        public NetworkManager NetworkManager { get; set; }
        public DeviceInfo DeviceInfo { get; set; }

        // Command history (max 20, no consecutive duplicates)
        public List<string> CommandHistory { get; set; } = new List<string>();

        // SSH stack (for SSH hopping, max 10 as per Reference.md)
        public Stack<SshSessionContext> SshStack { get; set; } = new Stack<SshSessionContext>();

        // Debug flag
        public bool DebugMode { get; set; } = false;

        // The real "where am I" for commands is always FilesystemManager.currentDirectory!

        // Constructor
        public SessionState()
        {
            Username = "player";
            Password = string.Empty;
            Hostname = "sbc";
            Domain = "node.zero";
            CurrentWorkingDirectory = "/"; // always set from FS pointer after session init
            CommandHistory = new List<string>();
            SshStack = new Stack<SshSessionContext>();
            DebugMode = false;
        }
    }

    /// <summary>
    /// Used to track SSH session context for hopping (expand as needed).
    /// Captures both the FS pointer and the string path.
    /// </summary>
    public class SshSessionContext
    {
        public string Username { get; set; }
        public string Hostname { get; set; }
        public string CurrentWorkingDirectory { get; set; }
        public FilesystemManager FilesystemManager { get; set; }
        public NetworkManager NetworkManager { get; set; }

        public SshSessionContext(string username, string hostname, string cwd,
            FilesystemManager fsManager, NetworkManager netManager)
        {
            Username = username;
            Hostname = hostname;
            CurrentWorkingDirectory = cwd;
            FilesystemManager = fsManager;
            NetworkManager = netManager;
        }
    }
}
