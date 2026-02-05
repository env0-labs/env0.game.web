using Env0.Core;
using Env0.Core.Objectives;

namespace env0.maintenance;

public sealed class MaintenanceModule : IContextModule
{
    private readonly int[] _containerSizes = { 1, 1, 1 };
    private readonly Random _rng = new(1977);
    private const string ScriptTag = "kill-child";
    private const int BatchPromptThreshold = 3;
    private const int AutomationTickInterval = 2;
    private const int NoteMaxLength = 120;
    private int _containerSizeIndex;
    private bool _initialized;
    private int _parentIndex = 1;
    private int? _parentSize;
    private int _processedCount;
    private int _containersCompletedSinceBatch;
    private bool _batchPromptActive;
    private bool _batchPromptDismissed;

    public IEnumerable<OutputLine> Handle(string input, SessionState state)
    {
        var output = new List<OutputLine>();

        if (state.IsComplete)
            return output;

        IncrementInputTicks(state);
        var automationDelta = ApplyAutomation(state);

        if (!_initialized)
        {
            _initialized = true;
            AddLine(output, "env0.maintenance booting");
            if (!string.IsNullOrWhiteSpace(state.MaintenanceMachineId))
                AddLine(output, $"{state.MaintenanceMachineId} is loading...");
            AddLine(output, string.Empty);
            AddLine(output, ObjectiveLine.Get(state, ContextRoute.Maintenance));
            AddLine(output, string.Empty);
            AppendAutomationUpdate(output, automationDelta);
            AddPrompt(output);
            return output;
        }

        AppendAutomationUpdate(output, automationDelta);

        if (state.MaintenanceVariant == MaintenanceVariant.Retention)
        {
            HandleRetentionCommand(input, state, output);
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
                ProcessNext(output, state);
                break;
            case "status":
                RenderStatus(output, state);
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
                if (!state.MaintenanceExitUnlocked)
                {
                    AddLine(output, "Exit is locked. Complete a batch first.");
                    AddLine(output, string.Empty);
                    AddLine(output, ObjectiveLine.Get(state, ContextRoute.Maintenance));
                    AddLine(output, string.Empty);
                    AddPrompt(output);
                    return output;
                }
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

        AddLine(output, ObjectiveLine.Get(state, ContextRoute.Maintenance));
        AddLine(output, string.Empty);
        AddPrompt(output);
        return output;
    }

    private void ProcessNext(List<OutputLine> output, SessionState state)
    {
        EnsureParentSize();

        if (_parentSize == null)
            return;

        state.ManualCompletions++;
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
            if (!_batchPromptDismissed && _containersCompletedSinceBatch >= BatchPromptThreshold)
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

    private void RenderStatus(List<OutputLine> output, SessionState state)
    {
        EnsureParentSize();

        AddLine(output, "STATUS");
        AddLine(output, string.Empty);
        AddLine(output, "Current parent container: ACTIVE");

        var remaining = _parentSize == null ? 0 : Math.Max(0, _parentSize.Value - _processedCount);
        AddLine(output, $"Children remaining: {remaining}");
        AddLine(output, $"Manual completions: {state.ManualCompletions}");
        AddLine(output, $"Automated completions: {state.AutomationCompleted}");
        AddLine(output, $"Batches recorded: {state.BatchesCompleted}");
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
            var completed = _containersCompletedSinceBatch;
            CreateBatch(state, completed, "manual");
            _containersCompletedSinceBatch = 0;
            _batchPromptActive = false;
            AddLine(output, "Batch recorded.");
            state.MaintenanceExitUnlocked = true;
            AddLine(output, "Exit unlocked. Type 'exit' to return to Records.");

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

    private static void IncrementInputTicks(SessionState state)
    {
        state.InputTicks++;
    }

    private static int ApplyAutomation(SessionState state)
    {
        if (!state.AutomationEnabled)
            return 0;

        var ticksSinceEnable = state.InputTicks - state.AutomationStartTick;
        if (ticksSinceEnable < 0)
            return 0;

        var total = ticksSinceEnable / AutomationTickInterval;
        if (total <= state.AutomationCompleted)
            return 0;

        var delta = total - state.AutomationCompleted;
        state.AutomationCompleted = total;
        CreateAutomationBatches(state);
        return delta;
    }

    private static void CreateAutomationBatches(SessionState state)
    {
        var totalBatches = state.AutomationCompleted / BatchPromptThreshold;
        while (state.AutomationBatchesCreated < totalBatches)
        {
            state.AutomationBatchesCreated++;
            var id = $"B-{state.NextBatchId + 1:0000}";
            state.NextBatchId++;
            state.BatchesCompleted++;

            state.MaintenanceBatches.Add(new MaintenanceBatch
            {
                Id = id,
                Count = BatchPromptThreshold,
                Source = "automation",
                CreatedTick = state.InputTicks,
                Category = null,
                Note = null,
                Submitted = false
            });
        }
    }

    private static void AppendAutomationUpdate(List<OutputLine> output, int delta)
    {
        if (delta <= 0)
            return;

        AddLine(output, $"AUTOMATION: processed {delta} unit(s).");
        AddLine(output, string.Empty);
    }

    private void HandleRetentionCommand(string input, SessionState state, List<OutputLine> output)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            RenderRetentionInvalidCommand(output);
            AddPrompt(output);
            return;
        }

        var parts = trimmed.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var verb = parts[0].ToLowerInvariant();

        switch (verb)
        {
            case "queue":
                RenderQueue(output, state);
                break;
            case "load":
                if (parts.Length >= 2 && string.Equals(parts[1], "cli", StringComparison.OrdinalIgnoreCase))
                {
                    AddLine(output, "Loading CLI...");
                    state.NextContext = ContextRoute.Terminal;
                    state.TerminalReturnContext = ContextRoute.Maintenance;
                    state.TerminalStartFilesystem = state.MaintenanceFilesystem;
                    state.IsComplete = true;
                    state.MaintenanceMachineId = null;
                    state.MaintenanceFilesystem = null;
                    return;
                }
                RenderRetentionInvalidCommand(output);
                break;
            case "open":
                RenderOpen(output, state, parts);
                break;
            case "file":
                HandleFile(output, state, parts);
                break;
            case "note":
                HandleNote(output, state, parts);
                break;
            case "submit":
                HandleSubmit(output, state, parts);
                break;
            case "summary":
                RenderSummary(output, state);
                break;
            case "status":
                RenderStatus(output, state);
                break;
            case "exit":
                AddLine(output, "Exiting...");
                state.NextContext = ContextRoute.Records;
                if (!string.IsNullOrWhiteSpace(state.RecordsReturnSceneId))
                    state.ResumeRecords = true;
                state.IsComplete = true;
                state.MaintenanceMachineId = null;
                state.MaintenanceFilesystem = null;
                return;
            default:
                RenderRetentionInvalidCommand(output);
                break;
        }

        AddPrompt(output);
    }

    private void RenderQueue(List<OutputLine> output, SessionState state)
    {
        var unfiled = state.MaintenanceBatches.Where(batch => !batch.Submitted).ToList();
        if (unfiled.Count == 0)
        {
            AddLine(output, "Queue empty.");
            AddLine(output, string.Empty);
            return;
        }

        AddLine(output, "Unfiled batches:");
        foreach (var batch in unfiled)
        {
            var category = string.IsNullOrWhiteSpace(batch.Category) ? "unassigned" : batch.Category;
            AddLine(output, $"{batch.Id} | source {batch.Source} | items {batch.Count} | category {category}");
        }
        AddLine(output, string.Empty);
    }

    private void RenderOpen(List<OutputLine> output, SessionState state, string[] parts)
    {
        if (parts.Length < 2)
        {
            AddLine(output, "Usage: open <batchId>");
            AddLine(output, string.Empty);
            return;
        }

        var batch = FindBatch(state, parts[1]);
        if (batch == null)
        {
            AddLine(output, $"Batch not found: {parts[1]}");
            AddLine(output, string.Empty);
            return;
        }

        AddLine(output, $"Batch {batch.Id}");
        AddLine(output, $"Source: {batch.Source}");
        AddLine(output, $"Items: {batch.Count}");
        AddLine(output, $"Category: {batch.Category ?? "unassigned"}");
        AddLine(output, $"Note: {batch.Note ?? "(none)"}");
        AddLine(output, $"Status: {(batch.Submitted ? "submitted" : "unfiled")}");
        AddLine(output, string.Empty);
    }

    private void HandleFile(List<OutputLine> output, SessionState state, string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLine(output, "Usage: file <batchId> <category>");
            AddLine(output, string.Empty);
            return;
        }

        var batch = FindBatch(state, parts[1]);
        if (batch == null)
        {
            AddLine(output, $"Batch not found: {parts[1]}");
            AddLine(output, string.Empty);
            return;
        }

        var category = parts[2].Trim().ToLowerInvariant();
        if (!IsValidCategory(category))
        {
            AddLine(output, "Invalid category. Use: retain, purge, escalate, unknown.");
            AddLine(output, string.Empty);
            return;
        }

        batch.Category = category;
        AddLine(output, $"Batch {batch.Id} categorized as {category}.");
        AddLine(output, string.Empty);
    }

