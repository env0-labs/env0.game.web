using System.Text.Json;
using System.Text.Json.Serialization;
using Env0.Core;
using env0.records.Engine;
using env0.records.Model;
using env0.records.Runtime;

namespace env0.records;

public sealed class RecordsModule : IContextModule
{
    private enum RecordsPhase
    {
        Booting,
        Running,
        Completed
    }

    private RecordsPhase _phase = RecordsPhase.Booting;
    private bool _booted;
    private List<string>? _availableStories;
    private SceneRepository? _repo;
    private GameState? _gameState;
    private ChoiceEvaluator? _evaluator;
    private EffectExecutor? _executor;
    private Dictionary<string, string>? _terminalFilesystemByRoom;

    public IEnumerable<OutputLine> Handle(string input, SessionState state)
    {
        var output = new List<OutputLine>();

        if (state.IsComplete || _phase == RecordsPhase.Completed)
            return output;

        if (_phase == RecordsPhase.Booting)
        {
            if (!_booted)
            {
                _booted = true;
                AddLine(output, "env0.records booting");
                AddLine(output, string.Empty);
            }

            if (_availableStories == null && !TryLoadStoryList(output, state))
                return output;

            var storyPath = _availableStories!.First();

            if (!File.Exists(storyPath))
            {
                AddLine(output, $"Story file not found: {storyPath}");
                state.IsComplete = true;
                _phase = RecordsPhase.Completed;
                return output;
            }

            var json = File.ReadAllText(storyPath);

            var story = JsonSerializer.Deserialize<StoryDefinition>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters = { new JsonStringEnumConverter() }
                }
            );

            if (story == null)
            {
                AddLine(output, "Story file could not be parsed.");
                state.IsComplete = true;
                _phase = RecordsPhase.Completed;
                return output;
            }

            _repo = new SceneRepository(story);
            _gameState = new GameState(_repo.StartSceneId);
            _evaluator = new ChoiceEvaluator();
            _executor = new EffectExecutor();
            _phase = RecordsPhase.Running;

            RenderScene(output, state);
            return output;
        }

        if (_phase == RecordsPhase.Running && _repo != null && _gameState != null && _evaluator != null && _executor != null)
        {
            var scene = _repo.Get(_gameState.CurrentSceneId);

            if (string.IsNullOrWhiteSpace(input) && state.ResumeRecords)
            {
                state.ResumeRecords = false;
                if (!string.IsNullOrWhiteSpace(state.RecordsReturnSceneId))
                {
                    _gameState.SetScene(state.RecordsReturnSceneId);
                    state.RecordsReturnSceneId = null;
                    scene = _repo.Get(_gameState.CurrentSceneId);
                }

                RenderScene(output, state);
                return output;
            }

            AddLine(output, string.Empty);

            if (!int.TryParse(input, out var selectedNumber))
            {
                AddLine(output, "Invalid input. Enter a number.");
                AddLine(output, string.Empty);
                RenderScene(output, state);
                return output;
            }

            var selectedChoice = scene.Choices.FirstOrDefault(c => c.Number == selectedNumber);
            if (selectedChoice == null)
            {
                AddLine(output, "No such option.");
                AddLine(output, string.Empty);
                RenderScene(output, state);
                return output;
            }

            var isEnabled = _evaluator.IsEnabled(selectedChoice, _gameState, out var disabledReason);
            if (!isEnabled)
            {
                AddLine(output, disabledReason ?? "That option is not available.");
                AddLine(output, string.Empty);
                RenderScene(output, state);
                return output;
            }

            var currentSceneId = scene.Id;
            _executor.Execute(selectedChoice.Effects, _gameState);

            if (IsTerminalTransition(selectedChoice) && TryGetTerminalFilesystem(currentSceneId, out var filesystem))
            {
                state.NextContext = ContextRoute.Terminal;
                state.TerminalReturnContext = ContextRoute.Records;
                state.TerminalStartFilesystem = filesystem;
                state.RecordsReturnSceneId = currentSceneId;
                state.IsComplete = true;
                return output;
            }

            if (!string.IsNullOrWhiteSpace(selectedChoice.ResultText))
            {
                AddLine(output, selectedChoice.ResultText);
                AddLine(output, string.Empty);
            }

            RenderScene(output, state);
            return output;
        }

        state.IsComplete = true;
        _phase = RecordsPhase.Completed;
        return output;
    }

    private bool TryLoadStoryList(List<OutputLine> output, SessionState state)
    {
        var storyDirectory = Path.Combine(AppContext.BaseDirectory, "story");

        if (!Directory.Exists(storyDirectory))
        {
            AddLine(output, $"Story directory not found: {storyDirectory}");
            state.IsComplete = true;
            _phase = RecordsPhase.Completed;
            return false;
        }

        var stories = Directory
            .GetFiles(storyDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName)
            .ToList();

        if (stories.Count == 0)
        {
            AddLine(output, $"No story JSON files found in {storyDirectory}.");
            state.IsComplete = true;
            _phase = RecordsPhase.Completed;
            return false;
        }

        _availableStories = stories;
        return true;
    }

    private void RenderScene(List<OutputLine> output, SessionState state)
    {
        if (_repo == null || _gameState == null || _evaluator == null)
        {
            state.IsComplete = true;
            _phase = RecordsPhase.Completed;
            return;
        }

        var scene = _repo.Get(_gameState.CurrentSceneId);

        AddLine(output, scene.Text);
        AddLine(output, string.Empty);

        if (scene.IsEnd)
        {
            AddLine(output, "Game ended.");
            state.IsComplete = true;
            _phase = RecordsPhase.Completed;
            return;
        }

        foreach (var choice in scene.Choices.OrderBy(c => c.Number))
        {
            var enabled = _evaluator.IsEnabled(choice, _gameState, out var reason);
            AddLine(
                output,
                enabled
                    ? $"{choice.Number}. {choice.Text}"
                    : $"{choice.Number}. {choice.Text} ({reason})"
            );
        }

        AddLine(output, string.Empty);
        AddPrompt(output, "> ");
    }

    private static void AddLine(List<OutputLine> output, string text)
    {
        output.Add(new OutputLine(OutputType.Standard, text));
    }

    private static void AddPrompt(List<OutputLine> output, string text)
    {
        output.Add(new OutputLine(OutputType.Standard, text, newLine: false));
    }

    private static bool IsTerminalTransition(ChoiceDefinition choice)
    {
        if (string.IsNullOrWhiteSpace(choice.Text))
            return false;

        return choice.Text.Contains("Sit down at the terminal", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetTerminalFilesystem(string sceneId, out string filesystem)
    {
        filesystem = string.Empty;
        if (string.IsNullOrWhiteSpace(sceneId))
            return false;

        _terminalFilesystemByRoom ??= LoadTerminalMappings();
        if (_terminalFilesystemByRoom.TryGetValue(sceneId, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            filesystem = value;
            return true;
        }

        return false;
    }

    private Dictionary<string, string> LoadTerminalMappings()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "Jsons", "Devices.json");

        if (!File.Exists(path))
            return map;

        try
        {
            var json = File.ReadAllText(path);
            var devices = JsonSerializer.Deserialize<List<DeviceMapping>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (devices == null)
                return map;

            foreach (var device in devices)
            {
                var roomId = device.RecordsRoomId?.Trim();
                var filesystem = device.Filesystem?.Trim();
                if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(filesystem))
                    continue;

                map[roomId] = filesystem;
            }
        }
        catch
        {
            return map;
        }

        return map;
    }

    private sealed class DeviceMapping
    {
        public string? RecordsRoomId { get; set; }
        public string? Filesystem { get; set; }
    }
}



