using env0.records.Model;

namespace env0.records.Engine;

public enum InputRouteKind
{
    ResolvedChoice,
    MetaCommand,
    Failure
}

public enum InputMetaCommand
{
    Options
}

public enum InputFailureKind
{
    UnknownVerb,
    KnownVerbButNoMatchingCommand,
    Ambiguous
}

public sealed class InputRouteResult
{
    private InputRouteResult(
        InputRouteKind kind,
        ChoiceDefinition? choice,
        InputMetaCommand? metaCommand,
        InputFailureKind? failureKind,
        string attemptedVerbToken,
        IReadOnlyList<string>? ambiguousCommands)
    {
        Kind = kind;
        Choice = choice;
        MetaCommand = metaCommand;
        FailureKind = failureKind;
        AttemptedVerbToken = attemptedVerbToken;
        AmbiguousCommands = ambiguousCommands;
    }

    public InputRouteKind Kind { get; }
    public ChoiceDefinition? Choice { get; }
    public InputMetaCommand? MetaCommand { get; }
    public InputFailureKind? FailureKind { get; }
    public string AttemptedVerbToken { get; }
    public IReadOnlyList<string>? AmbiguousCommands { get; }

    public static InputRouteResult ResolvedChoice(ChoiceDefinition choice)
    {
        if (choice is null)
            throw new ArgumentNullException(nameof(choice));

        return new InputRouteResult(
            InputRouteKind.ResolvedChoice,
            choice,
            null,
            null,
            string.Empty,
            null
        );
    }

    public static InputRouteResult Meta(InputMetaCommand metaCommand)
    {
        return new InputRouteResult(
            InputRouteKind.MetaCommand,
            null,
            metaCommand,
            null,
            string.Empty,
            null
        );
    }

    public static InputRouteResult Failure(
        InputFailureKind failureKind,
        string attemptedVerbToken,
        IReadOnlyList<string>? ambiguousCommands = null)
    {
        return new InputRouteResult(
            InputRouteKind.Failure,
            null,
            null,
            failureKind,
            attemptedVerbToken ?? string.Empty,
            ambiguousCommands
        );
    }
}
