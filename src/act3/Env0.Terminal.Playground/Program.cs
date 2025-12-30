using System;
using Env0.Terminal;
using Env0.Terminal.API_DTOs;

namespace Env0.Terminal.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = new TerminalEngineAPI();
            DebugUtility.Enabled = false;
            api.Initialize();

            Console.WriteLine("env0.terminal playground (type 'exit' to quit)");
            Console.WriteLine("----------------------------------------------");

            TerminalRenderState state = api.Execute(""); // First call

            while (true)
            {
                switch (state.Phase)
                {
                    case TerminalPhase.Booting:
                        if (state.BootSequenceLines != null)
                        {
                            foreach (var line in state.BootSequenceLines)
                            {
                                Console.WriteLine(line);
                                if (DebugUtility.Enabled)
                                    Console.WriteLine("[OutputType: Boot]");
                            }
                        }
                        state = api.Execute(""); // Advance to login
                        continue;

                    case TerminalPhase.Login:
                        while (state.Phase == TerminalPhase.Login)
                        {
                            PrintOutputLines(state);

                            if (state.IsLoginPrompt)
                            {
                                Console.Write(state.Prompt ?? "Username: ");
                                var username = Console.ReadLine();
                                if (username == null) continue;
                                state = api.Execute(username);
                            }
                            else if (state.IsPasswordPrompt)
                            {
                                Console.Write(state.Prompt ?? "Password: ");
                                var password = ReadPassword();
                                if (password == null) continue;
                                state = api.Execute(password);
                            }
                            else
                            {
                                break; // Unexpected state, return to outer loop
                            }
                        }
                        continue;

                    case TerminalPhase.Terminal:
                    default:
                        PrintOutputLines(state);

                        while (true)
                        {
                            Console.Write(state.Prompt);
                            var input = Console.ReadLine();
                            if (input == null) continue;

                            state = api.Execute(input);

                            // Immediately break to handle phase transitions
                            if (state.Phase == TerminalPhase.Login)
                                break;

                            PrintOutputLines(state);

                            if (state.IsError && !string.IsNullOrWhiteSpace(state.ErrorMessage))
                                Console.WriteLine($"[ERROR] {state.ErrorMessage}");

                            if (state.ShowMOTD && !string.IsNullOrWhiteSpace(state.MOTD))
                                Console.WriteLine($"[MOTD] {state.MOTD}");
                        }
                        continue;
                }
            }
        }

        /// <summary>
        /// Prints the output lines with optional debug output type tags.
        /// Clears the output lines after printing.
        /// </summary>
        static void PrintOutputLines(TerminalRenderState state)
        {
            if (state.OutputLines != null && state.OutputLines.Count > 0)
            {
                foreach (var line in state.OutputLines)
                {
                    Console.WriteLine(line.Text);
                    if (DebugUtility.Enabled)
                        Console.WriteLine($"[OutputType: {line.Type}]");
                }
                state.OutputLines.Clear();
            }
        }

        /// <summary>
        /// Reads a password from the console without echoing input.
        /// </summary>
        static string ReadPassword()
        {
            var password = string.Empty;
            ConsoleKeyInfo key;
            while (true)
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                        password = password.Substring(0, password.Length - 1);
                    continue;
                }
                if (char.IsControl(key.KeyChar))
                    continue;
                password += key.KeyChar;
            }
            Console.WriteLine();
            return password;
        }
    }
}
