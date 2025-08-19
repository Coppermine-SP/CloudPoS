namespace CloudInteractive.CloudPos.Services.Debounce;

public interface IDebouncedTask : IAsyncDisposable
{
    void Request();

    Task TriggerNowAsync(CancellationToken ct = default);
}

public interface IDebounceService : IAsyncDisposable
{
    IDebouncedTask Create(DebouncePolicy policy, Func<CancellationToken, Task> action);
}