namespace CloudInteractive.CloudPos.Services.Debounce;


internal sealed class DebouncedTask(DebouncePolicy policy, Func<CancellationToken, Task> action, ILogger? logger)
    : IDebouncedTask
{
    private readonly SemaphoreSlim _runLock = new(1, 1);   // single-flight
    private readonly Lock _gate = new();

    private DateTime _lastRunUtc = DateTime.MinValue;
    private CancellationTokenSource? _pendingCts;
    private bool _disposed;

    public void Request()
    {
        if (_disposed) return;

        var now = DateTime.UtcNow;
        if (policy.MaxInterval > TimeSpan.Zero && now - _lastRunUtc >= policy.MaxInterval)
        {
            _ = TriggerNowAsync();
            return;
        }

        lock (_gate)
        {
            _pendingCts?.Cancel();
            _pendingCts?.Dispose();
            _pendingCts = new CancellationTokenSource();
            var cts = _pendingCts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(policy.Debounce, cts.Token);
                    await RunOnceAsync(cts.Token);
                }
                catch (OperationCanceledException) { /* 합쳐짐 */ }
                catch (Exception ex) { logger?.LogError(ex, "Debounced action failed."); }
            }, cts.Token);
        }
    }

    public async Task TriggerNowAsync(CancellationToken ct = default)
    {
        if (_disposed) return;

        CancellationTokenSource? toCancel;
        lock (_gate)
        {
            toCancel = _pendingCts;
            _pendingCts = null;
        }

        try
        {
            await toCancel?.CancelAsync()!;
        }
        catch
        {
             //ignored
        }
        toCancel?.Dispose();
        
        var cancellationToken = ct;
        await RunOnceAsync(cancellationToken);
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        await _runLock.WaitAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();
            await action(ct);
            _lastRunUtc = DateTime.UtcNow;
        }
        finally
        {
            _runLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        CancellationTokenSource? toCancel;
        lock (_gate)
        {
            toCancel = _pendingCts;
            _pendingCts = null;
        }

        try
        {
            await toCancel?.CancelAsync()!;
        }
        catch
        {
            // ignored
        }

        toCancel?.Dispose();

        _runLock.Dispose();

        await Task.CompletedTask;
    }
}