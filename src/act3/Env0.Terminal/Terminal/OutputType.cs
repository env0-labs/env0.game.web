namespace Env0.Terminal.Terminal
{
    public enum OutputType
    {
        Standard,        // Default output
        Error,           // Errors
        System,          // System messages (e.g., prompts, banners)
        Boot,            // Boot sequence lines
        PromptInstruction, // Login or SSH prompt instructions
        MOTD,            // Message of the day/banner
        Debug,           // Debug/dev output
        Corrupted,       // For future "corrupted"/atmospheric output
        Scan,            // For nmap or similar “scan” operations
        Ping,            // For ping command outputs
        Auth,            // For ssh login prompts/output
    }
}