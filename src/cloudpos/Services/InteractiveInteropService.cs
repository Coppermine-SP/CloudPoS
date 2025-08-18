using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Services;

public class InteractiveInteropService(IJSRuntime js, NavigationManager nav)
{
    public enum ColorScheme {Auto = 0, Light = 1, Dark = 2}

    private static readonly Dictionary<ColorScheme, (string, string)> PreferredColorSchemeDictionary = new()
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

    public async Task<bool> IsPwaDisplayMode() => await js.InvokeAsync<bool>("isPwaDisplayMode");
    
    public async Task<ColorScheme> GetPreferredColorSchemeAsync()
    {
        ColorScheme value = (ColorScheme)await js.InvokeAsync<int>("getPreferredColorScheme");
        _currentColorScheme = PreferredColorSchemeDictionary[(ColorScheme)value];

        return value;
    }

    public async Task SetPreferredColorSchemeAsync(ColorScheme scheme)
    {
        await js.InvokeVoidAsync("setPreferredColorScheme", (int)scheme);
        await js.InvokeVoidAsync("setColorScheme", (int)scheme);
        _currentColorScheme = PreferredColorSchemeDictionary[scheme];
    }
    
    public async Task PlaySoundAsync(Sound sound)
    {
        await js.InvokeVoidAsync("playSound", nav.ToAbsoluteUri($"/media/{SoundFileDictionary[sound]}.mp3").ToString());
    }
    
    public enum NotifyType { Info=0, Success=1, Warning=2, Error=3 }
    public async Task ShowNotifyAsync(string message, NotifyType type, int duration = 3000)
    {
        await js.InvokeVoidAsync("showNotify",
            message, type, duration);
    }

    public async Task ShowOffCanvasAsync() =>
        await js.InvokeVoidAsync("showOffcanvas", "offcanvasResponsive");
}