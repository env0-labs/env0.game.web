namespace env0.records.Engine;

public static class RecordsErrorPolicy
{
    private const string HelpHint = "Type 'options' to list available commands.";

    public static IReadOnlyList<string> BuildErrorLines(
        InputFailureKind kind,
        string attemptedVerbToken,
        IReadOnlyList<string> allowedVerbs,
        IReadOnlyList<string> allowedObjects,
        IReadOnlyList<string>? ambiguousCommands = null,
        bool includeHelpHint = true)
    {
        var lines = new List<string>();

        switch (kind)
        {
            case InputFailureKind.UnknownVerb:
                lines.Add("Unrecognised action.");
                break;
            case InputFailureKind.KnownVerbButNoMatchingCommand:
                lines.Add(BuildKnownVerbMessage(attemptedVerbToken));
                break;
            case InputFailureKind.Ambiguous:
                lines.Add("Ambiguous command.");
                if (ambiguousCommands != null && ambiguousCommands.Count > 0)
                {
                    var joined = string.Join(", ", ambiguousCommands);
                    lines.Add($"Matching commands: {joined}");
                }
                break;
            default:
                lines.Add("Unrecognised action.");
                break;
        }

        lines.Add($"Actions: {FormatList(allowedVerbs)}");
        lines.Add($"Objects: {FormatList(allowedObjects)}");

        if (includeHelpHint)
            lines.Add(HelpHint);

        return lines;
    }

    private static string BuildKnownVerbMessage(string attemptedVerbToken)
    {
        if (string.IsNullOrWhiteSpace(attemptedVerbToken))
            return "No available command matches that action in this room.";

        if (string.Equals(attemptedVerbToken, "read", StringComparison.OrdinalIgnoreCase))
            return "No readable item matches that command in this room.";

        if (string.Equals(attemptedVerbToken, "use", StringComparison.OrdinalIgnoreCase))
            return "No operational interface matches that command in this room.";

        if (string.Equals(attemptedVerbToken, "go", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attemptedVerbToken, "enter", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attemptedVerbToken, "return", StringComparison.OrdinalIgnoreCase))
            return "No route matches that command from here.";

        return "No available command matches that action in this room.";
    }

    private static string FormatList(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
            return "(none)";

        return string.Join(", ", items);
    }
}
