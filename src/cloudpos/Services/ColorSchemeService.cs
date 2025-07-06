using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Services;

public sealed class ColorSchemeService(IJSRuntime js, NavigationManager nav) : IAsyncDisposable
{
    public enum ColorScheme {Auto = 0, Light = 1, Dark = 2}
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        js.InvokeAsync<IJSObjectReference>(
            "import", $"{nav.BaseUri}js/colorScheme.js").AsTask());

    public readonly Dictionary<ColorScheme, string> PreferredColorSchemeIconDictionary = new()
        {
            { ColorScheme.Auto, "bi-circle-half" },
            { ColorScheme.Light, "bi-sun-fill" },
            { ColorScheme.Dark, "bi-moon-stars-fill" }
        };
    
    public async Task<ColorScheme> GetPreferredColorSchemeAsync()
    {
        var module = await _moduleTask.Value;
        return (ColorScheme)await module.InvokeAsync<int>("getPreferredColorScheme");
    }

    public async void SetPreferredColorScheme(ColorScheme scheme)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setPreferredColorScheme", (int)scheme);
        await module.InvokeVoidAsync("setColorScheme", (int)scheme);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
            await (await _moduleTask.Value).DisposeAsync();
    }
}