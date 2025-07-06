using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Admin;

public partial class AdminPageLayout : LayoutComponentBase, IDisposable
{
    private struct MenuItem
    {
        public string Name;
        public string Url;
    }

    private readonly MenuItem[] _menuItems = new[]
    {
        new MenuItem() { Name = "테이블 뷰", Url = "Customer/Menu" },
        new MenuItem() { Name = "주문 뷰", Url = "Customer/History" },
        new MenuItem() { Name = "통계", Url = "Customer/History" },
        new MenuItem() { Name = "객체 관리자", Url = "Customer/History" },
        new MenuItem() { Name = "개발자 도구", Url = "Customer/History" }
    };

    private readonly ServerDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger _logger;
    private readonly ModalService _modal;
    private readonly SoundService _sound;
    private readonly ConfigurationService _config;
    private readonly ColorSchemeService _color;
    private readonly TableEventBroker _eventBroker;
    
    private string _currentColorSchemeIcon = "bi-circle-half";
    
    public AdminPageLayout(TableEventBroker eventBroker, ColorSchemeService color, ConfigurationService config, SoundService sound, ModalService modal, ILogger<AdminPageLayout> logger, ServerDbContext context, IHttpContextAccessor accessor, NavigationManager navigation)
    {
        _dbContext = context;
        _httpContextAccessor = accessor;
        _navigationManager = navigation;
        _logger = logger;
        _modal = modal;
        _sound = sound;
        _config = config;
        _color = color;
        _eventBroker = eventBroker;
        
        _eventBroker.Subscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }

    private async void OnBroadcastEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.StaffCall)
            await OnStaffCalled(e.TableId, DateTimeOffset.Now);
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _currentColorSchemeIcon =
                _color.PreferredColorSchemeIconDictionary[await _color.GetPreferredColorSchemeAsync()];
            StateHasChanged();
        }
    }
    
    private async Task OnColorChangeBtnClickAsync()
    {
        int current = (int)await _color.GetPreferredColorSchemeAsync();
        int next = (current + 1) % 3;
        _color.SetPreferredColorScheme((ColorSchemeService.ColorScheme)next);
        _currentColorSchemeIcon = _color.PreferredColorSchemeIconDictionary[(ColorSchemeService.ColorScheme)next];
        StateHasChanged();
    }

    private async Task OnStaffCalled(int sessionId, DateTimeOffset time)
    {
        var table = _dbContext.Tables.First(x => x.TableId == sessionId);

        await _sound.PlayAsync(SoundService.Sound.Ding);
        await _modal.ShowModalAsync("테이블 호출", $"테이블 {table.Name}에서 호출이 있습니다.", false);
    }
    
    public void Dispose()
    {
        _eventBroker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }
}