using System;
using System.Linq;

namespace Env0.Terminal.Terminal
{
public class ParsedCommand
{
    public string CommandName { get; }
    public string[] Arguments { get; }

    public ParsedCommand(string commandName, string[] arguments)
    {
        CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }
}

public class CommandParser
{
    private static readonly char[] DangerousChars = new[] { ';', '|', '&' };

    public ParsedCommand Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Strip dangerous characters
        foreach (var c in DangerousChars)
        {
            input = input.Replace(c.ToString(), "");
        }

        // Split input by whitespace
        var tokens = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
            return null;

        var command = tokens[0].ToLowerInvariant(); // Commands are case-insensitive
        var arguments = tokens.Skip(1).ToArray();

        return new ParsedCommand(command, arguments);
    }
}
}