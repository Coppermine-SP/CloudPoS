using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace CloudInteractive.CloudPos.Services;

public sealed class SoundService(IJSRuntime js, NavigationManager nav) : IAsyncDisposable
{
    public enum Sound { Chimes, Ding, Notify }

    private readonly Dictionary<Sound, string> _soundFileDictionary = new()
    {
        { Sound.Chimes, "chimes" },
        { Sound.Ding, "ding" },
        { Sound.Notify, "notify" }
    };
    
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        js.InvokeAsync<IJSObjectReference>(
            "import", $"{nav.BaseUri}js/sound.js").AsTask());

    public async Task PlayAsync(Sound sound)
    {
        var module = await _moduleTask.Value;
        module.InvokeVoidAsync("play", nav.ToAbsoluteUri($"/media/{_soundFileDictionary[sound]}.mp3").ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
            await (await _moduleTask.Value).DisposeAsync();
    }
}