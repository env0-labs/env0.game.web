using Env0.Terminal.Config;
using Env0.Terminal.Config.Pocos;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Network;
using Env0.Terminal.Terminal;
using Env0.Terminal.Login;
using Env0.Terminal.API_DTOs;
using System.Collections.Generic;
using System.Linq;

namespace Env0.Terminal
{
    public class TerminalEngineAPI
    {
        private TerminalStateManager _stateManager;
        private SessionState _session;
        private FilesystemManager _filesystemManager;
        private NetworkManager _networkManager;
        private SSHHandler _sshHandler;
        private CommandParser _commandParser;
        private CommandHandler _commandHandler;
        private LoginHandler _loginHandler;

        private bool _initialized = false;
        private bool _bootShown = false;

        // SSH login flow tracking
        private enum LoginMode { None, Local, Ssh }
        private LoginMode _loginMode = LoginMode.None;
        private string _pendingSshTarget = null;
        private DeviceInfo _pendingSshDevice = null;
        private string _pendingSshUser = null;

        private enum LoginStep { None, Username, Password }
        private LoginStep _loginStep = LoginStep.None;
        private string _capturedUsername = null;
        private string _capturedPassword = null;

        private TerminalPhase _phase = TerminalPhase.Booting;

        public void Initialize()
        {
            JsonLoader.LoadAll();

            var bootConfig = JsonLoader.BootConfig;
            var userConfig = JsonLoader.UserConfig;
            var devices = JsonLoader.Devices;
            var fsPoco = JsonLoader.Filesystems.ContainsKey("Filesystem_1.json")
                ? JsonLoader.Filesystems["Filesystem_1.json"]
                : null;

            if (devices == null || devices.Count == 0 || fsPoco == null)
                throw new System.Exception("Critical config missing: Devices or Filesystem_1.json");

            var fsRootNode = new FileEntry { Children = fsPoco.Root };
            _filesystemManager = FilesystemManager.FromPocoRoot(fsRootNode);

            var localDevice = devices[0];
            _networkManager = new NetworkManager(devices, localDevice);
            _sshHandler = new SSHHandler(_networkManager);

            _session = new SessionState
            {
                Username = userConfig.Username,
                Password = userConfig.Password,
                Hostname = localDevice.Hostname,
                Domain = "",
                CurrentWorkingDirectory = "/",
                FilesystemManager = _filesystemManager,
                NetworkManager = _networkManager,
                DeviceInfo = localDevice
            };

            _stateManager = new TerminalStateManager();
            _commandParser = new CommandParser();
            _commandHandler = new CommandHandler(_session.DebugMode);
            _loginHandler = new LoginHandler();

            _initialized = true;
            _phase = TerminalPhase.Booting;
            _bootShown = false;

            _loginMode = LoginMode.None;
            _loginStep = LoginStep.None;
            _pendingSshTarget = null;
            _pendingSshDevice = null;
            _pendingSshUser = null;
            _capturedUsername = null;
            _capturedPassword = null;
        }

