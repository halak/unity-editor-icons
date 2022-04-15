using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace QuickEye.Editor.IconWindow
{
    public class StopwatchScope : IDisposable
    {
        private readonly string _timerName;
        private readonly Stopwatch _timer;

        public StopwatchScope(string timerName)
        {
            _timerName = timerName;
            _timer = new Stopwatch();
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            var timeTaken = _timer.Elapsed;
            Debug.Log($"{_timerName}: {timeTaken:m\\:ss\\.fff}");
        }
    }
}