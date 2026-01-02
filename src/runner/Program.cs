using System;
using System.Collections.Generic;
using Env0.Terminal;
using Env0.Core;
using env0.maintenance;
using env0.records;

namespace Env0.Runner
{
    internal static class Program
    {
        private static int Main()
        {
            while (true)
            {
                Console.WriteLine("Select a context to launch:");
                Console.WriteLine("  1) Maintenance");
                Console.WriteLine("  2) Records");
                Console.WriteLine("  3) Terminal");
                Console.WriteLine("  4) Context (placeholder)");
                Console.WriteLine("  Q) Quit");
                Console.Write("> ");

                var input = Console.ReadLine();
                if (input == null)
                {
                    return 0;
                }

                input = input.Trim();
                if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }

                switch (input)
                {
                    case "1":
                        RunWithRouting(new MaintenanceModule());
                        break;
                    case "2":
                        RunWithRouting(new RecordsModule());
                        break;
                    case "3":
                        RunWithRouting(new TerminalModule());
                        break;
                    case "4":
                        Console.WriteLine("Context runner is a placeholder for now.");
                        break;
                    default:
                        Console.WriteLine("Unknown option. Please choose 1-4 or Q.");
                        break;
                }

                Console.WriteLine();
            }
        }

        private static void RunWithRouting(IContextModule module)
        {
            var next = RunModule(module);
            while (next != ContextRoute.None)
            {
                var routedModule = CreateModule(next);
                next = RunModule(routedModule);
            }
        }

        private static ContextRoute RunModule(IContextModule module)
        {
            var originalDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            var session = new SessionState { NextContext = ContextRoute.None };
            PrintOutput(module.Handle(string.Empty, session));

            while (!session.IsComplete)
            {
                var input = Console.ReadLine();
                if (input == null)
                {
                    session.IsComplete = true;
                    break;
                }

                PrintOutput(module.Handle(input, session));
            }

            Environment.CurrentDirectory = originalDirectory;
            return session.NextContext;
        }

        private static IContextModule CreateModule(ContextRoute route)
        {
            return route switch
            {
                ContextRoute.Maintenance => new MaintenanceModule(),
                ContextRoute.Records => new RecordsModule(),
                ContextRoute.Terminal => new TerminalModule(),
                _ => throw new InvalidOperationException($"Unknown route: {route}")
            };
        }

        private static void PrintOutput(IEnumerable<OutputLine> lines)
        {
            if (lines == null)
            {
                return;
            }

            foreach (var line in lines)
            {
                var text = line.Text ?? string.Empty;
                if (line.NewLine)
                {
                    Console.WriteLine(text);
                }
                else
                {
                    Console.Write(text);
                }
            }
        }
    }
}



