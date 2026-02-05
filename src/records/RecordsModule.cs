using System.Text.Json;
using System.Text.Json.Serialization;
using Env0.Core;
using Env0.Core.Objectives;
using env0.records.Engine;
using env0.records.Model;
using env0.records.Runtime;

namespace env0.records;

public sealed class RecordsModule : IContextModule
{
    private const int AutomationTickInterval = 2;
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
    private Dictionary<string, TerminalDeviceMapping>? _terminalDevicesByRoom;
    private readonly InputRouter _inputRouter = new();

    public IEnumerable<OutputLine> Handle(string input, SessionState state)
    {
        var output = new List<OutputLine>();

        if (state.IsComplete || _phase == RecordsPhase.Completed)
            return output;

        IncrementInputTicks(state);

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

            var route = _inputRouter.Resolve(input, scene);
            if (route.Kind == InputRouteKind.MetaCommand)
            {
                RenderScene(output, state, showNumbersOverride: true);
                return output;
            }

            if (route.Kind == InputRouteKind.Failure)
            {
                var allowedVerbs = GetDistinctSorted(scene.Choices.Select(choice => choice.Verb));
                var allowedObjects = GetDistinctSorted(scene.Choices.Select(choice => choice.Noun));
                var errorLines = RecordsErrorPolicy.BuildErrorLines(
                    route.FailureKind ?? InputFailureKind.UnknownVerb,
                    route.AttemptedVerbToken,
                    allowedVerbs,
                    allowedObjects,
                    route.AmbiguousCommands);

                foreach (var line in errorLines)
                    AddLine(output, line);

                AddLine(output, string.Empty);
                RenderScene(output, state);
                return output;
            }

            var selectedChoice = route.Choice;
            if (selectedChoice == null)
            {
                AddLine(output, "Unrecognised action.");
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

            if (IsTerminalTransition(selectedChoice) && TryGetTerminalDevice(currentSceneId, out var device))
            {
                state.NextContext = ContextRoute.Maintenance;
                state.MaintenanceVariant = string.Equals(currentSceneId, "rm_records_retention", StringComparison.OrdinalIgnoreCase)
                    ? MaintenanceVariant.Retention
                    : MaintenanceVariant.Processing;
                state.RecordsReturnSceneId = currentSceneId;
                state.MaintenanceMachineId = device!.Hostname;
                state.MaintenanceFilesystem = device.Filesystem;
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
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
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
        RenderScene(output, state, showNumbersOverride: false);
    }

    private void RenderScene(List<OutputLine> output, SessionState state, bool showNumbersOverride)
    {
        if (_repo == null || _gameState == null || _evaluator == null)
        {
            state.IsComplete = true;
            _phase = RecordsPhase.Completed;
            return;
        }

        var scene = _repo.Get(_gameState.CurrentSceneId);

        AddLine(output, scene.Text);
        AppendWorkStatus(output, state);
        AddLine(output, ObjectiveLine.Get(state, ContextRoute.Records));
        AddLine(output, string.Empty);

        if (scene.IsEnd)
        {
            AddLine(output, "Game ended.");
            state.IsComplete = true;
            _phase = RecordsPhase.Completed;
            return;
        }

        var orderedChoices = scene.Choices.OrderBy(c => c.Index).ToList();
        var availableChoices = new List<ChoiceDefinition>();

        foreach (var choice in orderedChoices)
        {
            if (_evaluator.IsEnabled(choice, _gameState, out _))
                availableChoices.Add(choice);
        }

        var showList = showNumbersOverride || state.ShowNumericOptions;
        if (showList)
        {
            AddLine(output, "Available:");
            foreach (var choice in availableChoices)
            {
                var line = $"[{choice.Index}] {choice.Verb} {choice.Noun}";
                AddLine(output, line);
            }
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
        if (string.IsNullOrWhiteSpace(choice.Verb) || string.IsNullOrWhiteSpace(choice.Noun))
            return false;

        return string.Equals(choice.Verb, "use", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(choice.Noun, "terminal", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetTerminalDevice(string sceneId, out TerminalDeviceMapping? device)
    {
        device = null;
        if (string.IsNullOrWhiteSpace(sceneId))
            return false;

        _terminalDevicesByRoom ??= LoadTerminalMappings();
        if (_terminalDevicesByRoom.TryGetValue(sceneId, out var value) &&
            value != null &&
            !string.IsNullOrWhiteSpace(value.Filesystem) &&
            !string.IsNullOrWhiteSpace(value.Hostname))
        {
            device = value;
            return true;
        }

        return false;
    }

    private Dictionary<string, TerminalDeviceMapping> LoadTerminalMappings()
    {
        var map = new Dictionary<string, TerminalDeviceMapping>(StringComparer.OrdinalIgnoreCase);
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
                var hostname = device.Hostname?.Trim();
                if (string.IsNullOrWhiteSpace(roomId) ||
                    string.IsNullOrWhiteSpace(filesystem) ||
                    string.IsNullOrWhiteSpace(hostname))
                    continue;

                map[roomId] = new TerminalDeviceMapping
                {
                    RecordsRoomId = roomId,
                    Filesystem = filesystem,
                    Hostname = hostname
                };
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
        public string? Hostname { get; set; }
    }

    private sealed class TerminalDeviceMapping
    {
        public string? RecordsRoomId { get; set; }
        public string? Filesystem { get; set; }
        public string? Hostname { get; set; }
    }

    private static List<string> GetDistinctSorted(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void IncrementInputTicks(SessionState state)
    {
        state.InputTicks++;
    }

    private static void AppendWorkStatus(List<OutputLine> output, SessionState state)
    {
        var automationTotal = GetAutomationTotal(state);
        if (!state.AutomationEnabled &&
            state.ManualCompletions == 0 &&
            automationTotal == 0 &&
            state.BatchesCompleted == 0)
            return;

        var line = $"Work status: manual {state.ManualCompletions} | automated {automationTotal} | batches {state.BatchesCompleted}";
        AddLine(output, line);
    }

    private static int GetAutomationTotal(SessionState state)
    {
        if (!state.AutomationEnabled)
            return 0;

        var ticksSinceEnable = state.InputTicks - state.AutomationStartTick;
        if (ticksSinceEnable < 0)
            return 0;

        return ticksSinceEnable / AutomationTickInterval;
    }

}



