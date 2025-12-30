using env0.adventure.Model;
using env0.adventure.Runtime;

namespace env0.adventure.Engine;

public sealed class ChoiceEvaluator
{
    public bool IsEnabled(ChoiceDefinition choice, GameState state, out string? disabledReason)
    {
        // RequiresAll: every listed flag must be true
        if (choice.RequiresAll is not null)
        {
            foreach (var flag in choice.RequiresAll)
            {
                if (!state.HasFlag(flag))
                {
                    disabledReason = choice.DisabledReason;
                    return false;
                }
            }
        }

        // RequiresNone: none of the listed flags may be true
        if (choice.RequiresNone is not null)
        {
            foreach (var flag in choice.RequiresNone)
            {
                if (state.HasFlag(flag))
                {
                    disabledReason = choice.DisabledReason;
                    return false;
                }
            }
        }

        disabledReason = null;
        return true;
    }
}