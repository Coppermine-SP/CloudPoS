using System.Reflection.Metadata.Ecma335;
using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Pages.Shared;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class OrderView(IDbContextFactory<ServerDbContext> factory, ILogger<OrderView> logger, TableEventBroker broker, ModalService modal, TableService table, InteractiveInteropService interop) : ComponentBase, IDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    private IJSObjectReference? _orderViewModule;
    private ServerDbContext? _context;
    private ServerDbContext Context => _context ??= factory.CreateDbContext();

    private Order? _selectedOrder;
    private string CurrencyFormat(int x) => $"{x:₩#,###}";

    private List<Order> GetOrders => Context.Orders
        .Where(x => x.Status == Order.OrderStatus.Received)
        .Include(x => x.Session)
        .ThenInclude(x => x!.Table)
        .Include(x => x.OrderItems)
        .ThenInclude(x => x.Item)
        .OrderBy(x => x.CreatedAt)
        .ToList();
    
    protected override void OnInitialized()
    {
        broker.Subscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _orderViewModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/showOffcanvas.js");
        }
    }
    
    private void OnBroadcastEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.Order)
            StateHasChanged();
    }

    private async void OnCompleteOrder()
    {
        if (_selectedOrder is null) return;
        if (!await modal.ShowAsync<AlertModal, bool>("주문 완료", ModalService.Params()
                .Add("IsCancelable", true)
                .Add("InnerHtml", $"정말 주문 #{_selectedOrder.OrderId}를 완료하시겠습니까?")
                .Build())) return;

        if (!await table.ChangeOrderStatusAsync(_selectedOrder.OrderId, Order.OrderStatus.Completed))
            _ =interop.ShowNotifyAsync("서버 오류가 발생하여 주문 상태를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);

        _selectedOrder = null;
    } 
    
    private async void OnCancelOrder()
    {
        logger.LogCritical("S");
        if (_selectedOrder is null) return;
        if (!await modal.ShowAsync<AlertModal, bool>("주문 취소", ModalService.Params()
                .Add("IsCancelable", true)
                .Add("InnerHtml", $"정말 주문 #{_selectedOrder.OrderId}를 취소하시겠습니까?")
                .Build())) return;

        if (!await table.ChangeOrderStatusAsync(_selectedOrder.OrderId, Order.OrderStatus.Cancelled))
            _ =interop.ShowNotifyAsync("서버 오류가 발생하여 주문 상태를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);

        _selectedOrder = null;
    }

    private async Task GetOrderAsync(Order order)
    {
        _selectedOrder = order;
        
        if (_orderViewModule is not null)
            await _orderViewModule.InvokeVoidAsync("showOffcanvas", "offcanvasResponsive");
        
        StateHasChanged();
    }

    public void Dispose()
    {
        broker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
        _context?.Dispose();
    }
    public async ValueTask DisposeAsync()
    {
        if (_orderViewModule is not null)
        {
            await _orderViewModule.DisposeAsync();
        }
    }
}