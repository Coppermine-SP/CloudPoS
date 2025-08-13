using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class CustomerPageLayout(InteractiveInteropService interop, TableService table, TableEventBroker broker, ILogger<CustomerPageLayout> logger, NavigationManager navigation, ConfigurationService config, ModalService modal, IDbContextFactory<ServerDbContext> factory) : PageLayoutBase(interop, modal), IDisposable
{
    private readonly InteractiveInteropService _interop = interop;
    private bool _init;
    private readonly ModalService _modal = modal;
    private TableSession? _session;

    protected override MenuItem[] GetMenuItems() =>
    [
        new() { Name = "메뉴", Url = "customer/menu" },
        new() { Name = "주문 내역", Url = "customer/history" }
    ];
    
    protected override async Task OnInitializedAsync()
    {
        _session = await table.GetSessionAsync();
        broker.Subscribe(_session!.TableId, OnTableEvent);
        await _interop.GetPreferredColorSchemeAsync();
        _init = true;
        StateHasChanged();
    }
    
    private async void OnTableEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.SessionEnd)
        {
            navigation.NavigateTo("/customer/receipt", replace: true, forceLoad: true);
        }

        else if (e.EventType == TableEventArgs.TableEventType.StaffCall)
        {
            _ = _interop.ShowNotifyAsync("직원 호출이 완료되었습니다.", InteractiveInteropService.NotifyType.Success);
        }

        else if (e.EventType == TableEventArgs.TableEventType.Message)
        {
            if (e.Data is not MessageEventArgs) return;
            _ = _interop.ShowNotifyAsync($"관리자의 메시지: {((MessageEventArgs)e.Data!).Message}",
                    InteractiveInteropService.NotifyType.Info);
        }
        else if (e.EventType == TableEventArgs.TableEventType.Order)
        {
            if (e.Data is not OrderEventArgs data) return;
            await using var context = await factory.CreateDbContextAsync();

            var order = await context.Orders
                .Include(x => x.OrderItems)
                .ThenInclude(orderItem => orderItem.Item)
                .FirstOrDefaultAsync(x => x.OrderId == data.OrderId);
            if (order is null) return;

            var orderTitle = order.OrderItems.First().Item.Name;
            if (order.OrderItems.Count > 1)
                orderTitle += $" 외 {order.OrderItems.Count - 1}개";

            if (data.EventType == OrderEventType.Created)
                _ = _interop.ShowNotifyAsync($"주문 \"{orderTitle}\"이(가) 접수되었습니다.", InteractiveInteropService.NotifyType.Success);
            else if (data.EventType == OrderEventType.Cancelled)
                _ = _interop.ShowNotifyAsync($"주문 \"{orderTitle}\"이(가) 취소되었습니다.",
                    InteractiveInteropService.NotifyType.Warning);
            else
                _ = _interop.ShowNotifyAsync($"주문 \"{orderTitle}\"이(가) 완료되었습니다.", InteractiveInteropService.NotifyType.Success);
            
            
        }
        logger.LogInformation($"Table {_session!.TableId} received event {e.EventType}");
    }

    void IDisposable.Dispose()
    {
        if(_init) broker.Unsubscribe(_session!.TableId, OnTableEvent);
    }

    private async Task OnCallBtnClickAsync()
    {
            if (await _modal.ShowAsync<AlertModal, bool>("테이블 콜", ModalService.Params()
                    .Add("InnerHtml", "정말 직원을 호출하시겠습니까?")
                    .Add("IsCancelable", true)
                    .Build()))
            {
                var session = await table.GetSessionAsync();
                broker.Publish(new TableEventArgs()
                {
                    TableId = session!.TableId,
                    EventType = TableEventArgs.TableEventType.StaffCall,
                    Data = session.SessionId
                });
            }

    }

    private async Task OnShareBtnClick()
    {
        await _modal.ShowAsync<ShareSessionModal, object?>("세션 공유하기", ModalService.Params()
            .Add("Session", await table.GetSessionAsync()!)
            .Build());
    }
}
