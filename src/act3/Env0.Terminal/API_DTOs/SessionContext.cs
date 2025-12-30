namespace Env0.Terminal.API_DTOs
{
    /// <summary>
    /// Represents a single session or SSH hop context for UI/API consumers.
    /// </summary>
    public class SessionContext
    {
        public string Username { get; set; }
        public string Hostname { get; set; }
        public string Prompt { get; set; }
    }
}