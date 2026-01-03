using env0.records.Model;

namespace env0.records.Engine;

public sealed class InputRouter
{
    private static readonly HashSet<string> HelpCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "help",
        "options"
    };

    private static bool IsHelpCommand(string input)
    {
        var normalized = ChoiceInputNormalizer.Normalize(input);
        return HelpCommands.Contains(normalized);
    }

    public InputRouteResult Resolve(string input, SceneDefinition scene)
    {
        if (string.IsNullOrWhiteSpace(input))
            return InputRouteResult.Failure(InputFailureKind.UnknownVerb, string.Empty);

        var trimmed = input.Trim();
        if (IsHelpCommand(trimmed))
            return InputRouteResult.Meta(InputMetaCommand.Options);

        if (IsDigitsOnly(trimmed) && int.TryParse(trimmed, out var index))
        {
            var choice = scene.Choices.FirstOrDefault(c => c.Index == index);
            return choice != null
                ? InputRouteResult.ResolvedChoice(choice)
                : InputRouteResult.Failure(InputFailureKind.UnknownVerb, string.Empty);
        }

        var normalized = ChoiceInputNormalizer.Normalize(trimmed);
        if (string.IsNullOrWhiteSpace(normalized))
            return InputRouteResult.Failure(InputFailureKind.UnknownVerb, string.Empty);

        var aliasMap = BuildAliasMap(scene);
        if (aliasMap.TryGetValue(normalized, out var matchedChoice))
            return InputRouteResult.ResolvedChoice(matchedChoice);

        var attemptedVerbToken = GetFirstToken(normalized);
        if (string.IsNullOrWhiteSpace(attemptedVerbToken))
            return InputRouteResult.Failure(InputFailureKind.UnknownVerb, string.Empty);

        var allowedVerbs = BuildAllowedVerbs(scene);
        return allowedVerbs.Contains(attemptedVerbToken)
            ? InputRouteResult.Failure(InputFailureKind.KnownVerbButNoMatchingCommand, attemptedVerbToken)
            : InputRouteResult.Failure(InputFailureKind.UnknownVerb, attemptedVerbToken);
    }

    private static bool IsDigitsOnly(string input)
    {
        foreach (var ch in input)
        {
            if (!char.IsDigit(ch))
                return false;
        }

        return input.Length > 0;
    }

    private static Dictionary<string, ChoiceDefinition> BuildAliasMap(SceneDefinition scene)
    {
        var map = new Dictionary<string, ChoiceDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var choice in scene.Choices)
        {
            foreach (var alias in choice.Aliases)
            {
                var normalized = ChoiceInputNormalizer.Normalize(alias);
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                if (map.TryGetValue(normalized, out var existingChoice) &&
                    !string.Equals(existingChoice.Id, choice.Id, StringComparison.OrdinalIgnoreCase))
                {
                    var existingCommand = $"{existingChoice.Verb} {existingChoice.Noun}";
                    var newCommand = $"{choice.Verb} {choice.Noun}";
                    throw new InvalidOperationException(
                        $"Alias collision for '{normalized}': {existingChoice.Id} ({existingCommand}) conflicts with {choice.Id} ({newCommand})."
                    );
                }

                map[normalized] = choice;
            }
        }

        return map;
    }

    private static HashSet<string> BuildAllowedVerbs(SceneDefinition scene)
    {
        var verbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var choice in scene.Choices)
        {
            if (!string.IsNullOrWhiteSpace(choice.Verb))
                verbs.Add(choice.Verb.Trim());
        }

        return verbs;
    }

    private static string GetFirstToken(string normalized)
    {
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length > 0 ? tokens[0] : string.Empty;
    }
}
