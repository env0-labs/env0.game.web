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

    private static string StoryDirectory => Path.Combine(AppContext.BaseDirectory, "story");

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
}

