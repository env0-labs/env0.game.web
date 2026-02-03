using System;

namespace Env0.Terminal.Terminal.Progress
{
    public static class AsciiMeter
    {
        public static string Bar(int value, int max, int width = 14, char fill = '#', char empty = '-')
        {
            if (width <= 0) width = 10;
            if (max <= 0) max = 1;
            if (value < 0) value = 0;
            if (value > max) value = max;

            var ratio = (double)value / max;
            var filled = (int)Math.Round(ratio * width, MidpointRounding.AwayFromZero);
            if (filled < 0) filled = 0;
            if (filled > width) filled = width;

            return "[" + new string(fill, filled) + new string(empty, width - filled) + "]";
        }

        public static string Percent(int value, int max)
        {
            if (max <= 0) max = 1;
            if (value < 0) value = 0;
            if (value > max) value = max;
            var pct = (int)Math.Round((double)value / max * 100, MidpointRounding.AwayFromZero);
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            return pct + "%";
        }
    }
}
