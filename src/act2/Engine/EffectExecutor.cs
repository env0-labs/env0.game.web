using env0.adventure.Model;
using env0.adventure.Runtime;

namespace env0.adventure.Engine;

public sealed class EffectExecutor
{
    public void Execute(IEnumerable<EffectDefinition> effects, GameState state)
    {
        foreach (var effect in effects)
        {
            switch (effect.Type)
            {
                case EffectType.SetFlag:
                    RequireValue(effect);
                    state.SetFlag(effect.Value!);
                    break;

                case EffectType.ClearFlag:
                    RequireValue(effect);
                    state.ClearFlag(effect.Value!);
                    break;

                case EffectType.GotoScene:
                    RequireValue(effect);
                    state.SetScene(effect.Value!);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown effect type: {effect.Type}"
                    );
            }
        }
    }

    private static void RequireValue(EffectDefinition effect)
    {
        if (string.IsNullOrWhiteSpace(effect.Value))
            throw new InvalidOperationException(
                $"Effect {effect.Type} requires a value."
            );
    }
}