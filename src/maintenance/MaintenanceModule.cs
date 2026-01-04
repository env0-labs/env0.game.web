using Env0.Core;

namespace env0.maintenance;

public sealed class MaintenanceModule : IContextModule
{
    private readonly int[] _containerSizes = { 7, 6, 5, 12, 4, 8 };
    private readonly Random _rng = new(1977);
    private const string ScriptTag = "kill-child";
    private int _containerSizeIndex;
    private bool _initialized;
    private int _parentIndex = 1;
    private int? _parentSize;
    private int _processedCount;
    private int _containersCompletedSinceBatch;
    private int _batchesCompleted;
    private bool _batchPromptActive;
    private bool _batchPromptDismissed;

    public IEnumerable<OutputLine> Handle(string input, SessionState state)
    {
        var output = new List<OutputLine>();

        if (state.IsComplete)
            return output;

        if (!_initialized)
        {
            _initialized = true;
            AddLine(output, "env0.maintenance booting");
            if (!string.IsNullOrWhiteSpace(state.MaintenanceMachineId))
                AddLine(output, $"{state.MaintenanceMachineId} is loading...");
            AddLine(output, string.Empty);
            AddPrompt(output);
            return output;
        }

        if (_batchPromptActive)
        {
            HandleBatchPrompt(input, state, output);
            return output;
        }

        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            RenderInvalidCommand(output);
            AddPrompt(output);
            return output;
        }

        switch (trimmed.ToLowerInvariant())
        {
            case "process":
                ProcessNext(output);
                break;
            case "status":
                RenderStatus(output);
                break;
            case "load cli":
                AddLine(output, "Loading CLI...");
                state.NextContext = ContextRoute.Terminal;
                state.TerminalReturnContext = ContextRoute.Maintenance;
                state.TerminalStartFilesystem = state.MaintenanceFilesystem;
                state.IsComplete = true;
                state.MaintenanceMachineId = null;
                state.MaintenanceFilesystem = null;
                return output;
            case "exit":
                AddLine(output, "Exiting...");
                state.NextContext = ContextRoute.Records;
                if (!string.IsNullOrWhiteSpace(state.RecordsReturnSceneId))
                    state.ResumeRecords = true;
                state.IsComplete = true;
                state.MaintenanceMachineId = null;
                state.MaintenanceFilesystem = null;
                return output;
            default:
                RenderInvalidCommand(output);
                break;
        }

