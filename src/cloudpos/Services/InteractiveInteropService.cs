using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Services;

public class InteractiveInteropService(IJSRuntime js, NavigationManager nav) : IAsyncDisposable
{
    public enum ColorScheme {Auto = 0, Light = 1, Dark = 2}
    public static readonly Dictionary<ColorScheme, (string, string)> PreferredColorSchemeDictionary = new()
    {
        { ColorScheme.Auto, ("자동","bi-circle-half") },
        { ColorScheme.Light, ("라이트","bi-sun-fill") },
        { ColorScheme.Dark, ("다크","bi-moon-stars-fill") }
    };
    private (string, string) _currentColorScheme = PreferredColorSchemeDictionary[ColorScheme.Auto];
    public (string, string) CurrentColorScheme => _currentColorScheme;
    
    public enum Sound { Chimes, Ding, Notify }
    private static readonly Dictionary<Sound, string> SoundFileDictionary = new()
    {
        { Sound.Chimes, "chimes" },
        { Sound.Ding, "ding" },
        { Sound.Notify, "notify" }
    };
    
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        js.InvokeAsync<IJSObjectReference>(
            "import", $"{nav.BaseUri}js/client.js").AsTask());
    
    public async Task<ColorScheme> GetPreferredColorSchemeAsync()
    {
        var module = await _moduleTask.Value;
        ColorScheme value = (ColorScheme)await module.InvokeAsync<int>("getPreferredColorScheme");
        _currentColorScheme = PreferredColorSchemeDictionary[(ColorScheme)value];

        return value;
    }

    public async Task SetPreferredColorSchemeAsync(ColorScheme scheme)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setPreferredColorScheme", (int)scheme);
        await module.InvokeVoidAsync("setColorScheme", (int)scheme);
        _currentColorScheme = PreferredColorSchemeDictionary[scheme];
    }
    
    public async Task PlaySoundAsync(Sound sound)
    {
        var module = await _moduleTask.Value;
        _ = module.InvokeVoidAsync("playSound", nav.ToAbsoluteUri($"/media/{SoundFileDictionary[sound]}.mp3").ToString());
    }
    
    public async Task<bool> ShowModalAsync(string title, string innerHtml, bool showNoBtn = true)
    {
        var module = await _moduleTask.Value;
        try
        {
            var result = await module.InvokeAsync<bool>("showModal", TimeSpan.FromMilliseconds(Timeout.Infinite), title,
                innerHtml, showNoBtn);
            return result;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
    
    public enum NotifyType { Info=0, Success=1, Warning=2, Error=3 }
    public async Task ShowNotifyAsync(string message, NotifyType type)
    {
        var module = await _moduleTask.Value;
        _ = module.InvokeVoidAsync("showNotify",
            message, type);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (!_moduleTask.IsValueCreated) return;

        try
        {
            await (await _moduleTask.Value).DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            //ignored
        }
            
    }
}