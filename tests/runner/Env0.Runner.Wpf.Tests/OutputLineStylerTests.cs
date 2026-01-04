using System.Windows;
using System.Windows.Media;
using Env0.Core;
using Env0.Runner.Wpf;
using Xunit;

namespace Env0.Runner.Wpf.Tests
{
    public class OutputLineStylerTests
    {
        [Fact]
        public void CreateRun_StylesErrorLines()
        {
            TestHelpers.RunSta(() =>
            {
                var line = new OutputLine(OutputType.Error, "bad");

                var run = OutputLineStyler.CreateRun(line);

                Assert.Equal(FontStyles.Italic, run.FontStyle);
                Assert.Equal(Color.FromRgb(88, 190, 88), ((SolidColorBrush)run.Foreground).Color);
            });
        }

        [Fact]
        public void CreateRun_StylesSystemLines()
        {
            TestHelpers.RunSta(() =>
            {
                var line = new OutputLine(OutputType.System, "boot");

                var run = OutputLineStyler.CreateRun(line);

                Assert.Equal(Color.FromRgb(88, 190, 88), ((SolidColorBrush)run.Foreground).Color);
                Assert.Equal(FontStyles.Normal, run.FontStyle);
            });
        }

        [Fact]
        public void CreateRun_LeavesStandardLinesDefault()
        {
            TestHelpers.RunSta(() =>
            {
                var line = new OutputLine(OutputType.Standard, "ok");

                var run = OutputLineStyler.CreateRun(line);

                Assert.Equal(Color.FromRgb(124, 255, 124), ((SolidColorBrush)run.Foreground).Color);
                Assert.Equal(FontStyles.Normal, run.FontStyle);
            });
        }
    }
}
