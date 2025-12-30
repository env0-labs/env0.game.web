using System;

namespace Env0.Terminal
{
    public static class DebugUtility
    {
        // Debug mode is OFF by default; set to true in Main or wherever you want extra output.
        public static bool Enabled = false;

        /// <summary>
        /// Print a debug message with a tag.
        /// </summary>
        public static void Print(string tag, string message)
        {
            if (Enabled)
                Console.WriteLine($"[DEBUG:{tag}] {message}");
        }

        /// <summary>
        /// Print a debug message with a tag and the caller method context.
        /// </summary>
        public static void PrintContext(string tag, string message, [System.Runtime.CompilerServices.CallerMemberName] string member = "")
        {
            if (Enabled)
                Console.WriteLine($"[DEBUG:{tag}:{member}] {message}");
        }
    }
}