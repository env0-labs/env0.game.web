using env0.records.Model;

namespace env0.records.Engine;

public sealed class SceneRepository
{
    private readonly Dictionary<string, SceneDefinition> _scenes;

    public string StartSceneId { get; }

    public SceneRepository(StoryDefinition story)
    {
        if (story is null)
            throw new InvalidOperationException("Story definition is required.");

        if (string.IsNullOrWhiteSpace(story.StartSceneId))
            throw new InvalidOperationException("Story must define a StartSceneId.");

        if (story.Scenes is null || story.Scenes.Count == 0)
            throw new InvalidOperationException("Story must define at least one scene.");

        ValidateScenes(story.Scenes);

        _scenes = story.Scenes.ToDictionary(
            s => s.Id,
            s => s
        );

        if (!_scenes.ContainsKey(story.StartSceneId))
            throw new InvalidOperationException($"Start scene not found: {story.StartSceneId}");

        ValidateGotoTargets();

        StartSceneId = story.StartSceneId;
    }

    public SceneDefinition Get(string sceneId)
    {
        if (!_scenes.TryGetValue(sceneId, out var scene))
            throw new InvalidOperationException($"Scene not found: {sceneId}");

        return scene;
    }

    private static void ValidateScenes(IEnumerable<SceneDefinition> scenes)
    {
        foreach (var scene in scenes)
        {
            if (string.IsNullOrWhiteSpace(scene.Id))
                throw new InvalidOperationException("Scene Id is required.");

            if (string.IsNullOrWhiteSpace(scene.Text))
                throw new InvalidOperationException($"Scene {scene.Id} must define text.");

            if (scene.Choices is null)
                throw new InvalidOperationException($"Scene {scene.Id} must define choices.");

            var choiceNumbers = new HashSet<int>();

            foreach (var choice in scene.Choices)
            {
                if (!choiceNumbers.Add(choice.Number))
                    throw new InvalidOperationException($"Scene {scene.Id} has duplicate choice number {choice.Number}.");

                if (string.IsNullOrWhiteSpace(choice.Text))
                    throw new InvalidOperationException($"Choice {choice.Number} in scene {scene.Id} must define text.");

                if (choice.Effects is null || choice.Effects.Count == 0)
                    throw new InvalidOperationException($"Choice {choice.Number} in scene {scene.Id} must define effects.");
            }
        }
    }

    private void ValidateGotoTargets()
    {
        foreach (var scene in _scenes.Values)
        {
            foreach (var choice in scene.Choices)
            {
                foreach (var effect in choice.Effects)
                {
                    if (effect.Type == EffectType.GotoScene)
                    {
                        if (string.IsNullOrWhiteSpace(effect.Value))
                            throw new InvalidOperationException($"Effect GotoScene in scene {scene.Id} choice {choice.Number} requires a target scene.");

                        if (!_scenes.ContainsKey(effect.Value))
                            throw new InvalidOperationException($"Unknown target scene: {effect.Value} referenced from {scene.Id}");
                    }
                }
            }
        }
    }
}