    private void HandleNote(List<OutputLine> output, SessionState state, string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLine(output, "Usage: note <batchId> <text>");
            AddLine(output, string.Empty);
            return;
        }

        var batch = FindBatch(state, parts[1]);
        if (batch == null)
        {
            AddLine(output, $"Batch not found: {parts[1]}");
            AddLine(output, string.Empty);
            return;
        }

        var note = parts[2].Trim();
        if (note.Length == 0)
        {
            AddLine(output, "Note cannot be empty.");
            AddLine(output, string.Empty);
            return;
        }

        if (note.Length > NoteMaxLength)
        {
            AddLine(output, $"Note too long. Max length is {NoteMaxLength}.");
            AddLine(output, string.Empty);
            return;
        }

        batch.Note = note;
        AddLine(output, $"Note recorded for {batch.Id}.");
        AddLine(output, string.Empty);
    }

    private void HandleSubmit(List<OutputLine> output, SessionState state, string[] parts)
    {
        if (parts.Length < 2)
        {
            AddLine(output, "Usage: submit <batchId>");
            AddLine(output, string.Empty);
            return;
        }

        var batch = FindBatch(state, parts[1]);
        if (batch == null)
        {
            AddLine(output, $"Batch not found: {parts[1]}");
            AddLine(output, string.Empty);
            return;
        }

        batch.Submitted = true;
        AddLine(output, $"Batch {batch.Id} submitted.");
        AddLine(output, string.Empty);
    }

    private void RenderSummary(List<OutputLine> output, SessionState state)
    {
        var batches = state.MaintenanceBatches;
        if (batches.Count == 0)
        {
            AddLine(output, "No batches recorded.");
            AddLine(output, string.Empty);
            return;
        }

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["retain"] = 0,
            ["purge"] = 0,
            ["escalate"] = 0,
            ["unknown"] = 0,
            ["unassigned"] = 0
        };

        foreach (var batch in batches)
        {
            var key = string.IsNullOrWhiteSpace(batch.Category) ? "unassigned" : batch.Category;
            if (!counts.ContainsKey(key))
                counts["unassigned"]++;
            else
                counts[key]++;
        }

        AddLine(output, "Summary:");
        AddLine(output, $"retain: {counts["retain"]}");
        AddLine(output, $"purge: {counts["purge"]}");
        AddLine(output, $"escalate: {counts["escalate"]}");
        AddLine(output, $"unknown: {counts["unknown"]}");
        AddLine(output, $"unassigned: {counts["unassigned"]}");
        AddLine(output, string.Empty);
    }

    private static MaintenanceBatch? FindBatch(SessionState state, string batchId)
    {
        return state.MaintenanceBatches.FirstOrDefault(batch =>
            string.Equals(batch.Id, batchId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidCategory(string category)
    {
        return category == "retain" ||
               category == "purge" ||
               category == "escalate" ||
               category == "unknown";
    }

    private static void RenderRetentionInvalidCommand(List<OutputLine> output)
    {
        AddLine(output, "ERROR");
        AddLine(output, string.Empty);
        AddLine(output, "Unrecognised command.");
        AddLine(output, string.Empty);
        AddLine(output, "Accepted commands:");
        AddLine(output, "- queue");
        AddLine(output, "- load cli");
        AddLine(output, "- open <batchId>");
        AddLine(output, "- file <batchId> <category>");
        AddLine(output, "- note <batchId> <text>");
        AddLine(output, "- submit <batchId>");
        AddLine(output, "- summary");
        AddLine(output, "- status");
        AddLine(output, "- exit");
        AddLine(output, string.Empty);
    }

    private void CreateBatch(SessionState state, int count, string source)
    {
        var id = $"B-{state.NextBatchId + 1:0000}";
        state.NextBatchId++;
        state.BatchesCompleted++;

        state.MaintenanceBatches.Add(new MaintenanceBatch
        {
            Id = id,
            Count = Math.Max(1, count),
            Source = source,
            CreatedTick = state.InputTicks,
            Category = null,
            Note = null,
            Submitted = false
        });
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




