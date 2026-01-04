using System;
using System.Collections.Generic;
using System.Linq;
using Env0.Core;
using env0.maintenance;

namespace Env0.Maintenance.Tests;

public sealed class MaintenanceModuleTests
{
    [Fact]
    public void Handle_FirstCall_BootsAndPromptsOnly()
    {
        var module = new MaintenanceModule();
        var session = new SessionState();

        var output = module.Handle(string.Empty, session).ToList();

        Assert.Contains(output, line => line.Text == "env0.maintenance booting");
        Assert.Contains(output, line => line.Text == "> " && line.NewLine == false);
        Assert.DoesNotContain(output, line => line.Text == "STATUS");
        Assert.False(session.IsComplete);
    }

    [Fact]
    public void Handle_Process_PrintsScriptLogAndReturnsPrompt()
    {
        var module = new MaintenanceModule();
        var session = new SessionState();
        module.Handle(string.Empty, session).ToList();

        var output = module.Handle("process", session).ToList();

        Assert.Contains(output, line => line.Text == "PROCESSING");
        Assert.Contains(output, line => line.Text == "[kill-child] start");
        Assert.Contains(output, line => line.Text == "[kill-child] done");
        Assert.Contains(output, line => line.Text == "> " && line.NewLine == false);
    }

    [Fact]
    public void Handle_Exit_SetsNextContextAndCompletes()
    {
        var module = new MaintenanceModule();
        var session = new SessionState();
        module.Handle(string.Empty, session).ToList();

        var output = module.Handle("Exit", session).ToList();

        Assert.Contains(output, line => line.Text == "Exiting...");
        Assert.True(session.IsComplete);
        Assert.Equal(ContextRoute.Records, session.NextContext);
    }

    [Fact]
    public void Handle_Status_PrintsContainerHeader()
    {
        var module = new MaintenanceModule();
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
        var module = new MaintenanceModule();
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

    [Fact]
    public void Handle_LoadCli_SetsTerminalRoutingAndClearsMaintenanceFields()
    {
        var module = new MaintenanceModule();
        var session = new SessionState
        {
            MaintenanceFilesystem = "Filesystem_1.json",
            MaintenanceMachineId = "proc.floor01"
        };
        module.Handle(string.Empty, session).ToList();

        var output = module.Handle("load cli", session).ToList();

        Assert.Contains(output, line => line.Text == "Loading CLI...");
        Assert.True(session.IsComplete);
        Assert.Equal(ContextRoute.Terminal, session.NextContext);
        Assert.Equal(ContextRoute.Maintenance, session.TerminalReturnContext);
        Assert.Equal("Filesystem_1.json", session.TerminalStartFilesystem);
        Assert.Null(session.MaintenanceMachineId);
        Assert.Null(session.MaintenanceFilesystem);
    }

    [Fact]
    public void Handle_Exit_WithReturnScene_SetsResumeRecords()
    {
        var module = new MaintenanceModule();
        var session = new SessionState
        {
            RecordsReturnSceneId = "start",
            MaintenanceFilesystem = "Filesystem_9.json",
            MaintenanceMachineId = "records.retention01"
        };
        module.Handle(string.Empty, session).ToList();

        var output = module.Handle("exit", session).ToList();

        Assert.Contains(output, line => line.Text == "Exiting...");
        Assert.True(session.IsComplete);
        Assert.True(session.ResumeRecords);
        Assert.Equal(ContextRoute.Records, session.NextContext);
        Assert.Equal("start", session.RecordsReturnSceneId);
        Assert.Null(session.MaintenanceMachineId);
        Assert.Null(session.MaintenanceFilesystem);
    }

    [Fact]
    public void Handle_BatchPrompt_InvalidInput_ReturnsPromptAndKeepsRunning()
    {
        var module = new MaintenanceModule();
        var session = new SessionState();
        module.Handle(string.Empty, session).ToList();

        ProcessUntilBatchPrompt(module, session);
        var response = module.Handle("maybe", session).ToList();

        Assert.Contains(response, line => line.Text == "Unrecognised input. Accepted input: y / n");
        Assert.Contains(response, line => line.Text == "> " && line.NewLine == false);
        Assert.False(session.IsComplete);
    }

    private static void ProcessUntilBatchPrompt(MaintenanceModule module, SessionState session)
    {
        var safety = 0;
        while (safety < 500)
        {
            safety++;
            var output = module.Handle("process", session).ToList();
            if (output.Any(line => line.Text == "Batch completed containers? (y/n)"))
                return;
        }

        throw new InvalidOperationException("Batch prompt was not reached within safety limit.");
    }
}


