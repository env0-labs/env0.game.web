using System;
using System.Collections.Generic;

namespace Env0.Runner.Wpf
{
    public sealed class InputHistory
    {
        private readonly List<string> _history = new List<string>();
        private int _index;

        public string Buffer { get; private set; } = string.Empty;

        public IReadOnlyList<string> History => _history;

        public int Index => _index;

        public void Append(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Buffer += text;
        }

        public void Set(string text)
        {
            Buffer = text ?? string.Empty;
        }

        public void Backspace()
        {
            if (Buffer.Length == 0)
                return;

            Buffer = Buffer[..^1];
        }

        public void Clear()
        {
            Buffer = string.Empty;
            _index = _history.Count;
        }

        public string Commit()
        {
            var input = Buffer;
            Buffer = string.Empty;
            AddToHistory(input);
            return input;
        }

        public void NavigateUp()
        {
            if (_history.Count == 0)
                return;

            _index = Math.Max(0, _index - 1);
            Buffer = _history[_index];
        }

        public void NavigateDown()
        {
            if (_history.Count == 0)
                return;

            _index = Math.Min(_history.Count, _index + 1);
            Buffer = _index >= _history.Count
                ? string.Empty
                : _history[_index];
        }

        private void AddToHistory(string input)
        {
            var trimmed = (input ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                _index = _history.Count;
                return;
            }

            if (_history.Count == 0 || !_history[^1].Equals(trimmed, StringComparison.Ordinal))
                _history.Add(trimmed);

            _index = _history.Count;
        }
    }
}
