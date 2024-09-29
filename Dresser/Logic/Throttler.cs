using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;

namespace Dresser.Logic
{
    internal class Throttler
    {
        private ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
        private Timer? _timer;
        private Stopwatch _stopwatch = new Stopwatch();
        private int _delayInMilliseconds;

        public Throttler()
        {
            _delayInMilliseconds = 0;
            if (_delayInMilliseconds == 0) return;
            _timer = new Timer(_delayInMilliseconds); // 1 second
            _timer.Elapsed += (sender, e) => ExecuteAction();
            _timer.AutoReset = true;
        }

        public void Throttle(Action action)
        {
            if (_delayInMilliseconds == 0)
            {
                action();
                return;
            }
            _actions.Enqueue(action);
            if (_timer != null && !_timer.Enabled)
            {
                if (_stopwatch.IsRunning && _stopwatch.ElapsedMilliseconds < _delayInMilliseconds)
                {
                    _timer.Start();
                }
                else
                {
                    ExecuteAction();
                    _timer.Start();
                }
                _stopwatch.Restart();
            }
        }
        
        private void ExecuteAction()
        {
            if (_actions.TryDequeue(out Action? action))
            {
                PluginLog.Debug($"Executing action at {DateTime.Now} ");
                action();
            }
            else
            {
                PluginLog.Debug($"Stop throttling action at {DateTime.Now}, ready for new instant execution");
                _timer?.Stop();
            }
        }
    }
}