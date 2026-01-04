using System;
using System.Threading;

namespace Env0.Runner.Wpf.Tests
{
    internal static class TestHelpers
    {
        public static void RunSta(Action action)
        {
            Exception captured = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (captured != null)
                throw captured;
        }
    }
}
