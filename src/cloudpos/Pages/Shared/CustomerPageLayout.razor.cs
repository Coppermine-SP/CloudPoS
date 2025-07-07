using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class CustomerPageLayout(InteractiveInteropService interop, TableService table, TableEventBroker broker, ILogger<CustomerPageLayout> logger, NavigationManager navigation) : PageLayoutBase(interop), IDisposable
{
    private readonly InteractiveInteropService _interop = interop;
    protected override MenuItem[] GetMenuItems() =>
    [
        new() { Name = "메뉴", Url = "Customer/Menu" },
        new() { Name = "주문 내역", Url = "Customer/History" }
    ];
    
    protected override void OnInitialized()
    {
        var result = table.ValidateSession(false);
        if (result == TableService.ValidateResult.Unauthorized)
        {
            logger.LogInformation("SessionId is null. Redirecting to /Customer/Authorize?Error=0.");
            navigation.NavigateTo("/Customer/Authorize?Error=0", replace: true, forceLoad: true);
            return;
        }
        if (result == TableService.ValidateResult.InvalidSessionId)
        {
            logger.LogWarning($"Invalid session. Redirecting to /Customer/Authorize?Error=2.");
            navigation.NavigateTo("/Customer/Authorize?Error=2", replace: true, forceLoad: true);
            return;
        }
        if (result == TableService.ValidateResult.SessionExpired)
        {
            logger.LogInformation("Session ended. Redirecting to /Customer/Authorize?Error=0.");
            navigation.NavigateTo("/Customer/Authorize?Error=1", replace: true,  forceLoad: true);
            return;
        }
        
        broker.Subscribe(table.GetSession()!.TableId, OnTableEvent);
        _ = _interop.GetPreferredColorSchemeAsync();
    }
    
    private void OnTableEvent(object? sender, TableEventArgs e)
    {
        switch (e.EventType)
        {
            case TableEventArgs.TableEventType.SessionEnd:
                navigation.NavigateTo("/Customer/Authorize?Error=3", replace: true,  forceLoad: true);
                break;
            case TableEventArgs.TableEventType.StaffCall:
                _ = _interop.ShowNotifyAsync("점원 호출이 전송되었습니다.", InteractiveInteropService.NotifyType.Success);
                break;
        }

        logger.LogInformation($"Table {table.GetSession()!.TableId} received event {e.EventType}");
    }
    
    public void Dispose()
    {
        if(table.GetSession() is not null) broker.Unsubscribe(table.GetSession()!.TableId, OnTableEvent);
        _ = _interop.DisposeAsync();
    }

    private async Task OnCallBtnClickAsync()
    {
        if (await _interop.ShowModalAsync("직원 호출", "정말 직원을 호출하시겠습니까?"))
        {
            await broker.PublishAsync(new TableEventArgs()
            {
                TableId = table.GetSession()!.TableId,
                EventType = TableEventArgs.TableEventType.StaffCall
            });
        }
    }
}