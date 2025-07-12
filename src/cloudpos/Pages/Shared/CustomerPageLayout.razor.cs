using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class CustomerPageLayout(InteractiveInteropService interop, TableService table, TableEventBroker broker, ILogger<CustomerPageLayout> logger, NavigationManager navigation, ConfigurationService config) : PageLayoutBase(interop), IDisposable
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
        var result = table.ValidateSession(false);
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
            _ = data.ShowAsModal
                ? _interop.ShowModalAsync("관리자의 메시지", data.Message, false)
                : _interop.ShowNotifyAsync($"관리자의 메시지: {((MessageEventArgs)e.Data!).Message}",
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
        if (await _interop.ShowModalAsync("직원 호출", "정말 직원을 호출하시겠습니까?"))
        {
            broker.Publish(new TableEventArgs()
            {
                TableId = table.GetSession()!.TableId,
                EventType = TableEventArgs.TableEventType.StaffCall,
                Data = table.GetSession()
            });
        }
    }

    private void OnShareBtnClick()
    {
        _ = _interop.ShowModalAsync("세션 공유", """
            <div class='alert alert-info shadow py-2 fw-normal m-0 d-flex flex-row justify-content-center align-items-center mb-4' style='font-size: 14px''>
               <div>
                   <i class="bi bi-info-circle-fill d-inline-block" style="margin-right: 8px;"></i>
               </div>
               <div>
               아래 QR 코드를 사용하여 다른 기기에서 세션에 참여할 수 있습니다.
               </div>
            </div>
            <div class='d-flex justify-content-center align-items-center'>
                <div class='card p-2 bg-white'>
                    <iframe src='/Customer/ShareSession'
                            width='128' height='128'
                            style='display:block;border:none;overflow:hidden'></iframe>
                </div>
            </div>
            """, false);
    }
}
