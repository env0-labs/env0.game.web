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

            var choiceIndexes = new HashSet<int>();
            var choiceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var aliasMap = new Dictionary<string, (string ChoiceId, string CanonicalCommand)>(StringComparer.OrdinalIgnoreCase);

            foreach (var choice in scene.Choices)
            {
                if (string.IsNullOrWhiteSpace(choice.Id))
                    throw new InvalidOperationException($"Scene {scene.Id} must define choice Ids.");

                if (!choiceIds.Add(choice.Id))
                    throw new InvalidOperationException($"Scene {scene.Id} has duplicate choice Id {choice.Id}.");

                if (choice.Index <= 0)
                    throw new InvalidOperationException($"Choice {choice.Id} in scene {scene.Id} must define a positive index.");

                if (!choiceIndexes.Add(choice.Index))
                    throw new InvalidOperationException($"Scene {scene.Id} has duplicate choice index {choice.Index}.");

                if (string.IsNullOrWhiteSpace(choice.Verb))
                    throw new InvalidOperationException($"Choice {choice.Id} in scene {scene.Id} must define a verb.");

                if (string.IsNullOrWhiteSpace(choice.Noun))
                    throw new InvalidOperationException($"Choice {choice.Id} in scene {scene.Id} must define a noun.");

                if (choice.Aliases is null || choice.Aliases.Count == 0)
                    throw new InvalidOperationException($"Choice {choice.Id} in scene {scene.Id} must define aliases.");

                ValidateAliases(scene.Id, choice, aliasMap);

                if (choice.Effects is null || choice.Effects.Count == 0)
                    throw new InvalidOperationException($"Choice {choice.Id} in scene {scene.Id} must define effects.");
            }
        }
    }

    private static void ValidateAliases(
        string sceneId,
        ChoiceDefinition choice,
        Dictionary<string, (string ChoiceId, string CanonicalCommand)> aliasMap)
    {
        var canonical = $"{choice.Verb} {choice.Noun}";
        var canonicalNormalized = ChoiceInputNormalizer.Normalize(canonical);
        var containsCanonical = false;

        foreach (var alias in choice.Aliases)
        {
            var normalized = ChoiceInputNormalizer.Normalize(alias);
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException($"Choice {choice.Id} in scene {sceneId} has an empty alias.");

            if (string.Equals(normalized, canonicalNormalized, StringComparison.OrdinalIgnoreCase))
                containsCanonical = true;

            if (aliasMap.TryGetValue(normalized, out var existing) &&
                !string.Equals(existing.ChoiceId, choice.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Alias collision in scene {sceneId}: '{alias}' matches {existing.ChoiceId} ({existing.CanonicalCommand}) and {choice.Id} ({canonical})."
                );
            }

            aliasMap[normalized] = (choice.Id, canonical);
        }

        if (!containsCanonical)
            throw new InvalidOperationException($"Choice {choice.Id} in scene {sceneId} must include canonical alias '{canonical}'.");
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
                            throw new InvalidOperationException($"Effect GotoScene in scene {scene.Id} choice {choice.Id} requires a target scene.");

                        if (!_scenes.ContainsKey(effect.Value))
                            throw new InvalidOperationException($"Unknown target scene: {effect.Value} referenced from {scene.Id}");
                    }
                }
            }
        }
    }
}

