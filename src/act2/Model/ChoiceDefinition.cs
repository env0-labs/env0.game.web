namespace env0.adventure.Model;

public sealed class ChoiceDefinition
{
    public required int Number { get; init; }
    public required string Text { get; init; }

    public List<string>? RequiresAll { get; init; }
    public List<string>? RequiresNone { get; init; }

    public string? DisabledReason { get; init; }

    public required List<EffectDefinition> Effects { get; init; }
    
    public string? ResultText { get; set; }

}