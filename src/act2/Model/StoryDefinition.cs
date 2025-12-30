namespace env0.adventure.Model;

public sealed class StoryDefinition
{
    public required string StartSceneId { get; init; }
    public required List<SceneDefinition> Scenes { get; init; }
}
