using Env0.Terminal.Terminal;

namespace Env0.Terminal.Terminal.Commands
{
    public class HelpCommand : ICommand
    {
        public CommandResult Execute(SessionState session, string[] args)
        {
            // Simple hardcoded help text. Update as new commands are added.
            string helpText = @"
Available commands:

  ls [dir]         List files and folders in a directory
  cd [dir]         Change current directory
  cat [file]       Show contents of a text file
  read [file]      Paginated view of a text file
  echo [text]      Print text to the terminal
  clear            Clear the terminal output
  ping [host]      Ping a network device (stub)
  nmap             List devices on the subnet (stub)
  ssh [host]       SSH into another device (stub)
  exit             Exit SSH session or terminal (contextual)
  sudo [cmd]       Attempt to run as root (Easter egg)
  help             Show this help message
  ifconfig         Show network interfaces

Type 'help [command]' for more info (not yet implemented).
";

            return new CommandResult(helpText.Trim(), OutputType.Standard);
        }
    }
}