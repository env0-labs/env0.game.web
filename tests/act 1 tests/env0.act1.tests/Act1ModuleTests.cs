using System;
using System.Collections.Generic;
using System.Linq;
using Env0.Core;
using env0.act1;

namespace Env0.Act1.Tests;

public sealed class Act1ModuleTests
{
    [Fact]
    public void Handle_FirstCall_BootsAndPromptsOnly()
    {
        var module = new Act1Module();
        var session = new SessionState();

        var output = module.Handle(string.Empty, session).ToList();

        Assert.Contains(output, line => line.Text == "env0.act1 booting");
        Assert.Contains(output, line => line.Text == "> " && line.NewLine == false);
        Assert.DoesNotContain(output, line => line.Text == "STATUS");
        Assert.False(session.IsComplete);
    }

    [Fact]
    public void Handle_Process_PrintsScriptLogAndReturnsPrompt()
    {
        var module = new Act1Module();
        var session = new SessionState();
        module.Handle(string.Empty, session).ToList();

        var output = module.Handle("process", session).ToList();

        Assert.Contains(output, line => line.Text == "PROCESSING");
        Assert.Contains(output, line => line.Text == "[kill-child] start");
        Assert.Contains(output, line => line.Text == "[kill-child] done");
        Assert.Contains(output, line => line.Text == "> " && line.NewLine == false);
    }

    [Fact]
    public void Handle_Status_PrintsContainerHeader()
    {
        var module = new Act1Module();
        var session = new SessionState();
        module.Handle(string.Empty, session).ToList();

        var output = module.Handle("status", session).ToList();

        Assert.Contains(output, line => line.Text == "STATUS");
        Assert.Contains(output, line => line.Text == "Current parent container: ACTIVE");
        Assert.Contains(output, line => line.Text.StartsWith("Children remaining:", StringComparison.Ordinal));
    }

    [Fact]
    public void Handle_BatchPrompt_AppearsAfterFiveParentsAndCanCompleteBatches()
    {
        var module = new Act1Module();
        var session = new SessionState();
        module.Handle(string.Empty, session).ToList();

        var promptCount = 0;
        var safety = 0;

        while (!session.IsComplete && safety < 500)
        {
            safety++;
            var output = module.Handle("process", session).ToList();

            if (output.Any(line => line.Text == "Batch completed containers? (y/n)"))
            {
                promptCount++;
                var response = module.Handle("y", session).ToList();
                Assert.Contains(response, line => line.Text == "Batch recorded.");
            }
        }

        Assert.Equal(3, promptCount);
        Assert.True(session.IsComplete);
    }
}
