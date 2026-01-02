using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Env0.Core;
using env0.records;

namespace Env0.Records.Tests;

public sealed class RecordsModuleTests
{
    [Fact]
    public void Handle_FirstCall_LoadsFirstStoryAndRendersScene()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var alpha = "test_records_alpha.json";
        var beta = "test_records_beta.json";

        CreateStoryFile(alpha, BuildRunningStoryJson("Alpha scene."));
        CreateStoryFile(beta, BuildEndStoryJson("start", "Beta scene."));

        try
        {
            var output = module.Handle(string.Empty, session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.Contains("env0.records booting", texts);
            Assert.Contains("Alpha scene.", texts);
            Assert.DoesNotContain("Beta scene.", texts);
            Assert.False(session.IsComplete);
        }
        finally
        {
            DeleteStoryFile(alpha);
            DeleteStoryFile(beta);
        }
    }

    [Fact]
    public void Handle_InvalidInput_DoesNotCompleteAndRendersScene()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_running.json";

        CreateStoryFile(story, BuildRunningStoryJson("Start scene."));

        try
        {
            var startOutput = module.Handle(string.Empty, session).ToList();
            Assert.False(session.IsComplete);
            Assert.Contains("Start scene.", startOutput.Select(line => line.Text));

            var invalidOutput = module.Handle("abc", session).ToList();
            var invalidTexts = invalidOutput.Select(line => line.Text).ToList();

            Assert.Contains("Invalid input. Enter a number.", invalidTexts);
            Assert.False(session.IsComplete);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_EndScene_CompletesSession()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_complete.json";

        CreateStoryFile(story, BuildEndStoryJson("start", "End scene."));

        try
        {
            var output = module.Handle(string.Empty, session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.Contains("End scene.", texts);
            Assert.Contains("Game ended.", texts);
            Assert.True(session.IsComplete);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_TerminalChoice_SetsTerminalRoutingState()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_terminal.json";
        var devicesJson = @"[
  { ""recordsRoomId"": ""start"", ""filesystem"": ""Filesystem_1.json"" }
]";

        CreateStoryFile(story, BuildTerminalStoryJson("Terminal scene."));
        CreateDevicesFile(devicesJson);

        try
        {
            module.Handle(string.Empty, session).ToList();
            var output = module.Handle("1", session).ToList();

            Assert.True(session.IsComplete);
            Assert.Equal(ContextRoute.Terminal, session.NextContext);
            Assert.Equal(ContextRoute.Records, session.TerminalReturnContext);
            Assert.Equal("Filesystem_1.json", session.TerminalStartFilesystem);
            Assert.Equal("start", session.RecordsReturnSceneId);
            Assert.DoesNotContain("Invalid input", output.Select(line => line.Text));
        }
        finally
        {
            DeleteStoryFile(story);
            DeleteDevicesFile();
        }
    }

    [Fact]
    public void Handle_ResumeRecords_RendersSceneWithoutInvalidInput()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_resume.json";

        CreateStoryFile(story, BuildTerminalStoryJson("Resume scene."));

        try
        {
            module.Handle(string.Empty, session).ToList();

            session.ResumeRecords = true;
            session.RecordsReturnSceneId = "start";

            var output = module.Handle(string.Empty, session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.Contains("Resume scene.", texts);
            Assert.DoesNotContain("Invalid input", texts);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    private static string StoryDirectory => Path.Combine(AppContext.BaseDirectory, "story");
    private static string DevicesDirectory => Path.Combine(AppContext.BaseDirectory, "Config", "Jsons");
    private static string DevicesPath => Path.Combine(DevicesDirectory, "Devices.json");

    private static void CreateStoryFile(string fileName, string json)
    {
        Directory.CreateDirectory(StoryDirectory);
        File.WriteAllText(Path.Combine(StoryDirectory, fileName), json);
    }

    private static void DeleteStoryFile(string fileName)
    {
        var path = Path.Combine(StoryDirectory, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void CreateDevicesFile(string json)
    {
        Directory.CreateDirectory(DevicesDirectory);
        File.WriteAllText(DevicesPath, json);
    }

    private static void DeleteDevicesFile()
    {
        if (File.Exists(DevicesPath))
        {
            File.Delete(DevicesPath);
        }

        if (Directory.Exists(DevicesDirectory) && !Directory.EnumerateFileSystemEntries(DevicesDirectory).Any())
        {
            Directory.Delete(DevicesDirectory);
        }
    }

    private static string BuildEndStoryJson(string startSceneId, string text)
    {
        return $@"{{
  ""StartSceneId"": ""{startSceneId}"",
  ""Scenes"": [
    {{
      ""Id"": ""{startSceneId}"",
      ""Text"": ""{text}"",
      ""IsEnd"": true,
      ""Choices"": []
    }}
  ]
}}";
    }

    private static string BuildRunningStoryJson(string text)
    {
        return $@"{{
  ""StartSceneId"": ""start"",
  ""Scenes"": [
    {{
      ""Id"": ""start"",
      ""Text"": ""{text}"",
      ""IsEnd"": false,
      ""Choices"": [
        {{
          ""Number"": 1,
          ""Text"": ""Go"",
          ""Effects"": [
            {{ ""Type"": ""GotoScene"", ""Value"": ""end"" }}
          ]
        }}
      ]
    }},
    {{
      ""Id"": ""end"",
      ""Text"": ""End scene."",
      ""IsEnd"": true,
      ""Choices"": []
    }}
  ]
}}";
    }

    private static string BuildTerminalStoryJson(string text)
    {
        return $@"{{
  ""StartSceneId"": ""start"",
  ""Scenes"": [
    {{
      ""Id"": ""start"",
      ""Text"": ""{text}"",
      ""IsEnd"": false,
      ""Choices"": [
        {{
          ""Number"": 1,
          ""Text"": ""Sit down at the terminal"",
          ""Effects"": [
            {{ ""Type"": ""GotoScene"", ""Value"": ""start"" }}
          ]
        }}
      ]
    }}
  ]
}}";
    }
}

