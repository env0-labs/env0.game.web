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

        CreateStoryFile(alpha, BuildSingleChoiceStoryJson("Alpha scene."));
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

        CreateStoryFile(story, BuildSingleChoiceStoryJson("Start scene."));

        try
        {
            var startOutput = module.Handle(string.Empty, session).ToList();
            Assert.False(session.IsComplete);
            Assert.Contains("Start scene.", startOutput.Select(line => line.Text));

            var invalidOutput = module.Handle("abc", session).ToList();
            var invalidTexts = invalidOutput.Select(line => line.Text).ToList();

            Assert.Contains("Unrecognised action.", invalidTexts);
            Assert.Contains("Actions: read", invalidTexts);
            Assert.Contains("Objects: memo", invalidTexts);
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
    public void Handle_TerminalChoice_SetsMaintenanceRoutingState()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_terminal.json";
        var devicesJson = @"[
  { ""recordsRoomId"": ""start"", ""filesystem"": ""Filesystem_1.json"", ""hostname"": ""proc.floor01"" }
]";

        CreateStoryFile(story, BuildTerminalStoryJson("Terminal scene."));
        CreateDevicesFile(devicesJson);

        try
        {
            module.Handle(string.Empty, session).ToList();
            var output = module.Handle("1", session).ToList();

            Assert.True(session.IsComplete);
            Assert.Equal(ContextRoute.Maintenance, session.NextContext);
            Assert.Equal("Filesystem_1.json", session.MaintenanceFilesystem);
            Assert.Equal("proc.floor01", session.MaintenanceMachineId);
            Assert.Equal("start", session.RecordsReturnSceneId);
            Assert.DoesNotContain("Unrecognised action.", output.Select(line => line.Text));
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
            Assert.DoesNotContain("Unrecognised action.", texts);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_ResolvesNumericAndAliasToSameChoice()
    {
        var numericModule = new RecordsModule();
        var aliasModule = new RecordsModule();
        var numericSession = new SessionState();
        var aliasSession = new SessionState();
        var story = "test_records_alias.json";

        CreateStoryFile(story, BuildSingleChoiceStoryJson("Alias scene.", "MEMO BODY"));

        try
        {
            numericModule.Handle(string.Empty, numericSession).ToList();
            aliasModule.Handle(string.Empty, aliasSession).ToList();

            var numericOutput = numericModule.Handle("1", numericSession).ToList();
            var aliasOutput = aliasModule.Handle("look at memo", aliasSession).ToList();

            Assert.Contains("MEMO BODY", numericOutput.Select(line => line.Text));
            Assert.Contains("MEMO BODY", aliasOutput.Select(line => line.Text));
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_HidesNumericLabelsByDefault()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_numbers_hidden.json";

        CreateStoryFile(story, BuildSingleChoiceStoryJson("Hidden numbers scene."));

        try
        {
            var output = module.Handle(string.Empty, session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.DoesNotContain("[1] read memo", texts);
            Assert.DoesNotContain("Available:", texts);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_OptionsShowsNumberedListWithoutTogglingState()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_options.json";

        CreateStoryFile(story, BuildSingleChoiceStoryJson("Options scene."));

        try
        {
            module.Handle(string.Empty, session).ToList();

            var output = module.Handle("options", session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.Contains("Available:", texts);
            Assert.Contains("[1] read memo", texts);
            Assert.False(session.ShowNumericOptions);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_UnknownVerb_ReportsActionsAndObjects()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_unknown_verb.json";

        CreateStoryFile(story, BuildSingleChoiceStoryJson("Unknown verb scene."));

        try
        {
            module.Handle(string.Empty, session).ToList();

            var output = module.Handle("poke memo", session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.Contains("Unrecognised action.", texts);
            Assert.Contains("Actions: read", texts);
            Assert.Contains("Objects: memo", texts);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_KnownVerbWithoutMatch_ReportsVerbSpecificMessage()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_known_verb.json";

        CreateStoryFile(story, BuildSingleChoiceStoryJson("Known verb scene."));

        try
        {
            module.Handle(string.Empty, session).ToList();

            var output = module.Handle("read banana", session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.Contains("No readable item matches that command in this room.", texts);
            Assert.Contains("Actions: read", texts);
            Assert.Contains("Objects: memo", texts);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_NumericSelectionWorksWhenNumbersHidden()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_numeric_hidden.json";

        CreateStoryFile(story, BuildSingleChoiceStoryJson("Numeric hidden scene.", "NUMERIC BODY"));

        try
        {
            module.Handle(string.Empty, session).ToList();

            var output = module.Handle("1", session).ToList();
            var texts = output.Select(line => line.Text).ToList();

            Assert.Contains("NUMERIC BODY", texts);
        }
        finally
        {
            DeleteStoryFile(story);
        }
    }

    [Fact]
    public void Handle_AliasCollisionFailsFast()
    {
        var module = new RecordsModule();
        var session = new SessionState();
        var story = "test_records_collision.json";

        CreateStoryFile(story, BuildAliasCollisionStoryJson());

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => module.Handle(string.Empty, session).ToList());
            Assert.Contains("Alias collision", ex.Message);
            Assert.Contains("read memo", ex.Message);
            Assert.Contains("start.read_memo", ex.Message);
            Assert.Contains("start.read_notice", ex.Message);
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

    private static string BuildSingleChoiceStoryJson(string text, string resultText = "MEMO BODY")
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
          ""Id"": ""start.read_memo"",
          ""Index"": 1,
          ""Verb"": ""read"",
          ""Noun"": ""memo"",
          ""Aliases"": [ ""read memo"", ""look at memo"" ],
          ""Effects"": [
            {{ ""Type"": ""GotoScene"", ""Value"": ""start"" }}
          ],
          ""ResultText"": ""{resultText}""
        }}
      ]
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
          ""Id"": ""start.use_terminal"",
          ""Index"": 1,
          ""Verb"": ""use"",
          ""Noun"": ""terminal"",
          ""Aliases"": [ ""use terminal"" ],
          ""Effects"": [
            {{ ""Type"": ""GotoScene"", ""Value"": ""start"" }}
          ]
        }}
      ]
    }}
  ]
}}";
    }

    private static string BuildAliasCollisionStoryJson()
    {
        return @"{
  ""StartSceneId"": ""start"",
  ""Scenes"": [
    {
      ""Id"": ""start"",
      ""Text"": ""Collision scene."",
      ""IsEnd"": false,
      ""Choices"": [
        {
          ""Id"": ""start.read_memo"",
          ""Index"": 1,
          ""Verb"": ""read"",
          ""Noun"": ""memo"",
          ""Aliases"": [ ""read memo"" ],
          ""Effects"": [
            { ""Type"": ""GotoScene"", ""Value"": ""start"" }
          ]
        },
        {
          ""Id"": ""start.read_notice"",
          ""Index"": 2,
          ""Verb"": ""read"",
          ""Noun"": ""notice"",
          ""Aliases"": [ ""read memo"" ],
          ""Effects"": [
            { ""Type"": ""GotoScene"", ""Value"": ""start"" }
          ]
        }
      ]
    }
  ]
}";
    }
}
