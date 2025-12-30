namespace env0.adventure.Model;

public sealed class EffectDefinition
{
    public required EffectType Type { get; init; }
    public string? Value { get; init; }
}