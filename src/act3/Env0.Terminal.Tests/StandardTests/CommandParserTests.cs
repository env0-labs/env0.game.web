using Xunit;
using Env0.Terminal.Terminal; // Change if your namespace is different

[Trait("TestType", "Critical")]
public class CommandParserTests
{
    [Fact]
    public void Parse_LowercaseCommand_SingleArg()
    {
        var parser = new CommandParser();
        var result = parser.Parse("ls /home");
        Assert.NotNull(result);
        Assert.Equal("ls", result.CommandName);
        Assert.Single(result.Arguments);
        Assert.Equal("/home", result.Arguments[0]);
    }

    [Fact]
    public void Parse_UppercaseCommand_SingleArg()
    {
        var parser = new CommandParser();
        var result = parser.Parse("LS /home/user");
        Assert.NotNull(result);
        Assert.Equal("ls", result.CommandName); // Should be lowercased
        Assert.Single(result.Arguments);
        Assert.Equal("/home/user", result.Arguments[0]);
    }

    [Fact]
    public void Parse_CommandWithDangerousChars_StripsThem()
    {
        var parser = new CommandParser();
        var result = parser.Parse("cat; rm -rf /");
        Assert.NotNull(result);
        Assert.Equal("cat", result.CommandName);
        // Dangerous chars removed, so args become ["rm", "-rf", "/"]
        Assert.Equal(new[] { "rm", "-rf", "/" }, result.Arguments);
    }

    [Fact]
    public void Parse_BlankInput_ReturnsNull()
    {
        var parser = new CommandParser();
        var result = parser.Parse("   ");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_MixedCaseCommand_MultipleArgs()
    {
        var parser = new CommandParser();
        var result = parser.Parse("eChO Hello   World");
        Assert.NotNull(result);
        Assert.Equal("echo", result.CommandName);
        Assert.Equal(new[] { "Hello", "World" }, result.Arguments);
    }
}