        public TerminalRenderState Execute(string input)
        {
            if (!_initialized) throw new System.Exception("TerminalEngineAPI not initialized. Call Initialize() first.");

            switch (_phase)
            {
                case TerminalPhase.Booting:
                    if (!_bootShown)
                    {
                        _bootShown = true;
                        var bootLines = JsonLoader.BootConfig?.BootText ?? new List<string>();

                        return new TerminalRenderState
                        {
                            Phase = TerminalPhase.Booting,
                            BootSequenceLines = bootLines,
                            OutputLines = ConvertBootLinesToOutputLines(bootLines),
                            IsLoginPrompt = false,
                            IsPasswordPrompt = false,
                            Prompt = null
                        };
                    }
                    // Move to local login on any input after boot
                    _phase = TerminalPhase.Login;
                    _loginMode = LoginMode.Local;
                    _loginStep = LoginStep.Username;
                    _capturedUsername = null;
                    _capturedPassword = null;
                    return new TerminalRenderState
                    {
                        Phase = TerminalPhase.Login,
                        IsLoginPrompt = true,
                        IsPasswordPrompt = false,
                        Prompt = "Username: ",
                        OutputLines = new List<TerminalOutputLine>(),
                        Output = string.Empty
                    };

                case TerminalPhase.Login:
                    // LOCAL LOGIN
                    if (_loginMode == LoginMode.Local)
                    {
                        if (_loginStep == LoginStep.Username)
                        {
                            if (string.IsNullOrWhiteSpace(input))
                            {
                                var lines = ConvertToOutputLines("Trying to log in as a ghost? You need a username.\n", isError: true);
                                return new TerminalRenderState
                                {
                                    Phase = TerminalPhase.Login,
                                    IsLoginPrompt = true,
                                    IsPasswordPrompt = false,
                                    Prompt = "Username: ",
                                    OutputLines = lines,
                                    Output = string.Join("\n", lines.Select(l => l.Text))
                                };
                            }
                            else
                            {
                                _capturedUsername = input;
                                _loginStep = LoginStep.Password;
                                return new TerminalRenderState
                                {
                                    Phase = TerminalPhase.Login,
                                    IsLoginPrompt = false,
                                    IsPasswordPrompt = true,
                                    Prompt = "Password: ",
                                    OutputLines = new List<TerminalOutputLine>(),
                                    Output = string.Empty
                                };
                            }
                        }
                        else if (_loginStep == LoginStep.Password)
                        {
                            _capturedPassword = input ?? "";
                            _loginHandler.SetUsername(_session, _capturedUsername);
                            _loginHandler.SetPassword(_session, _capturedPassword);
                            _phase = TerminalPhase.Terminal;
                            _loginMode = LoginMode.None;
                            _loginStep = LoginStep.None;
                            string warning = string.IsNullOrEmpty(_capturedPassword)
                                ? "no password? well you like to live dangerously... I'll allow it\n"
                                : "";
                            return BuildRenderState($"{warning}Login complete!\n\nType read tutorial.txt for instructions\n");
                        }
                        // Reset to username prompt on any weird state
                        _loginStep = LoginStep.Username;
                        _capturedUsername = null;
                        _capturedPassword = null;
                        return new TerminalRenderState
                        {
                            Phase = TerminalPhase.Login,
                            IsLoginPrompt = true,
                            IsPasswordPrompt = false,
                            Prompt = "Username: ",
                            OutputLines = new List<TerminalOutputLine>(),
                            Output = string.Empty
                        };
                    }

                    // SSH LOGIN
                    if (_loginMode == LoginMode.Ssh)
                    {
                        if (_loginStep == LoginStep.Username)
                        {
                            if (string.IsNullOrWhiteSpace(input))
                            {
                                var lines = ConvertToOutputLines(
                                    "Username required for SSH login.\n(To abort SSH login, type 'abort' as the username.)\n",
                                    isError: false);
                                return new TerminalRenderState
                                {
                                    Phase = TerminalPhase.Login,
                                    IsLoginPrompt = true,
                                    IsPasswordPrompt = false,
                                    Prompt = "Username: ",
                                    OutputLines = lines,
                                    Output = string.Join("\n", lines.Select(l => l.Text))
                                };
                            }
                            if (input.Trim().ToLower() == "abort")
                            {
                                if (_session.SshStack.Count > 0)
                                {
                                    var prev = _session.SshStack.Pop();
                                    _session.Username = prev.Username;
                                    _session.Hostname = prev.Hostname;
                                    _session.CurrentWorkingDirectory = prev.CurrentWorkingDirectory;
                                    _session.FilesystemManager = prev.FilesystemManager;
                                    _session.NetworkManager = prev.NetworkManager;
                                    _session.DeviceInfo = _session.NetworkManager.CurrentDevice;
                                }
                                _phase = TerminalPhase.Terminal;
                                _loginMode = LoginMode.None;
                                _loginStep = LoginStep.None;
                                _pendingSshTarget = null;
                                _pendingSshDevice = null;
                                _pendingSshUser = null;
                                return BuildRenderState("SSH login aborted.\n");
                            }
                            var candidateUser = input.Trim();
                            var expectedUser = _pendingSshDevice.Username;
                            if (!string.Equals(candidateUser, expectedUser))
                            {
                                var lines = ConvertToOutputLines(
                                    "Login failed\n(To abort SSH login, type 'abort' as the username.)\n",
                                    isError: false);
                                _loginStep = LoginStep.Username;
                                _pendingSshUser = null;
                                return new TerminalRenderState
                                {
                                    Phase = TerminalPhase.Login,
                                    IsLoginPrompt = true,
                                    IsPasswordPrompt = false,
                                    Prompt = "Username: ",
                                    OutputLines = lines,
                                    Output = string.Join("\n", lines.Select(l => l.Text))
                                };
                            }

                            _pendingSshUser = candidateUser;
                            _loginStep = LoginStep.Password;
                            return new TerminalRenderState
                            {
                                Phase = TerminalPhase.Login,
                                IsLoginPrompt = false,
                                IsPasswordPrompt = true,
                                Prompt = "Password: ",
                                OutputLines = ConvertToOutputLines("(To abort SSH login, type 'abort' as the password.)\n", isError: false),
                                Output = "(To abort SSH login, type 'abort' as the password.)\n"
                            };
                        }
                        if (_loginStep == LoginStep.Password)
                        {
                            if ((input ?? "").Trim().ToLower() == "abort")
                            {
                                if (_session.SshStack.Count > 0)
                                {
                                    var prev = _session.SshStack.Pop();
                                    _session.Username = prev.Username;
                                    _session.Hostname = prev.Hostname;
                                    _session.CurrentWorkingDirectory = prev.CurrentWorkingDirectory;
                                    _session.FilesystemManager = prev.FilesystemManager;
                                    _session.NetworkManager = prev.NetworkManager;
                                    _session.DeviceInfo = _session.NetworkManager.CurrentDevice;
                                }
                                _phase = TerminalPhase.Terminal;
                                _loginMode = LoginMode.None;
                                _loginStep = LoginStep.None;
                                _pendingSshTarget = null;
                                _pendingSshDevice = null;
                                _pendingSshUser = null;
                                return BuildRenderState("SSH login aborted.\n");
                            }

                            var user = _pendingSshUser;
                            var pass = input ?? "";

                            var expectedUser = _pendingSshDevice.Username;
                            var expectedPass = _pendingSshDevice.Password;

                            if (!string.Equals(user, expectedUser))
                            {
                                _loginStep = LoginStep.Username;
                                _pendingSshUser = null;
                                var lines = ConvertToOutputLines(
                                    "Login failed\n(To abort SSH login, type 'abort' as the username.)\n",
                                    isError: false);
                                return new TerminalRenderState
                                {
                                    Phase = TerminalPhase.Login,
                                    IsLoginPrompt = true,
                                    IsPasswordPrompt = false,
                                    Prompt = "Username: ",
                                    OutputLines = lines,
                                    Output = string.Join("\n", lines.Select(l => l.Text))
                                };
                            }
                            if (pass != expectedPass)
                            {
                                var lines = ConvertToOutputLines(
                                    "Login failed\n(To abort SSH login, type 'abort' as the password.)\n",
                                    isError: false);
                                return new TerminalRenderState
                                {
                                    Phase = TerminalPhase.Login,
                                    IsLoginPrompt = false,
                                    IsPasswordPrompt = true,
                                    Prompt = "Password: ",
                                    OutputLines = lines,
                                    Output = string.Join("\n", lines.Select(l => l.Text))
                                };
                            }

                            _session.SshStack.Push(new SshSessionContext(
                                _session.Username,
                                _session.Hostname,
                                _session.CurrentWorkingDirectory,
                                _session.FilesystemManager,
                                _session.NetworkManager
                            ));
                            _session.Username = user;
                            _session.Hostname = _pendingSshDevice.Hostname;
                            _session.CurrentWorkingDirectory = "/";
                            _session.DeviceInfo = _pendingSshDevice;
                            _session.FilesystemManager = _sshHandler.LoadFilesystemForDevice(_pendingSshDevice);
                            _session.NetworkManager.CurrentDevice = _pendingSshDevice;

                            var motd = !string.IsNullOrWhiteSpace(_pendingSshDevice.Motd)
                                ? _pendingSshDevice.Motd
                                : $"Connected to {_pendingSshDevice.Hostname} ({_pendingSshDevice.Ip})";

                            _phase = TerminalPhase.Terminal;
                            _loginMode = LoginMode.None;
                            _loginStep = LoginStep.None;
                            _pendingSshTarget = null;
                            _pendingSshDevice = null;
                            _pendingSshUser = null;
                            return BuildRenderState($"{motd}\n");
                        }
                    }

                    _loginMode = LoginMode.None;
                    _loginStep = LoginStep.None;
                    _pendingSshTarget = null;
                    _pendingSshDevice = null;
                    _pendingSshUser = null;
                    return new TerminalRenderState
                    {
                        Phase = TerminalPhase.Login,
                        IsLoginPrompt = true,
                        IsPasswordPrompt = false,
                        Prompt = "Username: ",
                        OutputLines = new List<TerminalOutputLine>(),
                        Output = string.Empty
                    };

                case TerminalPhase.Terminal:
                default:
                    var parsed = _commandParser.Parse(input);
                    if (parsed == null)
                        return BuildRenderState("", false);

                    if (parsed.CommandName == "ssh")
                    {
                        if (parsed.Arguments.Length == 0 || string.IsNullOrWhiteSpace(parsed.Arguments[0]))
                            return BuildRenderState("bash: ssh: Missing host\n\n", true);

                        string user = null, host = null;
                        var target = parsed.Arguments[0].Trim();
                        if (target.Contains("@"))
                        {
                            var split = target.Split('@');
                            user = split[0];
                            host = split[1];
                        }
                        else
                        {
                            host = target;
                        }

                        var device = _networkManager.FindDevice(host);
                        if (device == null || device.Ports == null || !device.Ports.Contains("22"))
                            return BuildRenderState($"ssh: connect to host {host} port 22: Connection refused\n", true);

                        if (device.Ip == _session.DeviceInfo.Ip)
                        {
                            return BuildRenderState(
                                $"ssh: you are already on {device.Hostname} ({device.Ip})\n",
                                true
                            );
                        }

                        foreach (var ctx in _session.SshStack)
                        {
                            if (ctx.Hostname == device.Hostname)
                            {
                                return BuildRenderState(
                                    $"ssh: cyclic login detected - already connected to {device.Hostname}\n",
                                    true
                                );
                            }
                        }

                        _pendingSshTarget = host;
                        _pendingSshDevice = device;
                        _pendingSshUser = user;

                        _phase = TerminalPhase.Login;
                        _loginMode = LoginMode.Ssh;

                        if (string.IsNullOrEmpty(user))
                        {
                            _loginStep = LoginStep.Username;
                            return new TerminalRenderState
                            {
                                Phase = TerminalPhase.Login,
                                IsLoginPrompt = true,
                                IsPasswordPrompt = false,
                                Prompt = "Username: ",
                                OutputLines = ConvertToOutputLines("(To abort SSH login, type 'abort' as the username.)\n", false)
                            };
                        }
                        else
                        {
                            _loginStep = LoginStep.Password;
                            return new TerminalRenderState
                            {
                                Phase = TerminalPhase.Login,
                                IsLoginPrompt = false,
                                IsPasswordPrompt = true,
                                Prompt = "Password: ",
                                OutputLines = ConvertToOutputLines("(To abort SSH login, type 'abort' as the password.)\n", false)
                            };
                        }
                    }
                    else if (parsed.CommandName == "exit")
                    {
                        if (_session.SshStack.Count > 0)
                        {
                            var prev = _session.SshStack.Pop();
                            _session.Username = prev.Username;
                            _session.Hostname = prev.Hostname;
                            _session.CurrentWorkingDirectory = prev.CurrentWorkingDirectory;
                            _session.FilesystemManager = prev.FilesystemManager;
                            _session.NetworkManager = prev.NetworkManager;
                            _session.DeviceInfo = _session.NetworkManager.CurrentDevice;

                            var banner = $"Connection to {_session.Hostname} closed.\n";
                            return BuildRenderState(banner, false);
                        }
                        else
                        {
                            return BuildRenderState("You are already at the local terminal.\n", false);
                        }
                    }

                    var result = _commandHandler.Execute(input, _session);
                    return BuildRenderState(result);

            }
        }

