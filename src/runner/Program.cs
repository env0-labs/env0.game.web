using System;
using System.Collections.Generic;
using Env0.Act3;
using Env0.Core;
using env0.act1;
using env0.act2;

namespace Env0.Runner
{
    internal static class Program
    {
        private static int Main()
        {
            while (true)
            {
                Console.WriteLine("Select an act to launch:");
                Console.WriteLine("  1) Act 1");
                Console.WriteLine("  2) Act 2");
                Console.WriteLine("  3) Act 3");
                Console.WriteLine("  4) Act 4 (placeholder)");
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
                        RunWithRouting(new Act1Module());
                        break;
                    case "2":
                        RunWithRouting(new Act2Module());
                        break;
                    case "3":
                        RunWithRouting(new Act3Module());
                        break;
                    case "4":
                        Console.WriteLine("Act 4 runner is a placeholder for now.");
                        break;
                    default:
                        Console.WriteLine("Unknown option. Please choose 1-4 or Q.");
                        break;
                }

                Console.WriteLine();
            }
        }

        private static void RunWithRouting(IActModule module)
        {
            var next = RunModule(module);
            while (next != ActRoute.None)
            {
                var routedModule = CreateModule(next);
                next = RunModule(routedModule);
            }
        }

        private static ActRoute RunModule(IActModule module)
        {
            var originalDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            var session = new SessionState { NextAct = ActRoute.None };
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
            return session.NextAct;
        }

        private static IActModule CreateModule(ActRoute route)
        {
            return route switch
            {
                ActRoute.Act1 => new Act1Module(),
                ActRoute.Act2 => new Act2Module(),
                ActRoute.Act3 => new Act3Module(),
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
