using System;

namespace Env0.Core.Objectives;

public static class ObjectiveLine
{
    public static string Get(SessionState state, ContextRoute route)
    {
        if (state == null)
            return string.Empty;

        // Early-game: teach the core loop explicitly.
        if (!state.MaintenanceExitUnlocked)
        {
            // Maintenance is the start route in the Avalonia runner.
            return route switch
            {
                ContextRoute.Maintenance => "Objective: process containers. Record a batch to unlock exit.",
                ContextRoute.Terminal => "Objective: follow maintenance instructions. Return when ready.",
                ContextRoute.Records => "Objective: return to Maintenance. Record your first batch.",
                _ => "Objective: process containers. Record a batch to unlock exit."
            };
        }

        // Once exit is unlocked, push the player back into Records.
        if (route == ContextRoute.Maintenance)
            return "Objective: exit maintenance. Return to Records for the next instruction.";

        return "Objective: follow Records. Keep work traceable."
            + (state.BatchesCompleted > 0 ? $" (batches: {state.BatchesCompleted})" : string.Empty);
    }
}
