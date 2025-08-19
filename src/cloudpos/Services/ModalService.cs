using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Services;

public readonly struct ModalResult<T>
{
    public bool IsCancelled { get; }
    public T? Value { get; }
    private ModalResult(bool cancelled, T? value) { IsCancelled = cancelled; Value = value; }
    public static ModalResult<T> Ok(T value) => new(false, value);
    public static ModalResult<T> Cancel() => new(true, default);
}

public sealed class ModalParameter
{
    internal readonly Dictionary<string, object?> Parameters;

    internal ModalParameter(Dictionary<string, object?> src)
        => Parameters = src;
}

public sealed class ModalParameterBuilder
{
    private readonly Dictionary<string, object?> _dict = new();
    public ModalParameterBuilder Add<T>(string key, T value)
    {
        _dict[key] = value; return this;
    }
    public ModalParameter Build() => new(_dict);
}

public sealed class ModalService
{
    private bool _modalOpen;
    private readonly object _sync = new();
    
    private Func<object?, Task>? _currentClose;

    public record ModalRequest(
        Type ComponentType,
        ModalParameter? Parameters,
        string? Title,
        Func<object?, Task> CloseCallback);

    public event Func<ModalRequest, Task>? OnModalRequested;
    public Task<ModalResult<TResult>> ShowAsync<TComponent, TResult>(
        string title,
        ModalParameter? param = null)
        where TComponent : IComponent
    {

        lock (_sync)
        {
            if (_modalOpen)
                throw new InvalidOperationException("A modal is already open in this session.");
            _modalOpen = true;
        }

        var tcs = new TaskCompletionSource<ModalResult<TResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);


        Func<object?, Task> close = obj =>
        {
            lock (_sync)
            {
                _modalOpen = false;
                _currentClose = null; 
            }

            if (obj is Exception ex)
            {
                tcs.TrySetException(ex);     
            }
            else if (obj is TResult value)
            {
                tcs.TrySetResult(ModalResult<TResult>.Ok(value)); 
            }
            else
            {
                tcs.TrySetResult(ModalResult<TResult>.Cancel());  
            }

            return Task.CompletedTask;
        };
        
        var req = new ModalRequest(
            ComponentType: typeof(TComponent),
            Parameters: param,
            Title: title,
            CloseCallback: close);
        
        lock (_sync) { _currentClose = close; }

        _ = OnModalRequested?.Invoke(req); 
        return tcs.Task;                    
    }
    
    public void CancelOpenModal(Exception? reason = null)
    {
        Func<object?, Task>? close;
        lock (_sync)
        {
            if (!_modalOpen) return;
            close = _currentClose;
        }

        if (close is not null)
        {
            _ = close(reason ?? null);
        }
    }

    public static ModalParameterBuilder Params() => new();
}