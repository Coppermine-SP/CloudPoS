using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Services;

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
    
    public record ModalRequest(
        Type ComponentType,
        ModalParameter? Parameters,
        string? Title,
        Func<object?, Task> CloseCallback);

    public event Func<ModalRequest, Task>? OnModalRequested;

    public Task<TResult?> ShowAsync<TComponent, TResult>(
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
        
        var tcs = new TaskCompletionSource<TResult?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        
        var req = new ModalRequest(
            ComponentType: typeof(TComponent),
            Parameters: param,
            Title: title,
            CloseCallback: obj =>
            {
                lock (_sync) { _modalOpen = false; }         
                if (obj is Exception ex)
                    tcs.TrySetException(ex);            
                else
                    tcs.TrySetResult((TResult?)obj);   
                return Task.CompletedTask;
            });

        _ = OnModalRequested?.Invoke(req);   
        return tcs.Task;                     
    }

    public static ModalParameterBuilder Params() => new();
}