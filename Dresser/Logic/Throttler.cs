using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace Dresser.Logic
{
    internal class Throttler<T>
    {
        private ConcurrentQueue<Func<T>> _actions = new ConcurrentQueue<Func<T>>();
        private Timer? _timer;
        private Stopwatch _stopwatch = new Stopwatch();
        private int _delayInMilliseconds;

        public Throttler(int seconds)
        {
            _delayInMilliseconds = seconds;
            if (_delayInMilliseconds == 0) return;
            _timer = new Timer(_delayInMilliseconds); // 1 second
            _timer.Elapsed += (sender, e) => ExecuteAction();
            _timer.AutoReset = true;
        }

        public T Throttle(Func<T> action)
        {
            if (_delayInMilliseconds == 0)
            {
                return action();
            }

            var tcs = new TaskCompletionSource<T>();
            _actions.Enqueue(() =>
            {
                try
                {
                    var result = action();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                return default!;
            });

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

            return tcs.Task.Result;
        }

        private void ExecuteAction()
        {
            if (_actions.TryDequeue(out Func<T>? action))
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