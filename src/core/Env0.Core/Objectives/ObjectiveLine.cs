using System;

namespace Env0.Core.Objectives;

public static class ObjectiveLine
{
    public static bool TryGet(SessionState state, ContextRoute route, out string line)
    {
        line = string.Empty;
        if (state == null)
            return false;

        var key = BuildKey(state, route);

        // Throttle: print only when objective meaningfully changes, or occasionally.
        var shouldRepeat = (state.InputTicks - state.LastObjectiveAtTick) >= 8;
        if (string.Equals(state.LastObjectiveKey, key, StringComparison.OrdinalIgnoreCase) && !shouldRepeat)
            return false;

        state.LastObjectiveKey = key;
        state.LastObjectiveAtTick = state.InputTicks;

        line = BuildLine(state, route);
        return !string.IsNullOrWhiteSpace(line);
    }

    private static string BuildKey(SessionState state, ContextRoute route)
    {
        if (!state.MaintenanceExitUnlocked)
            return $"phase:bootstrap|route:{route}|return:{state.TerminalReturnContext}|batches:{state.BatchesCompleted}|manual:{state.ManualCompletions}";

        return $"phase:post_unlock|route:{route}|return:{state.TerminalReturnContext}|batches:{state.BatchesCompleted}";
    }

    private static string BuildLine(SessionState state, ContextRoute route)
    {
        // Corporate/diegetic phrasing, still explicit.
        if (!state.MaintenanceExitUnlocked)
        {
            return route switch
            {
                ContextRoute.Maintenance => "Directive: process containers. Record a batch to unlock exit.",
                ContextRoute.Terminal => "Directive: follow maintenance instructions. Exit CLI when complete.",
                ContextRoute.Records => "Directive: return to Maintenance. Your first batch is still missing.",
                _ => "Directive: process containers. Record a batch to unlock exit."
            };
        }

        if (route == ContextRoute.Maintenance)
            return "Directive: exit maintenance. Return to Records for the next instruction.";

        if (route == ContextRoute.Terminal)
        {
            // Terminal is used as a sub-context from both Maintenance and Records.
            return state.TerminalReturnContext switch
            {
                ContextRoute.Records => "Directive: acquire retention evidence. Return to Records with verifiable output.",
                ContextRoute.Maintenance => "Directive: follow maintenance instructions. Exit CLI when complete.",
                _ => "Directive: run only what you can justify. Return when the machine state matches the record."
            };
        }

        return "Directive: follow Records. Keep work traceable.";
    }
}
