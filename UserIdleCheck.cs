using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Soldering_Mgmt;

public sealed class IdleSessionManager : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly Window _window;
    private readonly Func<Task> _onTimeoutAsync;
    private readonly DispatcherTimer _timer;
    private DateTime _lastActivityUtc;

    public IdleSessionManager(Window window, TimeSpan timeout, Func<Task> onTimeoutAsync)
    {
        _window = window;
        _timeout = timeout;
        _onTimeoutAsync = onTimeoutAsync;
        _lastActivityUtc = DateTime.UtcNow;

        if (_window.Content is FrameworkElement root)
        {
            root.PointerMoved += (_, __) => Touch();
            root.PointerPressed += (_, __) => Touch();
            root.PointerReleased += (_, __) => Touch();
            root.PointerWheelChanged += (_, __) => Touch();
            root.KeyDown += (_, __) => Touch();
            root.KeyUp += (_, __) => Touch();
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Touch() => _lastActivityUtc = DateTime.UtcNow;

    private async void Timer_Tick(object? sender, object e)
    {
        if (DateTime.UtcNow - _lastActivityUtc >= _timeout)
        {
            _timer.Stop();
            try
            {
                // ★ 타임아웃 콜백도 await
                await _onTimeoutAsync();
            }
            finally
            {
                Dispose();
            }
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
    }
}
