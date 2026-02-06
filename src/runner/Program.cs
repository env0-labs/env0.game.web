using System;
using System.Collections.Generic;
using Env0.Core;
using env0.maintenance;
using env0.records;

namespace Env0.Runner
{
    internal static class Program
    {
        private static readonly RecordsModule RecordsModuleInstance = new RecordsModule();

        private static int Main()
        {
            RunWithRouting(new MaintenanceModule());
            return 0;
        }

        private static void RunWithRouting(IContextModule module)
        {
            var session = new SessionState();
            var next = RunModule(module, session);
            while (next != ContextRoute.None)
            {
                var routedModule = CreateModule(next);
                next = RunModule(routedModule, session);
            }
        }

        private static ContextRoute RunModule(IContextModule module, SessionState session)
        {
            var originalDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            session.IsComplete = false;
            session.NextContext = ContextRoute.None;
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
                ContextRoute.Records => RecordsModuleInstance,
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
