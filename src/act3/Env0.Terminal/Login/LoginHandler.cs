using Env0.Terminal.Terminal;

namespace Env0.Terminal.Login
{
    /// <summary>
    /// Handles player login flow (for now: just stores username and password, no validation).
    /// </summary>
    public class LoginHandler
    {
        public void SetUsername(SessionState session, string username)
        {
            session.Username = username;
        }

        public void SetPassword(SessionState session, string password)
        {
            session.Password = password;
        }
    }
}