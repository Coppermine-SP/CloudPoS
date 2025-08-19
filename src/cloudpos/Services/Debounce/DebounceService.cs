namespace CloudInteractive.CloudPos.Services.Debounce;

public sealed class DebounceService(ILogger<DebounceService> logger) : IDebounceService
{
    private readonly List<IDebouncedTask> _owned = new();
    private bool _disposed;

    public IDebouncedTask Create(DebouncePolicy policy, Func<CancellationToken, Task> action)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DebounceService));
        var instance = new DebouncedTask(policy, action, logger);
        lock (_owned) _owned.Add(instance);
        return instance;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        IDebouncedTask[] snapshot;
        lock (_owned) snapshot = _owned.ToArray();

        foreach (var t in snapshot)
        {
            try
            {
                await t.DisposeAsync();
            }
            catch
            {
                //ignored
            }
        }

        lock (_owned) _owned.Clear();
    }
}