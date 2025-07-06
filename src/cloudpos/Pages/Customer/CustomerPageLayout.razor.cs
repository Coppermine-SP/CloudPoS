using System.Security.Claims;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class CustomerPageLayout : LayoutComponentBase, IDisposable
{
    private struct MenuItem
    {
        public string Name;
        public string Url;
    }

    private readonly MenuItem[] _menuItems = new[]
    {
        new MenuItem() { Name = "메뉴", Url = "Customer/Menu" },
        new MenuItem() { Name = "주문 내역", Url = "Customer/History" }
    };

    private readonly ServerDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger _logger;
    private readonly IJSRuntime _js;
    private readonly ModalService _modal;
    private readonly ColorSchemeService _color;
    private readonly TableEventBroker _eventBroker;
    
    private IJSObjectReference? _notifyModule;
    private TableSession? _session;
    private string _currentColorSchemeIcon = "bi-circle-half";
    
    public CustomerPageLayout(IJSRuntime js, TableEventBroker eventBroker, ColorSchemeService color, ModalService modal, ILogger<CustomerPageLayout> logger, ServerDbContext context, IHttpContextAccessor accessor, NavigationManager navigation, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = context;
        _httpContextAccessor = accessor;
        _navigationManager = navigation;
        _logger = logger;
        _modal = modal;
        _color = color;
        _eventBroker = eventBroker;
        _js = js;
    }

    protected override void OnInitialized()
    {
        var sessionId = _httpContextAccessor?.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid);
        if (sessionId is null)
        {
            _logger.LogInformation("SessionId is null. Redirecting to /Customer/Authorize?Error=0.");
            _navigationManager.NavigateTo("/Customer/Authorize?Error=0", replace: true, forceLoad: true);
            return;
        }

        _session = _dbContext.Sessions
            .Include(x => x.Table).
            FirstOrDefault(x => x.SessionId == Convert.ToInt32(sessionId.Value));
        if (_session is null)
        {
            _logger.LogWarning($"Invalid session(id={sessionId.Value}). Redirecting to /Customer/Authorize?Error=2.");
            _navigationManager.NavigateTo("/Customer/Authorize?Error=2", replace: true, forceLoad: true);
            return;
        }

        if (_session.EndedAt is not null)
        {
            _logger.LogInformation("Session ended. Redirecting to /Customer/Authorize?Error=0.");
            _navigationManager.NavigateTo("/Customer/Authorize?Error=1", replace: true,  forceLoad: true);
            return;
        }
        
        _eventBroker.Subscribe(_session.TableId, OnTableEvent);
    }
    
    private void OnTableEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.SessionEnd)
        {
            _navigationManager.NavigateTo("/Customer/Authorize?Error=3", replace: true,  forceLoad: true);
        }
        else if (e.EventType == TableEventArgs.TableEventType.StaffCall)
        {
            _ = _notifyModule!.InvokeVoidAsync("show",
                "직원 호출되었습니다.", "success");
        }
        
        _logger.LogInformation($"Table {_session!.TableId} received event {e.EventType}");
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                _notifyModule = await _js.InvokeAsync<IJSObjectReference>(
                    "import", $"{_navigationManager.BaseUri}js/messageBar.js");
                
                _currentColorSchemeIcon =
                    _color.PreferredColorSchemeIconDictionary[await _color.GetPreferredColorSchemeAsync()];
                StateHasChanged();
            }
        }
        catch
        {
            // ignored
        }
    }
    
    public void Dispose()
    {
        if(_session is not null) _eventBroker.Unsubscribe(_session!.TableId, OnTableEvent);
        if(_notifyModule is not null) _notifyModule.DisposeAsync();
        _ = _modal.DisposeAsync();
        _ = _color.DisposeAsync();
    }

    private async Task OnCallBtnClickAsync()
    {
        if (await _modal.ShowModalAsync("직원 호출", "정말 직원을 호출하시겠습니까?"))
        {
            await _eventBroker.PublishAsync(new TableEventArgs()
            {
                TableId = _session!.TableId,
                EventType = TableEventArgs.TableEventType.StaffCall
            });
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
    
}