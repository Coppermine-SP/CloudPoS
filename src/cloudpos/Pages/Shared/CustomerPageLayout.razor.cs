using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class CustomerPageLayout(InteractiveInteropService interop, TableService table, TableEventBroker broker, ILogger<CustomerPageLayout> logger, NavigationManager navigation, ConfigurationService config, ModalService modal) : PageLayoutBase(interop, modal), IDisposable
{
    private readonly InteractiveInteropService _interop = interop;
    private bool _init = false;
    
    protected override MenuItem[] GetMenuItems() =>
    [
        new() { Name = "메뉴", Url = "Customer/Menu" },
        new() { Name = "주문 내역", Url = "Customer/History" }
    ];
    
    protected override async Task OnInitializedAsync()
    {
        var result = await table.ValidateSessionAsync(false);
        if (result == TableService.ValidateResult.Unauthorized)
        {
            logger.LogInformation("Unauthorized. Redirecting to /Customer/Authorize?Error=0.");
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
            logger.LogInformation("Session ended. Redirecting to /Customer/Authorize?Error=1.");
            navigation.NavigateTo("/Customer/Authorize?Error=1", replace: true,  forceLoad: true);
            return;
        }
        
        broker.Subscribe(table.GetSession()!.TableId, OnTableEvent);
        await _interop.GetPreferredColorSchemeAsync();
        _init = true;
        StateHasChanged();
    }
    
    private void OnTableEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.SessionEnd)
        {
            navigation.NavigateTo("/Customer/Receipt", replace: true, forceLoad: true);
        }

        else if (e.EventType == TableEventArgs.TableEventType.StaffCall)
        {
            _ = _interop.ShowNotifyAsync("직원 호출이 완료되었습니다.", InteractiveInteropService.NotifyType.Success);
        }

        else if (e.EventType == TableEventArgs.TableEventType.Message)
        {
            if (e.Data is null || e.Data is not MessageEventArgs) return;
            var data = (MessageEventArgs)e.Data;
            _ = _interop.ShowNotifyAsync($"관리자의 메시지: {((MessageEventArgs)e.Data!).Message}",
                    InteractiveInteropService.NotifyType.Info);
        }
        else if (e.EventType == TableEventArgs.TableEventType.Order)
        {
            if (e.Data is null || e.Data is not OrderEventArgs) return;
            var data = (OrderEventArgs)e.Data;
            var orderTitle = data.Order.OrderItems.First().Item.Name;
            if (data.Order.OrderItems.Count > 1)
                orderTitle += $" 외 {data.Order.OrderItems.Count - 1}개";

            if (data.EventType == OrderEventType.Created)
                _ = _interop.ShowNotifyAsync($"주문 \"{orderTitle}\"이(가) 접수되었습니다.", InteractiveInteropService.NotifyType.Success);
            else if (data.EventType == OrderEventType.Cancelled)
                _ = _interop.ShowNotifyAsync($"주문 \"{orderTitle}\"이(가) 취소되었습니다.",
                    InteractiveInteropService.NotifyType.Warning);
            else
                _ = _interop.ShowNotifyAsync($"주문 \"{orderTitle}\"이(가) 완료되었습니다.", InteractiveInteropService.NotifyType.Success);
            
            
        }
        logger.LogInformation($"Table {table.GetSession()!.TableId} received event {e.EventType}");
    }
    
    public void Dispose()
    {
        if(_init) broker.Unsubscribe(table.GetSession()!.TableId, OnTableEvent);
    }

    private async Task OnCallBtnClickAsync()
    {
            if (await modal.ShowAsync<AlertModal, bool>("테이블 콜", ModalService.Params()
                    .Add("InnerHtml", "정말 직원을 호출하시겠습니까?")
                    .Add("IsCancelable", true)
                    .Build()))
            {
                broker.Publish(new TableEventArgs()
                {
                    TableId = table.GetSession()!.TableId,
                    EventType = TableEventArgs.TableEventType.StaffCall,
                    Data = table.GetSession()
                });
            }

    }

    private async Task OnShareBtnClick()
    {
        await modal.ShowAsync<ShareSessionModal, object?>("세션 공유하기", ModalService.Params()
            .Add("Session", table.GetSession()!)
            .Build());
    }
}
