namespace env0.adventure.Model;

public sealed class SceneDefinition
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public bool IsEnd { get; init; }
    public required List<ChoiceDefinition> Choices { get; init; }
}