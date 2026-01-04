using System.Windows.Documents;
using System.Windows.Media;
using Env0.Core;

namespace Env0.Runner.Wpf
{
    public static class OutputLineStyler
    {
        private static readonly SolidColorBrush StandardBrush = CreateFrozenBrush(124, 255, 124);
        private static readonly SolidColorBrush MutedBrush = CreateFrozenBrush(88, 190, 88);

        public static Run CreateRun(OutputLine line)
        {
            var text = line.Text ?? string.Empty;
            var run = new Run(text);

            switch (line.Type)
            {
                case OutputType.Error:
                    run.FontStyle = System.Windows.FontStyles.Italic;
                    run.Foreground = MutedBrush;
                    break;
                case OutputType.System:
                    run.Foreground = MutedBrush;
                    break;
                default:
                    run.Foreground = StandardBrush;
                    break;
            }

            return run;
        }

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            if (brush.CanFreeze)
                brush.Freeze();
            return brush;
        }
    }
}
