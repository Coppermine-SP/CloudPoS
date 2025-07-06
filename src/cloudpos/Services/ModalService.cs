using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace CloudInteractive.CloudPos.Services;

public sealed class ModalService(IJSRuntime js, NavigationManager nav) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        js.InvokeAsync<IJSObjectReference>(
            "import", $"{nav.BaseUri}js/modal.js").AsTask());

    public async Task<bool> ShowModalAsync(string title, string message, bool showNoBtn = true)
    {
        var module = await _moduleTask.Value;
        try
        {
            var result = await module.InvokeAsync<bool>("showModal", TimeSpan.FromMilliseconds(Timeout.Infinite), title,
                message, showNoBtn);
            return result;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
            await (await _moduleTask.Value).DisposeAsync();
    }
}