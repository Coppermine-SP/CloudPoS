using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class AdminPageLayout(ILogger<AdminPageLayout> logger, TableService table, InteractiveInteropService interop, TableEventBroker broker, NavigationManager navigation, ConfigurationService config) : PageLayoutBase(interop), IDisposable
{
    private readonly InteractiveInteropService _interop = interop;
    private bool _init = false;
    
    protected override MenuItem[] GetMenuItems() =>
    [
        new() { Name = "테이블 뷰", Url = "Customer/Menu" },
        new() { Name = "주문 뷰", Url = "Customer/History" },
        new() { Name = "통계", Url = "Customer/History" },
        new() { Name = "객체 관리자", Url = "Customer/History" },
        new() { Name = "개발자 도구", Url = "Customer/History" }
    ];

    protected override void OnInitialized()
    {
        if (table.ValidateSession(true) != TableService.ValidateResult.Ok)
        {
            logger.LogInformation("Unauthorized. Redirecting to /Administrative/Authorize");
            navigation.NavigateTo("/Administrative/Authorize", replace: true, forceLoad: true);
            return;
        }
        
        _init = true;
        broker.Subscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }
    
    private async void OnBroadcastEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.StaffCall)
            await OnStaffCalled(e.TableId, DateTimeOffset.Now);
    }
    
    
    private async Task OnStaffCalled(int sessionId, DateTimeOffset time)
    {
        await _interop.PlaySoundAsync(InteractiveInteropService.Sound.Ding);
        await _interop.ShowModalAsync("테이블 호출", $"테이블에서 호출이 있습니다.", false);
    }

    private async Task OnLogoutBtnClickAsync()
    {
        if (await _interop.ShowModalAsync("로그아웃", "정말 로그아웃하시겠습니까?"))
        {
            navigation.NavigateTo("/Administrative/Authorize?SignOut=true", replace: true, forceLoad: true);
        }
    }
    
    public void Dispose()
    {
        if(_init) broker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }
}