using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Runner.Avalonia;

/// <summary>
/// A very small "classic terminal" rendering surface.
///
/// PoC goals:
/// - Fixed grid (cols/rows)
/// - Addressable characters (per-cell)
/// - Simple CRT-ish effects (scanlines + flicker)
///
/// This is intentionally not a full terminal emulator.
/// </summary>
public sealed partial class CrtTerminalControl : Control
{
    public int Columns { get; set; } = 100;
    public int Rows { get; set; } = 40;

    public bool InlineInputActive => _inlineActive;

    private readonly List<StringBuilder> _lines = new();
    private int _cursorCol;
    private readonly List<long[]> _cellAges = new(); // per row, per col: unix ms when last written

    // Inline input editor overlay (drawn into the buffer at a fixed starting column)
    private bool _inlineActive;
    private int _inlineLineIndex;
    private int _inlineStartCol;
    private int _inlineLastLen;

    private Typeface _typeface = new Typeface("Consolas");
    private double _fontSize = 16;

    private double _cellW;
    private double _cellH;

    private readonly DispatcherTimer _timer;
    private long _ticks;

    public CrtTerminalControl()
    {
        Focusable = false;
        ClipToBounds = true;

        EnsureAtLeastOneLine();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) =>
        {
            _ticks++;
            InvalidateVisual();
        };
        _timer.Start();
    }

    public void Clear()
    {
        _lines.Clear();
        _cellAges.Clear();
        _cursorCol = 0;
        _inlineActive = false;
        _inlineLineIndex = 0;
        _inlineStartCol = 0;
        _inlineLastLen = 0;
        EnsureAtLeastOneLine();
        InvalidateVisual();
    }

    public void Append(string text, bool newLine)
    {
        if (text == null) text = string.Empty;

        // If the engine is producing output, cancel any inline editor.
        // (The runner will re-enter input mode after printing finishes.)
        _inlineActive = false;
        _inlineLastLen = 0;

        EnsureAtLeastOneLine();
        foreach (var ch in text)
        {
            if (ch == '\r')
                continue;

            if (ch == '\n')
            {
                NewLine();
                continue;
            }

            PutChar(ch);
        }

        if (newLine)
            NewLine();

        TrimScrollback();
        InvalidateVisual();
    }

    private void EnsureAtLeastOneLine()
    {
        if (_lines.Count == 0)
        {
            _lines.Add(new StringBuilder());
            _cellAges.Add(new long[Columns]);
        }
    }

    private void PutChar(char ch)
    {
        EnsureAtLeastOneLine();

        if (_cursorCol >= Columns)
            NewLine();

        var row = _lines[^1];
        // pad to current col
        while (row.Length < _cursorCol)
            row.Append(' ');

        if (row.Length == _cursorCol)
            row.Append(ch);
        else
            row[_cursorCol] = ch;

        var ages = _cellAges[^1];
        ages[_cursorCol] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _cursorCol++;
    }

    private void NewLine()
    {
        _lines.Add(new StringBuilder());
        _cellAges.Add(new long[Columns]);
        _cursorCol = 0;
    }

    private void TrimScrollback()
    {
        // Keep enough history so the last Rows can be shown, plus a little buffer.
        var maxLines = Math.Max(Rows * 10, Rows + 10);
        while (_lines.Count > maxLines)
        {
            _lines.RemoveAt(0);
            _cellAges.RemoveAt(0);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bg = new SolidColorBrush(Color.Parse("#050607"));
        context.FillRectangle(bg, new Rect(Bounds.Size));

        // Measure cell size from a representative glyph.
        var ft = new FormattedText(
            "W",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            Brushes.White
        );
        _cellW = ft.Width;
        _cellH = ft.Height * 1.2; // a bit of line spacing

        // Compute visible region
        var rows = Rows;
        var cols = Columns;

        var startLine = Math.Max(0, _lines.Count - rows);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Text draw
        for (int r = 0; r < rows; r++)
        {
            var lineIndex = startLine + r;
            if (lineIndex >= _lines.Count)
                break;

            var sb = _lines[lineIndex];
            var ages = _cellAges[lineIndex];

            for (int c = 0; c < cols; c++)
            {
                char ch = ' ';
                if (c < sb.Length)
                    ch = sb[c];

                // skip drawing spaces to reduce overdraw a bit
                if (ch == ' ')
                    continue;

                // Flicker: newer glyphs flicker more.
                var age = ages[c];
                var dt = age == 0 ? 999999 : (now - age);
                var freshness = Math.Clamp(1.0 - (dt / 2000.0), 0.0, 1.0);

                // cheap deterministic noise based on cell coords + tick
                var noise = Hash01(c, r, _ticks);
                var baseIntensity = 0.75 + 0.25 * noise;
                var flickerBoost = 0.35 * freshness;
                var intensity = Math.Clamp(baseIntensity + flickerBoost, 0.0, 1.0);

                var brush = new SolidColorBrush(Color.FromArgb((byte)(intensity * 255), 0x7d, 0xff, 0xb5));

                var x = c * _cellW;
                var y = r * _cellH;

                var glyph = new FormattedText(
                    ch.ToString(),
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _fontSize,
                    brush
                );

                context.DrawText(glyph, new Point(x, y));
            }
        }

        // CRT scanlines: darken every few pixels
        var scanBrush = new SolidColorBrush(Color.FromArgb(18, 0, 0, 0));
        for (double y = 0; y < Bounds.Height; y += 4)
        {
            context.FillRectangle(scanBrush, new Rect(0, y, Bounds.Width, 1));
        }

        // subtle vignette
        var vignette = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(40, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.2),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.8),
                new GradientStop(Color.FromArgb(50, 0, 0, 0), 1.0),
            }
        };
        context.FillRectangle(vignette, new Rect(Bounds.Size));

        // border glow-ish
        var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 0x0f, 0x2a, 0x1b)), 1);
        context.DrawRectangle(borderPen, new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1));
    }

    private static double Hash01(int x, int y, long t)
    {
        unchecked
        {
            long h = 1469598103934665603L;
            h ^= (uint)x; h *= 1099511628211L;
            h ^= (uint)y; h *= 1099511628211L;
            h ^= (uint)(t & 0xffffffff); h *= 1099511628211L;
            // map to 0..1
            var v = (h & 0xffff) / 65535.0;
            return v;
        }
    }
}
