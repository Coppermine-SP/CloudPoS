using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class AdminPageLayout(InteractiveInteropService interop, TableEventBroker broker, NavigationManager navigation, ConfigurationService config) : PageLayoutBase(interop), IDisposable
{
    private readonly InteractiveInteropService _interop = interop;
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
    
    public void Dispose()
    {
        broker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }
}