        public void Reset()
        {
            Initialize();
        }

        public void SetDebugMode(bool enabled)
        {
            if (_session != null)
                _session.DebugMode = enabled;

            _commandHandler = new CommandHandler(enabled);
        }

        private TerminalRenderState BuildRenderState(string output, bool isError = false)
        {
            return new TerminalRenderState
            {
                Phase = _phase,
                BootSequenceLines = _phase == TerminalPhase.Booting
                    ? JsonLoader.BootConfig?.BootText ?? new List<string>()
                    : null,

                IsLoginPrompt = _phase == TerminalPhase.Login && _loginStep == LoginStep.Username,
                IsPasswordPrompt = _phase == TerminalPhase.Login && _loginStep == LoginStep.Password,
                Prompt = _phase == TerminalPhase.Terminal
                    ? $"{_session.Username}@{_session.Hostname}:{_session.CurrentWorkingDirectory}$ "
                    : (_phase == TerminalPhase.Login && _loginStep == LoginStep.Username ? "Username: "
                      : (_phase == TerminalPhase.Login && _loginStep == LoginStep.Password ? "Password: " : "")),

                Output = output,
                CurrentDirectory = _session.CurrentWorkingDirectory,
                DirectoryListing = _filesystemManager.ListCurrentDirectory(),
                SessionStackDepth = _session.SshStack.Count,
                SessionStackView = BuildSessionStackView(),
                ClearScreen = false,
                IsError = isError,
                ErrorMessage = isError ? output : null,
                ShowMOTD = false,
                MOTD = _session.DeviceInfo?.Motd,
                DebugMode = _session.DebugMode,
                DebugInfo = null,
                DeviceInfo = _session.DeviceInfo
            };
        }