        AddPrompt(output);
        return output;
    }

    private void ProcessNext(List<OutputLine> output)
    {
        EnsureParentSize();

        if (_parentSize == null)
            return;

        AppendProcessHeader(output);
        AppendScriptLog(output);

        if (_processedCount < _parentSize.Value)
            _processedCount++;

        if (_processedCount >= _parentSize.Value)
        {
            AddLine(output, PhraseBank.Pick(PhraseBank.ParentCompleted, _rng));
            AddLine(output, PhraseBank.Pick(PhraseBank.ParentAssigned, _rng));
            _parentIndex++;
            _parentSize = null;
            _processedCount = 0;
            AddLine(output, string.Empty);

            _containersCompletedSinceBatch++;
            if (!_batchPromptDismissed && _containersCompletedSinceBatch >= 5)
            {
                AddLine(output, "Batch completed containers? (y/n)");
                AddPrompt(output);
                _batchPromptActive = true;
            }
        }
    }

    private void EnsureParentSize()
    {
        if (_parentSize != null)
            return;

        var size = _containerSizes[_containerSizeIndex % _containerSizes.Length];
        _containerSizeIndex++;
        _parentSize = size;
        _processedCount = 0;
    }

    private void RenderStatus(List<OutputLine> output)
    {
        EnsureParentSize();

        AddLine(output, "STATUS");
        AddLine(output, string.Empty);
        AddLine(output, "Current parent container: ACTIVE");

        var remaining = _parentSize == null ? 0 : Math.Max(0, _parentSize.Value - _processedCount);
        AddLine(output, $"Children remaining: {remaining}");
        AddLine(output, string.Empty);
        AddLine(output, "[ PARENT ]");

        if (_parentSize == null)
        {
            AddLine(output, " └─ child_01  [pending]");
        }
        else
        {
            for (var i = 1; i <= _parentSize.Value; i++)
            {
                var isLast = i == _parentSize.Value;
                var branch = isLast ? " └─" : " ├─";
                var status = i <= _processedCount ? "complete" : "pending";
                AddLine(output, $"{branch} child_{i:00}  [{status}]");
            }
        }
        AddLine(output, string.Empty);
    }
    private static void RenderInvalidCommand(List<OutputLine> output)
    {
        AddLine(output, "ERROR");
        AddLine(output, string.Empty);
        AddLine(output, "Unrecognised command.");
        AddLine(output, string.Empty);
        AddLine(output, "Accepted commands:");
        AddLine(output, "- process");
        AddLine(output, "- status");
        AddLine(output, string.Empty);
    }

    private static void AddLine(List<OutputLine> output, string text)
    {
        output.Add(new OutputLine(OutputType.Standard, text));
    }

    private static void AddPrompt(List<OutputLine> output)
    {
        output.Add(new OutputLine(OutputType.Standard, "> ", newLine: false));
    }

    private void HandleBatchPrompt(string input, SessionState state, List<OutputLine> output)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            _batchesCompleted++;
            _containersCompletedSinceBatch = 0;
            _batchPromptActive = false;
            AddLine(output, "Batch recorded.");
            AddLine(output, "Type 'exit' to exit this program at any time.");

            if (_batchesCompleted >= 3)
            {
                AddLine(output, "Batch threshold reached.");
                state.IsComplete = true;
                return;
            }

            AddLine(output, string.Empty);
            AddPrompt(output);
            return;
        }

        if (trimmed.Equals("n", StringComparison.OrdinalIgnoreCase))
        {
            _batchPromptActive = false;
            _batchPromptDismissed = true;
            AddLine(output, "Batch dismissed.");
            AddLine(output, string.Empty);
            AddPrompt(output);
            return;
        }

        AddLine(output, "Unrecognised input. Accepted input: y / n");
        AddLine(output, string.Empty);
        AddPrompt(output);
    }

    private void AppendProcessHeader(List<OutputLine> output)
    {
        AddLine(output, PhraseBank.Pick(PhraseBank.ProcessStep, _rng));
    }

    private void AppendScriptLog(List<OutputLine> output)
    {
        var tag = ScriptTag;
        AddLine(output, $"[{tag}] start");

        var steps = PhraseBank.GetScriptSteps(_rng, tag);
        foreach (var step in steps)
        {
            AddLine(output, step);
        }

        AddLine(output, $"[{tag}] done");
        AddLine(output, string.Empty);
    }

    private static class PhraseBank
    {
        public static readonly string[] ProcessStep =
        {
            "PROCESSING",
            "PROCESSING",
            "PROCESSING"
        };

        private static readonly string[] ScriptStepKeys =
        {
            "preflight",
            "lock",
            "execute",
            "commit",
            "cleanup",
            "unlock",
            "flush",
            "finalize"
        };

        public static readonly string[] ParentCompleted =
        {
            "Parent container completed.",
            "Parent container closed.",
            "Parent container finalised."
        };

        public static readonly string[] ParentAssigned =
        {
            "New parent container assigned.",
            "New parent container allocated.",
            "New parent container initialised."
        };

        public static string Pick(string[] pool, Random rng)
        {
            return pool[rng.Next(pool.Length)];
        }

        public static List<string> GetScriptSteps(Random rng, string tag)
        {
            var count = rng.Next(2, 6);
            var selected = new List<string>(count);
            var available = new List<string>(ScriptStepKeys);

            for (var i = 0; i < count; i++)
            {
                var index = rng.Next(available.Count);
                selected.Add(available[index]);
                available.RemoveAt(index);
            }

            selected = OrderSteps(selected);

            var lines = new List<string>(selected.Count);
            foreach (var step in selected)
            {
                lines.Add($"[{tag}] {step}");
            }

            return lines;
        }

        private static List<string> OrderSteps(List<string> steps)
        {
            if (steps.Count <= 1)
                return steps;

            if (steps.Contains("lock") && steps.Contains("unlock"))
            {
                steps.Remove("lock");
                steps.Remove("unlock");
                steps.Insert(0, "lock");
                steps.Add("unlock");
            }

            if (steps.Contains("preflight"))
            {
                steps.Remove("preflight");
                steps.Insert(0, "preflight");
            }

            if (steps.Contains("finalize"))
            {
                steps.Remove("finalize");
                steps.Add("finalize");
            }

            return steps;
        }
    }
}




