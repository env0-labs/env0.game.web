using Env0.Terminal.Terminal;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Network;

namespace Env0.Terminal.Terminal.Commands
{
    public class ExitCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            var result = new CommandResult();

            if (session.SshStack != null && session.SshStack.Count > 0)
            {
                // Pop previous SSH context
                var prev = session.SshStack.Pop();

                session.Username = prev.Username;
                session.Hostname = prev.Hostname;
                session.CurrentWorkingDirectory = prev.CurrentWorkingDirectory;
                session.FilesystemManager = prev.FilesystemManager;
                session.NetworkManager = prev.NetworkManager;

                result.AddLine($"Connection to {session.Hostname} closed.\n", OutputType.Standard);
                result.StateChanged = true;
                result.UpdatedSession = session;
            }
            else
            {
                result.AddLine("logout: Not implemented.\n", OutputType.Error);
            }

            return result;
        }
    }
}