        // NEW: Properly hand off typed output from commands to terminal state
        private TerminalRenderState BuildRenderState(CommandResult result)
        {
            return new TerminalRenderState
            {
                Phase = _phase,
                BootSequenceLines = _phase == TerminalPhase.Booting
                    ? JsonLoader.BootConfig?.BootText ?? new List<string>()
                    : null,

                IsLoginPrompt = _phase == TerminalPhase.Login && _loginStep == LoginStep.Username,
                IsPasswordPrompt = _phase == TerminalPhase.Login && _loginStep == LoginStep.Password,
                Prompt = _phase == TerminalPhase.Terminal
                    ? $"{_session.Username}@{_session.Hostname}:{_session.CurrentWorkingDirectory}$ "
                    : (_phase == TerminalPhase.Login && _loginStep == LoginStep.Username ? "Username: "
                        : (_phase == TerminalPhase.Login && _loginStep == LoginStep.Password ? "Password: " : "")),

                Output = result.Output,
                OutputLines = result.OutputLines,
                CurrentDirectory = _session.CurrentWorkingDirectory,
                DirectoryListing = _filesystemManager.ListCurrentDirectory(),
                SessionStackDepth = _session.SshStack.Count,
                SessionStackView = BuildSessionStackView(),
                ClearScreen = false,
                IsError = result.IsError,
                ErrorMessage = result.IsError ? result.Output : null,
                ShowMOTD = false,
                MOTD = _session.DeviceInfo?.Motd,
                DebugMode = _session.DebugMode,
                DebugInfo = null,
                DeviceInfo = _session.DeviceInfo
            };
        }

        
        private List<SessionContext> BuildSessionStackView()
        {
            var stack = new List<SessionContext>();
            foreach (var ssh in _session.SshStack)
            {
                stack.Add(new SessionContext
                {
                    Username = ssh.Username,
                    Hostname = ssh.Hostname,
                    Prompt = $"{ssh.Username}@{ssh.Hostname}:{ssh.CurrentWorkingDirectory}$ "
                });
            }
            stack.Add(new SessionContext
            {
                Username = _session.Username,
                Hostname = _session.Hostname,
                Prompt = $"{_session.Username}@{_session.Hostname}:{_session.CurrentWorkingDirectory}$ "
            });
            return stack;
        }

        // Helper: Convert plain string to typed output lines
        private List<TerminalOutputLine> ConvertToOutputLines(string text, bool isError = false)
        {
            if (string.IsNullOrEmpty(text))
                return new List<TerminalOutputLine>();

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            var outputType = isError ? OutputType.Error : OutputType.Standard;

            var result = new List<TerminalOutputLine>();
            foreach (var line in lines)
            {
                result.Add(new TerminalOutputLine(line, outputType));
            }
            return result;
        }

        // Helper: Convert boot text lines to typed output lines
        private List<TerminalOutputLine> ConvertBootLinesToOutputLines(List<string> bootLines)
        {
            var result = new List<TerminalOutputLine>();
            foreach (var line in bootLines ?? new List<string>())
            {
                result.Add(new TerminalOutputLine(line, OutputType.Boot));
            }
            return result;
        }
    }
}
