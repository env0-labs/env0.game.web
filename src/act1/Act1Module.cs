using Env0.Core;

namespace env0.act1;

public sealed class Act1Module : IActModule
{
    private readonly int[] _containerSizes = { 7, 6, 5, 12, 4, 8 };
    private int _containerSizeIndex;
    private bool _initialized;
    private int _parentIndex = 1;
    private int? _parentSize;
    private int _processedCount;

    public IEnumerable<OutputLine> Handle(string input, SessionState state)
    {
        var output = new List<OutputLine>();

        if (state.IsComplete)
            return output;

        if (!_initialized)
        {
            _initialized = true;
            AddLine(output, "env0.act1 booting");
            AddLine(output, string.Empty);
            AddPrompt(output);
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

        AddLine(output, "PROCESSING");
        AddLine(output, string.Empty);

        if (_processedCount < _parentSize.Value)
            _processedCount++;

        AddLine(output, "Batch continued.");
        AddLine(output, "Child process terminated.");
        AddLine(output, string.Empty);

        if (_processedCount >= _parentSize.Value)
        {
            AddLine(output, "Parent container completed.");
            AddLine(output, "New parent container assigned.");
            _parentIndex++;
            _parentSize = null;
            _processedCount = 0;
            AddLine(output, string.Empty);
        }

        AddLine(output, "Queue updated.");
        AddLine(output, string.Empty);
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
}
