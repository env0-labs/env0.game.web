namespace Env0.Terminal.Terminal
{
    public interface ICommand
    {
        CommandResult Execute(SessionState session, string[] args);
    }
}
