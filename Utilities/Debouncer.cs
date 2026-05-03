using System;
using System.Collections.Generic;
using System.Text;

namespace SimplexMethodApp.Utilities
{
    public class Debouncer
    {
        private CancellationTokenSource? _cts;

        public void Run(Action action, int delayMs = 500)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, token);

                    await MainThread.InvokeOnMainThreadAsync(action);
                }
                catch (TaskCanceledException) { }
            });
        }
    }
}
