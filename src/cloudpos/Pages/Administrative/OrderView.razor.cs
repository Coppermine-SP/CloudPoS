using System.Reflection.Metadata.Ecma335;
using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Pages.Shared;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class OrderView(IDbContextFactory<ServerDbContext> factory, TableEventBroker broker, ModalService modal, TableService table, InteractiveInteropService interop) : ComponentBase, IDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    private IJSObjectReference? _orderViewModule;
    private string _memoContent = string.Empty;
    private bool _shouldUpdateMemo = false;
    private int? _selectedOrderId;
    private List<Order>? _activeOrders;
    
    private string CurrencyFormat(int x) => $"{x:₩#,###}";
    
    private async Task<List<Order>> GetActiveOrdersAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Orders
            .Where(x => x.Status == Order.OrderStatus.Received)
            .Include(x => x.Session)
            .ThenInclude(x => x!.Table)
            .Include(x => x.OrderItems)
            .ThenInclude(x => x.Item)
            .OrderBy(x => x.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        broker.Subscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
        _activeOrders = await GetActiveOrdersAsync();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _orderViewModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/showOffcanvas.min.js");
            }
            catch
            {
                //ignored
            }
        }
    }
    
    private async void OnBroadcastEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.Order)
        {
            if(e.Data is not OrderEventArgs args) return;
            if(_selectedOrderId is not null && args.OrderId == _selectedOrderId && args.EventType is OrderEventType.MemoUpdated)
                _shouldUpdateMemo = true;
        }
        
        _activeOrders = await GetActiveOrdersAsync();
        StateHasChanged();
    }

    private async Task OnCompleteOrderAsync()
    {
        if (_selectedOrderId is null) return;
        if (!await modal.ShowAsync<AlertModal, bool>("주문 완료", ModalService.Params()
                .Add("IsCancelable", true)
                .Add("InnerHtml", $"정말 주문 #{_selectedOrderId}를 완료하시겠습니까?")
                .Build())) return;

        if (_selectedOrderId is null) return;
        if (!await table.ChangeOrderStatusAsync(_selectedOrderId.Value, Order.OrderStatus.Completed))
            _ =interop.ShowNotifyAsync("서버 오류가 발생하여 주문 상태를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);

        _selectedOrderId = null;
        StateHasChanged();
    }

    private async Task OnUpdateMemoAsync()
    {
        if (!await table.UpdateOrderMemoAsync(_selectedOrderId!.Value, _memoContent))
            _ = interop.ShowNotifyAsync("서버 오류가 발생하여 주문 메모를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);
        
        _shouldUpdateMemo = true;
        StateHasChanged();
    }
    
    private async Task OnCancelOrderAsync()
    {
        if (_selectedOrderId is null) return;
        if (!await modal.ShowAsync<AlertModal, bool>("주문 취소", ModalService.Params()
                .Add("IsCancelable", true)
                .Add("InnerHtml", $"정말 주문 #{_selectedOrderId}를 취소하시겠습니까?")
                .Build())) return;

        if (_selectedOrderId is null) return;
        if (!await table.ChangeOrderStatusAsync(_selectedOrderId.Value, Order.OrderStatus.Cancelled))
            _ =interop.ShowNotifyAsync("서버 오류가 발생하여 주문 상태를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);

        _selectedOrderId = null;
    }

    private async Task SetOrderAsync(Order o)
    {
        _selectedOrderId = o.OrderId;
        _shouldUpdateMemo = true;
        StateHasChanged();
        
        if (_orderViewModule is not null)
            await _orderViewModule.InvokeVoidAsync("showOffcanvas", "offcanvasResponsive");
    }

    public void Dispose()
        => broker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_orderViewModule is not null)
            {
                await _orderViewModule.DisposeAsync();
            }
        }
        catch
        {
            //ignored
        }
    }
}