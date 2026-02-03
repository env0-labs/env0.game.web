using System;
using System.Text;

namespace Runner.Avalonia;

public sealed partial class CrtTerminalControl
{
    public void BeginInlineInput()
    {
        EnsureAtLeastOneLine();
        _inlineActive = true;
        _inlineLineIndex = _lines.Count - 1;
        _inlineStartCol = _cursorCol;
        _inlineLastLen = 0;
        InvalidateVisual();
    }

    public void UpdateInlineInput(string text)
    {
        if (!_inlineActive)
            return;

        if (text == null)
            text = string.Empty;

        // Safety: if output has advanced the buffer, clamp.
        _inlineLineIndex = Math.Clamp(_inlineLineIndex, 0, _lines.Count - 1);

        var row = _lines[_inlineLineIndex];
        var ages = _cellAges[_inlineLineIndex];

        // Ensure we have space for start column.
        while (row.Length < _inlineStartCol)
            row.Append(' ');

        var max = Math.Max(_inlineLastLen, text.Length);
        for (int i = 0; i < max; i++)
        {
            var col = _inlineStartCol + i;
            if (col >= Columns)
                break;

            char ch = i < text.Length ? text[i] : ' ';

            if (row.Length == col)
                row.Append(ch);
            else if (row.Length > col)
                row[col] = ch;
            else
            {
                // pad
                while (row.Length < col)
                    row.Append(' ');
                row.Append(ch);
            }

            ages[col] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        _inlineLastLen = Math.Min(text.Length, Math.Max(0, Columns - _inlineStartCol));
        InvalidateVisual();
    }

    public void CommitInlineInput(string text)
    {
        if (!_inlineActive)
        {
            // still ensure we move to next line for a terminal feel
            NewLine();
            return;
        }

        UpdateInlineInput(text ?? string.Empty);
        _inlineActive = false;
        _inlineLastLen = 0;
        NewLine();
        TrimScrollback();
        InvalidateVisual();
    }
}
